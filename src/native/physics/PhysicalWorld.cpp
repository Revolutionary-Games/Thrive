// ------------------------------------ //
#include "PhysicalWorld.hpp"

#include <cstring>
#include <fstream>

#include "boost/circular_buffer.hpp"
#include "Jolt/Core/StreamWrapper.h"
#include "Jolt/Physics/Body/BodyCreationSettings.h"
#include "Jolt/Physics/Collision/CastResult.h"
#include "Jolt/Physics/Collision/RayCast.h"
#include "Jolt/Physics/Constraints/SixDOFConstraint.h"
#include "Jolt/Physics/PhysicsScene.h"
#include "Jolt/Physics/PhysicsSettings.h"
#include "Jolt/Physics/PhysicsSystem.h"

#include "core/Math.hpp"
#include "core/Mutex.hpp"
#include "core/Spinlock.hpp"
#include "core/TaskSystem.hpp"
#include "core/Time.hpp"

#include "ArrayRayCollector.hpp"
#include "BodyActivationListener.hpp"
#include "BodyControlState.hpp"
#include "ContactListener.hpp"
#include "PhysicsBody.hpp"
#include "StepListener.hpp"
#include "TrackedConstraint.hpp"

#ifdef JPH_DEBUG_RENDERER
#include "DebugDrawForwarder.hpp"
#endif

JPH_SUPPRESS_WARNINGS

// ------------------------------------ //
namespace Thrive::Physics
{

class PhysicalWorld::Pimpl
{
public:
    Pimpl() : durationBuffer(30)
    {
#ifdef JPH_DEBUG_RENDERER
        // Convex shapes
        // This is very expensive in terms of debug rendering data amount
        bodyDrawSettings.mDrawGetSupportFunction = false;

        // Wireframe is preferred when
        bodyDrawSettings.mDrawShapeWireframe = true;

        bodyDrawSettings.mDrawCenterOfMassTransform = true;

        // TODO: some of the extra settings should be enableable
#endif

        activeBodiesWithCollisions.reserve(50);
    }

    void AddPerStepControlBody(PhysicsBody& body)
    {
        bodiesStepControlLock.Lock();

        // TODO: avoid duplicates if someone else will also add items to this list
        bodiesWithPerStepControl.emplace_back(&body);

        bodiesStepControlLock.Unlock();
    }

    void RemovePerStepControlBody(PhysicsBody& body)
    {
        bodiesStepControlLock.Lock();

        for (auto iter = bodiesWithPerStepControl.begin(); iter != bodiesWithPerStepControl.end(); ++iter)
        {
            if ((*iter).get() == &body)
            {
                // TODO: if items can be in this vector for multiple reasons this will need to check that
                bodiesWithPerStepControl.erase(iter);
                bodiesStepControlLock.Unlock();
                return;
            }
        }

        bodiesStepControlLock.Unlock();

        LOG_ERROR("Didn't find body in internal vector of bodies needing operations each step");
    }

    float AddAndCalculateAverageTime(float duration)
    {
        durationBuffer.push_back(duration);

        const auto size = durationBuffer.size();

        if (size < 1)
        {
            LOG_ERROR("Duration circular buffer empty");
            return -1;
        }

        float durations = 0;

        for (const auto value : durationBuffer)
        {
            durations += value;
        }

        return durations / static_cast<float>(size);
    }

    inline void PushBodyWithActiveCollisions(PhysicsBody& body)
    {
        activeBodyWriteLock.Lock();

        activeBodiesWithCollisions.emplace_back(&body);

        activeBodyWriteLock.Unlock();
    }

    /// \brief Ensures a body is no longer referenced by any step data
    void NotifyBodyRemove(PhysicsBody* body) noexcept // NOLINT(*-make-member-function-const)
    {
        const auto size = activeBodiesWithCollisions.size();
        for (size_t i = 0; i < size; ++i)
        {
            if (activeBodiesWithCollisions[i] != body)
                continue;

            // Need to remove this, as we don't have to ensure the order we can swap this to be last in the vector
            // and pop the last
            if (i + 1 < size)
            {
                std::swap(activeBodiesWithCollisions[i], activeBodiesWithCollisions[size - 1]);
            }
            else
            {
                // Already last
            }

            activeBodiesWithCollisions.pop_back();

            return;
        }
    }

    void HandleExpiringBodyCollisions() // NOLINT(*-make-member-function-const)
    {
        // Mark all previous collision data as empty
        const auto size = activeBodiesWithCollisions.size();
        for (size_t i = 0; i < size; ++i)
        {
            activeBodiesWithCollisions[i]->ClearRecordedData();
        }

        activeBodiesWithCollisions.clear();
    }

    inline void IncrementStepCounter() noexcept
    {
        ++stepCounter;

        if (stepCounter >= std::numeric_limits<decltype(stepCounter)>::max())
        {
            // Skip the last value to ensure no uninitialized values cause a problem if they appear on this update
            stepCounter = 1;
        }
    }

public:
    BroadPhaseLayerInterface broadPhaseLayer;
    ObjectToBroadPhaseLayerFilter objectToBroadPhaseLayer;
    ObjectLayerPairFilter objectToObjectPair;

