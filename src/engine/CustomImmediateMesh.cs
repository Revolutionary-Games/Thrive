using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   An immediate mode style drawing mesh that actually works and doesn't explode with a lot of added data. This will
///   use all data added to this, so there's probably some upper limit that shouldn't be crossed for performance
///   reasons.
/// </summary>
/// <remarks>
///   <para>
///     Note that this class keeps one extra copy of the data in memory to avoid having to reallocate lists on each
///     mesh update even if the number of vertices didn't change
///   </para>
/// </remarks>
public class CustomImmediateMesh : IDisposable
{
    public readonly ArrayMesh Mesh;
    private readonly Material meshMaterial;

    private readonly Array arrays;

    private readonly List<Vector3> vertexData = new();
    private readonly List<Vector2> uvData = new();
    private readonly List<Color> colourData = new();
    private readonly List<Vector3> normalData = new();

    private readonly List<int> indexData = new();

    // To avoid re-allocating lists each frame, these are used as temporary places to copy data to Godot
    // TODO: improve this once Godot has a better way to pass in data
    // This can't use array pool as we can't pass a separate length variable to Godot. Technically with an index buffer
    // we could copy some unused data that doesn't get used in the end, but for now that isn't implemented.
    private Vector3[]? tempVertexData;
    private Vector2[]? tempUvData;
    private Color[]? tempColourData;
    private Vector3[]? tempNormalData;

    private int[]? tempIndexData;

    private bool started;
    private int indexCounter;

    private Mesh.PrimitiveType drawType;

    public CustomImmediateMesh(Material meshMaterial)
    {
        this.meshMaterial = meshMaterial;
        Mesh = new ArrayMesh();

        // Setup the mesh data we don't need to change
        arrays = new Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
    }

    public Aabb CustomBoundingBox
    {
        get => Mesh.CustomAabb;
        set => Mesh.CustomAabb = value;
    }

    /// <summary>
    ///   Starts a batch of update operations. Must be called first before content can be added
    /// </summary>
    /// <param name="geometryType">
    ///   The primitive geometry type that will be used to render this. Triangle list is the most usual.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartIfNeeded(Mesh.PrimitiveType geometryType)
    {
        if (started)
            return;

        started = true;
        drawType = geometryType;

        Clear();
    }

    /// <summary>
    ///   Finishes building the mesh. <see cref="StartIfNeeded"/> needs to have been called. Any geometry needs to be
    ///   added before calling this, otherwise the data won't be written to the generated mesh.
    /// </summary>
    public void Finish()
    {
        if (!started)
            return;

        started = false;

        RebuildGeometry();
    }

    // High level data building methods

    public void AddLine(Vector3 from, Vector3 to, Color colour)
    {
        if (drawType != Godot.Mesh.PrimitiveType.Lines)
            throw new InvalidOperationException("Wrong draw type for operation");

        vertexData.Add(from);
        vertexData.Add(to);

        colourData.Add(colour);
        colourData.Add(colour);
    }

    public void AddTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color colour)
    {
        if (drawType != Godot.Mesh.PrimitiveType.Triangles)
            throw new InvalidOperationException("Wrong draw type for operation");

        vertexData.Add(vertex1);
        vertexData.Add(vertex2);
        vertexData.Add(vertex3);

        colourData.Add(colour);
        colourData.Add(colour);
        colourData.Add(colour);
    }

    // Lower level data adding methods

    public void AddVertex(Vector3 position, Vector2 uv, Vector3 normal, Color color, bool addIndex)
    {
        vertexData.Add(position);
        uvData.Add(uv);
        normalData.Add(normal);
        colourData.Add(color);

        if (addIndex)
            indexData.Add(indexCounter++);
    }

    public void AddVertex(Vector3 position, Vector2 uv, Color color, bool addIndex)
    {
        vertexData.Add(position);
        uvData.Add(uv);
        colourData.Add(color);

        if (addIndex)
            indexData.Add(indexCounter++);
    }

    public void AddVertex(Vector3 position, Vector2 uv, bool addIndex)
    {
        vertexData.Add(position);
        uvData.Add(uv);

        if (addIndex)
            indexData.Add(indexCounter++);
    }

    public void AddVertex(Vector3 position, Color color, bool addIndex)
    {
        vertexData.Add(position);
        colourData.Add(color);

        if (addIndex)
            indexData.Add(indexCounter++);
    }

    public void AddVertex(Vector3 position, bool addIndex)
    {
        vertexData.Add(position);

        if (addIndex)
            indexData.Add(indexCounter++);
    }

    public void AddIndex(int index)
    {
        indexData.Add(index);
    }

    public int GetNextIndex()
    {
        return indexCounter;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Mesh.Dispose();
            arrays.Dispose();
        }
    }

    private void Clear()
    {
        vertexData.Clear();
        uvData.Clear();
        colourData.Clear();
        normalData.Clear();

        indexData.Clear();
        indexCounter = 0;

        Mesh.ClearSurfaces();
    }

    private void RebuildGeometry()
    {
        // If nothing drawn, skip
        if (vertexData.Count < 1)
            return;

        arrays[(int)Godot.Mesh.ArrayType.Vertex] = GetTemporaryCopy(vertexData, ref tempVertexData);

        if (uvData.Count > 0)
        {
            arrays[(int)Godot.Mesh.ArrayType.TexUV] = GetTemporaryCopy(uvData, ref tempUvData);
        }
        else
        {
            arrays[(int)Godot.Mesh.ArrayType.TexUV] = default(Variant);
        }

        if (normalData.Count > 0)
        {
            arrays[(int)Godot.Mesh.ArrayType.Normal] = GetTemporaryCopy(normalData, ref tempNormalData);
        }
        else
        {
            arrays[(int)Godot.Mesh.ArrayType.Normal] = default(Variant);
        }

        if (colourData.Count > 0)
        {
            arrays[(int)Godot.Mesh.ArrayType.Color] = GetTemporaryCopy(colourData, ref tempColourData);
        }
        else
        {
            arrays[(int)Godot.Mesh.ArrayType.Color] = default(Variant);
        }

        if (indexData.Count > 0)
        {
            arrays[(int)Godot.Mesh.ArrayType.Index] = GetTemporaryCopy(indexData, ref tempIndexData);
        }
        else
        {
            arrays[(int)Godot.Mesh.ArrayType.Index] = default(Variant);
        }

        // TODO: find out if we can use this for much higher performance:
        // Mesh.SurfaceUpdateRegion();

        Mesh.AddSurfaceFromArrays(drawType, arrays);
        Mesh.SurfaceSetMaterial(0, meshMaterial);
    }

    private T[] GetTemporaryCopy<T>(List<T> sourceData, ref T[]? tempStorage)
    {
        int size = sourceData.Count;

        // TODO: somehow try to figure out how the temporary storage can be more efficiently reused
        if (tempStorage == null || size != tempStorage.Length)
        {
            tempStorage = new T[size];
        }

        sourceData.CopyTo(tempStorage);

        return tempStorage;
    }
}
