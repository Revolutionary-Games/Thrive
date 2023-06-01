#pragma once

#include <memory>
#include <optional>

#include "Jolt/Physics/PhysicsSettings.h"

#include "Include.h"
#include "core/ForwardDefinitions.hpp"

#include "Layers.hpp"

namespace JPH
{
class PhysicsSystem;
class TempAllocator;
class JobSystemThreadPool;
class BodyID;
} // namespace JPH

namespace Thrive::Physics
{

/// \brief Main handling class of the physics simulation
class PhysicalWorld
{
public:
    PhysicalWorld();
    ~PhysicalWorld();

    /// \brief Process physics
    /// \returns True when enough time has passed and physics was stepped
    bool Process(float delta);

    // TODO: physics debug drawing

    /// \brief Cast a ray from start point to endOffset (i.e. end = start + endOffset)
    /// \returns When hit something a tuple of the fraction from start to end, the hit position, and the ID of the hit
    // body
    std::optional<std::tuple<float, JPH::Vec3, JPH::BodyID>> CastRay(JPH::RVec3 start, JPH::Vec3 endOffset);

private:
    /// \brief Creates the physics system
    void InitPhysicsWorld();

    void StepPhysics(JPH::JobSystemThreadPool& jobs, float time);

private:
    float elapsedSinceUpdate = 0;

    // TODO: rename all the following fields

    /// \brief The main part, the physics system that simulates this world
    std::unique_ptr<JPH::PhysicsSystem> physicsSystem;

    // Note the following variables are in a specific order for destruction

    BroadPhaseLayerInterface broadPhaseLayer;
    ObjectToBroadPhaseLayerFilter objectToBroadPhaseLayer;
    ObjectLayerPairFilter objectToObjectPair;

    std::unique_ptr<ContactListener> contactListener;

    // TODO: switch to this custom one
    // std::unique_ptr<TaskSystem> jobSystem;
    std::unique_ptr<JPH::JobSystemThreadPool> jobSystem;

    std::unique_ptr<JPH::TempAllocator> tempAllocator;

    // Simulation configuration
    float physicsFrameRate = 60;
    int collisionStepsPerUpdate = 1;
    int integrationSubSteps = 1;

    JPH::PhysicsSettings physicsSettings;

    // TODO: update this when changed
    JPH::Vec3 gravity = JPH::Vec3(0, -9.81f, 0);

    // Settings that only apply when creating a new physics system

    uint maxBodies = 10240;

    /// \details Jolt documentation says that 0 means automatic
    uint maxBodyMutexes = 0;

    uint maxBodyPairs = 65536;
    uint maxContactConstraints = 20480;
};

} // namespace Thrive::Physics
