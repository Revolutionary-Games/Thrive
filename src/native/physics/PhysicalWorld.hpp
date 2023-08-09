#pragma once

#include <memory>
#include <optional>

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/AllowedDOFs.h"
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

constexpr EAllowedDOFs AllRotationAllowed = EAllowedDOFs::RotationX | EAllowedDOFs::RotationY | EAllowedDOFs::RotationZ;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicsBody;
class StepListener;

/// \brief Main handling class of the physics simulation
///
/// Before starting the physics an allocator needs to be enabled for Jolt (for example the C interface library init
/// does this) and collision types registered.
class PhysicalWorld
{
    friend StepListener;

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

    // ------------------------------------ //
    // Bodies
    Ref<PhysicsBody> CreateMovingBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
        JPH::Quat rotation = JPH::Quat::sIdentity(), bool addToWorld = true);

    Ref<PhysicsBody> CreateMovingBodyWithAxisLock(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
        JPH::Quat rotation, JPH::Vec3 lockedAxes, bool lockRotation, bool addToWorld = true);

    Ref<PhysicsBody> CreateStaticBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
        JPH::Quat rotation = JPH::Quat::sIdentity(), bool addToWorld = true);

    /// \brief Add a body that has been created but not added to the physics simulation in this world
    void AddBody(PhysicsBody& body, bool activate);

    void DestroyBody(const Ref<PhysicsBody>& body);

    void SetDamping(JPH::BodyID bodyId, float damping, const float* angularDamping = nullptr);

    void ReadBodyTransform(JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const;

    void GiveImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse);
    void SetVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity);

    void SetAngularVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity);
    void GiveAngularImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse);

    void SetBodyControl(
        PhysicsBody& bodyWrapper, JPH::Vec3Arg movementImpulse, JPH::Quat targetRotation, float rotationRate);
    void DisableBodyControl(PhysicsBody& bodyWrapper);

    void SetPosition(JPH::BodyID bodyId, JPH::DVec3Arg position, bool activate = true);

    /// \brief Ensures body's Y coordinate is 0, if not moves it so that it is 0
    /// \returns True if the body's position changed, false if no fix was needed
    bool FixBodyYCoordinateToZero(JPH::BodyID bodyId);

    // ------------------------------------ //
    // Constraints

    //! \deprecated Use CreateMovingBodyWithAxisLock instead (this is kept just to show how other constraint types
    //! should be added in the future)
    Ref<TrackedConstraint> CreateAxisLockConstraint(PhysicsBody& body, JPH::Vec3 axis, bool lockRotation);

    void DestroyConstraint(TrackedConstraint& constraint);

    void SetGravity(JPH::Vec3 newGravity);
    void RemoveGravity();

    // ------------------------------------ //
    // Misc

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

    bool DumpSystemState(std::string_view path);

    inline void SetDebugLevel(int level) noexcept
    {
        debugDrawLevel = level;
    }

    void SetDebugCameraLocation(JPH::Vec3Arg position) noexcept;

protected:
    void PerformPhysicsStepOperations(float delta);

private:
    /// \brief Creates the physics system
    void InitPhysicsWorld();

    void StepPhysics(JPH::JobSystemThreadPool& jobs, float time);

    Ref<PhysicsBody> CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
        JPH::RVec3Arg position, JPH::Quat rotation = JPH::Quat::sIdentity(),
        JPH::EAllowedDOFs allowedDegreesOfFreedom = JPH::EAllowedDOFs::All);

    /// \brief Called after body has been created
    Ref<PhysicsBody> OnBodyCreated(Ref<PhysicsBody>&& body, bool addToWorld);

    /// \brief Called when body is added to the world (can happen multiple times for each body)
    void OnPostBodyAdded(PhysicsBody& body);

    /// \brief Applies physics body control operations
    /// \param delta Is the physics step delta
    void ApplyBodyControl(PhysicsBody& bodyWrapper, float delta);

    void DrawPhysics(float delta);

private:
    float elapsedSinceUpdate = 0;

    int bodyCount = 0;
    bool changesToBodies = true;
    int simulationsToNextOptimization = 1;
    float latestPhysicsTime = 0;
    float averagePhysicsTime = 0;

    /// \brief Debug draw level (0 is disabled)
    ///
    /// 1 is just bodies
    /// 2 is also contacts
    /// 3 is also active contact points
    /// 4 is also body bounding boxes and velocities
    /// 5 is also constraints
    /// 6 is also constraint limits
    /// 7 is also constraint reference frames
    int debugDrawLevel = 0;

    /// \brief The main part, the physics system that simulates this world
    std::unique_ptr<JPH::PhysicsSystem> physicsSystem;

    std::unique_ptr<ContactListener> contactListener;
    std::unique_ptr<BodyActivationListener> activationListener;
    std::unique_ptr<StepListener> stepListener;

    // TODO: switch to this custom one
    // std::unique_ptr<TaskSystem> jobSystem;
    std::unique_ptr<JPH::JobSystemThreadPool> jobSystem;

    std::unique_ptr<JPH::TempAllocator> tempAllocator;

    // Simulation configuration
    float physicsFrameRate = 60;
    int collisionStepsPerUpdate = 1;

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
