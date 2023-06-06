// ------------------------------------ //
#include "CInterop.h"

#include <cstdarg>
#include <cstring>

#include "Jolt/Core/Factory.h"
#include "Jolt/Core/Memory.h"
#include "Jolt/Jolt.h"
#include "Jolt/RegisterTypes.h"

#include "physics/PhysicalWorld.hpp"
#include "physics/PhysicsBody.hpp"
#include "physics/ShapeWrapper.hpp"
#include "physics/SimpleShapes.hpp"

#include "JoltTypeConversions.hpp"

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-reinterpret-cast"

// ------------------------------------ //
void PhysicsTrace(const char* fmt, ...);

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
    PhysicalWorld* physicalWorld, PhysicsShape* shape, JVec3 position, JQuat rotation /*= QuatIdentity*/)
{
    const auto body = reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
                          ->CreateMovingBody(reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->GetShape(),
                              Thrive::DVec3FromCAPI(position), Thrive::QuatFromCAPI(rotation));

    if (body)
        body->AddRef();

    return reinterpret_cast<PhysicsBody*>(body.get());
}

PhysicsBody* PhysicalWorldCreateStaticBody(
    PhysicalWorld* physicalWorld, PhysicsShape* shape, JVec3 position, JQuat rotation)
{
    const auto body = reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
                          ->CreateStaticBody(reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->GetShape(),
                              Thrive::DVec3FromCAPI(position), Thrive::QuatFromCAPI(rotation));

    if (body)
        body->AddRef();

    return reinterpret_cast<PhysicsBody*>(body.get());
}

void DestroyPhysicalWorldBody(PhysicalWorld* physicalWorld, PhysicsBody* body)
{
    if (physicalWorld == nullptr || body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)
        ->DestroyBody(reinterpret_cast<Thrive::Physics::PhysicsBody*>(body));
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

#pragma clang diagnostic pop

float PhysicalWorldGetPhysicsLatestTime(PhysicalWorld* physicalWorld)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->GetLatestPhysicsTime();
}

float PhysicalWorldGetPhysicsAverageTime(PhysicalWorld* physicalWorld)
{
    return reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld)->GetAveragePhysicsTime();
}

// ------------------------------------ //
void ReleasePhysicsBodyReference(PhysicsBody* body)
{
    if (body == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::PhysicsBody*>(body)->Release();
}

// ------------------------------------ //
PhysicsShape* CreateBoxShape(float halfSideLength)
{
    auto result = new Thrive::Physics::ShapeWrapper(Thrive::Physics::SimpleShapes::CreateBox(halfSideLength));
    result->AddRef();

    return reinterpret_cast<PhysicsShape*>(result);
}

PhysicsShape* CreateBoxShapeWithDimensions(JVecF3 halfDimensions)
{
    auto result = new Thrive::Physics::ShapeWrapper(
        Thrive::Physics::SimpleShapes::CreateBox(Thrive::Vec3FromCAPI(halfDimensions)));
    result->AddRef();

    return reinterpret_cast<PhysicsShape*>(result);
}

PhysicsShape* CreateSphereShape(float radius)
{
    auto result = new Thrive::Physics::ShapeWrapper(Thrive::Physics::SimpleShapes::CreateSphere(radius));
    result->AddRef();

    return reinterpret_cast<PhysicsShape*>(result);
}

void ReleaseShape(PhysicsShape* shape)
{
    if (shape == nullptr)
        return;

    reinterpret_cast<Thrive::Physics::ShapeWrapper*>(shape)->Release();
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
