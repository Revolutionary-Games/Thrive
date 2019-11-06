#pragma once

#include "engine/component_types.h"
#include "engine/typedefs.h"

#include "general/perlin_noise.h"
#include "microbe_stage/compounds.h"

#include <Entities/Component.h>
#include <Entities/System.h>

#include <vector>

namespace thrive {

/**
 * @brief Agents clouds that flow in the environment.
 */
class AgentCloudComponent : public Leviathan::Component {
public:
    /// The size of the compound cloud grid.
    int width, height;
    float gridSize;

    Float3 direction;

    float potency;

    /// The 2D array that contains the current compound clouds and those from
    /// last frame.
    std::vector<std::vector<float>> density;
    std::vector<std::vector<float>> oldDens;

    /// The color of the compound cloud.
    Float4 color;

    // Helpers for networking
    inline auto
        getRed() const
    {
        return color.X;
    }
    inline auto
        getGreen() const
    {
        return color.Y;
    }
    inline auto
        getBlue() const
    {
        return color.Z;
    }

    /**
     * @brief The compound id.
     */
    CompoundId m_compoundId = NULL_COMPOUND;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(AgentCloudComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::AGENT_CLOUD);

public:
    AgentCloudComponent(CompoundId Id, float red, float green, float blue);

    // /**
    // * @brief Lua bindings
    // *
    // * Exposes:
    // * - CompoundCloudComponent()
    // *
    // * @return
    // */
    // static void luaBindings(sol::state &lua);

    // void
    // load(
    //     const StorageContainer& storage
    // ) override;

    // StorageContainer
    // storage() const override;

    /// Rate should be less than one.
    float
        getPotency();
};



/**
 * @brief Moves the compound clouds.
 * @note This system currently does nothing but is kept for the future when this
 * is planned to be used
 */
class AgentCloudSystem
    : public Leviathan::System<std::tuple<Leviathan::Position&,
          AgentCloudComponent&,
          Leviathan::RenderNode&>> {
public:
    /**
     * @brief Updates the system
     */
    void
        Run(GameWorld& world);

    void
        CreateNodes(
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<AgentCloudComponent*, ObjectID>>&
                seconddata,
            const std::vector<std::tuple<Leviathan::RenderNode*, ObjectID>>&
                thirddata,
            const ComponentHolder<Leviathan::Position>& firstholder,
            const ComponentHolder<AgentCloudComponent>& secondholder,
            const ComponentHolder<Leviathan::RenderNode>& thirdholder)
    {
        TupleCachedComponentCollectionHelper(CachedComponents, firstdata,
            seconddata, thirddata, firstholder, secondholder, thirdholder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<AgentCloudComponent*, ObjectID>>&
                seconddata,
            const std::vector<std::tuple<Leviathan::RenderNode*, ObjectID>>&
                thirddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
        CachedComponents.RemoveBasedOnKeyTupleList(thirddata);
    }
};

} // namespace thrive
