#pragma once

#include <Jolt/Jolt.h>
#include <Jolt/Math/Real.h>

namespace Thrive
{

constexpr uint64_t INTEROP_MAGIC_VALUE = 42 * 42;

using OnDebugLines = void (*)(const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& lineBuffer);
using OnDebugTriangles = void (*)(
    const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& triangleBuffer);

/// \brief Contains pointers and other info passed through from ThriveNative to ThriveExtension during runtime setup phase
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

} // namespace Thrive
