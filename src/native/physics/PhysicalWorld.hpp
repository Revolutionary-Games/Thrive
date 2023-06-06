#pragma once

#include <memory>
#include <optional>

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/MotionType.h"

#include "core/ForwardDefinitions.hpp"

#include "Layers.hpp"

namespace JPH
{
class PhysicsSystem;
class TempAllocator;
class JobSystemThreadPool;
class BodyID;
class Shape;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicsBody;

/// \brief Main handling class of the physics simulation
///
/// Before starting the physics an allocator needs to be enabled for Jolt (for example the C interface library init
/// does this) and collision types registered.
class PhysicalWorld
{
    // Pimpl-idiom class for hiding some properties to reduce needed headers and size of this class
    class Pimpl;

public:
    PhysicalWorld();
    ~PhysicalWorld();

    // TODO: multithread this and allow physics to run while other stuff happens
    /// \brief Process physics
    /// \returns True when enough time has passed and physics was stepped
    bool Process(float delta);

    // TODO: physics debug drawing

    Ref<PhysicsBody> CreateMovingBody(
        const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position, JPH::Quat rotation = JPH::Quat::sIdentity());

    Ref<PhysicsBody> CreateStaticBody(
        const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position, JPH::Quat rotation = JPH::Quat::sIdentity());

    void DestroyBody(const Ref<PhysicsBody>& body);

    void ReadBodyTransform(JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const;

    void SetGravity(JPH::Vec3 newGravity);
    void RemoveGravity();

    /// \brief Cast a ray from start point to endOffset (i.e. end = start + endOffset)
    /// \returns When hit something a tuple of the fraction from start to end, the hit position, and the ID of the hit
    // body
    [[nodiscard]] std::optional<std::tuple<float, JPH::Vec3, JPH::BodyID>> CastRay(
        JPH::RVec3 start, JPH::Vec3 endOffset);

    [[nodiscard]] inline float GetLatestPhysicsTime() const
    {
        return latestPhysicsTime;
    }

    [[nodiscard]] inline float GetAveragePhysicsTime() const
    {
        return averagePhysicsTime;
    }

private:
    /// \brief Creates the physics system
    void InitPhysicsWorld();

    void StepPhysics(JPH::JobSystemThreadPool& jobs, float time);

    Ref<PhysicsBody> CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
        JPH::RVec3Arg position, JPH::Quat rotation = JPH::Quat::sIdentity());

    void OnPostBodyAdded(const Ref<PhysicsBody>& body);

private:
    float elapsedSinceUpdate = 0;

    int bodyCount = 0;
    bool changesToBodies = true;
    int simulationsToNextOptimization = 1;
    float latestPhysicsTime = 0;
    float averagePhysicsTime = 0;

    /// \brief The main part, the physics system that simulates this world
    std::unique_ptr<JPH::PhysicsSystem> physicsSystem;

    std::unique_ptr<ContactListener> contactListener;
    std::unique_ptr<BodyActivationListener> activationListener;

    // TODO: switch to this custom one
    // std::unique_ptr<TaskSystem> jobSystem;
    std::unique_ptr<JPH::JobSystemThreadPool> jobSystem;

    std::unique_ptr<JPH::TempAllocator> tempAllocator;

    // Simulation configuration
    float physicsFrameRate = 60;
    int collisionStepsPerUpdate = 1;
    int integrationSubSteps = 1;

    int simulationsBetweenBroadPhaseOptimization = 67;

    // Settings that only apply when creating a new physics system

    const uint maxBodies = 10240;

    /// \details Jolt documentation says that 0 means automatic
    const uint maxBodyMutexes = 0;

    const uint maxBodyPairs = 65536;
    const uint maxContactConstraints = 20480;

    // This is last to make sure resources held by this are deleted last
    std::unique_ptr<Pimpl> pimpl;
};

} // namespace Thrive::Physics
