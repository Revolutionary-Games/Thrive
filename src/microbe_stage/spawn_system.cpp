#include "microbe_stage/spawn_system.h"

// #include <algorithm>
// #include <cmath>
// #include <OgreVector3.h>
// #include <unordered_map>

// #include "scripting/luajit.h"
// #include "engine/component_factory.h"
// #include "engine/entity_filter.h"
// #include "engine/game_state.h"
// #include "engine/engine.h"
// #include "engine/entity.h"
#include "engine/player_data.h"
// #include "engine/rng.h"
// #include "engine/serialization.h"
// #include "engine/typedefs.h"
// #include "ogre/scene_node_system.h"

#include "generated/cell_stage_world.h"

#include "ThriveGame.h"

#include <Utility/Random.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// SpawnedComponent
////////////////////////////////////////////////////////////////////////////////

SpawnedComponent::SpawnedComponent(double newSpawnRadiusSqr) :
    Leviathan::Component(TYPE), spawnRadiusSqr(newSpawnRadiusSqr)
{}

// void
// SpawnedComponent::load(
//     const StorageContainer& storage
// ) {
//     Component::load(storage);
//     spawnRadiusSqr = storage.get<double>("spawnRadiusSqr");
// }


// StorageContainer
// SpawnedComponent::storage() const {
//     StorageContainer storage = Component::storage();
//     storage.set<double>("spawnRadiusSqr", spawnRadiusSqr);
//     return storage;
// }

////////////////////////////////////////////////////////////////////////////////
// SpawnSystem
////////////////////////////////////////////////////////////////////////////////
struct SpawnSystem::Implementation {
    SpawnerTypeId nextId = 0;
    std::unordered_map<SpawnerTypeId, SpawnType> spawnTypes;
    Float3 previousPlayerPosition = Float3(0, 0, 0);
    float timeSinceLastUpdate = 0;
};

// void SpawnSystem::luaBindings(
//     sol::state &lua
// ){
//     lua.new_usertype<SpawnSystem>("SpawnSystem",

//         sol::constructors<sol::types<>>(),

//         sol::base_classes, sol::bases<System>(),

//         "init", &SpawnSystem::init,

//         "addSpawnType", &SpawnSystem::addSpawnType,

//         "removeSpawnType", &SpawnSystem::removeSpawnType
//     );
// }

SpawnerTypeId
    SpawnSystem::addSpawnType(
        std::function<ObjectID(CellStageWorld&, Float3)> factoryFunction,
        double spawnDensity,
        double spawnRadius)
{
    SpawnType newSpawnType;
    newSpawnType.factoryFunction = factoryFunction;
    newSpawnType.spawnRadius = spawnRadius;
    newSpawnType.spawnRadiusSqr = std::pow(spawnRadius, 2);
    newSpawnType.spawnFrequency =
        spawnDensity * newSpawnType.spawnRadiusSqr * 4;
    newSpawnType.id = m_impl->nextId;
    m_impl->nextId++;
    m_impl->spawnTypes[newSpawnType.id] = newSpawnType;
    return newSpawnType.id;
}

void
    SpawnSystem::removeSpawnType(SpawnerTypeId spawnId)
{
    m_impl->spawnTypes.erase(spawnId);
}

bool
    SpawnSystem::updateDensity(SpawnerTypeId spawnId, double spawnDensity)
{
    const auto found = m_impl->spawnTypes.find(spawnId);

    if(found == m_impl->spawnTypes.end())
        return false;

    found->second.spawnFrequency =
        spawnDensity * found->second.spawnRadiusSqr * 4;
    return true;
}

void
    SpawnSystem::Release()
{
    Clear();
}

void
    SpawnSystem::Clear()
{
    Leviathan::System<
        std::tuple<SpawnedComponent&, Leviathan::Position&>>::Clear();
    LOG_INFO("Clearing spawn system spawners");
    m_impl->spawnTypes.clear();
    m_impl->previousPlayerPosition = Float3(0, 0, 0);
    m_impl->timeSinceLastUpdate = 0;
}
// ------------------------------------ //

SpawnSystem::SpawnSystem() : m_impl(new Implementation()) {}

SpawnSystem::~SpawnSystem() {}

