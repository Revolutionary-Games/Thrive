#pragma once

/// \file Defines conversion operators between Jolt and Godot types

#include <godot_cpp/variant/color.hpp>
#include <godot_cpp/variant/vector3.hpp>
#include <Jolt/Jolt.h>
#include <Jolt/Math/Real.h>

namespace Thrive
{

inline godot::Vector3 JoltToGodot(const JPH::DVec3Arg& vector)
{
    return {static_cast<float>(vector.GetX()), static_cast<float>(vector.GetY()), static_cast<float>(vector.GetZ())};
}

inline godot::Color JoltToGodot(const JPH::Float4& vector)
{
    return {vector.x, vector.y, vector.z, vector.w};
}

} // namespace Thrive
