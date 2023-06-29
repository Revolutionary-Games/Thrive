using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Physics body implemented by the Thrive native code library (this is a wrapper around that native handle)
/// </summary>
public class NativePhysicsBody : IDisposable, IEquatable<NativePhysicsBody>
{
    /// <summary>
    ///   This is only used by external code, not this class at all to know which bodies are in use without having to
    ///   allocate extra memory
    /// </summary>
    public bool Marked = true;

    private bool disposed;
    private IntPtr nativeInstance;

    internal NativePhysicsBody(IntPtr nativeInstance)
    {
        this.nativeInstance = nativeInstance;

        if (this.nativeInstance.ToInt64() == 0)
        {
            // TODO: should this crash the game?
            GD.PrintErr(
                "Physics body can't be created from null native pointer, we probably ran out of physics bodies");
        }
    }

    ~NativePhysicsBody()
    {
        Dispose(false);
    }

    public static bool operator ==(NativePhysicsBody? left, NativePhysicsBody? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NativePhysicsBody? left, NativePhysicsBody? right)
    {
        return !Equals(left, right);
    }

    public bool Equals(NativePhysicsBody? other)
    {
        if (other == null)
            return false;

        return nativeInstance.ToInt64() != 0 && nativeInstance == other.nativeInstance;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != this.GetType())
            return false;

        return Equals((NativePhysicsBody)obj);
    }

    public override int GetHashCode()
    {
        return nativeInstance.GetHashCode();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal IntPtr AccessBodyInternal()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(NativePhysicsBody));

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
            NativeMethods.ReleasePhysicsBodyReference(nativeInstance);
            nativeInstance = new IntPtr(0);
        }
    }
}

/// <summary>
///   Thrive native library methods related to bodies
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern void ReleasePhysicsBodyReference(IntPtr body);
}