    JPH::PhysicsSettings physicsSettings;

    boost::circular_buffer<float> durationBuffer;

    std::vector<Ref<PhysicsBody>> bodiesWithPerStepControl;

    Spinlock bodiesStepControlLock;

    JPH::Vec3 gravity = JPH::Vec3(0, -9.81f, 0);

    std::vector<PhysicsBody*> activeBodiesWithCollisions;

    Spinlock activeBodyWriteLock;

    uint32_t stepCounter = 0;

#ifdef JPH_DEBUG_RENDERER
    JPH::BodyManager::DrawSettings bodyDrawSettings;

    JPH::Vec3Arg debugDrawCameraLocation = {};
#endif
};

PhysicalWorld::PhysicalWorld() : pimpl(std::make_unique<Pimpl>())
{
#ifdef USE_OBJECT_POOLS
    tempAllocator = std::make_unique<JPH::TempAllocatorImpl>(32 * 1024 * 1024);
#else
    tempAllocator = std::make_unique<JPH::TempAllocatorMalloc>();
#endif

    InitPhysicsWorld();
}

PhysicalWorld::~PhysicalWorld()
{
    if (runningBackgroundSimulation)
    {
        LOG_ERROR("World is being destroyed while a background operation is in progress");
        while (runningBackgroundSimulation)
        {
            HYPER_THREAD_YIELD;
        }
    }

    if (bodyCount != 0)
    {
        LOG_ERROR(
            "PhysicalWorld destroyed while not all bodies were removed, existing bodies: " + std::to_string(bodyCount));
    }
}

// ------------------------------------ //
void PhysicalWorld::InitPhysicsWorld()
{
    physicsSystem = std::make_unique<JPH::PhysicsSystem>();
    physicsSystem->Init(maxBodies, maxBodyMutexes, maxBodyPairs, maxContactConstraints, pimpl->broadPhaseLayer,
        pimpl->objectToBroadPhaseLayer, pimpl->objectToObjectPair);
    physicsSystem->SetPhysicsSettings(pimpl->physicsSettings);

    physicsSystem->SetGravity(pimpl->gravity);

    // Contact listening
    contactListener = std::make_unique<ContactListener>();

    // contactListener->SetNextListener(something);
    physicsSystem->SetContactListener(contactListener.get());

    // Activation listening
    activationListener = std::make_unique<BodyActivationListener>();
    physicsSystem->SetBodyActivationListener(activationListener.get());

    stepListener = std::make_unique<StepListener>(*this);
    physicsSystem->AddStepListener(stepListener.get());
}

// ------------------------------------ //
bool PhysicalWorld::Process(float delta)
{
    if (runningBackgroundSimulation)
    {
        LOG_ERROR("May not call world sync process while background process is used");
        return false;
    }

    nextStepIsFresh = true;

    elapsedSinceUpdate += delta;

    const auto singlePhysicsFrame = 1 / physicsFrameRate;

    bool simulatedPhysics = false;
    float simulatedTime = 0;

    // TODO: limit max steps per frame to avoid massive potential for lag spikes
    // TODO: alternatively to this it is possible to use a bigger timestep at once but then collision steps and
    // integration steps should be incremented
    while (elapsedSinceUpdate > singlePhysicsFrame)
    {
        elapsedSinceUpdate -= singlePhysicsFrame;
        simulatedTime += singlePhysicsFrame;
        StepPhysics(singlePhysicsFrame);
        simulatedPhysics = true;
    }

    if (!simulatedPhysics)
        return false;

    DrawPhysics(simulatedTime);

    return true;
}

void PhysicalWorld::ProcessInBackground(float delta)
{
    bool previous = false;
    if (!runningBackgroundSimulation.compare_exchange_strong(previous, true))
    {
        LOG_ERROR("Trying to start another background physics run while previous wasn't waited for");
        return;
    }

    nextStepIsFresh = true;
    backgroundSimulatedTime = 0;

    elapsedSinceUpdate += delta;

    const auto singlePhysicsFrame = 1 / physicsFrameRate;

    if (elapsedSinceUpdate < singlePhysicsFrame)
    {
        // We can just early exit if there's nothing to do
        return;
    }

    runningBackgroundSimulation = true;

    TaskSystem::Get().QueueTask([this]() { StepAllPhysicsStepsInBackground(); });
}

bool PhysicalWorld::WaitForPhysicsToComplete()
{
    // For now this never sleep as it is assumed the physics should be done by the time the main thread gets here
    // or very close to done
    while (runningBackgroundSimulation)
    {
        HYPER_THREAD_YIELD;
    }

    if (nextStepIsFresh)
        return false;

    // Draw physics only here at the end to ensure this happens on the main thread
    DrawPhysics(backgroundSimulatedTime);

    backgroundSimulatedTime = 0;
    return true;
}

// ------------------------------------ //
Ref<PhysicsBody> PhysicalWorld::CreateMovingBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
    JPH::Quat rotation /* = JPH::Quat::sIdentity()*/, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to body create");
        return nullptr;
    }

    // TODO: multithreaded body adding?
    return OnBodyCreated(CreateBody(*shape, JPH::EMotionType::Dynamic, Layers::MOVING, position, rotation), addToWorld);
}

