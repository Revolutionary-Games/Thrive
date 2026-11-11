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
    private nint nativeInstance;

    private PhysicalWorld(nint nativeInstance)
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

#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif
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

    public float LatestPhysicsDuration
    {
        get
        {
            var result = NativeMethods.PhysicalWorldGetPhysicsLatestTime(AccessWorldInternal());
#if DEBUG
            CheckIfDisposedAfterNativeCall();
#endif
            return result;
        }
    }

    /// <summary>
    ///   Average time in seconds that physics simulation runs take. A run may
    ///   process multiple fixed-time physics steps.
    /// </summary>
    public float AveragePhysicsDuration
    {
        get
        {
            var result = NativeMethods.PhysicalWorldGetPhysicsAverageTime(AccessWorldInternal());
#if DEBUG
            CheckIfDisposedAfterNativeCall();
#endif
            return result;
        }
    }

    /// <summary>
    ///   Used to turn off metrics reporting when the game is closing to no longer access metrics object which may be
    ///   disposed of already
    /// </summary>
    public bool DisablePhysicsTimeRecording { get; set; }

    public static PhysicalWorld Create()
    {
        return new PhysicalWorld(NativeMethods.CreatePhysicalWorld());
    }

    /// <summary>
    ///   Sets the timestep for the physics simulation. The timestep is in seconds.
    /// </summary>
    /// <param name="timestep">Timestep to set. Note invalid values are ignored.</param>
    public void SetPhysicsTimestep(float timestep)
    {
        NativeMethods.PhysicalWorldSetPhysicsTimestep(AccessWorldInternal(), timestep);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Steps the physics simulation forward if enough time has passed
    /// </summary>
    /// <param name="delta">Time since the last call of this method</param>
    /// <returns>True when at least one physics simulation step was performed</returns>
    public bool ProcessPhysics(float delta)
    {
        bool processed = NativeMethods.ProcessPhysicalWorld(AccessWorldInternal(), delta);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif

        if (processed && !DisablePhysicsTimeRecording)
        {
            if (DebugOverlays.Instance.PerformanceMetricsVisible)
                DebugOverlays.Instance.ReportPhysicalWorldStats(this);
        }

        return processed;
    }

    /// <summary>
    ///   Runs physics but in the background thread. Must call <see cref="WaitUntilPhysicsRunEnds"/> after before
    ///   continuing using the world.
    /// </summary>
    /// <param name="delta">
    ///   Amount of time elapsed since the last call, used to simulate the right amount of passed time
    /// </param>
    public void ProcessPhysicsOnBackgroundThread(float delta)
    {
        NativeMethods.ProcessPhysicalWorldInBackground(AccessWorldInternal(), delta);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif
    }

    public bool WaitUntilPhysicsRunEnds()
    {
        bool processed = NativeMethods.WaitForPhysicsToCompleteInPhysicalWorld(AccessWorldInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif

        if (processed && !DisablePhysicsTimeRecording)
        {
            if (DebugOverlays.Instance.PerformanceMetricsVisible)
                DebugOverlays.Instance.ReportPhysicalWorldStats(this);
        }

        return processed;
    }

    /// <summary>
    ///   Creates a new moving body
    /// </summary>
    /// <param name="shape">The shape of the body</param>
    /// <param name="position">Initial position of the body</param>
    /// <param name="rotation">Initial rotation of the body</param>
    /// <param name="addToWorld">
    ///   If false, then the body won't be automatically added to the world and <see cref="AddBody"/> needs to be
    ///   called
    /// </param>
    /// <returns>The created physics body instance</returns>
    public NativePhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quaternion rotation,
        bool addToWorld = true)
    {
        if (!position.IsFinite() || !rotation.IsFinite())
            throw new ArgumentException("Position and rotation must be finite");

#if DEBUG
        var mass = shape.GetMass();
        if (mass is <= 0 or float.NaN)
            throw new ArgumentException("Movable shape must have a valid mass", nameof(shape));
#endif

        var result = new NativePhysicsBody(NativeMethods.PhysicalWorldCreateMovingBody(AccessWorldInternal(),
            shape.AccessShapeInternal(), new JVec3(position), new JQuat(rotation), addToWorld));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        shape.CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    /// <summary>
    ///   Creates a moving body with axis locks. When <see cref="lockRotation"/> is on, the locked axis is the only
    ///   one around which rotation is allowed.
    /// </summary>
    /// <returns>The created physics body</returns>
    public NativePhysicsBody CreateMovingBodyWithAxisLock(PhysicsShape shape, Vector3 position, Quaternion rotation,
        Vector3 lockedAxes, bool lockRotation, bool addToWorld = true)
    {
        if (lockedAxes.LengthSquared() < MathUtils.EPSILON)
            throw new ArgumentException("Locked axes needs to specify at least one locked axis", nameof(lockedAxes));

        if (!position.IsFinite() || !rotation.IsFinite())
            throw new ArgumentException("Position and rotation must be finite");

#if DEBUG
        var mass = shape.GetMass();
        if (mass is <= 0 or float.NaN)
            throw new ArgumentException("Movable shape must have a valid mass", nameof(shape));
#endif

        var result = new NativePhysicsBody(NativeMethods.PhysicalWorldCreateMovingBodyWithAxisLock(
            AccessWorldInternal(), shape.AccessShapeInternal(), new JVec3(position), new JQuat(rotation),
            new JVecF3(lockedAxes), lockRotation, addToWorld));

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        shape.CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    public NativePhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quaternion rotation,
        bool addToWorld = true)
    {
        if (!position.IsFinite() || !rotation.IsFinite())
            throw new ArgumentException("Position and rotation must be finite");

        var result = new NativePhysicsBody(NativeMethods.PhysicalWorldCreateStaticBody(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), addToWorld));

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        shape.CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    public NativePhysicsBody CreateSensor(PhysicsShape shape, Vector3 position, Quaternion rotation,
        bool detectSleepingBodies, bool detectStaticBodies = false)
    {
        if (!position.IsFinite() || !rotation.IsFinite())
            throw new ArgumentException("Position and rotation must be finite");

        var result = new NativePhysicsBody(NativeMethods.PhysicalWorldCreateSensor(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), detectSleepingBodies, detectStaticBodies));

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        shape.CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    /// <summary>
    ///   Adds an existing body back to this world
    /// </summary>
    /// <param name="body">The body to add</param>
    /// <param name="activate">When true, the body is activated (wakes from sleep etc.)</param>
    public void AddBody(NativePhysicsBody body, bool activate = true)
    {
        NativeMethods.PhysicalWorldAddBody(AccessWorldInternal(), body.AccessBodyInternal(), activate);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Detaches a body from the world for adding back later with <see cref="AddBody"/>. Very different from
    ///   destroying the body.
    /// </summary>
    /// <param name="body">The body to detach</param>
    public void DetachBody(NativePhysicsBody body)
    {
        NativeMethods.PhysicalWorldDetachBody(AccessWorldInternal(), body.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Destroys a body entirely on the native side.
    /// </summary>
    /// <param name="body">Body to be destroyed immediately. No longer valid for any physics calls after this</param>
    /// <param name="dispose">
    ///   When true, the body is disposed of automatically. If false, the caller can call Dispose when it wants to or
    ///   not call it at all, which should be fine, but then the body wrapper object may exist for a long time.
    /// </param>
    public void DestroyBody(NativePhysicsBody body, bool dispose = true)
    {
        NativeMethods.DestroyPhysicalWorldBody(AccessWorldInternal(), body.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        // As the body will be forcefully destroyed, all the collision writing resources can be freed
        body.NotifyCollisionRecordingStopped();

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

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public Transform3D ReadBodyTransform(NativePhysicsBody body)
    {
        var data = ReadBodyPosition(body);

        return new Transform3D(new Basis(data.Rotation), data.Position);
    }

    public (Vector3 Position, Quaternion Rotation) ReadBodyPosition(NativePhysicsBody body)
    {
        // TODO: could probably make things a bit more efficient if the C# body stored the body ID to avoid one level
        // of indirection here (the indirection is maybe on the C++ side -hhyyrylainen)
        NativeMethods.ReadPhysicsBodyTransform(AccessWorldInternal(), body.AccessBodyInternal(),
            out var position, out var orientation);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        return (position, orientation);
    }

    public (Vector3 Velocity, Vector3 AngularVelocity) ReadBodyVelocity(NativePhysicsBody body)
    {
        NativeMethods.ReadPhysicsBodyVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            out var velocity, out var angularVelocity);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        return (velocity, angularVelocity);
    }

    /// <summary>
    ///   Give an impulse to a physics body. Note that this implies activation if the impulse is non-zero.
    /// </summary>
    /// <param name="body">Body to give impulse to</param>
    /// <param name="impulse">Impulse amount</param>
    /// <exception cref="ArgumentException">If the body is not valid</exception>
    public void GiveImpulse(NativePhysicsBody body, Vector3 impulse)
    {
        if (body.IsDetached)
            throw new ArgumentException("Cannot give impulse to a detached body");

#if DEBUG
        if (!impulse.IsFinite())
            throw new ArgumentException("Impulse must be finite");
#endif

        NativeMethods.GiveImpulse(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(impulse));

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void GiveAngularImpulse(NativePhysicsBody body, Vector3 angularImpulse)
    {
        if (body.IsDetached)
            throw new ArgumentException("Cannot give angular impulse to a detached body");

#if DEBUG
        if (!angularImpulse.IsFinite())
            throw new ArgumentException("Angular impulse must be finite");
#endif

        NativeMethods.GiveAngularImpulse(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(angularImpulse));

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Applies microbe movement control on a physics body. Note that there has to be at least one active physics
    ///   body (not sleeping) to have this apply. If there are no active physics bodies, this has no effect.
    /// </summary>
    /// <param name="body">The physics body to control</param>
    /// <param name="movementImpulse">World-space movement vector</param>
    /// <param name="lookDirection">Target look rotation</param>
    /// <param name="rotationSpeedDivisor">
    ///   How fast the body rotates to face <see cref="lookDirection"/>, higher values are slower
    /// </param>
    public void ApplyBodyMicrobeControl(NativePhysicsBody body, Vector3 movementImpulse, Quaternion lookDirection,
        float rotationSpeedDivisor)
    {
#if DEBUG
        if (!lookDirection.IsNormalized())
            throw new ArgumentException("Look direction needs to be normalized");

        if (rotationSpeedDivisor <= 0)
            throw new ArgumentException("Rotation speed can't be zero or negative");

        if (!movementImpulse.IsFinite() || !lookDirection.IsFinite())
            throw new ArgumentException("Movement impulse and direction must be finite");
#endif

        // Too low speed divisor causes too fast rotation and instability that way
        if (rotationSpeedDivisor < 0.01f)
            rotationSpeedDivisor = 0.01f;

        body.MicrobeControlEnabled = true;

        NativeMethods.SetBodyControl(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(movementImpulse), new JQuat(lookDirection), rotationSpeedDivisor);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
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

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        body.MicrobeControlEnabled = false;
    }

    public void SetBodyPosition(NativePhysicsBody body, Vector3 position)
    {
#if DEBUG
        if (!position.IsFinite())
            throw new ArgumentException("Position must be finite");
#endif

        NativeMethods.SetBodyPosition(AccessWorldInternal(), body.AccessBodyInternal(), new JVec3(position));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void SetBodyPositionAndRotation(NativePhysicsBody body, Vector3 position, Quaternion rotation)
    {
#if DEBUG
        if (!position.IsFinite() || !rotation.IsFinite())
            throw new ArgumentException("Position and rotation must be finite");
#endif

        NativeMethods.SetBodyPositionAndRotation(AccessWorldInternal(), body.AccessBodyInternal(), new JVec3(position),
            new JQuat(rotation));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Sets velocity (and angular velocity) for a body
    /// </summary>
    public void SetBodyVelocity(NativePhysicsBody body, Vector3 velocity, Vector3 angularVelocity)
    {
#if DEBUG
        if (!velocity.IsFinite() || !angularVelocity.IsFinite())
            throw new ArgumentException("Velocity and angular velocity must be finite");
#endif

        NativeMethods.SetBodyVelocityAndAngularVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(velocity), new JVecF3(angularVelocity));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Only sets velocity without affecting angular velocity. This should only be used if only velocity is wanted
    ///   to be changed as it is much less efficient to use this and <see cref="SetOnlyBodyAngularVelocity"/> than
    ///   calling the combined method <see cref="SetBodyVelocity"/>
    /// </summary>
    public void SetOnlyBodyVelocity(NativePhysicsBody body, Vector3 velocity)
    {
#if DEBUG
        if (!velocity.IsFinite())
            throw new ArgumentException("Velocity must be finite");
#endif

        NativeMethods.SetBodyVelocity(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(velocity));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void SetOnlyBodyAngularVelocity(NativePhysicsBody body, Vector3 angularVelocity)
    {
#if DEBUG
        if (!angularVelocity.IsFinite())
            throw new ArgumentException("Velocity must be finite");
#endif

        NativeMethods.SetBodyAngularVelocity(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(angularVelocity));
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void SetBodyAllowSleep(NativePhysicsBody body, bool allowSleep)
    {
        NativeMethods.SetBodyAllowSleep(AccessWorldInternal(), body.AccessBodyInternal(), allowSleep);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public bool FixBodyYCoordinateToZero(NativePhysicsBody body)
    {
        var result = NativeMethods.FixBodyYCoordinateToZero(AccessWorldInternal(), body.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    public void ChangeBodyShape(NativePhysicsBody body, PhysicsShape shape, bool activate = true)
    {
        NativeMethods.ChangeBodyShape(AccessWorldInternal(), body.AccessBodyInternal(),
            shape.AccessShapeInternal(), activate);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
        shape.CheckIfDisposedAfterNativeCall();
#endif
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
#if DEBUG
        if (!axis.IsFinite())
            throw new ArgumentException("Axis must be finite");
#endif

        NativeMethods.PhysicsBodyAddAxisLock(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(axis),
            lockRotation);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void SetBodyCollisionsEnabledState(NativePhysicsBody body, bool collisionsEnabled)
    {
        NativeMethods.PhysicsBodySetCollisionEnabledState(AccessWorldInternal(), body.AccessBodyInternal(),
            collisionsEnabled);
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void BodyIgnoreCollisionsWithBody(NativePhysicsBody body, NativePhysicsBody otherBody)
    {
        NativeMethods.PhysicsBodyAddCollisionIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            otherBody.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
        otherBody.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void BodyRemoveCollisionIgnoreWith(NativePhysicsBody body, NativePhysicsBody otherBody)
    {
        NativeMethods.PhysicsBodyRemoveCollisionIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            otherBody.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
        otherBody.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void BodyClearCollisionsIgnores(NativePhysicsBody body)
    {
        NativeMethods.PhysicsBodyClearCollisionIgnores(AccessWorldInternal(), body.AccessBodyInternal());
#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
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

#if DEBUG
                CheckIfDisposedAfterNativeCall();
                body.CheckIfDisposedAfterNativeCall();
                for (int i = 0; i < size; ++i)
                    ignoredBodies[i].CheckIfDisposedAfterNativeCall();
#endif
            }
            finally
            {
                pinHandle.Free();
            }
        }
        else
        {
            Span<nint> nativePointers = stackalloc nint[ignoredBodies.Count];

            for (int i = 0; i < size; ++i)
            {
                nativePointers[i] = ignoredBodies[i].AccessBodyInternal();
            }

            NativeMethods.PhysicsBodySetCollisionIgnores(AccessWorldInternal(), body.AccessBodyInternal(),
                MemoryMarshal.GetReference(nativePointers), size);

#if DEBUG
            CheckIfDisposedAfterNativeCall();
            body.CheckIfDisposedAfterNativeCall();
            for (int i = 0; i < size; ++i)
                ignoredBodies[i].CheckIfDisposedAfterNativeCall();
#endif
        }
    }

    public void BodySetCollisionIgnores(NativePhysicsBody body, NativePhysicsBody singleIgnoredBody)
    {
        NativeMethods.PhysicsBodyClearAndSetSingleIgnore(AccessWorldInternal(), body.AccessBodyInternal(),
            singleIgnoredBody.AccessBodyInternal());

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
        singleIgnoredBody.CheckIfDisposedAfterNativeCall();
#endif
    }

    public PhysicsCollision[] BodyStartCollisionRecording(NativePhysicsBody body, int maxRecordedCollisions,
        out nint receiverOfAddressOfCollisionCount)
    {
        if (maxRecordedCollisions < 1)
            throw new ArgumentException("Need to record at least one collision", nameof(maxRecordedCollisions));

        var (collisionsArray, arrayAddress) = body.SetupCollisionRecording(maxRecordedCollisions);

        receiverOfAddressOfCollisionCount = NativeMethods.PhysicsBodyEnableCollisionRecording(AccessWorldInternal(),
            body.AccessBodyInternal(), arrayAddress, maxRecordedCollisions);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        if (receiverOfAddressOfCollisionCount == nint.Zero)
        {
            GD.PrintErr("Failed to start collision recording, result count variable pointer is null");
            throw new Exception("Native side collision recording start failed");
        }

        return collisionsArray;
    }

    public void BodyStopCollisionRecording(NativePhysicsBody body)
    {
        NativeMethods.PhysicsBodyDisableCollisionRecording(AccessWorldInternal(), body.AccessBodyInternal());

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif

        body.NotifyCollisionRecordingStopped();
    }

    /// <summary>
    ///   Add a collision filter callback for a body
    /// </summary>
    /// <param name="body">The body</param>
    /// <param name="filterCallback">
    ///   The filter. Note that this callable *must be* kept alive externally for this to work safely
    /// </param>
    public void BodyAddCollisionFilter(NativePhysicsBody body, OnCollisionFilterCallback filterCallback)
    {
        NativeMethods.PhysicsBodyAddCollisionFilter(AccessWorldInternal(), body.AccessBodyInternal(), filterCallback);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void BodyDisableCollisionFilter(NativePhysicsBody body)
    {
        NativeMethods.PhysicsBodyDisableCollisionFilter(AccessWorldInternal(), body.AccessBodyInternal());

#if DEBUG
        CheckIfDisposedAfterNativeCall();
        body.CheckIfDisposedAfterNativeCall();
#endif
    }

    public void SetGravity(JVecF3? gravity = null)
    {
        gravity ??= new JVecF3(0.0f, -9.81f, 0.0f);

        NativeMethods.PhysicalWorldSetGravity(AccessWorldInternal(), gravity.Value);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif
    }

    public void RemoveGravity()
    {
        NativeMethods.PhysicalWorldRemoveGravity(AccessWorldInternal());

#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif
    }

    /// <summary>
    ///   Casts a ray from start to (start + directionAndLength) collecting all hit objects in results
    /// </summary>
    /// <param name="start">Start world point</param>
    /// <param name="directionAndLength">Vector to add to start to get to the end point</param>
    /// <param name="results">Will be filled with the hit objects. Needs to have a size greater than 0</param>
    /// <returns>The number of hits in results, all other array indexes are left untouched</returns>
    public int CastRayGetAllHits(Vector3 start, Vector3 directionAndLength, PhysicsRayWithUserData[] results)
    {
        var result = NativeMethods.PhysicalWorldCastRayGetAll(AccessWorldInternal(), new JVec3(start),
            new JVecF3(directionAndLength), ref results[0], results.Length);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif

        return result;
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
        var result = NativeMethods.PhysicalWorldDumpPhysicsState(AccessWorldInternal(), path);

#if DEBUG
        CheckIfDisposedAfterNativeCall();
#endif

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal nint AccessWorldInternal()
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

#if DEBUG
    /// <summary>
    ///   Checks for disposal or an invalid handle after a native call. This keeps the wrapper alive in debug builds,
    ///   so it cannot diagnose premature finalization in release builds or a native call that crashes before returning.
    /// </summary>
    private void CheckIfDisposedAfterNativeCall([CallerMemberName] string caller = "")
    {
        if (disposed || nativeInstance == nint.Zero)
        {
            GD.PrintErr("PhysicalWorld is destroyed while it is being used!");
            throw new InvalidOperationException($"PhysicalWorld native handle is invalid after {caller}");
        }
    }
#endif

    private void ReleaseUnmanagedResources()
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.DestroyPhysicalWorld(nativeInstance);
            nativeInstance = new nint(0);
        }
    }

    private void SetUpdatedDebugLevel(int level)
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.PhysicalWorldSetDebugDrawLevel(AccessWorldInternal(), level);
#if DEBUG
            CheckIfDisposedAfterNativeCall();
#endif
        }
    }

    private void UpdateDebugCameraInfo(Vector3 position)
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.PhysicalWorldSetDebugDrawCameraLocation(AccessWorldInternal(), new JVecF3(position));
#if DEBUG
            CheckIfDisposedAfterNativeCall();
#endif
        }
    }
}

/// <content>
///   Thrive native library methods related to physics worlds
/// </content>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern nint CreatePhysicalWorld();

    [DllImport("thrive_native")]
    internal static extern void DestroyPhysicalWorld(nint physicalWorld);

    [DllImport("thrive_native")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool ProcessPhysicalWorld(nint physicalWorld, float delta);

    [DllImport("thrive_native")]
    internal static extern void ProcessPhysicalWorldInBackground(nint physicalWorld, float delta);

    [DllImport("thrive_native")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool WaitForPhysicsToCompleteInPhysicalWorld(nint physicalWorld);

    [DllImport("thrive_native")]
    internal static extern nint PhysicalWorldCreateMovingBody(nint physicalWorld, nint shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern nint PhysicalWorldCreateMovingBodyWithAxisLock(nint physicalWorld, nint shape,
        JVec3 position, JQuat rotation, JVecF3 lockedAxes, bool lockRotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern nint PhysicalWorldCreateStaticBody(nint physicalWorld, nint shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern nint PhysicalWorldCreateSensor(nint physicalWorld, nint shape,
        JVec3 position, JQuat rotation, bool detectSleepingBodies,
        bool detectStaticBodies);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldAddBody(nint physicalWorld, nint body, bool activate);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldDetachBody(nint physicalWorld, nint body);

    [DllImport("thrive_native")]
    internal static extern void DestroyPhysicalWorldBody(nint physicalWorld, nint body);

    [DllImport("thrive_native")]
    internal static extern void SetPhysicsBodyLinearDamping(nint physicalWorld, nint body, float damping);

    [DllImport("thrive_native")]
    internal static extern void SetPhysicsBodyLinearAndAngularDamping(nint physicalWorld, nint body,
        float linearDamping, float angularDamping);

    [DllImport("thrive_native")]
    internal static extern void ReadPhysicsBodyTransform(nint world, nint body, [Out] out JVec3 position,
        [Out] out JQuat orientation);

    [DllImport("thrive_native")]
    internal static extern void ReadPhysicsBodyVelocity(nint world, nint body, [Out] out JVecF3 velocity,
        [Out] out JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void GiveImpulse(nint world, nint body, JVecF3 impulse);

    [DllImport("thrive_native")]
    internal static extern void GiveAngularImpulse(nint world, nint body, JVecF3 angularImpulse);

    [DllImport("thrive_native")]
    internal static extern void SetBodyControl(nint world, nint body, JVecF3 movementImpulse,
        JQuat targetRotation, float reachTargetInSeconds);

    [DllImport("thrive_native")]
    internal static extern void DisableBodyControl(nint world, nint body);

    [DllImport("thrive_native")]
    internal static extern void SetBodyPosition(nint world, nint body, JVec3 position, bool activate = true);

    [DllImport("thrive_native")]
    internal static extern void SetBodyPositionAndRotation(nint world, nint body, JVec3 position, JQuat rotation,
        bool activate = true);

    [DllImport("thrive_native")]
    internal static extern void SetBodyVelocity(nint world, nint body, JVecF3 velocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyAngularVelocity(nint world, nint body, JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyVelocityAndAngularVelocity(nint world, nint body, JVecF3 velocity,
        JVecF3 angularVelocity);

    [DllImport("thrive_native")]
    internal static extern void SetBodyAllowSleep(nint world, nint body, bool allowSleep);

    [DllImport("thrive_native")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool FixBodyYCoordinateToZero(nint world, nint body);

    [DllImport("thrive_native")]
    internal static extern void ChangeBodyShape(nint world, nint body, nint shape, bool activate);

    [DllImport("thrive_native")]
    internal static extern nint PhysicsBodyAddAxisLock(nint physicalWorld, nint body, JVecF3 axis,
        bool lockRotation);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodySetCollisionEnabledState(nint physicalWorld,
        nint body, bool collisionsEnabled);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyAddCollisionIgnore(nint physicalWorld, nint body,
        nint addIgnore);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyRemoveCollisionIgnore(nint physicalWorld, nint body,
        nint removeIgnore);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyClearCollisionIgnores(nint physicalWorld, nint body);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodySetCollisionIgnores(nint physicalWorld, nint body,
        in nint ignoredBodies, int count);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyClearAndSetSingleIgnore(nint physicalWorld,
        nint body, nint onlyIgnoredBody);

    [DllImport("thrive_native")]
    internal static extern nint PhysicsBodyEnableCollisionRecording(nint physicalWorld, nint body,
        nint collisionRecordingTarget, int maxRecordedCollisions);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyDisableCollisionRecording(nint physicalWorld, nint body);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyAddCollisionFilter(nint physicalWorld, nint body,
        PhysicalWorld.OnCollisionFilterCallback callback);

    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyDisableCollisionFilter(nint physicalWorld, nint body);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetGravity(nint physicalWorld, JVecF3 gravity);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldRemoveGravity(nint physicalWorld);

    [DllImport("thrive_native")]
    internal static extern int PhysicalWorldCastRayGetAll(nint physicalWorld, JVec3 start,
        JVecF3 endOffset, ref PhysicsRayWithUserData dataReceiver, int maxHits);

    [DllImport("thrive_native")]
    internal static extern float PhysicalWorldGetPhysicsLatestTime(nint physicalWorld);

    [DllImport("thrive_native")]
    internal static extern float PhysicalWorldGetPhysicsAverageTime(nint physicalWorld);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetPhysicsTimestep(nint physicalWorld, float timestep);

    [DllImport("thrive_native", CharSet = CharSet.Ansi, BestFitMapping = false)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool PhysicalWorldDumpPhysicsState(nint physicalWorld, string path);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetDebugDrawLevel(nint physicalWorld, int level);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetDebugDrawCameraLocation(nint physicalWorld, JVecF3 position);
}
