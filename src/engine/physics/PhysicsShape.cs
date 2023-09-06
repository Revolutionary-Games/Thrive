using System;
using System.Buffers;
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

    // TODO: hashing and caching based on the parameters to avoid needing to constantly create new shapes
    public static PhysicsShape CreateMicrobeShape(ReadOnlySpan<JVecF3> organellePositions, float overallDensity,
        bool scaleAsBacteria, bool createAsSpheres = false)
    {
        if (createAsSpheres)
        {
            return new PhysicsShape(NativeMethods.CreateMicrobeShapeSpheres(
                MemoryMarshal.GetReference(organellePositions),
                (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
        }

        return new PhysicsShape(NativeMethods.CreateMicrobeShapeConvex(
            MemoryMarshal.GetReference(organellePositions),
            (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
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

        // TODO: pre-bake collision shapes for game export (the fallback conversion below should only need to be used
        // when debugging to make the release version perform better)

        var godotData = GD.Load<ConvexPolygonShape>(path);

        if (godotData == null)
        {
            // TODO: support for other shapes if we need them
            GD.PrintErr("Failed to load Godot physics shape for converting: ", path);
            return null;
        }

        var dataSource = godotData.Points;
        int points = dataSource.Length;

        if (points < 1)
            throw new NotSupportedException("Can't convert convex polygon with no points");

        // We need a temporary buffer in case the data byte format is not exactly right
        var pool = ArrayPool<JVecF3>.Shared;
        var buffer = pool.Rent(points);

        for (int i = 0; i < points; ++i)
        {
            buffer[i] = new JVecF3(dataSource[i]);
        }

        cached = new PhysicsShape(NativeMethods.CreateConvexShape(buffer[0], (uint)points, density));

        cache.WriteLoadedShape(path, density, cached);

        pool.Return(buffer);

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
    internal static extern IntPtr CreateMicrobeShapeConvex(in JVecF3 microbePoints, uint pointCount, float density,
        float scale);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateMicrobeShapeSpheres(in JVecF3 microbePoints, uint pointCount, float density,
        float scale);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateConvexShape(in JVecF3 convexPoints, uint pointCount, float density);

    [DllImport("thrive_native")]
    internal static extern void ReleaseShape(IntPtr shape);

    [DllImport("thrive_native")]
    internal static extern float ShapeGetMass(IntPtr shape);
}