Ref<PhysicsBody> PhysicalWorld::CreateMovingBodyWithAxisLock(const JPH::RefConst<JPH::Shape>& shape,
    JPH::RVec3Arg position, JPH::Quat rotation, JPH::Vec3 lockedAxes, bool lockRotation, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to body create");
        return nullptr;
    }

    JPH::EAllowedDOFs degreesOfFreedom = JPH::EAllowedDOFs::All;

    if (lockedAxes.GetX() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationX;

    if (lockedAxes.GetY() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationY;

    if (lockedAxes.GetZ() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationZ;

    if (lockRotation)
    {
        if (lockedAxes.GetX() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationY;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationZ;
        }

        if (lockedAxes.GetY() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationX;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationZ;
        }

        if (lockedAxes.GetZ() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationX;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationY;
        }
    }

    // TODO: multithreaded body adding?
    return OnBodyCreated(
        CreateBody(*shape, JPH::EMotionType::Dynamic, Layers::MOVING, position, rotation, degreesOfFreedom),
        addToWorld);
}

Ref<PhysicsBody> PhysicalWorld::CreateStaticBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
    JPH::Quat rotation /* = JPH::Quat::sIdentity()*/, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to static body create");
        return nullptr;
    }

    // TODO: multithreaded body adding?
    auto body = CreateBody(*shape, JPH::EMotionType::Static, Layers::NON_MOVING, position, rotation);

    if (body == nullptr)
        return nullptr;

    if (addToWorld)
    {
        physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::DontActivate);
        OnPostBodyAdded(*body);
    }

    return body;
}

Ref<PhysicsBody> PhysicalWorld::CreateSensor(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
    JPH::Quat rotation, JPH::EMotionType motionType, bool detectStaticBodies)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to sensor create");
        return nullptr;
    }

    // Being on the moving layer also makes the sensor to detect debris, sensor layer only detects moving bodies
    auto body = CreateBody(*shape, JPH::EMotionType::Static, detectStaticBodies ? Layers::MOVING : Layers::SENSOR,
        position, rotation, JPH::EAllowedDOFs::All, true);

    if (body == nullptr)
        return nullptr;

    // Detecting static bodies is probably rare enough that this is fine to have a bit slower path
    if (detectStaticBodies)
    {
        auto underlyingBody = physicsSystem->GetBodyLockInterface().TryGetBody(body->GetId());
        if (underlyingBody != nullptr)
        {
            underlyingBody->SetSensorDetectsStatic(true);
        }
    }

    physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::Activate);
    OnPostBodyAdded(*body);

    return body;
}

void PhysicalWorld::AddBody(PhysicsBody& body, bool activate)
{
    if (body.IsInWorld() && !body.IsDetached())
    {
        LOG_ERROR("Physics body is already in some world, not adding it to this world");
        return;
    }

    if (!body.IsInSpecificWorld(this))
    {
        // This constraint can probably be relaxed quite easily but for now this has not been required so this is not
        // done
        LOG_ERROR("Physics body can only be added back to the world it was created for");
        return;
    }

    // Create constraints if not done yet
    for (auto& constraint : body.GetConstraints())
    {
        if (!constraint->IsCreatedInWorld())
        {
            // TODO: constraint creation has to be skipped if the other body the constraint is on is currently
            // detached and created later
            if ((constraint->optionalSecondBody != nullptr && constraint->optionalSecondBody.get() != &body &&
                    constraint->optionalSecondBody->IsDetached()) ||
                (constraint->firstBody.get() != &body && constraint->firstBody->IsDetached()))
            {
                LOG_ERROR("Not implemented handling for deferring constraint creation for detached body");
                continue;
            }

            physicsSystem->AddConstraint(constraint->GetConstraint().GetPtr());
            constraint->OnRegisteredToWorld(*this);
        }
    }

    physicsSystem->GetBodyInterface().AddBody(
        body.GetId(), activate ? JPH::EActivation::Activate : JPH::EActivation::DontActivate);
    OnPostBodyAdded(body);
}

void PhysicalWorld::DetachBody(PhysicsBody& body)
{
    if (!body.IsInWorld() || body.IsDetached())
    {
        LOG_ERROR("Can't detach physics body not in world or detached already");
        return;
    }

    auto& bodyInterface = physicsSystem->GetBodyInterface();

    OnBodyPreLeaveWorld(body);

    bodyInterface.RemoveBody(body.GetId());

    OnPostBodyLeaveWorld(body);

    body.MarkDetached();
}

void PhysicalWorld::DestroyBody(const Ref<PhysicsBody>& body)
{
    if (body == nullptr)
        return;

    if (!body->IsInWorld())
    {
        LOG_ERROR("Cannot destroy a physics body not in the world");
        return;
    }

    auto& bodyInterface = physicsSystem->GetBodyInterface();

    // Special handling for bodies that are detached as part of their destruction logic has already been performed
    if (body->IsDetached())
    {
        bodyInterface.DestroyBody(body->GetId());
        body->MarkRemovedFromWorld();

        return;
    }

    OnBodyPreLeaveWorld(*body);

    bodyInterface.RemoveBody(body->GetId());

    // Permanently destroy the body
    bodyInterface.DestroyBody(body->GetId());
    body->MarkRemovedFromWorld();

    OnPostBodyLeaveWorld(*body);

    changesToBodies = true;
}

