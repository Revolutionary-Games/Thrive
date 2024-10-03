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

using namespace godot;

int Thrive::TestFunc(int test)
{
	return test;
}

bool Thrive::Unwrap(float p_texel_size, float *vertices, float *normals, int vertex_count, int *indices, int index_count, float *uvs)
{

	{	
		// set up input mesh
		xatlas::MeshDecl input_mesh;
		input_mesh.indexData = indices;
		input_mesh.indexCount = index_count;
		input_mesh.indexFormat = xatlas::IndexFormat::UInt32;

		input_mesh.vertexCount = vertex_count;
		input_mesh.vertexPositionData = vertices;
		input_mesh.vertexPositionStride = sizeof(float) * 3;
		input_mesh.vertexNormalData = normals;
		input_mesh.vertexNormalStride = sizeof(uint32_t) * 3;
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

		vertices = (float *)memalloc(sizeof(float) * output.vertexCount * 3);
		ERR_FAIL_NULL_V_MSG(vertices, false, "Out of memory.");
		uvs = (float *)memalloc(sizeof(float) * output.vertexCount * 2);
		ERR_FAIL_NULL_V_MSG(uvs, false, "Out of memory.");
		indices = (int *)memalloc(sizeof(int) * output.indexCount);
		ERR_FAIL_NULL_V_MSG(indices, false, "Out of memory.");

		float max_x = 0.0f;
		float max_y = 0.0f;
		for (uint32_t i = 0; i < output.vertexCount; i++) {
			vertices[i] = (float)output.vertexArray[i].xref;
			uvs[i * 2 + 0] = output.vertexArray[i].uv[0] / w;
			uvs[i * 2 + 1] = output.vertexArray[i].uv[1] / h;
			max_x = std::max(max_x, output.vertexArray[i].uv[0]);
			max_y = std::max(max_y, output.vertexArray[i].uv[1]);
		}
		
		vertex_count = output.vertexCount;

		for (uint32_t i = 0; i < output.indexCount; i++) {
			indices[i] = output.indexArray[i];
		}

		xatlas::Destroy(atlas);
	}

	return true;
}