// ------------------------------------ //
#include "CInterop.h"

#include <cstdarg>
#include <cstring>

#include "Jolt/Core/Factory.h"
#include "Jolt/Core/Memory.h"
#include "Jolt/Jolt.h"
#include "Jolt/RegisterTypes.h"

#include "physics/DebugDrawForwarder.hpp"
#include "physics/PhysicalWorld.hpp"
#include "physics/PhysicsBody.hpp"
#include "physics/ShapeCreator.hpp"
#include "physics/ShapeWrapper.hpp"
#include "physics/SimpleShapes.hpp"
#include "physics/TrackedConstraint.hpp"

#include "JoltTypeConversions.hpp"

#ifdef USE_OBJECT_POOLS
#include "boost/pool/singleton_pool.hpp"
#endif

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-reinterpret-cast"

// ------------------------------------ //
void PhysicsTrace(const char* fmt, ...);

#ifdef USE_OBJECT_POOLS
using ShapePool = boost::singleton_pool<Thrive::Physics::ShapeWrapper, sizeof(Thrive::Physics::ShapeWrapper)>;
#endif

#ifdef JPH_ENABLE_ASSERTS
bool PhysicsAssert(const char* expression, const char* message, const char* file, uint line);
#endif

int32_t CheckAPIVersion()
{
    return THRIVE_LIBRARY_VERSION;
}

int32_t InitThriveLibrary()
{
    // Register physics things
    JPH::RegisterDefaultAllocator();

    JPH::Trace = PhysicsTrace;
    JPH_IF_ENABLE_ASSERTS(JPH::AssertFailed = PhysicsAssert;)

    JPH::Factory::sInstance = new JPH::Factory();

    JPH::RegisterTypes();

    LOG_DEBUG("Native library init succeeded");
    return 0;
}

void ShutdownThriveLibrary()
{
    // Unregister physics
    JPH::UnregisterTypes();

    delete JPH::Factory::sInstance;
    JPH::Factory::sInstance = nullptr;

    SetLogForwardingCallback(nullptr);
}

// ------------------------------------ //
void SetLogLevel(int8_t level)
{
    Thrive::Logger::Get().SetLogLevel(static_cast<Thrive::LogLevel>(level));
}

void SetLogForwardingCallback(OnLogMessage callback)
{
    if (callback == nullptr)
    {
        Thrive::Logger::Get().SetLogTargetOverride(nullptr);
    }
    else
    {
        Thrive::Logger::Get().SetLogTargetOverride([callback](std::string_view message, Thrive::LogLevel level)
            { callback(message.data(), static_cast<int32_t>(message.length()), static_cast<int8_t>(level)); });

        LOG_DEBUG("Native log message forwarding setup");
    }
}

// ------------------------------------ //
PhysicalWorld* CreatePhysicalWorld()
{
    return reinterpret_cast<PhysicalWorld*>(new Thrive::Physics::PhysicalWorld());
}

void DestroyPhysicalWorld(PhysicalWorld* physicalWorld)
{
    if (physicalWorld == nullptr)
        return;

    delete reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld);
}

bool ProcessPhysicalWorld(PhysicalWorld* physicalWorld, float delta)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->Process(delta);
}

PhysicsBody* PhysicalWorldCreateMovingBody(
    PhysicalWorld* physicalWorld, PhysicsShape* shape, JVec3 position, JQuat rotation, bool addToWorld)
{
    const auto body = reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
                          ->CreateMovingBody(reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->GetShape(),
                              Thrive::DVec3FromCAPI(position), Thrive::QuatFromCAPI(rotation), addToWorld);

    if (body)
        body->AddRef();

    return reinterpret_cast<PhysicsBody*>(body.get());
}

PhysicsBody* PhysicalWorldCreateMovingBodyWithAxisLock(PhysicalWorld* physicalWorld, PhysicsShape* shape,
    JVec3 position, JQuat rotation, JVecF3 lockedAxes, bool lockRotation, bool addToWorld)
{
    const auto body =
        reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
            ->CreateMovingBodyWithAxisLock(reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->GetShape(),
                Thrive::DVec3FromCAPI(position), Thrive::QuatFromCAPI(rotation), Thrive::Vec3FromCAPI(lockedAxes),
                lockRotation, addToWorld);

    if (body)
        body->AddRef();

    return reinterpret_cast<PhysicsBody*>(body.get());
}

