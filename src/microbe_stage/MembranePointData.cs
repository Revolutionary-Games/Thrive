using System;
using System.Buffers;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Final, computed point data for a membrane. This is a separate class from <see cref="Membrane"/> to support
///   caching this. The mesh is only initialized the first time it is accessed from the main thread. See
///   <see cref="MembraneComputationHelpers"/> for easily using this.
/// </summary>
public sealed class MembranePointData : IMembraneDataSource, ICacheableData
{
    private readonly Lazy<(ArrayMesh Mesh, int SurfaceIndex)> finalMesh;

    private float radius;

    private bool radiusCalculated;
    private bool disposed;

    public MembranePointData(Vector2[] hexPositions, int hexPositionCount, MembraneType type,
        IReadOnlyList<Vector2> verticesToCopy)
    {
        HexPositions = hexPositions;
        Type = type;
        HexPositionCount = hexPositionCount;

        // Setup mesh to be generated (on the main thread) only when required
        finalMesh = new Lazy<(ArrayMesh Mesh, int SurfaceIndex)>(() =>
            MembraneShapeGenerator.GetThreadSpecificGenerator().GenerateMesh(this));

        // Copy the membrane data, this copied array can then be referenced by Membrane instances as long as there
        // might exist a reference to this class instance (that's why it is only released in the finalizer)
        int count = verticesToCopy.Count;
        var copyTarget = ArrayPool<Vector2>.Shared.Rent(count);

        for (int i = 0; i < count; ++i)
        {
            copyTarget[i] = verticesToCopy[i];
        }

        Vertices2D = copyTarget;
        VertexCount = count;
    }

    ~MembranePointData()
    {
        Dispose();

        // Now safe to return shared pool data that could have been referenced by a Membrane instance
        ReleaseSharedData();
    }

    /// <summary>
    ///   Organelle positions of the microbe, must have points for membrane generation to be valid. This includes all
    ///   hex positions of the organelles to work better with cells that have multihex organelles. As a result this
    ///   doesn't contain just the center positions of organelles but will contain multiple entries for multihex
    ///   organelles.
    /// </summary>
    public Vector2[] HexPositions { get; }

    public int HexPositionCount { get; }

    public MembraneType Type { get; }

    // TODO: check all uses when switching this
    public Vector2[] Vertices2D { get; }

    public int VertexCount { get; }

    public ArrayMesh GeneratedMesh => finalMesh.Value.Mesh;
    public int SurfaceIndex => finalMesh.Value.SurfaceIndex;

    public float Radius
    {
        get
        {
            if (!radiusCalculated)
                CalculateEncompassingCircleRadius();

            return radius;
        }
    }

    public bool MatchesCacheParameters(ICacheableData cacheData)
    {
        if (cacheData is IMembraneDataSource data)
            return this.MembraneDataFieldsEqual(data);

        return false;
    }

    public long ComputeCacheHash()
    {
        return this.ComputeMembraneDataHash();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;

            ArrayPool<Vector2>.Shared.Return(HexPositions);
        }
    }

    /// <summary>
    ///   Cheaper version of contains for absorbing stuff.Calculates a circle radius that contains all the points
    ///   (when it is placed at 0,0 local coordinate). Also now used for a ton of other stuff that just needs an
    ///   approximate membrane shape.
    /// </summary>
    private void CalculateEncompassingCircleRadius()
    {
        float distanceSquared = 0;

        foreach (var vertex in Vertices2D)
        {
            var currentDistance = vertex.LengthSquared();
            if (currentDistance > distanceSquared)
                distanceSquared = currentDistance;
        }

        radius = Mathf.Sqrt(distanceSquared);
        radiusCalculated = true;
    }

    private void ReleaseSharedData()
    {
        if (finalMesh.IsValueCreated)
            finalMesh.Value.Mesh.Dispose();

        ArrayPool<Vector2>.Shared.Return(Vertices2D);
    }
}

