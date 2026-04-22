#pragma once

#include "Include.h"

BEGIN_GODOT_INCLUDES;
#include "godot_cpp/variant/color.hpp"
#include "godot_cpp/variant/vector3.hpp"
END_GODOT_INCLUDES;

#include "interop/CStructures.h"

namespace Thrive
{
inline godot::Vector3 JToGodot(const JVec3& vec)
{
    return {static_cast<float>(vec.X), static_cast<float>(vec.Y), static_cast<float>(vec.Z)};
}

inline godot::Color JToGodot(const JColour& colour)
{
    return {colour.R, colour.G, colour.B, colour.A};
}

} // namespace Thrive