PhysicsBody* PhysicalWorldCreateStaticBody(
    PhysicalWorld* physicalWorld, PhysicsShape* shape, JVec3 position, JQuat rotation, bool addToWorld)
{
    const auto body = reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
                          ->CreateStaticBody(reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->GetShape(),
                              Thrive::DVec3FromCAPI(position), Thrive::QuatFromCAPI(rotation), addToWorld);

    if (body)
        body->AddRef();

    return reinterpret_cast<PhysicsBody*>(body.get());
}

void PhysicalWorldAddBody(PhysicalWorld* physicalWorld, PhysicsBody* body, bool activate)
{
    if (body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->AddBody(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body), activate);
}

void PhysicalWorldDetachBody(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    if (body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DetachBody(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

void DestroyPhysicalWorldBody(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    if (physicalWorld == nullptr || body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DestroyBody(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

void SetPhysicsBodyLinearDamping(PhysicalWorld* physicalWorld, PhysicsBody* body, float damping)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetDamping(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), damping, nullptr);
}

void SetPhysicsBodyLinearAndAngularDamping(
    PhysicalWorld* physicalWorld, PhysicsBody* body, float linearDamping, float angularDamping)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetDamping(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), linearDamping, &angularDamping);
}

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

void ReadPhysicsBodyTransform(
    PhysicalWorld* physicalWorld, PhysicsBody* body, JVec3* positionReceiver, JQuat* rotationReceiver)
{
#ifndef NDEBUG
    if (physicalWorld == nullptr || body == nullptr || positionReceiver == nullptr || rotationReceiver == nullptr)
    {
        LOG_ERROR("Physics body read transform call with invalid parameters");
        return;
    }
#endif

    JPH::DVec3 readPosition;
    JPH::Quat readQuat;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->ReadBodyTransform(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), readPosition, readQuat);

    *positionReceiver = Thrive::DVec3ToCAPI(readPosition);
    *rotationReceiver = Thrive::QuatToCAPI(readQuat);
}

void ReadPhysicsBodyVelocity(
    PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3* velocityReceiver, JVecF3* angularVelocityReceiver)
{
#ifndef NDEBUG
    if (physicalWorld == nullptr || body == nullptr || velocityReceiver == nullptr ||
        angularVelocityReceiver == nullptr)
    {
        LOG_ERROR("Physics body read velocity call with invalid parameters");
        return;
    }
#endif

    JPH::Vec3 readVelocity;
    JPH::Vec3 readAngular;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->ReadBodyVelocity(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), readVelocity, readAngular);

    *velocityReceiver = Thrive::Vec3ToCAPI(readVelocity);
    *angularVelocityReceiver = Thrive::Vec3ToCAPI(readAngular);
}

#pragma clang diagnostic pop

void GiveImpulse(PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 impulse)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->GiveImpulse(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), Thrive::Vec3FromCAPI(impulse));
}

void GiveAngularImpulse(PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 angularImpulse)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->GiveAngularImpulse(
            reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), Thrive::Vec3FromCAPI(angularImpulse));
}

void SetBodyControl(
    PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 movementImpulse, JQuat targetRotation, float rotationRate)
{
    if (physicalWorld == nullptr || body == nullptr)
    {
        LOG_ERROR("Invalid call to physics body applying control");
        return;
    }

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetBodyControl(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body), Thrive::Vec3FromCAPI(movementImpulse),
            Thrive::QuatFromCAPI(targetRotation), rotationRate);
}

void DisableBodyControl(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    if (physicalWorld == nullptr || body == nullptr)
    {
        return;
    }

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DisableBodyControl(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

void SetBodyPosition(PhysicalWorld* physicalWorld, PhysicsBody* body, JVec3 position, bool activate)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetPosition(
            reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), Thrive::DVec3FromCAPI(position), activate);
}

void SetBodyVelocity(PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 velocity)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetVelocity(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), Thrive::Vec3FromCAPI(velocity));
}

void SetBodyAngularVelocity(PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 angularVelocity)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetAngularVelocity(
            reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(), Thrive::Vec3FromCAPI(angularVelocity));
}

void SetBodyVelocityAndAngularVelocity(
    PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 velocity, JVecF3 angularVelocity)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetVelocityAndAngularVelocity(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId(),
            Thrive::Vec3FromCAPI(velocity), Thrive::Vec3FromCAPI(angularVelocity));
}

bool FixBodyYCoordinateToZero(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->FixBodyYCoordinateToZero(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->GetId());
}

void PhysicsBodyAddAxisLock(PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 axis, bool lockRotation)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->CreateAxisLockConstraint(
            *reinterpret_cast<Thrive::Physics::PhysicsBody*>(body), Thrive::Vec3FromCAPI(axis), lockRotation);
}

