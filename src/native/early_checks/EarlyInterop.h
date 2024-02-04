#pragma once

#include <cstdint>

#include "Include.h"

#include "../helpers/CPUCheckResult.h"

/// \file Defines all of the API methods that can be called from C# for this early check library

extern "C"
{
    /// \brief Checks that current CPU has required features to run Thrive
    [[maybe_unused]] EARLY_NATIVE_API CPU_CHECK_RESULT CheckRequiredCPUFeatures();

    /// \returns The API version the native library was compiled with
    [[maybe_unused]] EARLY_NATIVE_API int32_t CheckEarlyAPIVersion();
}