// ------------------------------------ //
void PhysicalWorld::SetDamping(JPH::BodyID bodyId, float damping, const float* angularDamping /*= nullptr*/)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for setting damping");
        return;
    }

    JPH::Body& body = lock.GetBody();
    auto* motionProperties = body.GetMotionProperties();

    motionProperties->SetLinearDamping(damping);

    if (angularDamping != nullptr)
        motionProperties->SetAngularDamping(*angularDamping);
}

// ------------------------------------ //
void PhysicalWorld::ReadBodyTransform(
    JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const
{
    JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (lock.Succeeded()) [[likely]]
    {
        const JPH::Body& body = lock.GetBody();

        positionReceiver = body.GetPosition();
        rotationReceiver = body.GetRotation();
    }
    else
    {
        LOG_ERROR("Couldn't lock body for reading transform");
        std::memset(&positionReceiver, 0, sizeof(positionReceiver));
        std::memset(&rotationReceiver, 0, sizeof(rotationReceiver));
    }
}

void PhysicalWorld::ReadBodyVelocity(
    JPH::BodyID bodyId, JPH::Vec3& velocityReceiver, JPH::Vec3& angularVelocityReceiver) const
{
    JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (lock.Succeeded()) [[likely]]
    {
        const JPH::Body& body = lock.GetBody();

        velocityReceiver = body.GetLinearVelocity();
        angularVelocityReceiver = body.GetAngularVelocity();
    }
    else
    {
        LOG_ERROR("Couldn't lock body for reading velocity");
        std::memset(&velocityReceiver, 0, sizeof(velocityReceiver));
        std::memset(&angularVelocityReceiver, 0, sizeof(angularVelocityReceiver));
    }
}

void PhysicalWorld::GiveImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for giving impulse");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.AddImpulse(impulse);
}

void PhysicalWorld::SetVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for setting velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetLinearVelocityClamped(velocity);
}

void PhysicalWorld::SetAngularVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for setting angular velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetAngularVelocityClamped(velocity);
}

void PhysicalWorld::GiveAngularImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for giving angular impulse");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.AddAngularImpulse(impulse);
}

void PhysicalWorld::SetVelocityAndAngularVelocity(
    JPH::BodyID bodyId, JPH::Vec3Arg velocity, JPH::Vec3Arg angularVelocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for setting velocity and angular velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetLinearVelocityClamped(velocity);
    body.SetAngularVelocityClamped(angularVelocity);
}

void PhysicalWorld::SetBodyControl(
    PhysicsBody& bodyWrapper, JPH::Vec3Arg movementImpulse, JPH::Quat targetRotation, float rotationRate)
{
    if (rotationRate <= 0) [[unlikely]]
    {
        LOG_ERROR("Invalid rotationRate variable for controlling a body, needs to be positive");
        return;
    }

    BodyControlState* state;

    bool justEnabled = bodyWrapper.EnableBodyControlIfNotAlready();

    state = bodyWrapper.GetBodyControlState();

    if (state == nullptr) [[unlikely]]
    {
        LOG_ERROR("Logic error in body control state creation (state should have been created)");
        return;
    }

    if (justEnabled) [[unlikely]]
    {
        pimpl->AddPerStepControlBody(bodyWrapper);
    }

    state->targetRotation = targetRotation;
    state->movement = movementImpulse;
    state->rotationRate = rotationRate;
}

void PhysicalWorld::DisableBodyControl(PhysicsBody& bodyWrapper)
{
    if (bodyWrapper.DisableBodyControl())
    {
        pimpl->RemovePerStepControlBody(bodyWrapper);
    }
}

void PhysicalWorld::SetPosition(JPH::BodyID bodyId, JPH::DVec3Arg position, bool activate)
{
    physicsSystem->GetBodyInterface().SetPosition(
        bodyId, position, activate ? JPH::EActivation::Activate : JPH::EActivation::DontActivate);
}

void PhysicalWorld::SetPositionAndRotation(
    JPH::BodyID bodyId, JPH::DVec3Arg position, JPH::QuatArg rotation, bool activate)
{
    if (!activate)
    {
        physicsSystem->GetBodyInterface().SetPositionAndRotationWhenChanged(
            bodyId, position, rotation, JPH::EActivation::DontActivate);
    }
    else
    {
        physicsSystem->GetBodyInterface().SetPositionAndRotation(
            bodyId, position, rotation, JPH::EActivation::Activate);
    }
}

void PhysicalWorld::SetBodyAllowSleep(JPH::BodyID bodyId, bool allowSleeping)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for setting allow sleep");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetAllowSleeping(allowSleeping);
}