// ------------------------------------ //
void PhysicsBodySetCollisionEnabledState(PhysicalWorld* physicalWorld, PhysicsBody* body, bool collisionsEnabled)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetCollisionDisabledState(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body), !collisionsEnabled);
}

// ------------------------------------ //
void PhysicsBodyAddCollisionIgnore(PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* addIgnore)
{
    bool handleDuplicates = true;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->AddCollisionIgnore(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body),
            *reinterpret_cast<Thrive::Physics::PhysicsBody*>(addIgnore), handleDuplicates);
}

void PhysicsBodyRemoveCollisionIgnore(PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* removeIgnore)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->RemoveCollisionIgnore(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body),
            *reinterpret_cast<Thrive::Physics::PhysicsBody*>(removeIgnore));
}

void PhysicsBodyClearCollisionIgnores(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->ClearCollisionIgnores(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

void PhysicsBodySetCollisionIgnores(
    PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* ignoredBodies[], int32_t count)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetCollisionIgnores(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body),
            reinterpret_cast<Thrive::Physics::PhysicsBody*&>(ignoredBodies), count);
}

void PhysicsBodyClearAndSetSingleIgnore(PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* onlyIgnoredBody)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetSingleCollisionIgnore(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body),
            *reinterpret_cast<Thrive::Physics::PhysicsBody*>(onlyIgnoredBody));
}

// ------------------------------------ //
int32_t* PhysicsBodyEnableCollisionRecording(
    PhysicalWorld* physicalWorld, PhysicsBody* body, char* collisionRecordingTarget, int32_t maxRecordedCollisions)
{
    return const_cast<int32_t*>(
        reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
            ->EnableCollisionRecording(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body),
                reinterpret_cast<Thrive::Physics::CollisionRecordListType>(collisionRecordingTarget),
                maxRecordedCollisions));
}

void PhysicsBodyDisableCollisionRecording(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DisableCollisionRecording(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

void PhysicsBodyAddCollisionFilter(
    PhysicalWorld* physicalWorld, PhysicsBody* body, OnFilterPhysicsCollision callback, bool calculateCollisionResponse)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->AddCollisionFilter(
            *reinterpret_cast<Thrive::Physics::PhysicsBody*>(body), callback, calculateCollisionResponse);
}

void PhysicsBodyDisableCollisionFilter(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DisableCollisionFilter(*reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
}

// ------------------------------------ //
void PhysicalWorldSetGravity(PhysicalWorld* physicalWorld, JVecF3 gravity)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->SetGravity(Thrive::Vec3FromCAPI(gravity));
}

void PhysicalWorldRemoveGravity(PhysicalWorld* physicalWorld)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->RemoveGravity();
}

float PhysicalWorldGetPhysicsLatestTime(PhysicalWorld* physicalWorld)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->GetLatestPhysicsTime();
}

float PhysicalWorldGetPhysicsAverageTime(PhysicalWorld* physicalWorld)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->GetAveragePhysicsTime();
}

bool PhysicalWorldDumpPhysicsState(PhysicalWorld* physicalWorld, const char* path)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->DumpSystemState(path);
}

void PhysicalWorldSetDebugDrawLevel(PhysicalWorld* physicalWorld, int32_t level)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->SetDebugLevel(level);
}

void PhysicalWorldSetDebugDrawCameraLocation(PhysicalWorld* physicalWorld, JVecF3 position)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->SetDebugCameraLocation(Thrive::Vec3FromCAPI(position));
}

