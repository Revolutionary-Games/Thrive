using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Wrapper for the native side physical world which is the main part of the physics simulation
/// </summary>
public class PhysicalWorld : IDisposable
{
    private bool disposed;
    private bool stackAllocWarned;
    private IntPtr nativeInstance;

    private PhysicalWorld(IntPtr nativeInstance)
    {
        this.nativeInstance = nativeInstance;

        var debugDrawer = DebugDrawer.Instance;
        debugDrawer.OnPhysicsDebugLevelChangedHandler += SetUpdatedDebugLevel;
        debugDrawer.OnPhysicsDebugCameraPositionChangedHandler += UpdateDebugCameraInfo;

        // Apply debug level set before this object was created (as we can't have received the signal about the
        // incremented debug level
        var currentDebugLevel = debugDrawer.DebugLevel;

        if (currentDebugLevel > 0)
        {
            SetUpdatedDebugLevel(currentDebugLevel);
            UpdateDebugCameraInfo(debugDrawer.DebugCameraLocation);
        }
    }

    ~PhysicalWorld()
    {
        Dispose(false);
    }

    /// <summary>
    ///   Callback to determine if a collision is allowed to happen. Note that the penetration amount is not
    ///   necessarily initialized if the callback is registered so that it doesn't want to calculate that info (as it
    ///   is not available without extra calculation when a collision begins)
    /// </summary>
    public delegate bool OnCollisionFilterCallback(ref PhysicsCollision collision);

    public float LatestPhysicsDuration => NativeMethods.PhysicalWorldGetPhysicsLatestTime(AccessWorldInternal());

    /// <summary>
    ///   Time in seconds on average that physics simulation steps take
    /// </summary>
    public float AveragePhysicsDuration => NativeMethods.PhysicalWorldGetPhysicsAverageTime(AccessWorldInternal());

    public static PhysicalWorld Create()
    {
        return new PhysicalWorld(NativeMethods.CreatePhysicalWorld());
    }

    /// <summary>
    ///   Steps the physics simulation forward, if enough time has passed
    /// </summary>
    /// <param name="delta">Time since the last call of this method</param>
    /// <returns>True when at least one physics simulation step was performed</returns>
    public bool ProcessPhysics(float delta)
    {
        bool processed = NativeMethods.ProcessPhysicalWorld(AccessWorldInternal(), delta);

        return processed;
    }

    /// <summary>
    ///   Runs physics but in the background thread. Must call <see cref="WaitUntilPhysicsRunEnds"/> after before
    ///   continuing using the world.
    /// </summary>
    /// <param name="delta">
    ///   Amount of time elapsed since the last call, used to simulate right amount of passed time
    /// </param>
    public void ProcessPhysicsOnBackgroundThread(float delta)
    {
        NativeMethods.ProcessPhysicalWorldInBackground(AccessWorldInternal(), delta);
    }

    public bool WaitUntilPhysicsRunEnds()
    {
        return NativeMethods.WaitForPhysicsToCompleteInPhysicalWorld(AccessWorldInternal());
    }