bool PhysicalWorld::FixBodyYCoordinateToZero(JPH::BodyID bodyId)
{
    decltype(std::declval<JPH::Body>().GetPosition()) position;

    {
        // TODO: maybe there's a way to avoid the double lock here? (setting position takes a lock as well)
        JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
        if (!lock.Succeeded()) [[unlikely]]
        {
            LOG_ERROR("Can't lock body for y-position fix");
            return false;
        }

        const JPH::Body& body = lock.GetBody();

        position = body.GetPosition();
    }

    // Likely is used here as this is only called from C# when drifting has actually been detected
    if (std::abs(position.GetY()) > 0.0005f) [[likely]]
    {
        SetPosition(bodyId, {position.GetX(), 0, position.GetZ()}, false);
        return true;
    }

    return false;
}

void PhysicalWorld::ChangeBodyShape(JPH::BodyID bodyId, const JPH::RefConst<JPH::Shape>& shape, bool activate)
{
    // For now this always recalculates mass and inertia
    physicsSystem->GetBodyInterface().SetShape(
        bodyId, shape, true, activate ? JPH::EActivation::Activate : JPH::EActivation::DontActivate);
}

// ------------------------------------ //
const int32_t* PhysicalWorld::EnableCollisionRecording(
    PhysicsBody& body, CollisionRecordListType collisionRecordingTarget, int maxRecordedCollisions)
{
    if (maxRecordedCollisions < 1)
    {
        LOG_ERROR("Cannot start recording less than 1 collision");
        DisableCollisionRecording(body);
        return nullptr;
    }

    body.SetCollisionRecordingTarget(collisionRecordingTarget, maxRecordedCollisions);

    if (body.MarkCollisionRecordingEnabled())
    {
        UpdateBodyUserPointer(body);
    }

    return body.GetRecordedCollisionTargetAddress();
}

void PhysicalWorld::DisableCollisionRecording(PhysicsBody& body)
{
    if (body.MarkCollisionRecordingDisabled())
    {
        UpdateBodyUserPointer(body);
    }

    body.ClearCollisionRecordingTarget();
}

void PhysicalWorld::AddCollisionIgnore(PhysicsBody& body, const PhysicsBody& ignoredBody, bool skipDuplicates)
{
    body.AddCollisionIgnore(ignoredBody, skipDuplicates);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

bool PhysicalWorld::RemoveCollisionIgnore(PhysicsBody& body, const PhysicsBody& noLongerIgnoredBody)
{
    const auto changes = body.RemoveCollisionIgnore(noLongerIgnoredBody);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }

    return changes;
}

void PhysicalWorld::SetCollisionIgnores(PhysicsBody& body, PhysicsBody* const& ignoredBodies, int ignoreCount)
{
    body.SetCollisionIgnores(ignoredBodies, ignoreCount);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::SetSingleCollisionIgnore(PhysicsBody& body, const PhysicsBody& onlyIgnoredBody)
{
    body.SetSingleCollisionIgnore(onlyIgnoredBody);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::ClearCollisionIgnores(PhysicsBody& body)
{
    body.ClearCollisionIgnores();

    if (body.MarkCollisionFilterDisabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::SetCollisionDisabledState(PhysicsBody& body, bool disableAllCollisions)
{
    if (!body.SetDisableAllCollisions(disableAllCollisions))
    {
        // No changes
        return;
    }

    if (disableAllCollisions)
    {
        body.MarkCollisionDisableFlagEnabled();
    }
    else
    {
        body.MarkCollisionDisableFlagDisabled();
    }

    UpdateBodyUserPointer(body);
}

void PhysicalWorld::AddCollisionFilter(PhysicsBody& body, CollisionFilterCallback callback)
{
    body.SetCollisionFilter(callback);

    if (body.MarkCollisionFilterCallbackUsed())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::DisableCollisionFilter(PhysicsBody& body)
{
    if (body.MarkCollisionFilterCallbackDisabled())
    {
        UpdateBodyUserPointer(body);
    }

    body.RemoveCollisionFilter();
}

// ------------------------------------ //
Ref<TrackedConstraint> PhysicalWorld::CreateAxisLockConstraint(PhysicsBody& body, JPH::Vec3 axis, bool lockRotation)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), body.GetId());
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Locking body for adding a constraint failed");
        return nullptr;
    }

    JPH::SixDOFConstraintSettings constraintSettings;

    // This was in an example at https://github.com/jrouwe/JoltPhysics/issues/359 but would require some extra
    // space calculation so this is left out
    // constraintSettings.mSpace = JPH::EConstraintSpace::LocalToBodyCOM;

    if (axis.GetX() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationX);

    if (axis.GetY() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationY);

    if (axis.GetZ() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationZ);

    if (lockRotation)
    {
        if (axis.GetX() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationY);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }

        if (axis.GetY() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationX);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }

        if (axis.GetZ() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationY);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }
    }

    // Needed for precision on the axis lock to actually stay relatively close to the target value
    constraintSettings.mPosition1 = constraintSettings.mPosition2 = lock.GetBody().GetCenterOfMassPosition();

    auto constraintPtr = (JPH::SixDOFConstraint*)constraintSettings.Create(JPH::Body::sFixedToWorld, lock.GetBody());

#ifdef USE_OBJECT_POOLS
    auto trackedConstraint =
        ConstructFromGlobalPool<TrackedConstraint>(JPH::Ref<JPH::Constraint>(constraintPtr), Ref<PhysicsBody>(&body));
