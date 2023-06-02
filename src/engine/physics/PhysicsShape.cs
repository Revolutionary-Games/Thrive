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
        return new PhysicsShape(NativeMethods.CreateBoxShapeWithDimensions(new NativeMethods.JVecF3(halfDimensions)));
    }

    public static PhysicsShape CreateSphere(float radius)
    {
        return new PhysicsShape(NativeMethods.CreateSphereShape(radius));
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
    internal static extern void ReleaseShape(IntPtr shape);
}
