#include <algorithm>
#include <cmath>
#include <OgreVector3.h>
#include <unordered_map>

#include "microbe_stage/spawn_system.h"
#include "scripting/luajit.h"
#include "engine/component_factory.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/player_data.h"
#include "engine/rng.h"
#include "engine/serialization.h"
#include "engine/typedefs.h"
#include "ogre/scene_node_system.h"

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// SpawnType
////////////////////////////////////////////////////////////////////////////////
struct SpawnType {
    double spawnRadius = 0.0;
    double spawnRadiusSqr = 0.0;
    double spawnFrequency = 0.0;
    sol::protected_function factoryFunction;
    SpawnerTypeId id = 0;
};

////////////////////////////////////////////////////////////////////////////////
// SpawnedComponent
////////////////////////////////////////////////////////////////////////////////
SpawnedComponent::SpawnedComponent() {}

void SpawnedComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<SpawnedComponent>("SpawnedComponent",
        "new", sol::factories([](){
                return std::make_unique<SpawnedComponent>();
            }),

        COMPONENT_BINDINGS(SpawnedComponent)
    );
}

void
SpawnedComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    spawnRadiusSqr = storage.get<double>("spawnRadiusSqr");
}


StorageContainer
SpawnedComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<double>("spawnRadiusSqr", spawnRadiusSqr);
    return storage;
}

REGISTER_COMPONENT(SpawnedComponent)

////////////////////////////////////////////////////////////////////////////////
// SpawnSystem
////////////////////////////////////////////////////////////////////////////////
struct SpawnSystem::Implementation {
    EntityFilter<SpawnedComponent, OgreSceneNodeComponent> entities;
    SpawnerTypeId nextId = 0;
    std::unordered_map<SpawnerTypeId, SpawnType> spawnTypes;
    Ogre::Vector3 previousPlayerPosition;
    unsigned int timeSinceLastUpdate = 0;
};

void SpawnSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<SpawnSystem>("SpawnSystem",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<System>(),

        "init", &SpawnSystem::init,

        "addSpawnType", &SpawnSystem::addSpawnType,

        "removeSpawnType", &SpawnSystem::removeSpawnType
    );
}

SpawnerTypeId SpawnSystem::addSpawnType(sol::protected_function factoryFunction, double spawnDensity, double spawnRadius) {
    SpawnType newSpawnType;
    newSpawnType.factoryFunction = factoryFunction;
    newSpawnType.spawnRadius = spawnRadius;
    newSpawnType.spawnRadiusSqr = std::pow(spawnRadius, 2);
    newSpawnType.spawnFrequency = spawnDensity * newSpawnType.spawnRadiusSqr * 4;
    newSpawnType.id = m_impl->nextId;
    m_impl->nextId++;
    m_impl->spawnTypes[newSpawnType.id] = newSpawnType;
    return newSpawnType.id;
}

void SpawnSystem::removeSpawnType(SpawnerTypeId spawnId) {
    m_impl->spawnTypes.erase(spawnId);
}

void SpawnSystem::doSpawnCycle() {
    // Getting the player position.
    std::string playerName = gameState()->engine()->playerData().playerName();
    std::unique_ptr<Entity> playerEntity(new Entity(playerName, gameState()));
    OgreSceneNodeComponent* playerSceneNode = static_cast<OgreSceneNodeComponent*>(playerEntity->getComponent(OgreSceneNodeComponent::TYPE_ID));
    Ogre::Vector3 playerPosition = playerSceneNode->m_transform.position;

    // Despawn entities.
    for(const auto& entry : m_impl->entities) {
        SpawnedComponent* spawnedComponent = std::get<0>(entry.second);
        Ogre::Vector3 spawnedEntityPosition = std::get<1>(entry.second)->m_transform.position;
        double squaredDistance = playerPosition.squaredDistance(spawnedEntityPosition);

        // If the entity is too far away from the player, despawn it.
        if(squaredDistance > spawnedComponent->spawnRadiusSqr) {
            std::unique_ptr<Entity> spawnedEntity(new Entity(entry.first, gameState()));
            spawnedEntity->destroy();
        }
    }

    // Spawn new entities.
    for(auto& st : m_impl->spawnTypes) {
        /*
        To actually spawn a given entity for a given attempt, two conditions should be met.
        The first condition is a random chance that adjusts the spawn frequency to the approprate
        amount. The second condition is whether the entity will spawn in a valid position.
        It is checked when the first condition is met and a position
        for the entity has been decided.

        To allow more than one entity of each type to spawn per spawn cycle, the SpawnSystem
        attempts to spawn each given entity multiple times depending on the spawnFrequency.
        numAttempts stores how many times the SpawnSystem attempts to spawn the given entity.
        */
        SpawnType& spawnType = st.second;
        unsigned numAttempts = std::max(int(spawnType.spawnFrequency * 2), 1);
        for(unsigned i = 0; i < numAttempts; i++)
            if(gameState()->engine()->rng().getDouble(0.0, numAttempts) < spawnType.spawnFrequency) {
                /*
                First condition passed. Choose a location for the entity.

                A random location in the square of sidelength 2*spawnRadius
                centered on the player is chosen. The corners
                of the square are outside the spawning region, but they
                will fail the second condition, so entities still only
                spawn within the spawning region.
                */
                double distanceX = gameState()->engine()->rng().getDouble(-spawnType.spawnRadius, spawnType.spawnRadius);
                double distanceY = gameState()->engine()->rng().getDouble(-spawnType.spawnRadius, spawnType.spawnRadius);

                // Distance from the player.
                Ogre::Vector3 displacement(distanceX, distanceY, 0.0);
                double squaredDistance = displacement.squaredLength();

                // Distance from the location of the player in the previous spawn cycle.
                Ogre::Vector3 previousDisplacement = displacement + playerPosition - m_impl->previousPlayerPosition;
                double previousSquaredDistance = previousDisplacement.squaredLength();

                if(squaredDistance <= spawnType.spawnRadiusSqr && previousSquaredDistance > spawnType.spawnRadiusSqr) {
                    // Second condition passed. Spawn the entity.
                    Entity* spawnedEntity = spawnType.factoryFunction(playerPosition + displacement);

                    // Giving the new entity a spawn component.
                    if(spawnedEntity->exists()) {
                        std::unique_ptr<SpawnedComponent> spawnedComponent(new SpawnedComponent());
                        spawnedComponent->spawnRadiusSqr = spawnType.spawnRadiusSqr;
                        spawnedEntity->addComponent(std::move(spawnedComponent));
                    }
                }
            }
    }

    // Updating the previous player location.
    m_impl->previousPlayerPosition = playerPosition;
}

SpawnSystem::SpawnSystem()
  : m_impl(new Implementation())
{
}

SpawnSystem::~SpawnSystem() {}

void
SpawnSystem::init(
    GameStateData* gameState
) {
    System::initNamed("SpawnSystem", gameState);
    m_impl->entities.setEntityManager(gameState->entityManager());
}


void
SpawnSystem::shutdown() {
    m_impl->entities.setEntityManager(nullptr);
    System::shutdown();
}

void
SpawnSystem::update(
    int,
    int logicTime
) {
    // Not used.
    m_impl->entities.clearChanges();

    m_impl->timeSinceLastUpdate += logicTime;
    if(m_impl->timeSinceLastUpdate > SPAWN_INTERVAL) {
        m_impl->timeSinceLastUpdate = 0;
        doSpawnCycle();
    }
}
