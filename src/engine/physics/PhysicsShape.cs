using System;
using System.Runtime.InteropServices;
using Godot;

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

    public static PhysicsShape CreateBox(float halfSideLength)
    {
        return new PhysicsShape(NativeMethods.CreateBoxShape(halfSideLength));
    }

    public static PhysicsShape CreateBox(Vector3 halfDimensions)
    {
        return new PhysicsShape(NativeMethods.CreateBoxShapeWithDimensions(new JVecF3(halfDimensions)));
    }

    public static PhysicsShape CreateSphere(float radius)
    {
        return new PhysicsShape(NativeMethods.CreateSphereShape(radius));
    }

    // TODO: hashing and caching based on the parameters to avoid needing to constantly create new shapes
    public static PhysicsShape CreateMicrobeShape(JVecF3[] organellePositions, float overallDensity,
        bool scaleAsBacteria, bool createAsSpheres = false)
    {
        var gch = GCHandle.Alloc(organellePositions, GCHandleType.Pinned);

        PhysicsShape result;
        try
        {
            if (createAsSpheres)
            {
                result = new PhysicsShape(NativeMethods.CreateMicrobeShapeSpheres(gch.AddrOfPinnedObject(),
                    (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
            }
            else
            {
                result = new PhysicsShape(NativeMethods.CreateMicrobeShapeConvex(gch.AddrOfPinnedObject(),
                    (uint)organellePositions.Length, overallDensity, scaleAsBacteria ? 0.5f : 1));
            }
        }
        finally
        {
            gch.Free();
        }

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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
    internal static extern IntPtr CreateBoxShape(float halfSideLength);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateBoxShapeWithDimensions(JVecF3 halfDimensions);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateSphereShape(float radius);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateMicrobeShapeConvex(IntPtr microbePoints, uint pointCount, float density,
        float scale);

    [DllImport("thrive_native")]
    internal static extern IntPtr CreateMicrobeShapeSpheres(IntPtr microbePoints, uint pointCount, float density,
        float scale);

    [DllImport("thrive_native")]
    internal static extern void ReleaseShape(IntPtr shape);
}
