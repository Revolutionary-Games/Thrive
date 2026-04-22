#pragma once

#include "Include.h"

#pragma clang diagnostic push
#pragma ide diagnostic ignored "modernize-use-using"

extern "C"
{
    typedef struct PhysicalWorld PhysicalWorld;
    typedef struct PhysicsBody PhysicsBody;
    typedef struct PhysicsShape PhysicsShape;
    typedef struct ThriveConfig ThriveConfig;
    typedef struct DebugDrawer DebugDrawer;
    typedef struct GodotVariant GodotVariant;

    typedef struct JVec3
    {
        double X, Y, Z;
    } JVec3;

    typedef struct JVecF3
    {
        float X, Y, Z;
    } JVecF3;

    typedef struct JQuat
    {
        float X, Y, Z, W;
    } JQuat;

    typedef struct JColour
    {
        float R, G, B, A;
    } JColour;

#ifdef __cplusplus
    // Layout sanity checks for wire types used across the ABI boundary
    static_assert(sizeof(JVec3) == 24, "JVec3 layout changed (expected 24 bytes)");
    static_assert(sizeof(JVecF3) == 12, "JVecF3 layout changed (expected 12 bytes)");
    static_assert(sizeof(JColour) == 16, "JColour layout changed (expected 16 bytes)");
#endif

    // See the C++ side for the layout of the contained members
    typedef struct PhysicsCollision
    {
        char CollisionData[PHYSICS_COLLISION_DATA_SIZE];
    } PhysicsCollision;

    typedef struct PhysicsRayWithUserData
    {
        char RayData[PHYSICS_RAY_DATA_SIZE];
    } PhysicsRayWithUserData;

    BEGIN_PACKED_STRUCT;

    typedef struct PACKED_STRUCT SubShapeDefinition
    {
        JQuat Rotation;
        JVecF3 Position;
        uint32_t UserData;
        PhysicsShape* Shape;
    } SubShapeDefinition;

    END_PACKED_STRUCT;

    static inline const JQuat QuatIdentity = JQuat{0, 0, 0, 1};

    /// Opaque type for passing through info on Thrive::NativeLibIntercommunication instances on the C# side
    typedef struct NativeLibIntercommunicationOpaque
    {
        void* Pointers;
    } NativeLibIntercommunicationOpaque;
}

#pragma clang diagnostic pop
