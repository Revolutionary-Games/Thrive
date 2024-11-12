#pragma once

#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/surface_tool.hpp>

namespace Thrive
{

bool Unwrap(godot::ArrayMesh& mesh, float texelSize);

void FinishUnwrap(const godot::Ref<godot::SurfaceTool>& surfaces_tools, const godot::Ref<godot::ArrayMesh>& mesh,
    uint64_t surface_format);

} // namespace Thrive
