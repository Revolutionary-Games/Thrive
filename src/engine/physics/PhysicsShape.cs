using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Wrapper for native side physics shape. And also contains factories for making various shapes.
/// </summary>
public class PhysicsShape : IDisposable
{
    private bool disposed;
    private IntPtr nativeInstance;

    private PhysicsShape(IntPtr nativeInstance)
    {
        this.nativeInstance = nativeInstance;
    }

    ~PhysicsShape()
    {
        Dispose(false);
    }

    public bool Disposed => disposed;

    public static PhysicsShape CreateBox(float halfSideLength, float density = 1000)
    {
        return new PhysicsShape(NativeMethods.CreateBoxShape(halfSideLength, density));
    }

    public static PhysicsShape CreateBox(Vector3 halfDimensions, float density = 1000)
    {
        return new PhysicsShape(NativeMethods.CreateBoxShapeWithDimensions(new JVecF3(halfDimensions), density));
    }

    public static PhysicsShape CreateSphere(float radius, float density = 1000)
    {
        return new PhysicsShape(NativeMethods.CreateSphereShape(radius, density));
    }

    public static PhysicsShape CreateCylinder(float halfHeight, float radius, float density = 1000)
    {
        return new PhysicsShape(NativeMethods.CreateCylinderShape(halfHeight, radius, density));
    }

    /// <summary>
    ///   Creates a microbe shape or returns from one from the cache
    /// </summary>
    /// <returns>The shape made from a convex body for the microbe</returns>
    public static PhysicsShape GetOrCreateMicrobeShape(IReadOnlyList<Vector2> membranePoints, int pointCount,
        bool scaleAsBacteria)
    {
        var cache = ProceduralDataCache.Instance;

        var hash = MembraneCollisionShape.ComputeMicrobeShapeCacheHash(membranePoints, pointCount, scaleAsBacteria);

        var result = cache.ReadMembraneCollisionShape(hash);

        if (result != null)
        {
            if (result.MatchesCacheParameters(membranePoints, pointCount, scaleAsBacteria))
            {
                return result.Shape;
            }

            CacheableDataExtensions.OnCacheHashCollision<MembraneCollisionShape>(hash);
        }

        // Need to convert the data to call the method that uses the native side to create the body
        // TODO: find out if a more performant way can be done to copy this data or not (luckily only needed when cache
        // is missing data for this membrane)
        var convertedData = ArrayPool<JVecF3>.Shared.Rent(membranePoints.Count);

        for (int i = 0; i < pointCount; ++i)
        {
            convertedData[i] = new JVecF3(membranePoints[i].X, 0, membranePoints[i].Y);
        }

        // The rented array from the pool will be returned when the cache entry is disposed
        result = new MembraneCollisionShape(CreateMicrobeShape(new ReadOnlySpan<JVecF3>(convertedData, 0, pointCount),
                Constants.BASE_CELL_DENSITY,
                scaleAsBacteria),
            convertedData, pointCount, Constants.BASE_CELL_DENSITY, scaleAsBacteria);

        cache.WriteMembraneCollisionShape(ref result);

        return result.Shape;
    }

