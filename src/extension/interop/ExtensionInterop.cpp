// ------------------------------------ //
#include "ExtensionInterop.h"

#include <godot_cpp/variant/vector3.hpp>

#include "core/ThriveConfig.hpp"
#include "nodes/DebugDrawer.hpp"

#include "atlas/atlas_unwrap.hpp"

using namespace godot;

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

bool Unwrap(float p_texel_size, float *vertices, float *normals, int vertex_count, int *indices, int index_count, float *uvs)
{
	return Thrive::Unwrap(p_texel_size, vertices, normals, vertex_count, indices, index_count, uvs);
}