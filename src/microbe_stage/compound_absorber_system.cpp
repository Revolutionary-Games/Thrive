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

        const auto unadjustedgrabRadius =
            membrane.calculateEncompassingCircleRadius();

        // Skip if not initialized //
        if(unadjustedgrabRadius < 1)
            continue;

        // Each membrane absorbs a certain amount of each compound.
        for(auto& entry : clouds) {

            CompoundCloudComponent* compoundCloud = entry.second;

            // Skip clouds that are out of range
            const auto gridSize = compoundCloud->getGridSize();
            const int halfWidth =
                static_cast<int>(compoundCloud->getWidth() * gridSize / 2);
            const int halfHeight =
                static_cast<int>(compoundCloud->getHeight() * gridSize / 2);

            const auto& cloudPos = compoundCloud->getPosition();

            const auto grabRadius = unadjustedgrabRadius * gridSize;

            const Float3 relative = origin - cloudPos;

            if(relative.X < -halfWidth - grabRadius ||
                relative.X > halfWidth + grabRadius ||
                relative.Z < -halfHeight - grabRadius ||
                relative.Z > halfHeight + grabRadius)
                continue;

            int x_start = (relative.X + halfWidth - grabRadius) / gridSize;

            if(x_start < 0)
                x_start = 0;

            int x_end = (relative.X + halfWidth + grabRadius) / gridSize;

            const auto width = static_cast<int>(compoundCloud->getWidth());
            if(x_end > width)
                x_end = width;

            int z_start = (relative.Z + halfHeight - grabRadius) / gridSize;

            if(z_start < 0)
                z_start = 0;

            int z_end = (relative.Z + halfHeight + grabRadius) / gridSize;

            const auto height = static_cast<int>(compoundCloud->getHeight());
            if(z_end > height)
                z_end = height;

            const auto diameter = std::pow(grabRadius, 2);

            const int cloudSpaceHalfWidth =
                static_cast<int>(compoundCloud->getWidth() / 2);
            const int cloudSpaceHalfHeight =
                static_cast<int>(compoundCloud->getHeight() / 2);

            // Iterate though all of the points inside the bounding box.
            for(int x = x_start; x < x_end; x++) {
                for(int y = z_start; y < z_end; y++) {

                    // LOG_WRITE(
                    //     "Pos: " + std::to_string(x) + ", " +
                    //     std::to_string(y));

                    // And skip everything outside the circle
                    if(std::pow(x - cloudSpaceHalfWidth - relative.X, 2) +
                            std::pow(y - cloudSpaceHalfHeight - relative.Y, 2) >
                        diameter)
                        continue;

                    // LOG_WRITE("Checking absorb pos: " + std::to_string(x) +
                    //           ", " + std::to_string(y));

                    // Each cloud has 4 things
                    static_assert(CLOUDS_IN_ONE == 4,
                        "Clouds packed into one has changed");

                    // Absorb all the 4 compounds

                    const auto id1 = compoundCloud->getCompoundId1();
                    const auto id2 = compoundCloud->getCompoundId2();
                    const auto id3 = compoundCloud->getCompoundId3();
                    const auto id4 = compoundCloud->getCompoundId4();

                    if(id1 != NULL_COMPOUND && absorber.canAbsorbCompound(id1))
                        absorbFromCloud(compoundCloud, id1, absorber, x, y);
                    if(id2 != NULL_COMPOUND && absorber.canAbsorbCompound(id2))
                        absorbFromCloud(compoundCloud, id2, absorber, x, y);
                    if(id3 != NULL_COMPOUND && absorber.canAbsorbCompound(id3))
                        absorbFromCloud(compoundCloud, id3, absorber, x, y);
                    if(id4 != NULL_COMPOUND && absorber.canAbsorbCompound(id4))
                        absorbFromCloud(compoundCloud, id4, absorber, x, y);
                }
            }
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
