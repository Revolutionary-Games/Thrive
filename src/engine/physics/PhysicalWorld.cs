using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Wrapper for the native side physical world which is the main part of the physics simulation
/// </summary>
public class PhysicalWorld : IDisposable
{
    private bool disposed;
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

    public float LatestPhysicsDuration => NativeMethods.PhysicalWorldGetPhysicsLatestTime(AccessWorldInternal());

    /// <summary>
    ///   Time in seconds on average that physics simulation steps take
    /// </summary>
    public float AveragePhysicsDuration => NativeMethods.PhysicalWorldGetPhysicsAverageTime(AccessWorldInternal());

    public static PhysicalWorld Create()
    {
        return new PhysicalWorld(NativeMethods.CreatePhysicalWorld());
    }

    public bool ProcessPhysics(float delta)
    {
        return NativeMethods.ProcessPhysicalWorld(AccessWorldInternal(), delta);
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
    public PhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation, bool addToWorld = true)
    {
        return new PhysicsBody(NativeMethods.PhysicalWorldCreateMovingBody(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), addToWorld));
    }

    public PhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quat rotation, bool addToWorld = true)
    {
        return new PhysicsBody(NativeMethods.PhysicalWorldCreateStaticBody(AccessWorldInternal(),
            shape.AccessShapeInternal(),
            new JVec3(position), new JQuat(rotation), addToWorld));
    }

    public void AddBody(PhysicsBody body, bool activate = true)
    {
        NativeMethods.PhysicalWorldAddBody(AccessWorldInternal(), body.AccessBodyInternal(), activate);
    }

    public void DestroyBody(PhysicsBody body)
    {
        NativeMethods.DestroyPhysicalWorldBody(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void SetDamping(PhysicsBody body, float linearDamping, float? angularDamping = null)
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

    public Transform ReadBodyTransform(PhysicsBody body)
    {
        // TODO: could probably make things a bit more efficient if the C# body stored the body ID to avoid one level
        // of indirection here
        NativeMethods.ReadPhysicsBodyTransform(AccessWorldInternal(), body.AccessBodyInternal(),
            out JVec3 position, out JQuat orientation);

        return new Transform(new Basis(orientation), position);
    }

    public void GiveImpulse(PhysicsBody body, Vector3 impulse)
    {
        NativeMethods.GiveImpulse(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(impulse));
    }

    public void ApplyBodyMicrobeControl(PhysicsBody body, Vector3 movementImpulse, Quat lookDirection,
        float rotationSpeedDivisor)
    {
        if (rotationSpeedDivisor < 1)
            throw new ArgumentException("Rotation rate divisor needs to be 1 or above");

        NativeMethods.SetBodyControl(AccessWorldInternal(), body.AccessBodyInternal(),
            new JVecF3(movementImpulse), new JQuat(lookDirection), rotationSpeedDivisor);
    }

    public void DisableMicrobeBodyControl(PhysicsBody body)
    {
        NativeMethods.DisableBodyControl(AccessWorldInternal(), body.AccessBodyInternal());
    }

    public void SetBodyPosition(PhysicsBody body, Vector3 position)
    {
        NativeMethods.SetBodyPosition(AccessWorldInternal(), body.AccessBodyInternal(), new JVec3(position));
    }

    public bool FixBodyYCoordinateToZero(PhysicsBody body)
    {
        return NativeMethods.FixBodyYCoordinateToZero(AccessWorldInternal(), body.AccessBodyInternal());
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
    public void AddAxisLockConstraint(PhysicsBody body, Vector3 axis, bool lockRotation)
    {
        NativeMethods.PhysicsBodyAddAxisLock(AccessWorldInternal(), body.AccessBodyInternal(), new JVecF3(axis),
            lockRotation);
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

    public bool DumpPhysicsState(string path)
    {
        return NativeMethods.PhysicalWorldDumpPhysicsState(AccessWorldInternal(), path);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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
    internal static extern IntPtr PhysicalWorldCreateMovingBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicalWorldCreateStaticBody(IntPtr physicalWorld, IntPtr shape,
        JVec3 position, JQuat rotation, bool addToWorld);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldAddBody(IntPtr physicalWorld, IntPtr body, bool activate);

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
    internal static extern void GiveImpulse(IntPtr world, IntPtr body, JVecF3 impulse);

    [DllImport("thrive_native")]
    internal static extern void SetBodyControl(IntPtr world, IntPtr body, JVecF3 movementImpulse,
        JQuat targetRotation, float reachTargetInSeconds);

    [DllImport("thrive_native")]
    internal static extern void DisableBodyControl(IntPtr world, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern void SetBodyPosition(IntPtr world, IntPtr body, JVec3 position, bool activate = true);

    [DllImport("thrive_native")]
    internal static extern bool FixBodyYCoordinateToZero(IntPtr world, IntPtr body);

    [DllImport("thrive_native")]
    internal static extern IntPtr PhysicsBodyAddAxisLock(IntPtr physicalWorld, IntPtr body, JVecF3 axis,
        bool lockRotation);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldSetGravity(IntPtr physicalWorld, JVecF3 gravity);

    [DllImport("thrive_native")]
    internal static extern void PhysicalWorldRemoveGravity(IntPtr physicalWorld);

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
