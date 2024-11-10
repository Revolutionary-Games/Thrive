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

// This is based on Godot's script: /modules/xatlas_unwrap/register_types.cpp
// But made to work with UV1 layer and without lightmaps

#include "atlas_unwrap.hpp"

#include "godot_cpp/templates/local_vector.hpp"
#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/surface_tool.hpp>
#include <godot_cpp/core/class_db.hpp>

#include "xatlas.h"

using namespace godot;

bool Thrive::Unwrap(godot::ArrayMesh& mesh, float texelSize)
{
    // Data for the xatlas library
    LocalVector<float> vertices;
    LocalVector<float> normals;
    LocalVector<int> indices;

    uint64_t surfaceFormat = mesh.surface_get_format(0);

    Array arrays = mesh.surface_get_arrays(0);

    PackedVector3Array initialVertices = arrays[Mesh::ARRAY_VERTEX];
    uint64_t vertexCount = initialVertices.size();

    PackedVector3Array initialNormals = arrays[Mesh::ARRAY_NORMAL];

    vertices.resize((uint32_t)(vertexCount * 3));
    normals.resize((uint32_t)(vertexCount * 3));

    for (int j = 0; j < vertexCount; j++)
    {
        Vector3 vertex = initialVertices[j];
        Vector3 normal = initialNormals[j];

        vertices[j * 3 + 0] = vertex.x;
        vertices[j * 3 + 1] = vertex.y;
        vertices[j * 3 + 2] = vertex.z;
        normals[j * 3 + 0] = normal.x;
        normals[j * 3 + 1] = normal.y;
        normals[j * 3 + 2] = normal.z;
    }

    PackedInt32Array initialIndices = arrays[Mesh::ARRAY_INDEX];
    uint64_t indexCount = initialIndices.size();

    // Taken from xatlas.h
    float eps = 1.19209290e-7F;
    for (int j = 0; j < indexCount / 3; j++)
    {
        Vector3 p0 = initialVertices[initialIndices[j * 3 + 0]];
        Vector3 p1 = initialVertices[initialIndices[j * 3 + 1]];
        Vector3 p2 = initialVertices[initialIndices[j * 3 + 2]];

        if ((p0 - p1).length_squared() < eps || (p1 - p2).length_squared() < eps || (p2 - p0).length_squared() < eps)
        {
            continue;
        }

        indices.push_back(initialIndices[j * 3 + 0]);
        indices.push_back(initialIndices[j * 3 + 1]);
        indices.push_back(initialIndices[j * 3 + 2]);
    }

    // set up input mesh
    xatlas::MeshDecl inputMesh;
    inputMesh.indexData = indices.ptr();
    inputMesh.indexCount = indices.size();
    inputMesh.indexFormat = xatlas::IndexFormat::UInt32;

    inputMesh.vertexCount = vertices.size() / 3;
    inputMesh.vertexPositionData = vertices.ptr();
    inputMesh.vertexPositionStride = sizeof(float) * 3;
    inputMesh.vertexNormalData = normals.ptr();
    inputMesh.vertexNormalStride = sizeof(float) * 3;
    inputMesh.vertexUvData = nullptr;
    inputMesh.vertexUvStride = 0;

    xatlas::ChartOptions chartOptions;
    chartOptions.fixWinding = true;

    ERR_FAIL_COND_V_MSG(texelSize <= 0.0f, false, "Texel size must be greater than 0.");

    xatlas::PackOptions packOptions;
    packOptions.padding = 1;

    packOptions.maxChartSize = 1024;
    packOptions.blockAlign = true;
    packOptions.texelsPerUnit = 1.0f / texelSize;

    xatlas::Atlas* atlas = xatlas::Create();

    xatlas::AddMeshError err = xatlas::AddMesh(atlas, inputMesh, 1);
    ERR_FAIL_COND_V_MSG(err != xatlas::AddMeshError::Success, false, xatlas::StringForEnum(err));

    xatlas::Generate(atlas, chartOptions, packOptions);

    ERR_FAIL_COND_V_MSG(atlas->chartCount == 0, false, "No charts generated");

    float w = (float)(atlas->width);
    float h = (float)(atlas->height);

    if (w == 0 || h == 0)
    {
        xatlas::Destroy(atlas);
        ERR_FAIL_COND_V_MSG(w == 0 || h == 0, false, "could not bake because there is no area");
    }

    const xatlas::Mesh& output = atlas->meshes[0];

    mesh.clear_surfaces();

    Ref<SurfaceTool> surfacesTools;
    surfacesTools.instantiate();
    surfacesTools->begin(Mesh::PRIMITIVE_TRIANGLES);
    indexCount = output.indexCount;

    PackedColorArray initialColors = arrays[Mesh::ARRAY_COLOR];
    PackedVector2Array initialUV2 = arrays[Mesh::ARRAY_TEX_UV2];
    PackedFloat32Array initialTangents = arrays[Mesh::ARRAY_TANGENT];
    PackedInt32Array initialBones = arrays[Mesh::ARRAY_BONES];
    PackedFloat32Array initialWeights = arrays[Mesh::ARRAY_WEIGHTS];

    for (int i = 0; i < indexCount; i += 3)
    {
        for (int j = 0; j < 3; j++)
        {
            // Index of an original vertex corresponding to the generated one.
            int vertex_id = output.vertexArray[output.indexArray[i + j]].xref;

            if (surfaceFormat & Mesh::ARRAY_FORMAT_COLOR)
            {
                surfacesTools->set_color(initialColors[vertex_id]);
            }
            if (surfaceFormat & Mesh::ARRAY_FORMAT_TEX_UV2)
            {
                surfacesTools->set_uv2(initialUV2[vertex_id]);
            }
            if (surfaceFormat & Mesh::ARRAY_FORMAT_NORMAL)
            {
                surfacesTools->set_normal(initialNormals[vertex_id]);
            }
            if (surfaceFormat & Mesh::ARRAY_FORMAT_TANGENT)
            {
                Plane t;
                t.normal = Vector3(initialTangents[vertex_id * 4], initialTangents[vertex_id * 4 + 1],
                    initialTangents[vertex_id * 4 + 2]);
                t.d = initialTangents[vertex_id * 4 + 3];
                surfacesTools->set_tangent(t);
            }
            if (surfaceFormat & Mesh::ARRAY_FORMAT_BONES)
            {
                surfacesTools->set_bones(initialBones.slice(vertex_id, vertex_id + 3));
            }
            if (surfaceFormat & Mesh::ARRAY_FORMAT_WEIGHTS)
            {
                surfacesTools->set_weights(initialWeights.slice(vertex_id, vertex_id + 3));
            }

            Vector2 uv(output.vertexArray[output.indexArray[i + j]].uv[0] / w,
                output.vertexArray[output.indexArray[i + j]].uv[1] / h);

            surfacesTools->set_uv(uv);

            surfacesTools->add_vertex(initialVertices[vertex_id]);
        }
    }

    surfacesTools->index();
    callable_mp_static(&FinishUnwrap).call_deferred(surfacesTools, Ref<ArrayMesh>(&mesh), surfaceFormat);

    xatlas::Destroy(atlas);

    return true;
}

void Thrive::FinishUnwrap(Ref<SurfaceTool> surfacesTools, Ref<ArrayMesh> mesh, uint64_t surfaceFormat)
{
    surfacesTools->commit(mesh, surfaceFormat);
}
