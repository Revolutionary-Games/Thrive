#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C# specifically in this extension type

extern "C"
{
    // ------------------------------------ //
    // General

    /// \brief Gets the version of this. This requires an existing instance of ThriveConfig object to be created
    ///
    /// This mostly exists to check everything was loaded fine and can now be used
    /// \return The version number
    [[maybe_unused]] THRIVE_NATIVE_API int32_t ExtensionGetVersion(ThriveConfig* thriveConfig);
}
