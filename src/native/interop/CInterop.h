#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C#

extern "C"
{
    /// \returns The API version the native library was compiled with, if different from C# the library should not be
    /// used
    [[maybe_unused]] THRIVE_NATIVE_API int32_t CheckAPIVersion();

    /// \brief Prepares the native library for use, must be called first (right after the version check)
    [[maybe_unused]] THRIVE_NATIVE_API int32_t InitThriveLibrary();

    /// \brief Prepares the native library for shutdown should be called before the process is ended and after all
    /// other calls to the library have been performed
    [[maybe_unused]] THRIVE_NATIVE_API void ShutdownThriveLibrary();

    // ------------------------------------ //

    [[maybe_unused]] THRIVE_NATIVE_API PhysicalWorld* CreatePhysicalWorld();

    [[maybe_unused]] THRIVE_NATIVE_API void DestroyPhysicalWorld(PhysicalWorld* physicalWorld);
}
