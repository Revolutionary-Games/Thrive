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
		ClassDB::bind_method(D_METHOD("unwrap"), &ExtendedArrayMesh::UnwrapMesh);
	}
	
	bool ExtendedArrayMesh::UnwrapMesh()
	{
		try
		{
			return Thrive::Unwrap(1.0f, *this);
		}
		catch(...)
		{
			ERR_FAIL_COND_V_MSG(true, false, "An exception in C++ code");
			return false;
		}
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