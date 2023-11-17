#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C#

extern "C"
{
    typedef void (*OnLogMessage)(const char* message, int32_t messageLength, int8_t logLevel);
    typedef void (*OnLineDraw)(JVec3 from, JVec3 to, JColour colour);
    typedef void (*OnTriangleDraw)(JVec3 vertex1, JVec3 vertex2, JVec3 vertex3, JColour colour);

    typedef bool (*OnFilterPhysicsCollision)(PhysicsCollision* potentialCollision);

    // ------------------------------------ //
    // General

    /// \returns The API version the native library was compiled with, if different from C# the library should not be
    /// used
    [[maybe_unused]] THRIVE_NATIVE_API int32_t CheckAPIVersion();

    /// \brief Prepares the native library for use, must be called first (right after the version check)
    [[maybe_unused]] THRIVE_NATIVE_API int32_t InitThriveLibrary();

    /// \brief Prepares the native library for shutdown should be called before the process is ended and after all
    /// other calls to the library have been performed
    [[maybe_unused]] THRIVE_NATIVE_API void ShutdownThriveLibrary();

    // ------------------------------------ //
    // Logging

    [[maybe_unused]] THRIVE_NATIVE_API void SetLogLevel(int8_t level);
    [[maybe_unused]] THRIVE_NATIVE_API void SetLogForwardingCallback(OnLogMessage callback);

    // ------------------------------------ //
    // Physics world

    [[maybe_unused]] THRIVE_NATIVE_API PhysicalWorld* CreatePhysicalWorld();
    [[maybe_unused]] THRIVE_NATIVE_API void DestroyPhysicalWorld(PhysicalWorld* physicalWorld);

    [[maybe_unused]] THRIVE_NATIVE_API bool ProcessPhysicalWorld(PhysicalWorld* physicalWorld, float delta);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsBody* PhysicalWorldCreateMovingBody(PhysicalWorld* physicalWorld,
        PhysicsShape* shape, JVec3 position, JQuat rotation = QuatIdentity, bool addToWorld = true);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsBody* PhysicalWorldCreateMovingBodyWithAxisLock(
        PhysicalWorld* physicalWorld, PhysicsShape* shape, JVec3 position, JQuat rotation, JVecF3 lockedAxes,
        bool lockRotation, bool addToWorld = true);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsBody* PhysicalWorldCreateStaticBody(PhysicalWorld* physicalWorld,
        PhysicsShape* shape, JVec3 position, JQuat rotation = QuatIdentity, bool addToWorld = true);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldAddBody(
        PhysicalWorld* physicalWorld, PhysicsBody* body, bool activate);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldDetachBody(PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void DestroyPhysicalWorldBody(PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void SetPhysicsBodyLinearDamping(
        PhysicalWorld* physicalWorld, PhysicsBody* body, float damping);

    [[maybe_unused]] THRIVE_NATIVE_API void SetPhysicsBodyLinearAndAngularDamping(
        PhysicalWorld* physicalWorld, PhysicsBody* body, float linearDamping, float angularDamping);

    [[maybe_unused]] THRIVE_NATIVE_API void ReadPhysicsBodyTransform(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVec3* positionReceiver, JQuat* rotationReceiver);

    [[maybe_unused]] THRIVE_NATIVE_API void ReadPhysicsBodyVelocity(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3* velocityReceiver, JVecF3* angularVelocityReceiver);

    [[maybe_unused]] THRIVE_NATIVE_API void GiveImpulse(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 impulse);

    [[maybe_unused]] THRIVE_NATIVE_API void GiveAngularImpulse(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 angularImpulse);

    [[maybe_unused]] THRIVE_NATIVE_API void ApplyBodyControl(PhysicalWorld* physicalWorld, PhysicsBody* body,
        JVecF3 movementImpulse, JQuat targetRotation, float rotationRate);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyControl(PhysicalWorld* physicalWorld, PhysicsBody* body,
        JVecF3 movementImpulse, JQuat targetRotation, float rotationRate);
    [[maybe_unused]] THRIVE_NATIVE_API void DisableBodyControl(PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyPosition(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVec3 position, bool activate);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyVelocity(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 velocity);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyAngularVelocity(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 angularVelocity);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyVelocityAndAngularVelocity(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 velocity, JVecF3 angularVelocity);

    [[maybe_unused]] THRIVE_NATIVE_API void SetBodyAllowSleep(
        PhysicalWorld* physicalWorld, PhysicsBody* body, bool allowSleep);

    [[maybe_unused]] THRIVE_NATIVE_API bool FixBodyYCoordinateToZero(PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void ChangeBodyShape(
        PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsShape* shape, bool activate);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyAddAxisLock(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVecF3 axis, bool lockRotation);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodySetCollisionEnabledState(
        PhysicalWorld* physicalWorld, PhysicsBody* body, bool collisionsEnabled);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyAddCollisionIgnore(
        PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* addIgnore);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyRemoveCollisionIgnore(
        PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* removeIgnore);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyClearCollisionIgnores(
        PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodySetCollisionIgnores(
        PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* ignoredBodies[], int32_t count);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyClearAndSetSingleIgnore(
        PhysicalWorld* physicalWorld, PhysicsBody* body, PhysicsBody* onlyIgnoredBody);

    /// Sets up collision recording for a body. The returned value is a pointer to read the currently active collisions
    /// that have been written to collisionRecordingTarget
    [[maybe_unused]] THRIVE_NATIVE_API int32_t* PhysicsBodyEnableCollisionRecording(
        PhysicalWorld* physicalWorld, PhysicsBody* body, char* collisionRecordingTarget, int32_t maxRecordedCollisions);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyDisableCollisionRecording(
        PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyAddCollisionFilter(
        PhysicalWorld* physicalWorld, PhysicsBody* body, OnFilterPhysicsCollision callback);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyDisableCollisionFilter(
        PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldSetGravity(PhysicalWorld* physicalWorld, JVecF3 gravity);
    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldRemoveGravity(PhysicalWorld* physicalWorld);

    [[maybe_unused]] THRIVE_NATIVE_API int32_t PhysicalWorldCastRayGetAll(PhysicalWorld* physicalWorld, JVec3 start,
        JVecF3 endOffset, PhysicsRayWithUserData* dataReceiver, int32_t maxHits);

    [[maybe_unused]] THRIVE_NATIVE_API float PhysicalWorldGetPhysicsLatestTime(PhysicalWorld* physicalWorld);
    [[maybe_unused]] THRIVE_NATIVE_API float PhysicalWorldGetPhysicsAverageTime(PhysicalWorld* physicalWorld);

    [[maybe_unused]] THRIVE_NATIVE_API bool PhysicalWorldDumpPhysicsState(
        PhysicalWorld* physicalWorld, const char* path);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldSetDebugDrawLevel(
        PhysicalWorld* physicalWorld, int32_t level = 0);
    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldSetDebugDrawCameraLocation(
        PhysicalWorld* physicalWorld, JVecF3 position);

    // ------------------------------------ //
    // Body functions
    [[maybe_unused]] THRIVE_NATIVE_API void ReleasePhysicsBodyReference(PhysicsBody* body);

    /// Set user data for a physics body, note that currently all data needs to be the same size to fully work,
    /// which is specified by Thrive::PHYSICS_USER_DATA_SIZE
    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodySetUserData(
        PhysicsBody* body, const char* data, int32_t dataLength);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyForceClearRecordingTargets(PhysicsBody* body);

    // ------------------------------------ //
    // Physics shapes
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateBoxShape(float halfSideLength, float density = 1000);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateBoxShapeWithDimensions(
        JVecF3 halfDimensions, float density = 1000);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateSphereShape(float radius, float density = 1000);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateCylinderShape(
        float halfHeight, float radius, float density = 1000);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateMicrobeShapeConvex(
        JVecF3* points, uint32_t pointCount, float density, float scale, float thickness);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateMicrobeShapeSpheres(
        JVecF3* points, uint32_t pointCount, float density, float scale);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateConvexShape(
        JVecF3* points, uint32_t pointCount, float density, float scale = 1, float convexRadius = 0.01f);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateStaticCompoundShape(
        SubShapeDefinition* subShapes, uint32_t shapeCount);

    [[maybe_unused]] THRIVE_NATIVE_API void ReleaseShape(PhysicsShape* shape);

    [[maybe_unused]] THRIVE_NATIVE_API float ShapeGetMass(PhysicsShape* shape);

    [[maybe_unused]] THRIVE_NATIVE_API JVecF3 ShapeCalculateResultingAngularVelocity(
        PhysicsShape* shape, JVecF3 appliedTorque, float deltaTime = 1);

    [[maybe_unused]] THRIVE_NATIVE_API uint32_t ShapeGetSubShapeFromIndex(PhysicsShape* shape, uint32_t subShapeData);

    [[maybe_unused]] THRIVE_NATIVE_API uint32_t ShapeGetSubShapeFromIndexWithRemainder(
        PhysicsShape* shape, uint32_t subShapeData, uint32_t& remainder);

    // ------------------------------------ //
    // Misc
    [[maybe_unused]] THRIVE_NATIVE_API bool SetDebugDrawerCallbacks(OnLineDraw lineDraw, OnTriangleDraw triangleDraw);
    [[maybe_unused]] THRIVE_NATIVE_API void DisableDebugDrawerCallbacks();
}