#else
    auto trackedConstraint = Ref<TrackedConstraint>(
        new TrackedConstraint(JPH::Ref<JPH::Constraint>(constraintPtr), Ref<PhysicsBody>(&body)));
#endif

    if (body.IsInWorld() && !body.IsDetached())
    {
        // Immediately register the constraint if the body is in the world currently

        // TODO: multithreaded adding?
        physicsSystem->AddConstraint(trackedConstraint->GetConstraint().GetPtr());
        trackedConstraint->OnRegisteredToWorld(*this);
    }

    return trackedConstraint;
}

void PhysicalWorld::DestroyConstraint(TrackedConstraint& constraint)
{
    // TODO: allow multithreading
    physicsSystem->RemoveConstraint(constraint.GetConstraint().GetPtr());
    constraint.OnDestroyByWorld(*this);
}

// ------------------------------------ //
std::optional<std::tuple<float, JPH::Vec3, JPH::BodyID>> PhysicalWorld::CastRay(JPH::RVec3 start, JPH::Vec3 endOffset)
{
    // The Jolt samples app has some really nice alternative cast modes that could be added in the future

    JPH::RRayCast ray{start, endOffset};

    // Cast ray
    JPH::RayCastResult hit;

    // TODO: could ignore certain groups
    bool hitSomething = physicsSystem->GetNarrowPhaseQuery().CastRay(ray, hit);

    if (!hitSomething)
        return {};

    const auto resultPosition = ray.GetPointOnRay(hit.mFraction);
    const auto resultFraction = hit.mFraction;
    const auto resultID = hit.mBodyID;

    // Could do something with the hit sub-shape
    // hit.mSubShapeID2

    // Or material
    // JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), hit.mBodyID);
    // if (lock.Succeeded())
    // {
    //     const JPH::Body& resultBody = lock.GetBody();
    //     const JPH::PhysicsMaterial* material = resultBody.GetShape()->GetMaterial(hit.mSubShapeID2);
    // }
    // else
    // {
    //     LOG_ERROR("Failed to get body read lock for ray cast");
    // }

    return std::tuple<float, JPH::Vec3, JPH::BodyID>(resultFraction, resultPosition, resultID);
}

int PhysicalWorld::CastRayGetAllUserData(
    JPH::RVec3 start, JPH::Vec3 endOffset, PhysicsRayWithUserData* dataReceiver, int maxHits)
{
    if (maxHits < 1 || dataReceiver == nullptr)
    {
        LOG_ERROR("Physics ray collection given no storage space for results");
        return 0;
    }

    JPH::RRayCast ray{start, endOffset};

    ArrayRayCollector rayCollector{dataReceiver, maxHits, physicsSystem->GetBodyLockInterface()};

    JPH::RayCastSettings settings;

    // TODO: should the option to treat convex as solid be set to false?
    // settings.mTreatConvexAsSolid = false;

    // TODO: could ignore certain groups
    physicsSystem->GetNarrowPhaseQuery().CastRay(ray, settings, rayCollector);

    return rayCollector.GetHitCount();
}

// ------------------------------------ //
void PhysicalWorld::SetGravity(JPH::Vec3 newGravity)
{
    pimpl->gravity = newGravity;

    physicsSystem->SetGravity(pimpl->gravity);
}

void PhysicalWorld::RemoveGravity()
{
    SetGravity(JPH::Vec3(0, 0, 0));
}

// ------------------------------------ //
bool PhysicalWorld::DumpSystemState(std::string_view path)
{
    // Dump a Jolt snapshot to the path
    JPH::Ref<JPH::PhysicsScene> scene = new JPH::PhysicsScene();
    scene->FromPhysicsSystem(physicsSystem.get());

    std::ofstream stream(path.data(), std::ofstream::out | std::ofstream::trunc | std::ofstream::binary);
    JPH::StreamOutWrapper wrapper(stream);

    if (stream.is_open()) [[likely]]
    {
        scene->SaveBinaryState(wrapper, true, true);
    }
    else
    {
        LOG_ERROR(std::string("Can't dump physics state to non-writable file at: ") + path.data());
        return false;
    }

    return true;
}

// ------------------------------------ //
void PhysicalWorld::StepAllPhysicsStepsInBackground()
{
    const auto singlePhysicsFrame = 1 / physicsFrameRate;

    while (elapsedSinceUpdate > singlePhysicsFrame)
    {
        elapsedSinceUpdate -= singlePhysicsFrame;
        backgroundSimulatedTime += singlePhysicsFrame;
        StepPhysics(singlePhysicsFrame);
    }

    runningBackgroundSimulation = false;
}

