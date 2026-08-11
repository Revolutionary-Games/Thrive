#pragma once

#include <Jolt/Jolt.h>

// These must be included after the main header to work
#include <Jolt/Core/Color.h>
#include <Jolt/Math/Real.h>

#include "interop/CStructures.h"

namespace Thrive
{

constexpr uint64_t INTEROP_MAGIC_VALUE = 42 * 42;

// These don't use optimized vector3 instances as those can different between compilations, so would result in
// incompatibilities and crashes
using OnDebugLines = void (*)(const std::vector<std::tuple<JVec3, JVec3, JColour>>& lineBuffer);
using OnDebugTriangles = void (*)(const std::vector<std::tuple<JVec3, JVec3, JVec3, JColour>>& triangleBuffer);

/// \brief Contains pointers and other info passed through from ThriveNative to ThriveExtension during the runtime setup
/// phase
class NativeLibIntercommunication
{
public:
    NativeLibIntercommunication()
    {
        SanityCheckValue = INTEROP_MAGIC_VALUE;
    }

    // Callback receivers

    // Sanity check value to make sure reader and writer are probably synchronized
    uint64_t SanityCheckValue;

    OnDebugLines DebugLineReceiver = nullptr;
    OnDebugTriangles DebugTriangleReceiver = nullptr;

    // Flags
    bool PhysicsDebugSupported = false;
};

inline JVec3 JoltToJVec3(JPH::RVec3Arg vec)
{
    return JVec3(vec.GetX(), vec.GetY(), vec.GetZ());
}

inline JVec3 JoltToJVec3(JPH::Vec3Arg vec)
{
    return JVec3(vec.GetX(), vec.GetY(), vec.GetZ());
}

inline JColour JoltToJColour(const JPH::ColorArg& color)
{
    constexpr float multiplier = 1 / 255.0f;
    return {static_cast<float>(color.r) * multiplier, static_cast<float>(color.g) * multiplier,
        static_cast<float>(color.b) * multiplier, static_cast<float>(color.a) * multiplier};
}

} // namespace Thrive
