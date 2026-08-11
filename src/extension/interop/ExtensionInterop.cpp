// ------------------------------------ //
#include "ExtensionInterop.h"

BEGIN_GODOT_INCLUDES;
#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/core/object.hpp>
#include <godot_cpp/variant/variant.hpp>
#include <godot_cpp/variant/vector3.hpp>
#include <godot_cpp/variant/color.hpp>
END_GODOT_INCLUDES;

#include "atlas/atlas_unwrap.hpp"
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

// Explicit conversions to Godot types from wider types
static inline godot::Vector3 ToGodotVec3(const JVec3& v)
{
    return {static_cast<float>(v.X), static_cast<float>(v.Y), static_cast<float>(v.Z)};
}

static inline godot::Color ToGodotColor(const JColour& c)
{
    return {c.R, c.G, c.B, c.A};
}

void DebugDrawerAddLine(DebugDrawer* drawerInstance, JVec3* from, JVec3* to, JColour* colour)
{
    auto* drawer = reinterpret_cast<Thrive::DebugDrawer*>(drawerInstance);
    if (!drawer || !from || !to || !colour)
        return;

    drawer->AddLine(ToGodotVec3(*from), ToGodotVec3(*to), ToGodotColor(*colour));
}

bool ArrayMeshUnwrap(GodotVariant* mesh, float texelSize)
{
    const auto variant = reinterpret_cast<godot::Variant*>(mesh);

    if (variant->get_type() != godot::Variant::OBJECT)
    {
        return false;
    }

    auto obj = (godot::Object*)variant;

    if (!obj->is_class("ArrayMesh"))
    {
        return false;
    }

    auto arrayMesh = godot::Object::cast_to<godot::ArrayMesh>(obj);

    return Thrive::Unwrap(*arrayMesh, texelSize);
}

void DebugDrawerAddPoint(DebugDrawer* drawerInstance, JVec3* position, JColour* colour)
{
    auto* drawer = reinterpret_cast<Thrive::DebugDrawer*>(drawerInstance);
    if (!drawer || !position || !colour)
        return;

    drawer->AddPoint(ToGodotVec3(*position), ToGodotColor(*colour));
}
