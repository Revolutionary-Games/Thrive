#include "compound_absorber_system.h"
#include "microbe_stage/agent_cloud_system.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/membrane_system.h"
#include "microbe_stage/simulation_parameters.h"

#include "generated/cell_stage_world.h"

#include <Script/ScriptConversionHelpers.h>
#include <add_on/scriptarray/scriptarray.h>

#include <boost/range/adaptor/map.hpp>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CompoundAbsorberComponent
////////////////////////////////////////////////////////////////////////////////

CompoundAbsorberComponent::CompoundAbsorberComponent() :
    Leviathan::Component(TYPE)
{
}

float
    CompoundAbsorberComponent::absorbedCompoundAmount(CompoundId id) const
{
    const auto& iter = m_absorbedCompounds.find(id);
    if(iter != m_absorbedCompounds.cend()) {
        return iter->second;
    } else {
        return 0.0f;
    }
}

bool
    CompoundAbsorberComponent::canAbsorbCompound(CompoundId id) const
{
    return m_canAbsorbCompound.find(id) != m_canAbsorbCompound.end();
}

void
    CompoundAbsorberComponent::setAbsorbtionCapacity(double capacity)
{
    m_absorbtionCapacity = capacity;
}

void
    CompoundAbsorberComponent::enable()
{
    m_enabled = true;
}

void
    CompoundAbsorberComponent::disable()
{
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
//         container.set<float>("amount",
//         this->absorbedCompoundAmount(compoundId));
//         compounds.append(container);
//     }
//     storage.set<StorageList>("compounds", compounds);
//     storage.set<bool>("enabled", m_enabled);
//     return storage;
// }

void
    CompoundAbsorberComponent::setAbsorbedCompoundAmount(CompoundId id,
        float amount)
{
    m_absorbedCompounds[id] = amount;
}


void
    CompoundAbsorberComponent::setCanAbsorbCompound(CompoundId id,
        bool canAbsorb)
{
    if(canAbsorb) {
        m_canAbsorbCompound.insert(id);
    } else {
        m_canAbsorbCompound.erase(id);
    }
}

CScriptArray*
    CompoundAbsorberComponent::getAbsorbedCompounds()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(
        std::begin(m_absorbedCompounds | boost::adaptors::map_keys),
        std::end(m_absorbedCompounds | boost::adaptors::map_keys),
        Leviathan::ScriptExecutor::Get()->GetASEngine());
}

////////////////////////////////////////////////////////////////////////////////
// CompoundAbsorberSystem
////////////////////////////////////////////////////////////////////////////////
void
    CompoundAbsorberSystem::Run(CellStageWorld& world,
        std::unordered_map<ObjectID, CompoundCloudComponent*>& clouds)
{

    auto& absorbersIndex = m_absorbers.CachedComponents.GetIndex();
    auto& agentsIndex = m_agents.CachedComponents.GetIndex();

    // For all entities that have a membrane and are able to absorb stuff do...
    for(const auto& value : absorbersIndex) {
        // EntityId entity = value.first;
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
        for(auto& entry : clouds) {

            // TODO: reimplement

            // CompoundCloudComponent* compoundCloud = entry.second;
            // CompoundId id = compoundCloud->m_compoundId;
            // int x_start = (origin.X - sideLength / 2 -
            // compoundCloud->offsetX) /
            //                   compoundCloud->gridSize +
            //               compoundCloud->width / 2;
            // x_start = x_start > 0 ? x_start : 0;
            // int x_end = (origin.X + sideLength / 2 - compoundCloud->offsetX)
            // /
            //                 compoundCloud->gridSize +
            //             compoundCloud->width / 2;
            // x_end = x_end < compoundCloud->width ? x_end :
            // compoundCloud->width;

            // int z_start = (origin.Z - sideLength / 2 -
            // compoundCloud->offsetZ) /
            //                   compoundCloud->gridSize +
            //               compoundCloud->height / 2;
            // z_start = z_start > 0 ? z_start : 0;
            // int z_end = (origin.Z + sideLength / 2 - compoundCloud->offsetZ)
            // /
            //                 compoundCloud->gridSize +
            //             compoundCloud->height / 2;
            // z_end =
            //     z_end < compoundCloud->height ? z_end :
            //     compoundCloud->height;

            // // Iterate though all of the points inside the bounding box.
            // for(int x = x_start; x < x_end; x++) {
            //     for(int y = z_start; y < z_end; y++) {
            //         // TODO: this seems like a very expensive loop
            //         // This contains method is very expensive as it can loop
            //         // over all of the vertices
            //         if(membrane.contains((x - compoundCloud->width / 2) *
            //                                      compoundCloud->gridSize -
            //                                  origin.X +
            //                                  compoundCloud->offsetX,
            //                (y - compoundCloud->height / 2) *
            //                        compoundCloud->gridSize -
            //                    origin.Z + compoundCloud->offsetZ)) {
            //             if(absorber.m_enabled == true &&
            //                 absorber.canAbsorbCompound(id)) {
            //                 float amount =
            //                     compoundCloud->amountAvailable(x, y, .2) /
            //                     5000.0f;
            //                 // if (CompoundRegistry::isAgentType(id)){
            //                 //
            //                 (*CompoundRegistry::getAgentEffect(id))(entity,
            //                 //    amount);
            //                 //
            //                 this->entityManager()->removeEntity(compoundEntity);
            //                 //}
            //                 // else
            //                 if(absorber.m_absorbtionCapacity >=
            //                     amount *
            //                     SimulationParameters::compoundRegistry
            //                                  .getTypeData(id)
            //                                  .volume) {
            //                     absorber.m_absorbedCompounds[id] +=
            //                         compoundCloud->takeCompound(x, y, .2) /
            //                         5000.0f;
            //                     //
            //                     this->entityManager()->removeEntity(compoundEntity);
            //                 }
            //             }
            //             // Absorb .2 (third parameter) of the available
            //             // compounds.
            //             // membrane->absorbCompounds();
            //         }
            //     }
            // }
        }

        // Each membrane absorbs a certain amount of each agent.
        // TODO: agent absorption from a cloud (this would probably work very
        // similarly as the normal compounds instead of like this (currently the
        // toxins are physics "bullets" that hit cells)
        // for(auto& entry : agentsIndex) {
        //     AgentCloudComponent& agent = std::get<0>(*entry.second);
        //     Leviathan::Position& agentNode = std::get<1>(*entry.second);
        //     CompoundId id = agent.m_compoundId;

        //     const Float3 agentPos = agentNode.Members._Position;

        //     if(membrane.contains(agentPos.X - origin.X, agentPos.Z -
        //     origin.Z)) {
        //         if(absorber.m_enabled == true &&
        //         absorber.canAbsorbCompound(id))
        //         {
        //             float amount = agent.getPotency();
        //             // if (SimulationParameters::compoundRegistry.
        //             //     getTypeData(id).isAgent){
        //             //     (*SimulationParameters::compoundRegistry.
        //             //         getTypeData(id).agentEffect)(value.first,
        //             //         amount);
        //             //     world.DestroyEntity(entry.first);
        //             // }
        //         }
        //         // Absorb .2 (third parameter) of the available compounds.
        //         // membrane->absorbCompounds();
        //     }
        // }
    }
}
