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

    static const inline JQuat QuatIdentity = JQuat{0, 0, 0, 1};
}

#pragma clang diagnostic pop
