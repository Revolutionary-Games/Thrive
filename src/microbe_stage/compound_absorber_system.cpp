#include "compound_absorber_system.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/agent_cloud_system.h"
#include "microbe_stage/membrane_system.h"

// #include "bullet/collision_filter.h"
// #include "bullet/collision_system.h"
// #include "bullet/rigid_body_system.h"
// #include "engine/component_factory.h"
// #include "engine/engine.h"
// #include "engine/entity_filter.h"
// #include "engine/game_state.h"
// #include "engine/serialization.h"
// #include "game.h"
// #include "ogre/scene_node_system.h"
// #include "scripting/luajit.h"
// #include "util/make_unique.h"
// #include "microbe_stage/compound.h"
// #include "microbe_stage/compound_registry.h"

// #include "tinyxml.h"

// #include <OgreEntity.h>
// #include <OgreSceneManager.h>
// #include <stdexcept>

#include "generated/cell_stage_world.h"

// #include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CompoundAbsorberComponent
////////////////////////////////////////////////////////////////////////////////

// void CompoundAbsorberComponent::luaBindings(
//     sol::state &lua
// ){
//     lua.new_usertype<CompoundAbsorberComponent>("CompoundAbsorberComponent",

//         "new", sol::factories([](){
//                 return std::make_unique<CompoundAbsorberComponent>();
//             }),

//         COMPONENT_BINDINGS(CompoundAbsorberComponent),

//         "absorbedCompoundAmount", &CompoundAbsorberComponent::absorbedCompoundAmount,
        
//         "getAbsorbedCompounds", [](CompoundAbsorberComponent& us, sol::this_state s){
            
//             THRIVE_BIND_ITERATOR_TO_TABLE(us.getAbsorbedCompounds());
//         },
        
//         "setAbsorbedCompoundAmount", &CompoundAbsorberComponent::setAbsorbedCompoundAmount,
//         "setCanAbsorbCompound", &CompoundAbsorberComponent::setCanAbsorbCompound,
//         "setAbsorbtionCapacity", &CompoundAbsorberComponent::setAbsorbtionCapacity,
//         "enable", &CompoundAbsorberComponent::enable,
//         "disable", &CompoundAbsorberComponent::disable
//     );
// }

CompoundAbsorberComponent::CompoundAbsorberComponent() :
    Leviathan::Component(TYPE)
{

}

float
CompoundAbsorberComponent::absorbedCompoundAmount(
    CompoundId id
) const {
    const auto& iter = m_absorbedCompounds.find(id);
    if (iter != m_absorbedCompounds.cend()) {
        return iter->second;
    }
    else {
        return 0.0f;
    }
}

bool
CompoundAbsorberComponent::canAbsorbCompound(
    CompoundId id
) const {
    return m_canAbsorbCompound.find(id) != m_canAbsorbCompound.end();
}

void
CompoundAbsorberComponent::setAbsorbtionCapacity(
    double capacity
) {
    m_absorbtionCapacity = capacity;
}

void
CompoundAbsorberComponent::enable(){
    m_enabled = true;
}

void
CompoundAbsorberComponent::disable(){
    m_enabled = false;
}

// void
// CompoundAbsorberComponent::load(
//     const StorageContainer& storage
// ) {
//     Component::load(storage);
//     StorageList compounds = storage.get<StorageList>("compounds");
//     for (const StorageContainer& container : compounds) {
//         CompoundId compoundId = container.get<CompoundId>("compoundId");
//         float amount = container.get<float>("amount");
//         m_absorbedCompounds[compoundId] = amount;
//         m_canAbsorbCompound.insert(compoundId);
//     }
//     m_enabled = storage.get<bool>("enabled");
// }

// StorageContainer
// CompoundAbsorberComponent::storage() const {
//     StorageContainer storage = Component::storage();
//     StorageList compounds;
//     compounds.reserve(m_canAbsorbCompound.size());
//     for (CompoundId compoundId : m_canAbsorbCompound) {
//         StorageContainer container;
//         container.set<CompoundId>("compoundId", compoundId);
//         container.set<float>("amount", this->absorbedCompoundAmount(compoundId));
//         compounds.append(container);
//     }
//     storage.set<StorageList>("compounds", compounds);
//     storage.set<bool>("enabled", m_enabled);
//     return storage;
// }

void
CompoundAbsorberComponent::setAbsorbedCompoundAmount(
    CompoundId id,
    float amount
) {
    m_absorbedCompounds[id] = amount;
}


