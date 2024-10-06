/**************************************************************************/
/*  register_types.cpp                                                    */
/**************************************************************************/
/*                         This file is part of:                          */
/*                             GODOT ENGINE                               */
/*                        https://godotengine.org                         */
/**************************************************************************/
/* Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md). */
/* Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.                  */
/*                                                                        */
/* Permission is hereby granted, free of charge, to any person obtaining  */
/* a copy of this software and associated documentation files (the        */
/* "Software"), to deal in the Software without restriction, including    */
/* without limitation the rights to use, copy, modify, merge, publish,    */
/* distribute, sublicense, and/or sell copies of the Software, and to     */
/* permit persons to whom the Software is furnished to do so, subject to  */
/* the following conditions:                                              */
/*                                                                        */
/* The above copyright notice and this permission notice shall be         */
/* included in all copies or substantial portions of the Software.        */
/*                                                                        */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/

#include "atlas_unwrap.hpp"

#include "xatlas.h"

#include <godot_cpp/core/class_db.hpp>

#include <godot_cpp/classes/mesh.hpp>
#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/surface_tool.hpp>
#include <godot_cpp/classes/ref.hpp>
#include "godot_cpp/templates/pair.hpp"
#include "godot_cpp/templates/local_vector.hpp"

using namespace godot;

int Thrive::TestFunc(int test)
{
	return test;
}

