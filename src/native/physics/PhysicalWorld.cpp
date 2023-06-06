// ------------------------------------ //
#include "PhysicalWorld.hpp"

#include <cstring>

// TODO: switch to a custom thread pool
#include "boost/circular_buffer.hpp"
#include "Jolt/Core/JobSystemThreadPool.h"
#include "Jolt/Physics/Body/BodyCreationSettings.h"
#include "Jolt/Physics/Collision/CastResult.h"
#include "Jolt/Physics/Collision/RayCast.h"
#include "Jolt/Physics/PhysicsSettings.h"
#include "Jolt/Physics/PhysicsSystem.h"

// #include "core/TaskSystem.hpp"

#include "core/Time.hpp"

#include "BodyActivationListener.hpp"
#include "ContactListener.hpp"
#include "PhysicsBody.hpp"

JPH_SUPPRESS_WARNINGS

// ------------------------------------ //
namespace Thrive::Physics
{

class PhysicalWorld::Pimpl
{
public:
    Pimpl() : durationBuffer(30)
    {
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

public:
    BroadPhaseLayerInterface broadPhaseLayer;
    ObjectToBroadPhaseLayerFilter objectToBroadPhaseLayer;
    ObjectLayerPairFilter objectToObjectPair;

    JPH::PhysicsSettings physicsSettings;

    boost::circular_buffer<float> durationBuffer;

    JPH::Vec3 gravity = JPH::Vec3(0, -9.81f, 0);
};

PhysicalWorld::PhysicalWorld() : pimpl(std::make_unique<Pimpl>())
{
#ifdef USE_OBJECT_POOLS
    tempAllocator = std::make_unique<JPH::TempAllocatorImpl>(32 * 1024 * 1024);
#else
    tempAllocator = std::make_unique<JPH::TempAllocatorMalloc>();
#endif

    // Create job system
    // TODO: configurable threads (should be about 1-8), or well if we share thread with other systems then maybe up
    // to like any cores not used by the C# background tasks
    int physicsThreads = 2;
    jobSystem =
        std::make_unique<JPH::JobSystemThreadPool>(JPH::cMaxPhysicsJobs, JPH::cMaxPhysicsBarriers, physicsThreads);

    InitPhysicsWorld();
}

PhysicalWorld::~PhysicalWorld()
{
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
}

// ------------------------------------ //
bool PhysicalWorld::Process(float delta)
{
    // TODO: update thread count if changed (we won't need this when we have the custom job system done)

    elapsedSinceUpdate += delta;

    const auto singlePhysicsFrame = 1 / physicsFrameRate;

    bool simulatedPhysics = false;

    // TODO: limit max steps per frame to avoid massive potential for lag spikes
    // TODO: alternatively to this it is possible to use a bigger timestep at once but then collision steps and
    // integration steps should be incremented
    while (elapsedSinceUpdate > singlePhysicsFrame)
    {
        elapsedSinceUpdate -= singlePhysicsFrame;
        StepPhysics(*jobSystem, singlePhysicsFrame);
        simulatedPhysics = true;
    }

    if (!simulatedPhysics)
        return false;

    // TODO: Trigger stuff from the collision detection (but maybe some stuff needs to trigger for each step?)

    return true;
}

// ------------------------------------ //
Ref<PhysicsBody> PhysicalWorld::CreateMovingBody(
    const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position, JPH::Quat rotation /* = JPH::Quat::sIdentity()*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to body create");
        return nullptr;
    }

    // TODO: multithreaded body adding?
    auto body = CreateBody(*shape, JPH::EMotionType::Dynamic, Layers::MOVING, position, rotation);

    if (body == nullptr)
        return nullptr;

    physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::Activate);
    OnPostBodyAdded(body);

    return body;
}

Ref<PhysicsBody> PhysicalWorld::CreateStaticBody(
    const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position, JPH::Quat rotation /* = JPH::Quat::sIdentity()*/)
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

    physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::DontActivate);
    OnPostBodyAdded(body);

    return body;
}

void PhysicalWorld::DestroyBody(const Ref<PhysicsBody>& body)
{
    if (body == nullptr)
        return;

    auto& bodyInterface = physicsSystem->GetBodyInterface();
    bodyInterface.RemoveBody(body->GetId());
    body->MarkRemovedFromWorld();

    // Permanently destroy the body
    // TODO: we'll probably want to allow some way to re-add bodies at some point
    bodyInterface.DestroyBody(body->GetId());

    // Remove the extra body reference that we added for the physics system keeping a pointer to the body
    body->Release();
    --bodyCount;

    changesToBodies = true;
}

// ------------------------------------ //
void PhysicalWorld::ReadBodyTransform(
    JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const
{
    JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (lock.Succeeded())
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
void PhysicalWorld::StepPhysics(JPH::JobSystemThreadPool& jobs, float time)
{
    if (changesToBodies)
    {
        if (simulationsToNextOptimization <= 0)
        {
            // Queue an optimization
            simulationsToNextOptimization = simulationsBetweenBroadPhaseOptimization;
        }

        changesToBodies = false;
    }

    // Optimize broadphase (but at most quite rarely)
    if (simulationsToNextOptimization > 0)
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

    // TODO: apply per physics frame forces

    const auto result =
        physicsSystem->Update(time, collisionStepsPerUpdate, integrationSubSteps, tempAllocator.get(), &jobs);

    const auto elapsed = std::chrono::duration_cast<SecondDuration>(TimingClock::now() - start).count();

    switch (result)
    {
        case JPH::EPhysicsUpdateError::None:
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

Ref<PhysicsBody> PhysicalWorld::CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
    JPH::RVec3Arg position, JPH::Quat rotation /*= JPH::Quat::sIdentity()*/)
{
    // TODO: should we add these kinds of checks
    // // Sanity check some layer stuff
    // if (motionType == JPH::EMotionType::Dynamic && layer == Layers::NON_MOVING){
    //     LOG_ERROR("Incorrect motion type for layer specified");
    //     return nullptr;
    // }

    const auto body = physicsSystem->GetBodyInterface().CreateBody(
        JPH::BodyCreationSettings(&shape, position, rotation, motionType, layer));

    if (body == nullptr)
    {
        LOG_ERROR("Ran out of physics bodies");
        return nullptr;
    }

    changesToBodies = true;

    return {new PhysicsBody(body, body->GetID())};
}

void PhysicalWorld::OnPostBodyAdded(const Ref<PhysicsBody>& body)
{
    body->MarkUsedInWorld();

    // Add an extra reference to the body to keep it from being deleted while in this world
    body->AddRef();
    ++bodyCount;
}

} // namespace Thrive::Physics
