#pragma once

#include "general/perlin_noise.h"
#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>
#include <engine/component_types.h>

namespace thrive {

// TODO: add more variables here for more complex fluid dynamics
class FluidEffectComponent : public Leviathan::Component {
public:
    FluidEffectComponent();

    REFERENCE_HANDLE_UNCOUNTED_TYPE(MembraneComponent);
    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::FLUID_EFFECT);
};

class FluidSystem
    : public Leviathan::System<
          std::tuple<FluidEffectComponent&, Leviathan::Physics&>> {
public:
    //! Updates the membrane calculations every frame
    FluidSystem();

    void
        Run(GameWorld& world);

    void
        CreateNodes(
            const std::vector<std::tuple<FluidEffectComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<Leviathan::Physics*, ObjectID>>&
                seconddata,
            const ComponentHolder<FluidEffectComponent>& firstholder,
            const ComponentHolder<Leviathan::Physics>& secondholder)
    {
        TupleCachedComponentCollectionHelper(
            CachedComponents, firstdata, seconddata, firstholder, secondholder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<FluidEffectComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<Leviathan::Physics*, ObjectID>>&
                seconddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
    }

    Float2
        getVelocityAt(Float2 position);

private:
    Float2
        sampleNoise(Float2 pos, float time);

    float millisecondsPassed = 0.0;
    PerlinNoise noiseDisturbancesX;
    PerlinNoise noiseDisturbancesY;
    PerlinNoise noiseCurrentsX;
    PerlinNoise noiseCurrentsY;

    static const Float2 scale;

    static constexpr float maxForceApplied = 0.525f;
    static constexpr float disturbanceTimescale = 0.001f;
    static constexpr float currentsTimescale = 0.001f / 500.0f;
    static constexpr float currentsStretchingMultiplier = 1.0f / 10.0f;
    static constexpr float minCurrentIntensity = 0.4f;
    static constexpr float disturbanceToCurrentsRatio = 0.15f;
    static constexpr float positionScaling = 0.05f;
};

} // namespace thrive
