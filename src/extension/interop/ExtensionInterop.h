#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C# specifically in this extension type

extern "C"
{
    // ------------------------------------ //
    // General

    [[maybe_unused]] THRIVE_NATIVE_API int32_t ExtensionGetVersion(ThriveConfig* thriveConfig);
}