    /// <summary>
    ///   Creates a new moving body
    /// </summary>
    /// <param name="shape">The shape for the body</param>
    /// <param name="position">Initial position of the body</param>
    /// <param name="rotation">Initial rotation of the body</param>
    /// <param name="addToWorld">
    ///   If false then the body won't be automatically added to the world and <see cref="AddBody"/> needs to be called
    /// </param>
    /// <returns>The created physics body instance</returns>
    public NativePhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation,
        bool addToWorld = true)
    {
        return new NativePhysicsBody(NativeMethods.PhysicalWorldCreateMovingBody(AccessWorldInternal(),
            shape.AccessShapeInternal(), new JVec3(position), new JQuat(rotation), addToWorld));
    }

    /// <summary>
    ///   Creates a moving body with axis locks. When <see cref="lockRotation"/> is on, the locked axis is the only
    ///   one around which rotation is allowed.
    /// </summary>
    /// <returns>The created physics body</returns>
    public NativePhysicsBody CreateMovingBodyWithAxisLock(PhysicsShape shape, Vector3 position, Quat rotation,
        Vector3 lockedAxes, bool lockRotation, bool addToWorld = true)
    {
        if (lockedAxes.LengthSquared() < MathUtils.EPSILON)
            throw new ArgumentException("Locked axes needs to specify at least one locked axis", nameof(lockedAxes));

        return new NativePhysicsBody(NativeMethods.PhysicalWorldCreateMovingBodyWithAxisLock(AccessWorldInternal(),
            shape.AccessShapeInternal(), new JVec3(position), new JQuat(rotation), new JVecF3(lockedAxes), lockRotation,
            addToWorld));
    }

    public NativePhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quat rotation,
        bool addToWorld = true)
    {
        return new NativePhysicsBody(NativeMethods.PhysicalWorldCreateStaticBody(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), addToWorld));
    }

    public NativePhysicsBody CreateSensor(PhysicsShape shape, Vector3 position, Quat rotation,
        bool detectSleepingBodies, bool detectStaticBodies = false)
    {
        return new NativePhysicsBody(NativeMethods.PhysicalWorldCreateSensor(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), detectSleepingBodies, detectStaticBodies));
    }

    /// <summary>
    ///   Adds an existing body back to this world
    /// </summary>
    /// <param name="body">The body to add</param>
    /// <param name="activate">When true the body is activated (wakes from sleep etc.)</param>
    public void AddBody(NativePhysicsBody body, bool activate = true)
    {
        NativeMethods.PhysicalWorldAddBody(AccessWorldInternal(), body.AccessBodyInternal(), activate);
    }

    /// <summary>
    ///   Detaches a body from the world for adding back later with <see cref="AddBody"/>. Very different from
    ///   destroying the body.
    /// </summary>
    /// <param name="body">The body to detach</param>
    public void DetachBody(NativePhysicsBody body)
    {
        NativeMethods.PhysicalWorldDetachBody(AccessWorldInternal(), body.AccessBodyInternal());
    }

    /// <summary>
    ///   Destroys a body entirely on the native side.
    /// </summary>
    /// <param name="body">Body to be destroyed immediately. No longer valid for any physics calls after this</param>
    /// <param name="dispose">
    ///   When true the body is disposed automatically. If false the caller can call dispose when it wants to or
    ///   not call it at all, which should be fine but then the body wrapper object may exist for a long time.
    /// </param>
    public void DestroyBody(NativePhysicsBody body, bool dispose = true)
    {
        // As the body will be forcefully destroyed, all the collision writing resources can be freed
        body.NotifyCollisionRecordingStopped();

        NativeMethods.DestroyPhysicalWorldBody(AccessWorldInternal(), body.AccessBodyInternal());

        if (dispose)
            body.Dispose();
    }

    public void SetDamping(NativePhysicsBody body, float linearDamping, float? angularDamping = null)
    {
        if (angularDamping != null)
        {
            NativeMethods.SetPhysicsBodyLinearAndAngularDamping(AccessWorldInternal(), body.AccessBodyInternal(),
                linearDamping, angularDamping.Value);
        }
        else
        {
            NativeMethods.SetPhysicsBodyLinearDamping(AccessWorldInternal(), body.AccessBodyInternal(), linearDamping);
        }
    }

    public Transform ReadBodyTransform(NativePhysicsBody body)
    {
        var data = ReadBodyPosition(body);

        return new Transform(new Basis(data.Rotation), data.Position);
    }

    public (Vector3 Position, Quat Rotation) ReadBodyPosition(NativePhysicsBody body)
    {
        // TODO: could probably make things a bit more efficient if the C# body stored the body ID to avoid one level
        // of indirection here (the indirection is maybe on the C++ side -hhyyrylainen)
        NativeMethods.ReadPhysicsBodyTransform(AccessWorldInternal(), body.AccessBodyInternal(),
            out var position, out var orientation);

        return (position, orientation);
    }

    public (Vector3 Velocity, Vector3 AngularVelocity) ReadBodyVelocity(NativePhysicsBody body)
    {
        NativeMethods.ReadPhysicsBodyVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            out var velocity, out var angularVelocity);

        return (velocity, angularVelocity);
    }

    public void GiveImpulse(NativePhysicsBody body, Vector3 impulse)
    {
        NativeMethods.GiveImpulse(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(impulse));
    }

    public void GiveAngularImpulse(NativePhysicsBody body, Vector3 angularImpulse)
    {
        NativeMethods.GiveAngularImpulse(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(angularImpulse));
    }

    /// <summary>
    ///   Applies microbe movement control on a physics body. Note that there has to be at least one active physics
    ///   body (not sleeping) to have this apply. If there are no active physics bodies this has no effect.
    /// </summary>
    /// <param name="body">The physics body to control</param>
    /// <param name="movementImpulse">World-space movement vector</param>
    /// <param name="lookDirection">Target look rotation</param>
    /// <param name="rotationSpeedDivisor">
    ///   How fast the body rotates to face <see cref="lookDirection"/>, higher values are slower
    /// </param>
    public void ApplyBodyMicrobeControl(NativePhysicsBody body, Vector3 movementImpulse, Quat lookDirection,
        float rotationSpeedDivisor)
    {
#if DEBUG
        if (!lookDirection.IsNormalized())
            throw new ArgumentException("Look direction needs to be normalized");

        if (rotationSpeedDivisor <= 0)
            throw new ArgumentException("Rotation speed can't be zero or negative");
#endif

        // Too low speed divisor causes too fast rotation and instability that way
        if (rotationSpeedDivisor < 0.01f)
            rotationSpeedDivisor = 0.01f;

        body.MicrobeControlEnabled = true;

        NativeMethods.SetBodyControl(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(movementImpulse), new JQuat(lookDirection), rotationSpeedDivisor);
    }

    public void DisableMicrobeBodyControl(NativePhysicsBody body)
    {
        if (!body.MicrobeControlEnabled)
        {
            // Skip trying to disable if already disabled, this is done to not need an extra variable in places trying
            // to disable this to check first
            return;
        }

        NativeMethods.DisableBodyControl(AccessWorldInternal(), body.AccessBodyInternal());
        body.MicrobeControlEnabled = false;
    }

    public void SetBodyPosition(NativePhysicsBody body, Vector3 position)
    {
        NativeMethods.SetBodyPosition(AccessWorldInternal(), body.AccessBodyInternal(), new JVec3(position));
    }

    public void SetBodyPositionAndRotation(NativePhysicsBody body, Vector3 position, Quat rotation)
    {
        NativeMethods.SetBodyPositionAndRotation(AccessWorldInternal(), body.AccessBodyInternal(), new JVec3(position),
            new JQuat(rotation));
    }

    /// <summary>
    ///   Sets velocity for a body
    /// </summary>
    public void SetBodyVelocity(NativePhysicsBody body, Vector3 velocity, Vector3 angularVelocity)
    {
        NativeMethods.SetBodyVelocityAndAngularVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(velocity),
            new JVecF3(angularVelocity));
    }

    /// <summary>
    ///   Only sets velocity without affecting angular velocity. This should only be used if only velocity is wanted
    ///   to be changed as it is much less efficient to use this and <see cref="SetOnlyBodyAngularVelocity"/> than
    ///   calling the combined method <see cref="SetBodyVelocity"/>
    /// </summary>
    public void SetOnlyBodyVelocity(NativePhysicsBody body, Vector3 velocity)
    {
        NativeMethods.SetBodyVelocity(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(velocity));
    }

    public void SetOnlyBodyAngularVelocity(NativePhysicsBody body, Vector3 angularVelocity)
    {
        NativeMethods.SetBodyAngularVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(angularVelocity));
    }

    public void SetBodyAllowSleep(NativePhysicsBody body, bool allowSleep)
    {
        NativeMethods.SetBodyAllowSleep(AccessWorldInternal(), body.AccessBodyInternal(), allowSleep);
    }

    public bool FixBodyYCoordinateToZero(NativePhysicsBody body)
    {
        return NativeMethods.FixBodyYCoordinateToZero(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void ChangeBodyShape(NativePhysicsBody body, PhysicsShape shape, bool activate = true)
    {
        NativeMethods.ChangeBodyShape(AccessWorldInternal(), body.AccessBodyInternal(),
            shape.AccessShapeInternal(), activate);
    }

    /// <summary>
    ///   Makes this body unable to move on the given axis. Used to make microbes move only in a 2D plane. Call after
    ///   the body is added to the world.
    /// </summary>
    /// <param name="body">The body to add the axis lock on</param>
    /// <param name="axis">
    ///   The axis to lock this body to, for example <see cref="Vector3.Up"/> for microbe stage objects
    /// </param>
    /// <param name="lockRotation">When true also locks rotation to only happen around the given axis</param>
    public void AddAxisLockConstraint(NativePhysicsBody body, Vector3 axis, bool lockRotation)
    {
        NativeMethods.PhysicsBodyAddAxisLock(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(axis),
            lockRotation);
    }

    public void SetBodyCollisionsEnabledState(NativePhysicsBody body, bool collisionsEnabled)
    {
        NativeMethods.PhysicsBodySetCollisionEnabledState(AccessWorldInternal(), body.AccessBodyInternal(),
            collisionsEnabled);
    }

    public void BodyIgnoreCollisionsWithBody(NativePhysicsBody body, NativePhysicsBody otherBody)
    {
        NativeMethods.PhysicsBodyAddCollisionIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            otherBody.AccessBodyInternal());
    }

    public void BodyRemoveCollisionIgnoreWith(NativePhysicsBody body, NativePhysicsBody otherBody)
    {
        NativeMethods.PhysicsBodyRemoveCollisionIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            otherBody.AccessBodyInternal());
    }

    public void BodyClearCollisionsIgnores(NativePhysicsBody body)
    {
        NativeMethods.PhysicsBodyClearCollisionIgnores(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void BodySetCollisionIgnores(NativePhysicsBody body, IReadOnlyList<NativePhysicsBody> ignoredBodies)
    {
        // Optimization if the list is empty
        if (ignoredBodies.Count < 1)
        {
            BodyClearCollisionsIgnores(body);
            return;
        }

        var size = ignoredBodies.Count;

        if (size * 8 > Constants.MAX_STACKALLOC)
        {
            if (!stackAllocWarned)
            {
                GD.PrintErr("Stackalloc not usable due to size of collision ignore list, performance problem");
                stackAllocWarned = true;
            }

            // Less efficient, simpler approach here as this is not meant to be triggered in any sensible use
            var array = ignoredBodies.Select(b => b.AccessBodyInternal()).ToArray();

            var pinHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            try
            {
                NativeMethods.PhysicsBodySetCollisionIgnores(AccessWorldInternal(), body.AccessBodyInternal(),
                    pinHandle.AddrOfPinnedObject(), size);
            }
            finally
            {
                pinHandle.Free();
            }
        }
        else
        {
            Span<IntPtr> nativePointers = stackalloc IntPtr[ignoredBodies.Count];

            for (int i = 0; i < size; ++i)
            {
                nativePointers[i] = ignoredBodies[i].AccessBodyInternal();
            }

            NativeMethods.PhysicsBodySetCollisionIgnores(AccessWorldInternal(), body.AccessBodyInternal(),
                MemoryMarshal.GetReference(nativePointers), size);
        }
    }

    public void BodySetCollisionIgnores(NativePhysicsBody body, NativePhysicsBody singleIgnoredBody)
    {
        NativeMethods.PhysicsBodyClearAndSetSingleIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            singleIgnoredBody.AccessBodyInternal());
    }

    public PhysicsCollision[] BodyStartCollisionRecording(NativePhysicsBody body, int maxRecordedCollisions,
        out IntPtr receiverOfAddressOfCollisionCount)
    {
        if (maxRecordedCollisions < 1)
            throw new ArgumentException("Need to record at least one collision", nameof(maxRecordedCollisions));

        var (collisionsArray, arrayAddress) =
            body.SetupCollisionRecording(maxRecordedCollisions);

        receiverOfAddressOfCollisionCount = NativeMethods.PhysicsBodyEnableCollisionRecording(AccessWorldInternal(),
            body.AccessBodyInternal(), arrayAddress, maxRecordedCollisions);

        if (receiverOfAddressOfCollisionCount == IntPtr.Zero)
        {
            GD.PrintErr("Failed to start collision recording, result count variable pointer is null");
            throw new Exception("Native side collision recording start failed");
        }

        return collisionsArray;
    }

    public void BodyStopCollisionRecording(NativePhysicsBody body)
    {
        body.NotifyCollisionRecordingStopped();
        NativeMethods.PhysicsBodyDisableCollisionRecording(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void BodyAddCollisionFilter(NativePhysicsBody body, OnCollisionFilterCallback filterCallback)
    {
        NativeMethods.PhysicsBodyAddCollisionFilter(AccessWorldInternal(), body.AccessBodyInternal(), filterCallback);
    }

    public void BodyDisableCollisionFilter(NativePhysicsBody body)
    {
        NativeMethods.PhysicsBodyDisableCollisionFilter(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void SetGravity(JVecF3? gravity = null)
    {
        gravity ??= new JVecF3(0.0f, -9.81f, 0.0f);

        NativeMethods.PhysicalWorldSetGravity(AccessWorldInternal(), gravity.Value);
    }

    public void RemoveGravity()
    {
        NativeMethods.PhysicalWorldRemoveGravity(AccessWorldInternal());
    }

    /// <summary>
    ///   Casts a ray from start to (start + directionAndLength) collecting all hit objects in results
    /// </summary>
    /// <param name="start">Start world point</param>
    /// <param name="directionAndLength">Vector to add to start to get to the end point</param>
    /// <param name="results">Will be filled with the hit objects. Needs to have size greater than 0</param>
    /// <returns>The number of hits in results, all other array indexes are left untouched</returns>
    public int CastRayGetAllHits(Vector3 start, Vector3 directionAndLength, PhysicsRayWithUserData[] results)
    {
        return NativeMethods.PhysicalWorldCastRayGetAll(AccessWorldInternal(), new JVec3(start),
            new JVecF3(directionAndLength), ref results[0], results.Length);
    }

    /// <summary>
    ///   Variant of raycast that automatically rents an array from the buffer pool, which must be returned with
    ///   <see cref="ReturnRayCastBuffer"/> after use.
    /// </summary>
    public int CastRayGetAllHits(Vector3 start, Vector3 directionAndLength, int maxHits,
        out PhysicsRayWithUserData[] results)
    {
        results = ArrayPool<PhysicsRayWithUserData>.Shared.Rent(maxHits);

        return CastRayGetAllHits(start, directionAndLength, results);
    }

    /// <summary>
    ///   Return a buffer from raycasting
    /// </summary>
    public void ReturnRayCastBuffer(PhysicsRayWithUserData[] buffer)
    {
        ArrayPool<PhysicsRayWithUserData>.Shared.Return(buffer);
    }

    public bool DumpPhysicsState(string path)
    {
        return NativeMethods.PhysicalWorldDumpPhysicsState(AccessWorldInternal(), path);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IntPtr AccessWorldInternal()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicalWorld));

        return nativeInstance;
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            DebugDrawer.Instance.OnPhysicsDebugLevelChangedHandler -= SetUpdatedDebugLevel;

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

    private void SetUpdatedDebugLevel(int level)
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.PhysicalWorldSetDebugDrawLevel(AccessWorldInternal(), level);
        }
    }

    private void UpdateDebugCameraInfo(Vector3 position)
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.PhysicalWorldSetDebugDrawCameraLocation(AccessWorldInternal(), new JVecF3(position));
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
    internal static extern void ProcessPhysicalWorldInBackground(IntPtr physicalWorld, float delta);

    [DllImport("thrive_native")]
    internal static extern bool WaitForPhysicsToCompleteInPhysicalWorld(IntPtr physicalWorld);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateMovingBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateMovingBodyWithAxisLock(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, JVecF3 lockedAxes, bool lockRotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateStaticBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateSensor(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, bool detectSleepingBodies,
        bool detectStaticBodies);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldAddBody(IntPtr physicalWorld, IntPtr body, bool activate);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldDetachBody(IntPtr physicalWorld, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void DestroyPhysicalWorldBody(IntPtr physicalWorld, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void SetPhysicsBodyLinearDamping(IntPtr physicalWorld, IntPtr body, float damping);

    [DllImport("thrive_native")]
    internal static extern void SetPhysicsBodyLinearAndAngularDamping(IntPtr physicalWorld, IntPtr body,
        float linearDamping, float angularDamping);

    [DllImport("thrive_native")]
    internal static extern void ReadPhysicsBodyTransform(IntPtr world, IntPtr body, [Out] out JVec3 position,
        [Out] out JQuat orientation);

    [DllImport("thrive_native")]
    internal static extern void ReadPhysicsBodyVelocity(IntPtr world, IntPtr body, [Out] out JVecF3 velocity,
        [Out] out JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void GiveImpulse(IntPtr world, IntPtr body, JVecF3 impulse);

    [DllImport("thrive_native")]
    internal static extern void GiveAngularImpulse(IntPtr world, IntPtr body, JVecF3 angularImpulse);

    [DllImport("thrive_native")]
    internal static extern void SetBodyControl(IntPtr world, IntPtr body, JVecF3 movementImpulse,
        JQuat targetRotation, float reachTargetInSeconds);

    [DllImport("thrive_native")]
    internal static extern void DisableBodyControl(IntPtr world, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void SetBodyPosition(IntPtr world, IntPtr body, JVec3 position, bool activate = true);

    [DllImport("thrive_native")]
    internal static extern void SetBodyPositionAndRotation(IntPtr world, IntPtr body, JVec3 position, JQuat rotation,
        bool activate = true);

    [DllImport("thrive_native")]
    internal static extern void SetBodyVelocity(IntPtr world, IntPtr body, JVecF3 velocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyAngularVelocity(IntPtr world, IntPtr body, JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyVelocityAndAngularVelocity(IntPtr world, IntPtr body, JVecF3 velocity,
        JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyAllowSleep(IntPtr world, IntPtr body, bool allowSleep);

    [DllImport("thrive_native")]
    internal static extern bool FixBodyYCoordinateToZero(IntPtr world, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void ChangeBodyShape(IntPtr world, IntPtr body, IntPtr shape, bool activate);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicsBodyAddAxisLock(IntPtr physicalWorld, IntPtr body, JVecF3 axis,
        bool lockRotation);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodySetCollisionEnabledState(IntPtr physicalWorld,
        IntPtr body, bool collisionsEnabled);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyAddCollisionIgnore(IntPtr physicalWorld, IntPtr body,
        IntPtr addIgnore);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyRemoveCollisionIgnore(IntPtr physicalWorld, IntPtr body,
        IntPtr removeIgnore);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyClearCollisionIgnores(IntPtr physicalWorld, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodySetCollisionIgnores(IntPtr physicalWorld, IntPtr body,
        in IntPtr ignoredBodies, int count);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyClearAndSetSingleIgnore(IntPtr physicalWorld,
        IntPtr body, IntPtr onlyIgnoredBody);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicsBodyEnableCollisionRecording(IntPtr physicalWorld, IntPtr body,
        IntPtr collisionRecordingTarget, int maxRecordedCollisions);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyDisableCollisionRecording(IntPtr physicalWorld, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyAddCollisionFilter(IntPtr physicalWorld, IntPtr body,
        PhysicalWorld.OnCollisionFilterCallback callback);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyDisableCollisionFilter(IntPtr physicalWorld, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetGravity(IntPtr physicalWorld, JVecF3 gravity);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldRemoveGravity(IntPtr physicalWorld);

    [DllImport("thrive_native")]
    internal static extern int PhysicalWorldCastRayGetAll(IntPtr physicalWorld, JVec3 start,
        JVecF3 endOffset, ref PhysicsRayWithUserData dataReceiver, int maxHits);

    [DllImport("thrive_native")]
    internal static extern float PhysicalWorldGetPhysicsLatestTime(IntPtr physicalWorld);

    [DllImport("thrive_native")]
    internal static extern float PhysicalWorldGetPhysicsAverageTime(IntPtr physicalWorld);

    [DllImport("thrive_native", CharSet = CharSet.Ansi, BestFitMapping = false)]
    internal static extern bool PhysicalWorldDumpPhysicsState(IntPtr physicalWorld, string path);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetDebugDrawLevel(IntPtr physicalWorld, int level);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetDebugDrawCameraLocation(IntPtr physicalWorld, JVecF3 position);
}