    public static PhysicsShape CreateMicrobeShape(ReadOnlySpan<JVecF3> organellePositions, float overallDensity,
        bool scaleAsBacteria, bool createAsSpheres = false)
    {
        if (createAsSpheres)
        {
            return new PhysicsShape(NativeMethods.CreateMicrobeShapeSpheres(
                MemoryMarshal.GetReference(organellePositions),
                (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
        }

        return new PhysicsShape(NativeMethods.CreateMicrobeShapeConvex(MemoryMarshal.GetReference(organellePositions),
            (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
    }

    public static PhysicsShape CreateCombinedShapeStatic(
        IReadOnlyList<(PhysicsShape Shape, Vector3 Position, Quaternion Rotation)> subShapes)
    {
        var pool = ArrayPool<SubShapeDefinition>.Shared;

        // Need some temporary memory to hold the sub-shapes in
        var count = subShapes.Count;
        var buffer = pool.Rent(count);

        try
        {
            for (int i = 0; i < count; ++i)
            {
                var data = subShapes[i];
                buffer[i] = new SubShapeDefinition(data.Position, data.Rotation, data.Shape.AccessShapeInternal());
            }

            // TODO: does this need to fix the buffer memory?
            return new PhysicsShape(NativeMethods.CreateStaticCompoundShape(buffer[0], (uint)count));

            // return new PhysicsShape(NativeMethods.CreateStaticCompoundShape(pin.AddrOfPinnedObject(), (uint)count));
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    /// <summary>
    ///   Loads a physics shape from a Godot resource
    /// </summary>
    /// <param name="path">Path to the Godot resource</param>
    /// <param name="density">
    ///   The density of the created body. Note that this avoid caching if the same shape has different density so
    ///   avoid slight density changes if they wouldn't have any concrete impact anyway
    /// </param>
    /// <returns>The loaded shape or null if there is an error processing</returns>
    public static PhysicsShape? CreateShapeFromGodotResource(string path, float density)
    {
        var cache = ProceduralDataCache.Instance;

        var cached = cache.ReadLoadedShape(path, density);

        if (cached != null)
            return cached;

        // Base scale for physics bodies. This is now always one as shapes are assumed to be scaled correctly when
        // saved to disk
        const float scale = 1;

        // TODO: pre-bake collision shapes for game export (the fallback conversion below should only need to be used
        // when debugging to make the release version perform better)

        var godotData = GD.Load<Shape3D>(path);

        if (godotData == null)
        {
            GD.PrintErr("Physics shape data is not a Shape3D derived type, when loading path: ", path);
            return null;
        }

        // This is probably similar kind of configuration thing for Jolt as the margin is in Godot Bullet integration
        var margin = godotData.Margin > 0 ? godotData.Margin : 0.01f;

        switch (godotData)
        {
            case SphereShape3D sphereShape:
            {
                cached = new PhysicsShape(NativeMethods.CreateSphereShape(sphereShape.Radius * scale, density));
                break;
            }

            case BoxShape3D boxShape:
            {
                // Need to convert from full lengths to half lengths (as Godot 4 switched to full lengths for boxes)
                cached = new PhysicsShape(
                    NativeMethods.CreateBoxShapeWithDimensions(new JVecF3(boxShape.Size * 0.5f * scale), density));
                break;
            }

            case CapsuleShape3D capsuleShape:
            {
                cached = new PhysicsShape(NativeMethods.CreateCapsuleShape(capsuleShape.Height * 0.5f * scale,
                    capsuleShape.Radius * scale, density));
                break;
            }

            case CylinderShape3D cylinderShape:
            {
                cached = new PhysicsShape(NativeMethods.CreateCylinderShape(cylinderShape.Height * 0.5f * scale,
                    cylinderShape.Radius * scale, density));
                break;
            }

            case ConvexPolygonShape3D convexShape:
            {
                var dataSource = convexShape.Points;
                int points = dataSource.Length;

                if (points < 1)
                    throw new NotSupportedException("Can't convert convex polygon with no points");

                // We need a temporary buffer in case the data byte format is not exactly right when coming directly
                // from Godot
                var pool = ArrayPool<JVecF3>.Shared;
                var buffer = pool.Rent(points);

                for (int i = 0; i < points; ++i)
                {
                    buffer[i] = new JVecF3(dataSource[i]);
                }

                // TODO: does this need to fix the buffer memory?
                cached = new PhysicsShape(NativeMethods.CreateConvexShape(buffer[0], (uint)points, density, scale,
                    margin));

                pool.Return(buffer);
                break;
            }

            case ConcavePolygonShape3D:
            {
                GD.PrintErr(
                    "Jolt, the physics engine, does not support concave shapes, shape cannot be loaded from path: ",
                    path);
                return null;
            }

            // Probably don't need to support these weird types for a long time
            /*case HeightMapShape3D heightMapShape:
            case WorldBoundaryShape3D worldBoundaryShape:*/

            default:
                GD.PrintErr("Failed to load Godot physics shape for converting (unknown type): ", path);
                return null;
        }

        cache.WriteLoadedShape(path, density, cached);

        return cached;
    }

    /// <summary>
    ///   Gets the mass of this shape, unit size of normal density has mass of 1000 so in most cases the mass should be
    ///   divided by 1000 for processing purposes (though physics forces will work correctly with unadjusted values)
    /// </summary>
    /// <returns>The mass</returns>
    public float GetMass()
    {
        return NativeMethods.ShapeGetMass(AccessShapeInternal());
    }

    public uint GetSubShapeIndexFromData(uint subShapeData)
    {
        return NativeMethods.ShapeGetSubShapeIndex(AccessShapeInternal(), subShapeData);
    }

    /// <summary>
    ///   Calculates how much angular velocity this shape would get given the torque (based on this shapes rotational
    ///   inertia)
    /// </summary>
    /// <param name="torque">The raw torque to apply</param>
    /// <returns>Resulting angular velocities around the same axes as the torque was given in</returns>
    public Vector3 CalculateResultingTorqueFromInertia(Vector3 torque)
    {
        return NativeMethods.ShapeCalculateResultingAngularVelocity(AccessShapeInternal(), new JVecF3(torque));
    }

    /// <summary>
    ///   Calculates how much of a rotation around the y-axis is kept if applied to this shape
    /// </summary>
    /// <returns>A speed factor that is roughly around 0-1 range but doesn't follow any hard limits</returns>
    public float TestYRotationInertiaFactor()
    {
        // We give torque vector to apply to the shape and then compare
        // the resulting angular velocities to see how fast the shape can turn
        // We use a multiplier here to ensure the float values don't get very low very fast
        var torqueToTest = Vector3.Up * 1000;

        var velocities = CalculateResultingTorqueFromInertia(torqueToTest);

        // Detect how much torque was preserved
        var speedFraction = velocities.Y / torqueToTest.Y;
        speedFraction *= 1000;

        return speedFraction;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IntPtr AccessShapeInternal()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicsShape));

        return nativeInstance;
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            disposed = true;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.ReleaseShape(nativeInstance);
            nativeInstance = new IntPtr(0);
        }
    }
}

/// <summary>
///   Thrive native library methods related to physics shapes
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern IntPtr CreateBoxShape(float halfSideLength, float density);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateBoxShapeWithDimensions(JVecF3 halfDimensions, float density);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateSphereShape(float radius, float density);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateCylinderShape(float halfHeight, float radius, float density);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateCapsuleShape(float halfHeight, float radius, float density);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateMicrobeShapeConvex(in JVecF3 microbePoints, uint pointCount, float density,
        float scale, float thickness = 1);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateMicrobeShapeSpheres(in JVecF3 microbePoints, uint pointCount, float density,
        float scale);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateConvexShape(in JVecF3 convexPoints, uint pointCount, float density,
        float scale = 1, float convexRadius = 0.01f);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateStaticCompoundShape(in SubShapeDefinition subShapes, uint shapeCount);

    [DllImport("thrive_native")]
    internal static extern void ReleaseShape(IntPtr shape);

    [DllImport("thrive_native")]
    internal static extern float ShapeGetMass(IntPtr shape);

    [DllImport("thrive_native")]
    internal static extern uint ShapeGetSubShapeIndex(IntPtr shape, uint subShapeData);

    [DllImport("thrive_native")]
    internal static extern uint ShapeGetSubShapeIndexWithRemainder(IntPtr shape, uint subShapeData,
        out uint remainder);

    [DllImport("thrive_native")]
    internal static extern JVecF3 ShapeCalculateResultingAngularVelocity(IntPtr shape, JVecF3 appliedTorque,
        float deltaTime = 1);
}
