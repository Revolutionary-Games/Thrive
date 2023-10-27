#pragma once

#include "Include.h"

#pragma clang diagnostic push
#pragma ide diagnostic ignored "modernize-use-using"

extern "C"
{
    typedef struct PhysicalWorld PhysicalWorld;
    typedef struct PhysicsBody PhysicsBody;
    typedef struct PhysicsShape PhysicsShape;

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

    // See the C++ side for the layout of the contained members
    typedef struct PhysicsCollision{
        char CollisionData[PHYSICS_COLLISION_DATA_SIZE];
    } PhysicsCollision;

    typedef struct PhysicsRayWithUserData{
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

    static const inline JQuat QuatIdentity = JQuat{0, 0, 0, 1};
}

#pragma clang diagnostic pop
