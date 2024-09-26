using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Handles drawing debug lines
/// </summary>
[GodotAutoload]
public partial class DebugDrawer : ControlWithInput
{
    private static DebugDrawer? instance;

    private readonly StringName enablePhysicsName = new("enable_physics_debug");
    private readonly StringName disablePhysicsName = new("disable_physics_debug");
    private readonly StringName incrementPhysicsName = new("increment_physics_debug_level");
    private readonly StringName debugLevelName = new("debug_level");
    private readonly StringName cameraPosition = new("debug_camera_location");

    private IntPtr nativeInstance;

    private bool physicsDebugSupported;
    private bool warnedAboutNotBeingSupported;

    private DebugDrawer()
    {
        if (Engine.IsEditorHint())
            return;

        instance = this;
    }

    public delegate void OnPhysicsDebugLevelChanged(int level);

    public delegate void OnPhysicsDebugCameraPositionChanged(Vector3 position);

    public event OnPhysicsDebugLevelChanged? OnPhysicsDebugLevelChangedHandler;
    public event OnPhysicsDebugCameraPositionChanged? OnPhysicsDebugCameraPositionChangedHandler;

    public static DebugDrawer Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int DebugLevel => Get(debugLevelName).AsInt32();
    public Vector3 DebugCameraLocation => Get(cameraPosition).AsVector3();
    public bool PhysicsDebugDrawAvailable => physicsDebugSupported;

    public static void DumpPhysicsState(PhysicalWorld world)
    {
        var path = ProjectSettings.GlobalizePath(Constants.PHYSICS_DUMP_PATH);

        GD.Print("Starting dumping of physics world state to: ", path);

        if (world.DumpPhysicsState(path))
        {
            GD.Print("Physics dump finished");
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
            return;

        // TODO: apparently there is a problem with C# script attached also to a node so we have to init here manually
        Call("init");

        Connect("OnPhysicsDebugLevelChanged", new Callable(this, nameof(OnPhysicsLevelChanged)));
        Connect("OnPhysicsDebugCameraPositionChanged", new Callable(this, nameof(OnPhysicsCameraLocationChanged)));

        try
        {
            // StringNames aren't stored in this method as these are called all just once
            var nativeCallResult = Call("get_native_instance");

            nativeInstance = new IntPtr(nativeCallResult.AsInt64());

            if (nativeInstance == IntPtr.Zero)
                throw new Exception("Returned native instance is null");
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to get native side of DebugDrawer: ", e);
        }

        var registerResult = Call("register_debug_draw");
        physicsDebugSupported = registerResult.AsBool();

        if (GetTree().DebugCollisionsHint)
        {
            GD.Print("Enabling physics debug drawing on next frame as debug for that was enabled on the scene tree");
            Invoke.Instance.Queue(IncrementPhysicsDebugLevel);
        }
        else
        {
            // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162
            if (Constants.AUTOMATICALLY_TURN_ON_PHYSICS_DEBUG_DRAW)
            {
                GD.Print("Starting with debug draw on due to debug draw constant being enabled");
                Invoke.Instance.Queue(IncrementPhysicsDebugLevel);
            }

            // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // This isn't meant to be re-attached after remove so cleanup is done here
        Call("remove_debug_draw");
    }

    public void DebugLine(Vector3 from, Vector3 to, Color color)
    {
        if (nativeInstance == IntPtr.Zero)
        {
            GD.PrintErr("Native side of debug draw not initialized");
            return;
        }

        NativeMethods.DebugDrawerAddLine(nativeInstance, from, to, color);
    }

    public void DebugPoint(Vector3 position, Color color)
    {
        if (nativeInstance == IntPtr.Zero)
        {
            GD.PrintErr("Native side of debug draw not initialized");
            return;
        }

        NativeMethods.DebugDrawerAddPoint(nativeInstance, position, color);
    }

    [RunOnKeyDown("d_physics_debug", Priority = -2)]
    public void IncrementPhysicsDebugLevel()
    {
        if (!PhysicsDebugDrawAvailable)
        {
            if (!warnedAboutNotBeingSupported)
            {
                GD.PrintErr("The version of the loaded native Thrive library doesn't support physics " +
                    "debug drawing, because it is not the debug version of the library, " +
                    "debug drawing will not be attempted");
                warnedAboutNotBeingSupported = true;
            }
        }
        else
        {
            Call(incrementPhysicsName);
        }
    }

    public void EnablePhysicsDebug()
    {
        Call(enablePhysicsName);
    }

    public void DisablePhysicsDebugLevel()
    {
        Call(disablePhysicsName);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            enablePhysicsName.Dispose();
            disablePhysicsName.Dispose();
            incrementPhysicsName.Dispose();
            debugLevelName.Dispose();
            cameraPosition.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnPhysicsLevelChanged(int level)
    {
        OnPhysicsDebugLevelChangedHandler?.Invoke(level);
    }

    private void OnPhysicsCameraLocationChanged(Vector3 position)
    {
        OnPhysicsDebugCameraPositionChangedHandler?.Invoke(position);
    }
}

/// <summary>
///   Native methods used directly by <see cref="DebugDrawer"/> for more performance when calling.
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_extension")]
    internal static extern void
        DebugDrawerAddLine(IntPtr drawerInstance, in Vector3 from, in Vector3 to, in Color colour);

    [DllImport("thrive_extension")]
    internal static extern void DebugDrawerAddPoint(IntPtr drawerInstance, in Vector3 position, in Color colour);
}