// ------------------------------------ //
void PhysicalWorld::StepPhysics(float time)
{
    if (changesToBodies) [[unlikely]]
    {
        if (simulationsToNextOptimization <= 0)
        {
            // Queue an optimization
            simulationsToNextOptimization = simulationsBetweenBroadPhaseOptimization;
        }

        changesToBodies = false;
    }

    // Optimize broadphase (but at most quite rarely)
    if (simulationsToNextOptimization > 0) [[unlikely]]
    {
        if (--simulationsToNextOptimization <= 0)
        {
            simulationsToNextOptimization = 0;

            // Time to optimize
            physicsSystem->OptimizeBroadPhase();
        }
    }

    // TODO: physics processing time tracking with a high resolution timer (should get the average time over the last
    // second)
    const auto start = TimingClock::now();

    // Per physics step forces are applied in PerformPhysicsStepOperations triggered by the step listener

    // TODO: ensure that our custom task system is not (much) slower than the Jolt inbuilt one
    auto& jobExecutor = TaskSystem::Get();

    const auto result = physicsSystem->Update(time, collisionStepsPerUpdate, tempAllocator.get(), &jobExecutor);

    nextStepIsFresh = false;

    const auto elapsed = std::chrono::duration_cast<SecondDuration>(TimingClock::now() - start).count();

    switch (result)
    {
        [[likely]] case JPH::EPhysicsUpdateError::None:
            break;
        case JPH::EPhysicsUpdateError::ManifoldCacheFull:
            LOG_ERROR("Physics update error: manifold cache full");
            break;
        case JPH::EPhysicsUpdateError::BodyPairCacheFull:
            LOG_ERROR("Physics update error: body pair cache full");
            break;
        case JPH::EPhysicsUpdateError::ContactConstraintsFull:
            LOG_ERROR("Physics update error: contact constraints full");
            break;
        default:
            LOG_ERROR("Physics update error: unknown");
    }

    latestPhysicsTime = elapsed;

    averagePhysicsTime = pimpl->AddAndCalculateAverageTime(elapsed);
}

void PhysicalWorld::ReportBodyWithActiveCollisions(PhysicsBody& body)
{
    pimpl->PushBodyWithActiveCollisions(body);
}

void PhysicalWorld::PerformPhysicsStepOperations(float delta)
{
    // Only fresh steps increment the counter to that collision data can be preserved
    if (nextStepIsFresh)
        pimpl->IncrementStepCounter();

    // Collision setup
    contactListener->ReportStepNumber(pimpl->stepCounter, !nextStepIsFresh);

    if (nextStepIsFresh)
        pimpl->HandleExpiringBodyCollisions();

    // Apply per-step physics body state

    // This is locked just for safety, but it should be the case that no physics modify operations should be allowed
    // once physics runs have started
    pimpl->bodiesStepControlLock.Lock();

    // TODO: multithreading if there's a ton of bodies using this
    for (const auto& bodyPtr : pimpl->bodiesWithPerStepControl)
    {
        auto& body = *bodyPtr;

        if (body.GetBodyControlState() != nullptr) [[likely]]
            ApplyBodyControl(body, delta);
    }

    pimpl->bodiesStepControlLock.Unlock();
}

Ref<PhysicsBody> PhysicalWorld::CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
    JPH::RVec3Arg position, JPH::Quat rotation /*= JPH::Quat::sIdentity()*/,
    JPH::EAllowedDOFs allowedDegreesOfFreedom /*= JPH::EAllowedDOFs::All*/, bool isSensor /*= false*/)
{
#ifndef NDEBUG
    // Sanity check some layer stuff
    if (motionType == JPH::EMotionType::Dynamic && layer == Layers::NON_MOVING)
    {
        LOG_ERROR("Incorrect motion type for layer specified");
        return nullptr;
    }
#endif

    auto creationSettings = JPH::BodyCreationSettings(&shape, position, rotation, motionType, layer);

    if (isSensor)
    {
        creationSettings.mIsSensor = true;
    }

    creationSettings.mAllowedDOFs = allowedDegreesOfFreedom;

    const auto body = physicsSystem->GetBodyInterface().CreateBody(creationSettings);

    if (body == nullptr) [[unlikely]]
    {
        LOG_ERROR("Ran out of physics bodies");
        return nullptr;
    }

    changesToBodies = true;

#ifdef USE_OBJECT_POOLS
    return ConstructFromGlobalPool<PhysicsBody>(body, body->GetID());
#else
    return {new PhysicsBody(body, body->GetID())};
#endif
}

Ref<PhysicsBody> PhysicalWorld::OnBodyCreated(Ref<PhysicsBody>&& body, bool addToWorld)
{
    if (body == nullptr) [[unlikely]]
        return nullptr;

    // Safety check for pointer data alignment
    if (reinterpret_cast<decltype(STUFFED_POINTER_DATA_MASK)>(body.get()) & STUFFED_POINTER_DATA_MASK) [[unlikely]]
    {
        LOG_ERROR("Allocated PhysicsBody doesn't follow alignment requirements! It uses low bits in the pointer.");
        std::abort();
    }

    if (addToWorld)
    {
        physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::Activate);
        OnPostBodyAdded(*body);
    }

    return std::move(body);
}

void PhysicalWorld::OnPostBodyAdded(PhysicsBody& body)
{
    body.MarkUsedInWorld(this);

    // Add an extra reference to the body to keep it from being deleted while in this world
    // TODO: does detached body also need to keep an extra reference?
    body.AddRef();
    ++bodyCount;
}

void PhysicalWorld::OnBodyPreLeaveWorld(PhysicsBody& body)
{
    // TODO: allow detaching bodies to keep the constraint data intact for re-creating constraints when adding them
    // back
    // Destroy constraints
    while (!body.GetConstraints().empty())
    {
        DestroyConstraint(*body.GetConstraints().back());
    }

    if (body.GetBodyControlState() != nullptr)
        DisableBodyControl(body);
}

