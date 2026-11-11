using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

/// <summary>
///   Helpers for uploading generated mesh data to Godot.
/// </summary>
public static class MeshHelpers
{
    /// <summary>
    ///   Adds a surface while keeping the managed owners of the native mesh data alive until the upload finishes.
    ///   This is a workaround for a Godot bug.
    /// </summary>
    public static void AddSurfaceFromArraysWithKeepAlive(this ArrayMesh mesh, Mesh.PrimitiveType primitive,
        Array arrays)
    {
        // Godot's generated binding extracts native handles without keeping their managed owners alive. Supplying
        // these optional arguments avoids hidden temporary containers that we cannot keep alive from the caller.
        // The using declarations keep these containers alive through the call and release them afterwards.
        var blendShapes = new Array<Array>();

        // Typed Godot arrays do not implement IDisposable, so own their underlying untyped array explicitly.
        using var blendShapeStorage = (Array)blendShapes;
        using var lods = new Dictionary();

        mesh.AddSurfaceFromArrays(primitive, arrays, blendShapes, lods);

        // KeepAlive must be after the native call, including in release builds. The numeric data has already been
        // copied into Godot-owned packed arrays, but the surface array wrapper still owns the native array handle.
        GC.KeepAlive(arrays);
        GC.KeepAlive(mesh);

        // We don't need to keep alive lods or blend shapes here because the using statement causes the same kind of
        // lifetime behaviour of keeping them alive. We still keep references to the mesh and arrays because we don't
        // know if our caller has them in a using statement.
    }
}
