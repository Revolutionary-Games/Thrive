// ------------------------------------ //
#include "ExtensionInterop.h"

#include <godot_cpp/variant/vector3.hpp>

#include "core/ThriveConfig.hpp"
#include "nodes/DebugDrawer.hpp"

// ------------------------------------ //
int32_t ExtensionGetVersion(ThriveConfig* thriveConfig)
{
    if (thriveConfig == nullptr)
    {
        ERR_PRINT("ThriveConfig is null in call");
        return -1;
    }

    return reinterpret_cast<Thrive::ThriveConfig*>(thriveConfig)->GetVersion();
}

// ------------------------------------ //
static_assert(
    sizeof(godot::Vector3) == sizeof(JVecF3), "for efficiency these are assumed to have the same memory layout");

static_assert(
    sizeof(godot::Color) == sizeof(JColour), "for efficiency these are assumed to have the same memory layout");

void DebugDrawerAddLine(DebugDrawer* drawerInstance, JVecF3* from, JVecF3* to, JColour* colour)
{
    // This is called from C# directly with the Godot variants of the classes so this assumes that is so and casts
    // things
    reinterpret_cast<Thrive::DebugDrawer*>(drawerInstance)
        ->AddLine(*reinterpret_cast<godot::Vector3*>(from), *reinterpret_cast<godot::Vector3*>(to),
            *reinterpret_cast<godot::Color*>(colour));
}

void DebugDrawerAddPoint(DebugDrawer* drawerInstance, JVecF3* position, JColour* colour)
{
    reinterpret_cast<Thrive::DebugDrawer*>(drawerInstance)
        ->AddPoint(*reinterpret_cast<godot::Vector3*>(position), *reinterpret_cast<godot::Color*>(colour));
}

int32_t ExtensionTestFunc(int num)
{
	return Thrive::TestFunc(num);
}

bool ExtensionUnwrap(float p_texel_size, const float *p_vertices, const float *p_normals, int p_vertex_count, const int *p_indices, int p_index_count, float **r_uv, int **r_vertex, int *r_vertex_count, int **r_index, int *r_index_count, int *r_size_hint_x, int *r_size_hint_y)
{
	return Thrive::Unwrap(p_texel_size, p_vertices, p_normals, p_vertex_count, p_indices, p_index_count, r_uv, r_vertex,  r_vertex_count, r_index, r_index_count, r_size_hint_x, r_size_hint_y);
}