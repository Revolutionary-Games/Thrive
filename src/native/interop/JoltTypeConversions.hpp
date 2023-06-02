#pragma once

#include "Jolt/Math/DVec3.h"
#include "Jolt/Math/Quat.h"

#include "Include.h"

#include "CStructures.h"

/// \file Implements type conversion between the C API types and Jolt types

namespace Thrive
{

FORCE_INLINE JPH::DVec3 DVec3FromCAPI(JVec3 vec)
{
    return {vec.X, vec.Y, vec.Z};
}

FORCE_INLINE JVec3 DVec3ToCAPI(JPH::DVec3 vec)
{
    return JVec3{vec.GetX(), vec.GetY(), vec.GetZ()};
}

FORCE_INLINE JPH::Vec3 Vec3FromCAPI(JVecF3 vec)
{
    return {vec.X, vec.Y, vec.Z};
}

FORCE_INLINE JVecF3 Vec3ToCAPI(JPH::Vec3 vec)
{
    return JVecF3{vec.GetX(), vec.GetY(), vec.GetZ()};
}

FORCE_INLINE JPH::Quat QuatFromCAPI(JQuat quat)
{
    return {quat.X, quat.Y, quat.Z, quat.W};
}

FORCE_INLINE JQuat QuatToCAPI(JPH::Quat quat)
{
    return JQuat{quat.GetX(), quat.GetY(), quat.GetZ(), quat.GetW()};
}

} // namespace Thrive
