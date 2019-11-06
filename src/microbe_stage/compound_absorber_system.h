#pragma once

#include "engine/component_types.h"

#include "microbe_stage/agent_cloud_system.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/membrane_system.h"

#include <Entities/Component.h>
#include <Entities/System.h>

#include <unordered_set>

class CScriptArray;

namespace thrive {

class CellStageWorld;

/**
 * @brief Absorbs compound from clouds
 */
class CompoundAbsorberComponent : public Leviathan::Component {
public:
    // /**
    // * @brief Lua bindings
    // *
    // * Exposes:
    // * - CompoundAbsorberComponent::absorbedCompoundAmount
    // * - CompoundAbsorberComponent::getAbsorbedCompounds
    // * - CompoundAbsorberComponent::canAbsorbCompound
    // * - CompoundAbsorberComponent::setCanAbsorbCompound
    // * - CompoundAbsorberComponent::setAbsorbtionCapacity
    // * - CompoundAbsorberComponent::enable
    // * - CompoundAbsorberComponent::disable
    // *
    // * @return
    // */
    // static void luaBindings(sol::state &lua);

    CompoundAbsorberComponent();

    REFERENCE_HANDLE_UNCOUNTED_TYPE(CompoundAbsorberComponent);

    /**
     * @brief The compounds absorbed in the last time step
     */
    std::unordered_map<CompoundId, float> m_absorbedCompounds;

    /**
     * @brief Whether a particular compound id can be absorbed
     */
    std::unordered_set<CompoundId> m_canAbsorbCompound;

    /**
     * @brief Whether anything can be absorbed
     */
    bool m_enabled = true;

    float scale = 1.0f;

    /**
     * @brief The amount of compound volume that can be absorbed
     */
    double m_absorbtionCapacity = 0;

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::ABSORBER);

    /**
     * @brief The absorbed amount in the last time step
     *
     * @param id
     *   The compound id to get the amount for
     *
     * @return
     */
    float
        absorbedCompoundAmount(CompoundId id) const;

    /**
     * @brief Whether an compound can be absorbed
     *
     * @param id
     *   The compound id to check
     *
     * @return
     */
    bool
        canAbsorbCompound(CompoundId id) const;

    /**
     * @brief Sets the absorbtion capacity
     *
     * @param capacity
     *   The new capacity
     */
    void
        setAbsorbtionCapacity(double capacity);

    /**
     * Sets m_enabled to true
     */
    void
        enable();

    /**
     * Sets m_enabled to false
     */
    void
        disable();

    // Set radiusHalved to true
    void
        setGrabScale(float scale);


    // void
    // load(
    //     const StorageContainer& storage
    // ) override;

    // StorageContainer
    // storage() const override;

    /**
     * @brief Sets the amount of absorbed compounds
     *
     * Use this for e.g. resetting the absorbed amount down
     * to zero.
     *
     * @param id
     *   The compound id to change the amount for
     * @param amount
     *   The new amount
     */
    void
        setAbsorbedCompoundAmount(CompoundId id, float amount);

    /**
     * @brief Sets whether an compound can be absorbed
     *
     * @param id
     *   The compound id to set the flag for
     * @param canAbsorb
     *   Whether to absorb the compound
     */
    void
        setCanAbsorbCompound(CompoundId id, bool canAbsorb);

    //! \brief Wrapper for scripts to get all the absorbed compounds
    //! \todo It would probably be better to give, size and then a get method
    CScriptArray*
        getAbsorbedCompounds();
};


/**
 * @brief Absorbs compounds from CompoundCloudComponent and
 * AgentCloudComponent into membranes
 */
class CompoundAbsorberSystem {
public:
    /**
     * @brief Updates the system
     * @todo Once agents are a cloud this needs to absorb them
     * @todo Currently this does not take 'elapsed' into account so cells absorb
     * stuff faster at higher framerates
     */
    void
        Run(CellStageWorld& world,
            std::unordered_map<ObjectID, CompoundCloudComponent*>& clouds,
            float elapsed);

    void
        CreateNodes(
            const std::vector<std::tuple<AgentCloudComponent*, ObjectID>>&
                agentData,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                scenenodeData,
            const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                membraneData,
            const std::vector<std::tuple<CompoundAbsorberComponent*, ObjectID>>&
                absorberData,
            const Leviathan::ComponentHolder<AgentCloudComponent>& agentHolder,
            const Leviathan::ComponentHolder<Leviathan::Position>&
                scenenodeHolder,
            const Leviathan::ComponentHolder<MembraneComponent>& membraneHolder,
            const Leviathan::ComponentHolder<CompoundAbsorberComponent>&
                absorberHolder)
    {
        decltype(m_agents)::TupleCachedComponentCollectionHelper(
            m_agents.CachedComponents, agentData, scenenodeData, agentHolder,
            scenenodeHolder);

        decltype(m_absorbers)::TupleCachedComponentCollectionHelper(
            m_absorbers.CachedComponents, membraneData, absorberData,
            scenenodeData, membraneHolder, absorberHolder, scenenodeHolder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<AgentCloudComponent*, ObjectID>>&
                agentData,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                scenenodeData,
            const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                membraneData,
            const std::vector<std::tuple<CompoundAbsorberComponent*, ObjectID>>&
                absorberData)
    {
        m_agents.CachedComponents.RemoveBasedOnKeyTupleList(agentData);
        m_agents.CachedComponents.RemoveBasedOnKeyTupleList(scenenodeData);

        m_absorbers.CachedComponents.RemoveBasedOnKeyTupleList(membraneData);
        m_absorbers.CachedComponents.RemoveBasedOnKeyTupleList(absorberData);
        m_absorbers.CachedComponents.RemoveBasedOnKeyTupleList(scenenodeData);
    }

    void
        Clear()
    {
        m_agents.Clear();
        m_absorbers.Clear();
    }

private:
    void
        absorbFromCloud(CompoundCloudComponent* compoundCloud,
            CompoundId id,
            CompoundAbsorberComponent& absorber,
            int x,
            int y);

private:
    // All entities that have a compoundCloudsComponent.
    // These are all the toxins.
    Leviathan::SystemCachedComponentCollectionStorage<
        std::tuple<AgentCloudComponent&, Leviathan::Position&>>
        m_agents;


    Leviathan::SystemCachedComponentCollectionStorage<
        std::tuple<MembraneComponent&,
            CompoundAbsorberComponent&,
            Leviathan::Position&>>
        m_absorbers;
};

} // namespace thrive
