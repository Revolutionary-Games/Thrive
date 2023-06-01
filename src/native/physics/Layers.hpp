#pragma once

#include "Jolt/Jolt.h"
#include "Jolt/Physics/Collision/BroadPhase/BroadPhaseLayer.h"
#include "Jolt/Physics/Collision/ObjectLayer.h"

#include "core/Logger.hpp"

namespace Thrive::Physics
{

/// \brief Overall layer configuration, note that in Jolt these are not meant to be gameplay categories and there
/// should only exist a couple of these
namespace Layers
{
static constexpr JPH::ObjectLayer NON_MOVING = 0;
static constexpr JPH::ObjectLayer MOVING = 1;
static constexpr JPH::ObjectLayer DEBRIS = 2;
static constexpr JPH::ObjectLayer SENSOR = 3;
static constexpr JPH::ObjectLayer PROJECTILE = 4;
static constexpr JPH::ObjectLayer NUM_LAYERS = 8;
}; // namespace Layers

/// \brief Configuration for which object layer types can collide with each other
class ObjectLayerPairFilter : public JPH::ObjectLayerPairFilter
{
public:
    [[nodiscard]] bool ShouldCollide(JPH::ObjectLayer object1, JPH::ObjectLayer object2) const override
    {
        switch (object1)
        {
            case Layers::NON_MOVING:
                return object2 == Layers::MOVING || object2 == Layers::DEBRIS || object2 == Layers::PROJECTILE;
            case Layers::MOVING:
                return object2 == Layers::NON_MOVING || object2 == Layers::MOVING || object2 == Layers::SENSOR ||
                    object2 == Layers::PROJECTILE;
            case Layers::DEBRIS:
                return object2 == Layers::NON_MOVING;
            case Layers::SENSOR:
                return object2 == Layers::MOVING;
            case Layers::PROJECTILE:
                return object2 == Layers::NON_MOVING || object2 == Layers::MOVING;
            default:
                LOG_ERROR("Invalid object layer checked for collision");
                return false;
        }
    }
};

/// \brief Broadphase layers (there needs to be a mapping from each object layer to a broadphase layer)
namespace BroadPhaseLayers
{
static constexpr JPH::BroadPhaseLayer NON_MOVING(0);
static constexpr JPH::BroadPhaseLayer MOVING(1);
static constexpr JPH::BroadPhaseLayer DEBRIS(2);
static constexpr JPH::BroadPhaseLayer SENSOR(3);
static constexpr JPH::BroadPhaseLayer PROJECTILE(4);
static constexpr uint NUM_LAYERS(5);
}; // namespace BroadPhaseLayers

/// \brief Broadphase layer handling, converts object layers to broadphase layers
class BroadPhaseLayerInterface final : public JPH::BroadPhaseLayerInterface
{
public:
    [[nodiscard]] uint GetNumBroadPhaseLayers() const override
    {
        return BroadPhaseLayers::NUM_LAYERS;
    }

    [[nodiscard]] JPH::BroadPhaseLayer GetBroadPhaseLayer(JPH::ObjectLayer layer) const override
    {
        // This is where the layer mapping is defined
        // Hopefully this gets optimized very well by the compiler (the samples used a map but that was probably worse
        // approach than this for performance)

        switch (layer)
        {
            case Layers::NON_MOVING:
                return BroadPhaseLayers::NON_MOVING;
            case Layers::MOVING:
                return BroadPhaseLayers::MOVING;
            case Layers::DEBRIS:
                return BroadPhaseLayers::DEBRIS;
            case Layers::SENSOR:
                return BroadPhaseLayers::SENSOR;
            case Layers::PROJECTILE:
                return BroadPhaseLayers::PROJECTILE;
            case Layers::NUM_LAYERS:
            default:
                LOG_ERROR("Attempt to get broadphase layer that doesn't exist");
                std::abort();
        }
    }

#if defined(JPH_EXTERNAL_PROFILE) || defined(JPH_PROFILE_ENABLED)
    const char* GetBroadPhaseLayerName(JPH::BroadPhaseLayer layer) const override
    {
        switch ((JPH::BroadPhaseLayer::Type)layer)
        {
            case (JPH::BroadPhaseLayer::Type)BroadPhaseLayers::NON_MOVING:
                return "NON_MOVING";
            case (JPH::BroadPhaseLayer::Type)BroadPhaseLayers::MOVING:
                return "MOVING";
            case (JPH::BroadPhaseLayer::Type)BroadPhaseLayers::DEBRIS:
                return "DEBRIS";
            case (JPH::BroadPhaseLayer::Type)BroadPhaseLayers::SENSOR:
                return "SENSOR";
            case (JPH::BroadPhaseLayer::Type)BroadPhaseLayers::PROJECTILE:
                return "PROJECTILE";
            default:
                return "INVALID";
        }
    }
#endif // JPH_EXTERNAL_PROFILE || JPH_PROFILE_ENABLED
};

/// \brief Specifies which object layers can collide with which broadphase layers
class ObjectToBroadPhaseLayerFilter : public JPH::ObjectVsBroadPhaseLayerFilter
{
public:
    [[nodiscard]] bool ShouldCollide(JPH::ObjectLayer objectLayer, JPH::BroadPhaseLayer broadPhaseLayer) const override
    {
        switch (objectLayer)
        {
            case Layers::NON_MOVING:
                return broadPhaseLayer == BroadPhaseLayers::MOVING || broadPhaseLayer == BroadPhaseLayers::PROJECTILE;
            case Layers::MOVING:
                return broadPhaseLayer == BroadPhaseLayers::NON_MOVING || broadPhaseLayer == BroadPhaseLayers::MOVING ||
                    broadPhaseLayer == BroadPhaseLayers::SENSOR || broadPhaseLayer == BroadPhaseLayers::PROJECTILE;
            case Layers::DEBRIS:
                return broadPhaseLayer == BroadPhaseLayers::NON_MOVING;
            case Layers::SENSOR:
                return broadPhaseLayer == BroadPhaseLayers::MOVING;
            case Layers::PROJECTILE:
                return broadPhaseLayer == BroadPhaseLayers::NON_MOVING || broadPhaseLayer == BroadPhaseLayers::MOVING;
            default:
                LOG_ERROR("Invalid object layer checked for collision against broadphase layers");
                return false;
        }
    }
};

} // namespace Thrive::Physics
