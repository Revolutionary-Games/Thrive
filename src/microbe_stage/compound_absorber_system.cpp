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
{}

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

        CompoundAbsorberComponent& absorber = std::get<1>(*value.second);

        // Skip if disabled
        if(!absorber.m_enabled)
            continue;

        // EntityId entity = value.first;
        MembraneComponent& membrane = std::get<0>(*value.second);
        Leviathan::Position& sceneNode = std::get<2>(*value.second);

        // Clear absorbed compounds
        absorber.m_absorbedCompounds.clear();

        // Find the position of the cell.
        const Float3 origin = sceneNode.Members._Position;

        // This version is used for world coordinate calculations
        const auto grabRadius = membrane.calculateEncompassingCircleRadius();

        // This version is used when working with cloud local coordinates
        const auto localGrabRadius = grabRadius / CLOUD_RESOLUTION;

        // Skip if not initialized //
        if(grabRadius < 1)
            continue;

        const auto localGrabRadiusSquared =
            std::pow(grabRadius / CLOUD_RESOLUTION, 2);

        // Each membrane absorbs a certain amount of each compound.
        for(auto& entry : clouds) {

            CompoundCloudComponent* compoundCloud = entry.second;

            // Skip clouds that are out of range
            if(!CompoundCloudSystem::cloudContainsPositionWithRadius(
                   compoundCloud->m_position, origin, grabRadius))
                continue;

            auto [cloudRelativeX, cloudRelativeY] =
                CompoundCloudSystem::convertWorldToCloudLocalForGrab(
                    compoundCloud->m_position, origin);

            // Calculate all circle positions and grab from all the valid
            // positions

            // For simplicity all points within a bounding box around the
            // relative origin point is calculated and that is restricted by
            // checking if the point is within the circle before grabbing
            // TODO: maybe it would be worth it to switch to integers here (they
            // are already floored in convertWorldToCloudLocalForGrab)
            for(float x = cloudRelativeX - localGrabRadius;
                x <= cloudRelativeX + localGrabRadius; x += 1) {
                for(float y = cloudRelativeY - localGrabRadius;
                    y <= cloudRelativeY + localGrabRadius; y += 1) {

                    // Negative coordinates are always outside the cloud area
                    if(x < 0 || y < 0)
                        continue;

                    // Circle check
                    if(std::pow(x - cloudRelativeX, 2) +
                            std::pow(y - cloudRelativeY, 2) >
                        localGrabRadiusSquared) {
                        // Not in it
                        continue;
                    }

                    // Then just need to check that it is within the cloud
                    const size_t localX = static_cast<size_t>(x);
                    const size_t localY = static_cast<size_t>(y);

                    if(localX < CLOUD_SIMULATION_WIDTH &&
                        localY < CLOUD_SIMULATION_HEIGHT) {

                        // Found a valid position

                        // LOG_WRITE("Checking absorb pos: " + std::to_string(x)
                        // +
                        //           ", " + std::to_string(y));

                        // Each cloud has 4 things
                        static_assert(CLOUDS_IN_ONE == 4,
                            "Clouds packed into one has changed");

                        // Absorb all of the 4 compounds that can be in a cloud
                        // entity

                        const auto id1 = compoundCloud->getCompoundId1();
                        const auto id2 = compoundCloud->getCompoundId2();
                        const auto id3 = compoundCloud->getCompoundId3();
                        const auto id4 = compoundCloud->getCompoundId4();

                        if(id1 != NULL_COMPOUND &&
                            absorber.canAbsorbCompound(id1))
                            absorbFromCloud(compoundCloud, id1, absorber, x, y);
                        if(id2 != NULL_COMPOUND &&
                            absorber.canAbsorbCompound(id2))
                            absorbFromCloud(compoundCloud, id2, absorber, x, y);
                        if(id3 != NULL_COMPOUND &&
                            absorber.canAbsorbCompound(id3))
                            absorbFromCloud(compoundCloud, id3, absorber, x, y);
                        if(id4 != NULL_COMPOUND &&
                            absorber.canAbsorbCompound(id4))
                            absorbFromCloud(compoundCloud, id4, absorber, x, y);
                    }
                }
            }
        }

        // This will be used once agents are made into clouds
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


void
    CompoundAbsorberSystem::absorbFromCloud(
        CompoundCloudComponent* compoundCloud,
        CompoundId id,
        CompoundAbsorberComponent& absorber,
        int x,
        int y)
{
    float amount = compoundCloud->amountAvailable(id, x, y, .2) / 5000.0f;

    if(amount < Leviathan::EPSILON)
        return;

    // TODO: this would apply to absorbing agents from a cloud
    // if (CompoundRegistry::isAgentType(id)){
    //
    // (*CompoundRegistry::getAgentEffect(id))(entity,
    //     //    amount);
    //     //
    //     this->entityManager()->removeEntity(compoundEntity);
    //     //}
    //     // else
    if(absorber.m_absorbtionCapacity >=
        amount *
            SimulationParameters::compoundRegistry.getTypeData(id).volume) {

        // LOG_WRITE("Absorbing stuff: " + std::to_string(id) +
        //           " at (cloud local): " + std::to_string(x) + ", " +
        //           std::to_string(y) + " amount: " + std::to_string(amount));
        absorber.m_absorbedCompounds[id] +=
            compoundCloud->takeCompound(id, x, y, .2) / 5000.0f;
    }
    // Absorb .2 (third parameter) of the available
    // compounds.
    // membrane->absorbCompounds();
}
