#pragma once

#include <memory>
#include <optional>

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/AllowedDOFs.h"
#include "Jolt/Physics/Body/MotionType.h"

#include "core/ForwardDefinitions.hpp"

#include "Layers.hpp"
#include "PhysicsCollision.hpp"
#include "PhysicsRayWithUserData.hpp"

namespace JPH
{
class PhysicsSystem;
class TempAllocator;
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

    /// \brief Process physics
    /// \returns True when enough time has passed and physics was stepped
    bool Process(float delta);

    /// \brief Processes as many physics steps as needed in the background
    ///
    /// Note that WaitForPhysicsToCompete must be called after this to ensure that the physics has finished
    void ProcessInBackground(float delta);

    void WaitForPhysicsToComplete();

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

    /// \brief Detaches a body to remove it from the simulation. It can be added back with AddBody
    ///
    /// Note that currently all constraints this body is part of will be deleted permanently (i.e. they won't be
    /// restored even if this body is added back to the world). Also bodies are world specific so the body cannot be
    /// added back to a different physics world.
    void DetachBody(PhysicsBody& body);

    void DestroyBody(const Ref<PhysicsBody>& body);

    void SetDamping(JPH::BodyID bodyId, float damping, const float* angularDamping = nullptr);

    void ReadBodyTransform(JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const;
    void ReadBodyVelocity(JPH::BodyID bodyId, JPH::Vec3& velocityReceiver, JPH::Vec3& angularVelocityReceiver) const;

    void GiveImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse);
    void SetVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity);

    void SetAngularVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity);
    void GiveAngularImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse);

    void SetVelocityAndAngularVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity, JPH::Vec3Arg angularVelocity);

    /// \brief Enables (or updates settings) for a body to have per step movement control
    ///
    /// This is thread safe as long as no two same bodies get this called at the same time
    void SetBodyControl(
        PhysicsBody& bodyWrapper, JPH::Vec3Arg movementImpulse, JPH::Quat targetRotation, float rotationRate);

    void DisableBodyControl(PhysicsBody& bodyWrapper);

    void SetPosition(JPH::BodyID bodyId, JPH::DVec3Arg position, bool activate = true);

    void SetBodyAllowSleep(JPH::BodyID bodyId, bool allowSleeping);

    /// \brief Ensures body's Y coordinate is 0, if not moves it so that it is 0
    /// \returns True if the body's position changed, false if no fix was needed
    bool FixBodyYCoordinateToZero(JPH::BodyID bodyId);

    void ChangeBodyShape(JPH::BodyID bodyId, const JPH::RefConst<JPH::Shape>& shape, bool activate = true);

    // ------------------------------------ //
    // Collisions

    /// \brief Starts collision recording. collisionRecordingTarget must have at least space for maxRecordedCollisions
    /// elements, otherwise this will overwrite random memory
    const int32_t* EnableCollisionRecording(
        PhysicsBody& body, CollisionRecordListType collisionRecordingTarget, int maxRecordedCollisions);

    void DisableCollisionRecording(PhysicsBody& body);

    /// \brief Makes body ignore collisions with ignoredBody
    void AddCollisionIgnore(PhysicsBody& body, const PhysicsBody& ignoredBody, bool skipDuplicates);

    /// \brief Removes a previously added body ignore
    ///
    /// Note that this removes the ignore just from body so if the ignore relationship is two-ways this doesn't make
    /// collisions happen
    /// \returns True when removed, false if the body was not ignored
    bool RemoveCollisionIgnore(PhysicsBody& body, const PhysicsBody& noLongerIgnoredBody);

    /// \brief Sets an exact list of ignored bodies for body. Removes all existing ignores
    /// \param ignoredBodies list of bodies to ignore (should be a pointer to array of references)
    /// \param ignoreCount specifies the length of the ignoredBodies array, note that instead of passing an array of
    /// length 0 calling ClearCollisionIgnores is preferred
    void SetCollisionIgnores(PhysicsBody& body, PhysicsBody* const& ignoredBodies, int ignoreCount);

    /// \brief More efficient variant of clearing all ignores and setting just one
    void SetSingleCollisionIgnore(PhysicsBody& body, const PhysicsBody& onlyIgnoredBody);

    /// \brief Clears all collision ignores on a body
    void ClearCollisionIgnores(PhysicsBody& body);

    /// \brief When called with true this disables all collisions for the given body (can be restored by calling this
    /// method again with false parameter)
    void SetCollisionDisabledState(PhysicsBody& body, bool disableAllCollisions);

    void AddCollisionFilter(PhysicsBody& body, CollisionFilterCallback callback);

    void DisableCollisionFilter(PhysicsBody& body);

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

    /// \brief Cast a ray from start point to start + endOffset like CastRay but find all hits (up to a limit) and
    /// return the bodies' user data with the collision info
    /// \returns The number of hits or 0 if nothing is hit. This only writes to dataReceiver up to the number of hits
    /// received everything else is untouched
    int CastRayGetAllUserData(
        JPH::RVec3 start, JPH::Vec3 endOffset, PhysicsRayWithUserData dataReceiver[], int maxHits);

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

    /// \brief Called by PhysicsBody when it has a recorded collision. This is done to reset bodies that haven't
    /// received new collisions on the next physics update
    void ReportBodyWithActiveCollisions(PhysicsBody& body);

protected:
    void PerformPhysicsStepOperations(float delta);

private:
    /// \brief Creates the physics system
    void InitPhysicsWorld();

    void StepPhysics(float time);

    Ref<PhysicsBody> CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
        JPH::RVec3Arg position, JPH::Quat rotation = JPH::Quat::sIdentity(),
        JPH::EAllowedDOFs allowedDegreesOfFreedom = JPH::EAllowedDOFs::All);

    /// \brief Called after body has been created
    Ref<PhysicsBody> OnBodyCreated(Ref<PhysicsBody>&& body, bool addToWorld);

    /// \brief Called when body is added to the world (can happen multiple times for each body)
    void OnPostBodyAdded(PhysicsBody& body);

    void OnBodyPreLeaveWorld(PhysicsBody& body);
    void OnPostBodyLeaveWorld(PhysicsBody& body);

    /// \brief Updates the user pointer for a body to enable / disable newly set bitflags in the pointer for some
    /// various features
    void UpdateBodyUserPointer(const PhysicsBody& body);

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

    std::unique_ptr<JPH::TempAllocator> tempAllocator;

    // Simulation configuration
    float physicsFrameRate = 60;
    int collisionStepsPerUpdate = 1;

    int simulationsBetweenBroadPhaseOptimization = 67;

    /// When running multiple physics steps with a single call to the simulation update methods this is used to not
    /// discard collision recording information after the first step allowing application logic to read it
    bool nextStepIsFresh = true;

    std::atomic<bool> runningBackgroundSimulation{false};

    // Settings that only apply when creating a new physics system

    const unsigned int maxBodies = 10240;

    /// \details Jolt documentation says that 0 means automatic
    const unsigned int maxBodyMutexes = 0;

    const unsigned int maxBodyPairs = 65536;
    const unsigned int maxContactConstraints = 20480;

    // This is last to make sure resources held by this are deleted last
    std::unique_ptr<Pimpl> pimpl;
};

} // namespace Thrive::Physics