void
CompoundAbsorberComponent::setCanAbsorbCompound(
    CompoundId id,
    bool canAbsorb
) {
    if (canAbsorb) {
        m_canAbsorbCompound.insert(id);
    }
    else {
        m_canAbsorbCompound.erase(id);
    }
}

////////////////////////////////////////////////////////////////////////////////
// CompoundAbsorberSystem
////////////////////////////////////////////////////////////////////////////////
void
CompoundAbsorberSystem::Run(
    CellStageWorld &world,
    std::unordered_map<ObjectID, CompoundCloudComponent*> &clouds
) {

    auto &absorbersIndex = m_absorbers.CachedComponents.GetIndex();
    auto &agentsIndex = m_agents.CachedComponents.GetIndex();

    // For all entities that have a membrane and are able to absorb stuff do...
    for (const auto& value : absorbersIndex)
    {
        //EntityId entity = value.first;
        MembraneComponent& membrane = std::get<0>(*value.second);
        CompoundAbsorberComponent& absorber = std::get<1>(*value.second);
        Leviathan::Position& sceneNode = std::get<2>(*value.second);

        // Clear absorbed compounds
        absorber.m_absorbedCompounds.clear();

        // Find the bounding box of the membrane.
        int sideLength = membrane.getCellDimensions();
        // Find the position of the membrane.
        const Float3 origin = sceneNode.Members._Position;


        // Each membrane absorbs a certain amount of each compound.
        for (auto& entry : clouds)
        {
            CompoundCloudComponent* compoundCloud = entry.second;
            CompoundId id = compoundCloud->m_compoundId;
            int x_start = (origin.X - sideLength/2 - compoundCloud->offsetX) /
                compoundCloud->gridSize + compoundCloud->width/2;
            x_start = x_start > 0 ? x_start : 0;
            int x_end = (origin.X + sideLength/2 - compoundCloud->offsetX) /
                compoundCloud->gridSize + compoundCloud->width/2;
            x_end = x_end < compoundCloud->width ? x_end : compoundCloud->width;

            int z_start = (origin.Z - sideLength/2 - compoundCloud->offsetZ) /
                compoundCloud->gridSize + compoundCloud->height/2;
            z_start = z_start > 0 ? z_start : 0;
            int z_end = (origin.Z + sideLength/2 - compoundCloud->offsetZ) /
                compoundCloud->gridSize + compoundCloud->height/2;
            z_end = z_end < compoundCloud->height ? z_end : compoundCloud->height;

            // Iterate though all of the points inside the bounding box.
            for (int x = x_start; x < x_end; x++)
            {
                for (int y = z_start; y < z_end; y++)
                {
                    if (membrane.contains((x-compoundCloud->width/2) *
                            compoundCloud->gridSize-origin.X + compoundCloud->offsetX,
                            (y-compoundCloud->height/2) * compoundCloud->gridSize -
                            origin.Z + compoundCloud->offsetZ))
                    {
                        if (absorber.m_enabled == true && absorber.canAbsorbCompound(id)) {
                            float amount = compoundCloud->amountAvailable(x, y, .2) / 5000.0f;
                            //if (CompoundRegistry::isAgentType(id)){
                            //    (*CompoundRegistry::getAgentEffect(id))(entity, amount);
                            //    this->entityManager()->removeEntity(compoundEntity);
                            //}
                            //else
                                if(absorber.m_absorbtionCapacity >= amount
                                    * CompoundRegistry::getCompoundUnitVolume(id))
                                {
                                    absorber.m_absorbedCompounds[id] +=
                                        compoundCloud->takeCompound(x, y, .2) / 5000.0f;
                                //this->entityManager()->removeEntity(compoundEntity);
                            }
                        }
                        // Absorb .2 (third parameter) of the available compounds.
                        //membrane->absorbCompounds();
                    }
                }
            }
        }

        // Each membrane absorbs a certain amount of each agent.
        for (auto& entry : agentsIndex)
        {
            AgentCloudComponent& agent = std::get<0>(*entry.second);
            Leviathan::Position& agentNode = std::get<1>(*entry.second);
            CompoundId id = agent.m_compoundId;

            const Float3 agentPos = agentNode.Members._Position;

            if (membrane.contains(agentPos.X - origin.X, agentPos.Z - origin.Z)) {
                if (absorber.m_enabled == true && absorber.canAbsorbCompound(id)) {
                    float amount = agent.getPotency();
                    if (CompoundRegistry::isAgentType(id)){
                        (*CompoundRegistry::getAgentEffect(id))(value.first, amount);
                        world.DestroyEntity(entry.first);
                    }
                }
                // Absorb .2 (third parameter) of the available compounds.
                //membrane->absorbCompounds();
            }
        }
    }
}
