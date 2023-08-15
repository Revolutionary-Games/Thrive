using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DefaultEcs;
using Godot;

/// <summary>
///   Physics body implemented by the Thrive native code library (this is a wrapper around that native handle)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class NativePhysicsBody : IDisposable, IEquatable<NativePhysicsBody>
{
    /// <summary>
    ///   This is only used by external code, not this class at all to know which bodies are in use without having to
    ///   allocate extra memory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This being the first field makes the memory layout non-optimal but <see cref="activeCollisionCount"/> is
    ///     put as the next field to maybe help reduce the amount of padding a bit
    ///   </para>
    /// </remarks>
    public bool Marked = true;

    private static readonly ArrayPool<PhysicsCollision> CollisionDataBufferPool = ArrayPool<PhysicsCollision>.Create();

    private static readonly int EntityDataSize = Marshal.SizeOf<Entity>();

    private static readonly int OffsetToCollisionCount =
        Marshal.OffsetOf<NativePhysicsBody>(nameof(activeCollisionCount)).ToInt32();

    // Storage variables for collision recording, when these are active the pin handles are used to pin down these
    // pieces of memory to ensure the native code size can directly write here with pointers
    private int activeCollisionCount;
    private PhysicsCollision[]? activeCollisions;

    private GCHandle activeCollisionsPinHandle;
    private GCHandle activeCollisionCountPinHandle;

    private IntPtr nativeInstance;
    private bool disposed;

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

    /// <summary>
    ///   Active collisions for this body. Only updated if started through
    ///   <see cref="PhysicalWorld.BodyStartCollisionRecording"/>
    /// </summary>
    public PhysicsCollision[]? ActiveCollisions => activeCollisions;

    public static bool operator ==(NativePhysicsBody? left, NativePhysicsBody? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NativePhysicsBody? left, NativePhysicsBody? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///   Stores an entity in this body's user data for use in collision callbacks
    /// </summary>
    /// <param name="entity">The entity data to store</param>
    public void SetEntityReference(in Entity entity)
    {
        NativeMethods.PhysicsBodySetUserData(AccessBodyInternal(), entity, EntityDataSize);
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
        if (obj.GetType() != GetType())
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

    internal (IntPtr CollisionCountAddress, PhysicsCollision[] CollisionsArray, IntPtr ArrayAddress)
        SetupCollisionRecording(int maxCollisions)
    {
        // Ensure no previous state. This is safe as each physics body can only be recording one set of collisions
        // at once, so all of our very briefly dangling pointers will be fixed very soon after this method returns
        NotifyCollisionRecordingStopped();

        // Pin us so that the native code can write to our collision count field
        activeCollisionCountPinHandle = GCHandle.Alloc(this, GCHandleType.Pinned);

        activeCollisionCount = 0;

        activeCollisions = CollisionDataBufferPool.Rent(maxCollisions);
        activeCollisionsPinHandle = GCHandle.Alloc(activeCollisions, GCHandleType.Pinned);

        return (activeCollisionCountPinHandle.AddrOfPinnedObject() + OffsetToCollisionCount, activeCollisions,
            activeCollisionsPinHandle.AddrOfPinnedObject());
    }

    internal void NotifyCollisionRecordingStopped()
    {
        if (activeCollisions != null)
        {
            CollisionDataBufferPool.Return(activeCollisions);
            activeCollisions = null;

            activeCollisionsPinHandle.Free();
        }

        if (activeCollisionCountPinHandle.IsAllocated)
            activeCollisionCountPinHandle.Free();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            ForceStopCollisionRecording();

            NativeMethods.ReleasePhysicsBodyReference(nativeInstance);
            nativeInstance = new IntPtr(0);
        }
    }

    /// <summary>
    ///   Ensures that no pointers are left given to the native side that will become invalid after this object is
    ///   disposed
    /// </summary>
    private void ForceStopCollisionRecording()
    {
        if (activeCollisions == null)
            return;

        GD.PrintErr("Force stopping collision reporting! This should not happen when properly destroying bodies");

        NativeMethods.PhysicsBodyForceClearRecordingTargets(nativeInstance);

        NotifyCollisionRecordingStopped();
    }
}

/// <summary>
///   Thrive native library methods related to bodies
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern void ReleasePhysicsBodyReference(IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodySetUserData(IntPtr body, in Entity userData, int userDataSize);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyForceClearRecordingTargets(IntPtr body);
}
