#pragma once

#include "atlas_unwrap.hpp"
#include "ExtendedArrayMesh.hpp"

#include <godot_cpp/core/class_db.hpp>

#include <godot_cpp/classes/array_mesh.hpp>

namespace Thrive
{
	void ExtendedArrayMesh::_bind_methods()
	{
		using namespace godot;
		
		//ClassDB::bind_method(D_METHOD("get_native_instance"), &ExtendedArrayMesh::GetThis);
		ClassDB::bind_method(D_METHOD("unwrap", "texel_size"), &ExtendedArrayMesh::UnwrapMesh);
	}
	
	bool ExtendedArrayMesh::UnwrapMesh(float texel_size)
	{
		return Thrive::Unwrap(texel_size, *this);
	}
	
	ExtendedArrayMesh::ExtendedArrayMesh()
	{
		// Nothing here
	}
	
	ExtendedArrayMesh::~ExtendedArrayMesh()
	{
		// Nothing here
	}
}