void PhysicalWorld::OnPostBodyLeaveWorld(PhysicsBody& body)
{
    pimpl->NotifyBodyRemove(&body);

    // Remove the extra body reference that we added for the physics system keeping a pointer to the body
    body.Release();
    --bodyCount;
}

void PhysicalWorld::UpdateBodyUserPointer(const PhysicsBody& body)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), body.GetId());
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Can't lock body for updating user pointer bits, the enabled / disabled feature won't apply on it");
    }
    else [[likely]]
    {
        JPH::Body& joltBody = lock.GetBody();
        joltBody.SetUserData(body.CalculateUserPointer());
    }
}

// ------------------------------------ //
void PhysicalWorld::ApplyBodyControl(PhysicsBody& bodyWrapper, float delta)
{
    // Normalize delta to 60Hz update rate to make gameplay logic not depend on the physics framerate
    float normalizedDelta = delta / (1 / 60.0f);

    BodyControlState* controlState = bodyWrapper.GetBodyControlState();
    const auto bodyId = bodyWrapper.GetId();

    // This method is called by the step listener meaning that all bodies are already locked so this needs to be used
    // like this. The call to activate probably needs to be protected with a lock if body control is applied in the
    // future by multiple threads.
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterfaceNoLock(), bodyId);
    if (!lock.Succeeded()) [[unlikely]]
    {
        LOG_ERROR("Couldn't lock body for applying body control");
        return;
    }

    JPH::Body& body = lock.GetBody();

    // Ensure this doesn't cause a crash if there's a bug elsewhere in body handling
    if (!body.IsInBroadPhase())
    {
        LOG_ERROR("Body not in broadphase used in body control");
        return;
    }

    if (controlState->movement.LengthSq() > 0.000001f)
    {
        body.AddImpulse(controlState->movement * normalizedDelta);

        // Activate inactive bodies when controlled to ensure they cannot accumulate a lot of impulse and eventually
        // shoot off at high velocity when touched
        if (!body.IsActive())
        {
            physicsSystem->GetBodyInterfaceNoLock().ActivateBody(bodyId);
        }
    }

    // A really simple rotation matching based on JPH::Body::MoveKinematic approach. Now this doesn't seem to need
    // to have any rotation value being close to target threshold or overshoot detection.
    const auto& currentRotation = body.GetRotation();

    // Can use conjugated here as the rotation is always a unit rotation
    const auto difference = controlState->targetRotation * currentRotation.Conjugated();

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"
    JPH::Vec3 axis;
#pragma clang diagnostic pop

    float angle;
    difference.GetAxisAngle(axis, angle);
    body.SetAngularVelocityClamped(axis * (angle / controlState->rotationRate * normalizedDelta));

    // TODO: should enough applied angular velocity also wake up the body?
}

#pragma clang diagnostic push
#pragma ide diagnostic ignored "readability-make-member-function-const"
#pragma ide diagnostic ignored "readability-convert-member-functions-to-static"

void PhysicalWorld::DrawPhysics(float delta)
{
    if (debugDrawLevel < 1) [[likely]]
    {
#ifdef JPH_DEBUG_RENDERER
        contactListener->SetDebugDraw(nullptr);
#endif

        return;
    }

#ifdef JPH_DEBUG_RENDERER
    auto& drawer = DebugDrawForwarder::GetInstance();

    if (!drawer.HasAReceiver())
        return;

    drawer.SetCameraPositionForLOD(pimpl->debugDrawCameraLocation);

    if (!drawer.TimeToRenderDebug(delta))
    {
        // Rate limiting the drawing
        // New contacts will be drawn on the next non-rate limited frame
        contactListener->SetDrawOnlyNewContacts(true);
        return;
    }

    if (debugDrawLevel > 2)
    {
        contactListener->SetDebugDraw(&drawer);
        contactListener->SetDrawOnlyNewContacts(false);
    }
    else
    {
        contactListener->SetDebugDraw(nullptr);
    }

    pimpl->bodyDrawSettings.mDrawBoundingBox = debugDrawLevel > 1;
    pimpl->bodyDrawSettings.mDrawVelocity = debugDrawLevel > 1;

    physicsSystem->DrawBodies(pimpl->bodyDrawSettings, &drawer);

    if (debugDrawLevel > 3)
    {
        contactListener->DrawActiveContacts(drawer);
    }

    if (debugDrawLevel > 4)
        physicsSystem->DrawConstraints(&drawer);

    if (debugDrawLevel > 5)
        physicsSystem->DrawConstraintLimits(&drawer);

    if (debugDrawLevel > 6)
        physicsSystem->DrawConstraintReferenceFrame(&drawer);

    drawer.FlushOutput();
#endif
}

void PhysicalWorld::SetDebugCameraLocation(JPH::Vec3Arg position) noexcept
{
#ifdef JPH_DEBUG_RENDERER
    pimpl->debugDrawCameraLocation = position;
#else
    UNUSED(position);
#endif
}

#pragma clang diagnostic pop

} // namespace Thrive::Physics
