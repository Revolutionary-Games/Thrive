using System;
using System.Runtime.InteropServices;
using Godot;

public class PhysicalWorld : IDisposable
{
    private bool disposed;
    private IntPtr nativeInstance;

    private PhysicalWorld(IntPtr nativeInstance)
    {
        this.nativeInstance = nativeInstance;
    }

    ~PhysicalWorld()
    {
        Dispose(false);
    }

    public static PhysicalWorld Create()
    {
        return new PhysicalWorld(NativeMethods.CreatePhysicalWorld());
    }

    public bool ProcessPhysics(float delta)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        return NativeMethods.ProcessPhysicalWorld(nativeInstance, delta);
    }

    public PhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        return new PhysicsBody(NativeMethods.PhysicalWorldCreateMovingBody(nativeInstance, shape.AccessShapeInternal(),
            new NativeMethods.JVec3(position), new NativeMethods.JQuat(rotation)));
    }

    public PhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quat rotation)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        return new PhysicsBody(NativeMethods.PhysicalWorldCreateStaticBody(nativeInstance, shape.AccessShapeInternal(),
            new NativeMethods.JVec3(position), new NativeMethods.JQuat(rotation)));
    }

    public void DestroyBody(PhysicsBody body)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        NativeMethods.DestroyPhysicalWorldBody(nativeInstance, body.AccessBodyInternal());
    }

    public Transform ReadBodyTransform(PhysicsBody body)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        // TODO: could probably make things a bit more efficient if the C# body stored the body ID to avoid one level
        // of indirection here
        NativeMethods.ReadPhysicsBodyTransform(nativeInstance, body.AccessBodyInternal(),
            out NativeMethods.JVec3 position, out NativeMethods.JQuat orientation);

        return new Transform(new Basis(orientation.ToQuat()), position.ToVec3());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
            NativeMethods.DestroyPhysicalWorld(nativeInstance);
            nativeInstance = new IntPtr(0);
        }
    }
}

/// <summary>
///   Thrive native library methods related to physics worlds
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern IntPtr CreatePhysicalWorld();

    [DllImport("thrive_native")]
    internal static extern void DestroyPhysicalWorld(IntPtr physicalWorld);

    [DllImport("thrive_native")]
    internal static extern bool ProcessPhysicalWorld(IntPtr physicalWorld, float delta);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateMovingBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateStaticBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation);

    [DllImport("thrive_native")]
    internal static extern void ReadPhysicsBodyTransform(IntPtr world, IntPtr body, [Out] out JVec3 position,
        [Out] out JQuat orientation);

    [DllImport("thrive_native")]
    internal static extern void DestroyPhysicalWorldBody(IntPtr physicalWorld, IntPtr body);
}