void
    SpawnSystem::Run(CellStageWorld& world, float elapsed)
{
    if(!world.GetNetworkSettings().IsAuthoritative)
        return;

    m_impl->timeSinceLastUpdate += elapsed;

    while(m_impl->timeSinceLastUpdate > SPAWN_INTERVAL) {
        m_impl->timeSinceLastUpdate -= SPAWN_INTERVAL;

        Float3 playerPosition = Float3(0, 0, 0);

        // Hybrid client-server version
        if(ThriveGame::Get()) {

            // Getting the player position.

            auto controlledEntity =
                ThriveGame::Get()->playerData().activeCreature();

            // Skip if no player entity //
            if(controlledEntity == NULL_OBJECT)
                continue;

            try {

                playerPosition = world.GetComponent_Position(controlledEntity)
                                     .Members._Position;

            } catch(const Leviathan::NotFound& e) {

                LOG_WARNING(
                    "SpawnSystem: no Position component in activeCreature, "
                    "exception:");
                e.PrintToLog();
                return;
            }
        }

        // Remove the y-position from player position
        playerPosition.Y = 0;
        int entitiesDeleted = 0;

        // Despawn entities.
        for(const auto& entry : CachedComponents.GetIndex()) {
            // delete a max of two entities per step to reduce lag from deleting
            // tons of entities at once
            if(entitiesDeleted < 2) {
                SpawnedComponent& spawnedComponent = std::get<0>(*entry.second);
                const Float3 spawnedEntityPosition =
                    std::get<1>(*entry.second).Members._Position;
                float squaredDistance =
                    (playerPosition - spawnedEntityPosition).LengthSquared();
                // If the entity is too far away from the player, despawn it.
                if(squaredDistance > spawnedComponent.spawnRadiusSqr) {
                    entitiesDeleted++;
                    world.QueueDestroyEntity(entry.first);
                }
            } else {
                // get out of loop if you hit max
                break;
            }
        }

        Leviathan::Random* random = Leviathan::Random::Get();

        // Spawn new entities.
        for(auto& st : m_impl->spawnTypes) {
            /*
            To actually spawn a given entity for a given attempt, two
            conditions should be met.  The first condition is a random
            chance that adjusts the spawn frequency to the approprate
            amount. The second condition is whether the entity will
            spawn in a valid position.  It is checked when the first
            condition is met and a position for the entity has been
            decided.

            To allow more than one entity of each type to spawn per
            spawn cycle, the SpawnSystem attempts to spawn each given
            entity multiple times depending on the spawnFrequency.
            numAttempts stores how many times the SpawnSystem attempts
            to spawn the given entity.
            */
            SpawnType& spawnType = st.second;
            unsigned numAttempts =
                std::max(int(spawnType.spawnFrequency * 2), 1);
            for(unsigned i = 0; i < numAttempts; i++) {
                if(random->GetNumber(0.0f, numAttempts) <
                    spawnType.spawnFrequency) {
                    /*
                    First condition passed. Choose a location for the entity.

                    A random location in the square of sidelength 2*spawnRadius
                    centered on the player is chosen. The corners
                    of the square are outside the spawning region, but they
                    will fail the second condition, so entities still only
                    spawn within the spawning region.
                    */
                    float distanceX = random->GetNumber(
                        static_cast<float>(-spawnType.spawnRadius),
                        spawnType.spawnRadius);
                    float distanceZ = random->GetNumber(
                        static_cast<float>(-spawnType.spawnRadius),
                        spawnType.spawnRadius);

                    // Distance from the player.
                    Float3 displacement(distanceX, 0, distanceZ);
                    float squaredDistance = displacement.LengthSquared();

                    // Distance from the location of the player in the previous
                    // spawn cycle.
                    Float3 previousDisplacement =
                        displacement + playerPosition -
                        m_impl->previousPlayerPosition;
                    float previousSquaredDistance =
                        previousDisplacement.LengthSquared();

                    if(squaredDistance <= spawnType.spawnRadiusSqr &&
                        previousSquaredDistance > spawnType.spawnRadiusSqr) {
                        // Second condition passed. Spawn the entity.
                        ObjectID spawnedEntity = spawnType.factoryFunction(
                            world, playerPosition + displacement);

                        // Giving the new entity a spawn component.
                        if(spawnedEntity != NULL_OBJECT) {

                            try {
                                world.Create_SpawnedComponent(
                                    spawnedEntity, spawnType.spawnRadiusSqr);
                            } catch(const Leviathan::Exception& e) {

                                LOG_ERROR("SpawnSystem failed to add "
                                          "SpawnedComponent, "
                                          "exception:");
                                e.PrintToLog();
                            }
                        }
                    }
                }
            }
        }

        // Updating the previous player location.
        m_impl->previousPlayerPosition = playerPosition;
    }
}
