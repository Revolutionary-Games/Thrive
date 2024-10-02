#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C# specifically in this extension type

// This file uses always API_EXPORT as this always exports (and this cannot use the same macro as the general interop
// as that defines only it to build when that project is being built so this would result in non-exported DLL symbols)

extern "C"
{
    // ------------------------------------ //
    // General

    /// \brief Gets the version of this. This requires an existing instance of ThriveConfig object to be created
    ///
    /// This mostly exists to check everything was loaded fine and can now be used
    /// \return The version number
    [[maybe_unused]] API_EXPORT int32_t ExtensionGetVersion(ThriveConfig* thriveConfig);

    // ------------------------------------ //
    // DebugDrawer direct access calls
    [[maybe_unused]] API_EXPORT void DebugDrawerAddLine(DebugDrawer* drawerInstance, JVecF3* from, JVecF3* to, JColour* colour);

    [[maybe_unused]] API_EXPORT void DebugDrawerAddPoint(DebugDrawer* drawerInstance, JVecF3* point, JColour* colour);
	
	[[maybe_unused]] int32_t ExtensionTestFunc(int num);

    [[maybe_unused]] bool ExtensionUnwrap(float p_texel_size, const float* p_vertices, const float* p_normals,
        int p_vertex_count,
        const int* p_indices, int p_index_count, float** r_uv, int** r_vertex, int* r_vertex_count, int** r_index,
        int* r_index_count, int* r_size_hint_x, int* r_size_hint_y);
}