bool Thrive::Unwrap(float p_texel_size, ArrayMesh& mesh)
{
	// Data for the xatlas library
	LocalVector<float> vertices;
	LocalVector<float> normals;
	LocalVector<int> indices;
	LocalVector<float> uvs;
	
	uint64_t surface_format = mesh.surface_get_format(0);
	
	Array arrays = mesh.surface_get_arrays(0);
	
	PackedVector3Array rvertices = arrays[Mesh::ARRAY_VERTEX];
	uint64_t vc = rvertices.size();

	PackedVector3Array rnormals = arrays[Mesh::ARRAY_NORMAL];

	vertices.resize((uint32_t)(vc * 3));
	normals.resize((uint32_t)(vc * 3));
	uvs.resize((uint32_t)(vc * 2));

	for (int j = 0; j < vc; j++) {
		Vector3 v = rvertices[j];
		Vector3 n = rnormals[j];

		vertices[j * 3 + 0] = v.x;
		vertices[j * 3 + 1] = v.y;
		vertices[j * 3 + 2] = v.z;
		normals[j * 3 + 0] = n.x;
		normals[j * 3 + 1] = n.y;
		normals[j * 3 + 2] = n.z;
		uvs[j * 2 + 0] = 0.0f;
		uvs[j * 2 + 1] = 0.0f;
	}

	PackedInt32Array rindices = arrays[Mesh::ARRAY_INDEX];
	uint64_t ic = rindices.size();

	float eps = 1.19209290e-7F; // Taken from xatlas.h
	if (ic == 0) {
		for (int j = 0; j < vc / 3; j++) {
			Vector3 p0 = rvertices[j * 3 + 0];
			Vector3 p1 = rvertices[j * 3 + 1];
			Vector3 p2 = rvertices[j * 3 + 2];

			if ((p0 - p1).length_squared() < eps || (p1 - p2).length_squared() < eps || (p2 - p0).length_squared() < eps) {
				continue;
			}

			indices.push_back(j * 3 + 0);
			indices.push_back(j * 3 + 1);
			indices.push_back(j * 3 + 2);
		}

	} else {
		for (int j = 0; j < ic / 3; j++) {
			Vector3 p0 = rvertices[rindices[j * 3 + 0]];
			Vector3 p1 = rvertices[rindices[j * 3 + 1]];
			Vector3 p2 = rvertices[rindices[j * 3 + 2]];

			if ((p0 - p1).length_squared() < eps || (p1 - p2).length_squared() < eps || (p2 - p0).length_squared() < eps) {
				continue;
			}

			indices.push_back(rindices[j * 3 + 0]);
			indices.push_back(rindices[j * 3 + 1]);
			indices.push_back(rindices[j * 3 + 2]);
		}
	}
	
	// set up input mesh
	xatlas::MeshDecl input_mesh;
	input_mesh.indexData = indices.ptr();
	input_mesh.indexCount = indices.size();
	input_mesh.indexFormat = xatlas::IndexFormat::UInt32;

	input_mesh.vertexCount = vertices.size() / 3;
	input_mesh.vertexPositionData = vertices.ptr();
	input_mesh.vertexPositionStride = sizeof(float) * 3;
	input_mesh.vertexNormalData = normals.ptr();
	input_mesh.vertexNormalStride = sizeof(float) * 3;
	input_mesh.vertexUvData = nullptr;
	input_mesh.vertexUvStride = 0;

	xatlas::ChartOptions chart_options;
	chart_options.fixWinding = true;

	ERR_FAIL_COND_V_MSG(p_texel_size <= 0.0f, false, "Texel size must be greater than 0.");

	xatlas::PackOptions pack_options;
	pack_options.padding = 1;
	pack_options.maxChartSize = 4094; // Lightmap atlassing needs 2 for padding between meshes, so 4096-2
	pack_options.blockAlign = true;
	pack_options.texelsPerUnit = 1.0f / p_texel_size;

	xatlas::Atlas *atlas = xatlas::Create();

	xatlas::AddMeshError err = xatlas::AddMesh(atlas, input_mesh, 1);
	ERR_FAIL_COND_V_MSG(err != xatlas::AddMeshError::Success, false, xatlas::StringForEnum(err));

	xatlas::Generate(atlas, chart_options, pack_options);
	
	ERR_FAIL_COND_V_MSG(atlas->chartCount == 0, false, "No charts generated");
	
	float w = (float)(atlas->width);
	float h = (float)(atlas->height);
	
	if (w == 0 || h == 0)
	{
		xatlas::Destroy(atlas);
		ERR_FAIL_COND_V_MSG(w == 0 || h == 0, false, "could not bake because there is no area");
	}

	const xatlas::Mesh &output = atlas->meshes[0];
	
	mesh.clear_surfaces();
	
	Ref<SurfaceTool> surfaces_tools;
	surfaces_tools.instantiate();
	surfaces_tools->begin(Mesh::PRIMITIVE_TRIANGLES);
	
	vc = output.vertexCount;
	ic = output.indexCount;
	
	PackedColorArray rcolors = arrays[Mesh::ARRAY_COLOR];
	PackedVector2Array r_uv2 = arrays[Mesh::ARRAY_TEX_UV2];
	PackedFloat32Array rtangent = arrays[Mesh::ARRAY_TANGENT];
	PackedInt32Array rbones = arrays[Mesh::ARRAY_BONES];
	PackedFloat32Array rweights = arrays[Mesh::ARRAY_WEIGHTS];
	
	for (int i = 0; i < ic; i += 3) {
		
		for (int j = 0; j < 3; j++) {
			// Index of an original vertex corresponding to the generated one.
			int vertex_id = output.vertexArray[output.indexArray[i + j]].xref;
			
			if (surface_format & Mesh::ARRAY_FORMAT_COLOR) {
                surfaces_tools->set_color(rcolors[vertex_id]);
			}
			if (surface_format & Mesh::ARRAY_FORMAT_TEX_UV2) {
				surfaces_tools->set_uv2(r_uv2[vertex_id]);
			}
			if (surface_format & Mesh::ARRAY_FORMAT_NORMAL) {
				surfaces_tools->set_normal(rnormals[vertex_id]);
			}
			if (surface_format & Mesh::ARRAY_FORMAT_TANGENT) {
				Plane t;
				t.normal = Vector3(rtangent[vertex_id * 4], rtangent[vertex_id * 4 + 1], rtangent[vertex_id * 4 + 2]);
				t.d = rtangent[vertex_id * 4 + 3];
				surfaces_tools->set_tangent(t);
			}
			if (surface_format & Mesh::ARRAY_FORMAT_BONES) {
				surfaces_tools->set_bones(rbones.slice(vertex_id, vertex_id + 3));
			}
			if (surface_format & Mesh::ARRAY_FORMAT_WEIGHTS) {
				surfaces_tools->set_weights(rweights.slice(vertex_id, vertex_id + 3));
			}
			
			Vector2 uv(output.vertexArray[output.indexArray[i + j]].uv[0] / w,output.vertexArray[output.indexArray[i + j]].uv[1] / h);
			
			surfaces_tools->set_uv(uv);
			
			surfaces_tools->add_vertex(rvertices[vertex_id]);
		}
	}
	
	surfaces_tools->index();
	surfaces_tools->commit(Ref<ArrayMesh>((ArrayMesh *)&mesh), surface_format);

	xatlas::Destroy(atlas);

	return true;
}