// ------------------------------------ //
void ReleasePhysicsBodyReference(PhysicsBody* body)
{
    if (body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->Release();
}

void PhysicsBodySetUserData(PhysicsBody* body, const char* data, int32_t dataLength)
{
    if (!reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->SetUserData(data, dataLength))
    {
        LOG_ERROR("PhysicsBodySetUserData: called with wrong data length, cannot store the data");
    }
}

// ------------------------------------ //
template<class... ArgsT>
inline Thrive::Physics::ShapeWrapper* CreateShapeWrapper(ArgsT&&... args)
{
    Thrive::Physics::ShapeWrapper* result;

#ifdef USE_OBJECT_POOLS
    result = Thrive::ConstructFromGlobalPoolRaw<Thrive::Physics::ShapeWrapper>(std::forward<ArgsT>(args)...);
#else
    result = new Thrive::Physics::ShapeWrapper(std::forward<ArgsT>(args)...);
#endif

    if (!result)
        LOG_ERROR("Failed to allocate ShapeWrapper");

    result->AddRef();
    return result;
}

PhysicsShape* CreateBoxShape(float halfSideLength, float density)
{
    return reinterpret_cast<PhysicsShape*>(
        CreateShapeWrapper(Thrive::Physics::SimpleShapes::CreateBox(halfSideLength, density)));
}

PhysicsShape* CreateBoxShapeWithDimensions(JVecF3 halfDimensions, float density)
{
    return reinterpret_cast<PhysicsShape*>(
        CreateShapeWrapper(Thrive::Physics::SimpleShapes::CreateBox(Thrive::Vec3FromCAPI(halfDimensions), density)));
}

PhysicsShape* CreateSphereShape(float radius, float density)
{
    return reinterpret_cast<PhysicsShape*>(
        CreateShapeWrapper(Thrive::Physics::SimpleShapes::CreateSphere(radius, density)));
}

PhysicsShape* CreateMicrobeShapeConvex(JVecF3* points, uint32_t pointCount, float density, float scale)
{
    // We don't want to do any extra data copies here (as the C# marshalling already copies stuff) so this API takes
    // in the JVecF3 pointer

    return reinterpret_cast<PhysicsShape*>(CreateShapeWrapper(
        Thrive::Physics::ShapeCreator::CreateMicrobeShapeConvex(points, pointCount, density, scale)));
}

PhysicsShape* CreateMicrobeShapeSpheres(JVecF3* points, uint32_t pointCount, float density, float scale)
{
    // We don't want to do any extra data copies here (as the C# marshalling already copies stuff) so this API takes
    // in the JVecF3 pointer

    return reinterpret_cast<PhysicsShape*>(CreateShapeWrapper(
        Thrive::Physics::ShapeCreator::CreateMicrobeShapeSpheres(points, pointCount, density, scale)));
}

void ReleaseShape(PhysicsShape* shape)
{
    if (shape == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->Release();
}

// ------------------------------------ //
bool SetDebugDrawerCallbacks(OnLineDraw lineDraw, OnTriangleDraw triangleDraw)
{
#ifdef JPH_DEBUG_RENDERER
    if (!lineDraw || !triangleDraw)
    {
        DisableDebugDrawerCallbacks();
        return false;
    }

    auto& instance = Thrive::Physics::DebugDrawForwarder::GetInstance();

    instance.SetOutputLineReceiver([lineDraw](JPH::RVec3Arg from, JPH::RVec3Arg to, JPH::Float4 colour)
        { lineDraw(Thrive::DVec3ToCAPI(from), Thrive::DVec3ToCAPI(to), Thrive::ColorToCAPI(colour)); });

    instance.SetOutputTriangleReceiver(
        [triangleDraw](JPH::RVec3Arg v1, JPH::RVec3Arg v2, JPH::RVec3Arg v3, JPH::Float4 colour)
        {
            triangleDraw(
                Thrive::DVec3ToCAPI(v1), Thrive::DVec3ToCAPI(v2), Thrive::DVec3ToCAPI(v3), Thrive::ColorToCAPI(colour));
        });
    return true;
#else
    UNUSED(lineDraw);
    UNUSED(triangleDraw);
    return false;
#endif
}

void DisableDebugDrawerCallbacks()
{
#ifdef JPH_DEBUG_RENDERER
    Thrive::Physics::DebugDrawForwarder::GetInstance().ClearOutputReceivers();
#endif
}

#pragma clang diagnostic pop

void PhysicsTrace(const char* fmt, ...)
{
    const char prefix[] = "[Jolt:Trace] ";
    constexpr size_t prefixLength = sizeof(prefix);

    // Format the message
    va_list list;
    va_start(list, fmt);
    char buffer[1024];
    vsnprintf(buffer + prefixLength, sizeof(buffer) - prefixLength, fmt, list);
    va_end(list);

    std::memcpy(buffer, prefix, prefixLength);
    buffer[1023] = 0;

    LOG_INFO(std::string_view(buffer, std::strlen(buffer)));
}

#ifdef JPH_ENABLE_ASSERTS
bool PhysicsAssert(const char* expression, const char* message, const char* file, uint line)
{
    LOG_ERROR(std::string("Jolt assert failed in ") + file + ":" + std::to_string(line) + " (" + expression + ") " +
        (message ? message : ""));

    // True seems to indicate that break is wanted (TODO: maybe set to false in production?)
    return true;
}
#endif