/// <summary>
///   Cache entry that holds the base collision shape of a microbe (the physics shape of the membrane)
/// </summary>
public sealed class MembraneCollisionShape : ICacheableData
{
    /// <summary>
    ///   Only access this through <see cref="MembranePoints"/>
    /// </summary>
    private readonly JVecF3[] membranePoints;

    private bool disposed;

    public MembraneCollisionShape(PhysicsShape shape, JVecF3[] membranePoints, int pointCount, float density,
        bool isBacteria)
    {
        Shape = shape;
        this.membranePoints = membranePoints;
        PointCount = pointCount;
        Density = density;
        IsBacteria = isBacteria;
    }

    public PhysicsShape Shape { get; }

    public int PointCount { get; }
    public float Density { get; }
    public bool IsBacteria { get; }

    private JVecF3[] MembranePoints
    {
        get
        {
            if (disposed)
                throw new ObjectDisposedException("Cannot access membrane points after dispose");

            return membranePoints;
        }
    }

    public static long ComputeMicrobeShapeCacheHash(JVecF3[] points, int pointCount, float density, bool isBacteria)
    {
        unchecked
        {
            long hash = ~(long)pointCount;
            hash ^= density.GetHashCode();

            for (int i = 0; i < pointCount; ++i)
            {
                hash ^= i * 17 + points[i].GetHashCode();
            }

            hash ^= isBacteria ? 7907 : 7867;

            return hash;
        }
    }

    public static long ComputeMicrobeShapeCacheHash(IReadOnlyList<Vector2> points, int pointCount, float density,
        bool isBacteria)
    {
        if (pointCount > points.Count)
            throw new ArgumentException("Point count is more than list size", nameof(pointCount));

        unchecked
        {
            long hash = ~(long)pointCount;
            hash ^= density.GetHashCode();

            for (int i = 0; i < pointCount; ++i)
            {
                var point = points[i];
                hash ^= i * 17 + JVecF3.GetCompatibleHashCode(point.X, 0, point.Y);
            }

            hash ^= isBacteria ? 7907 : 7867;

            return hash;
        }
    }

    public bool MatchesCacheParameters(ICacheableData cacheData)
    {
        if (cacheData is not MembraneCollisionShape data)
            return false;

        var count = PointCount;
        if (data.PointCount != count)
            return false;

        if (!MatchesParameters(data.Density, data.IsBacteria))
            return false;

        var points = MembranePoints;
        var otherPoints = data.MembranePoints;

        for (int i = 0; i < count; ++i)
        {
            if (points[i] != otherPoints[i])
                return false;
        }

        return true;
    }

    public bool MatchesCacheParameters(IReadOnlyList<Vector2> otherMembranePoints, int pointCount, float density,
        bool isBacteria)
    {
        if (!MatchesParameters(density, isBacteria))
            return false;

        var count = PointCount;
        if (pointCount != count)
            return false;

        var points = MembranePoints;

        for (int i = 0; i < count; ++i)
        {
            var point = points[i];
            var otherPoint = otherMembranePoints[i];

            if (!point.X.Equals(otherPoint.X) || !point.Z.Equals(otherPoint.Y))
                return false;
        }

        return true;
    }

    public bool MatchesParameters(float density, bool isBacteria)
    {
        // It'd probably fine to do an exact compare on the density as the calculations to derive densities
        // probably are pretty stable, but it doesn't hurt if things that aren't exactly the same density are
        // considered equal
        if (Math.Abs(density - Density) > 0.000001f || isBacteria != IsBacteria)
            return false;

        return true;
    }

    public long ComputeCacheHash()
    {
        return ComputeMicrobeShapeCacheHash(MembranePoints, PointCount, Density, IsBacteria);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;

            // Access directly to not fail to read this data here, we are being disposed so going through the property
            // would fail already.
            ArrayPool<JVecF3>.Shared.Return(membranePoints);

            // Don't dispose the shape as it can still be used by other places on the C# side (the finalizer on the C#
            // class will then signal to the native side that the shape is no longer used)
        }
    }
}
