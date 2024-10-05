#pragma once

#include "atlas_unwrap.hpp"

#include <godot_cpp/core/class_db.hpp>

#include <godot_cpp/classes/array_mesh.hpp>

namespace Thrive
{

class ExtendedArrayMesh final : public godot::ArrayMesh
{
	GDCLASS(ExtendedArrayMesh, godot::ArrayMesh)
	
protected:
    static void _bind_methods();
	
public:
	ExtendedArrayMesh();
	~ExtendedArrayMesh();
	[[nodiscard]] bool UnwrapMesh();
};

}  // namespace Thrive
