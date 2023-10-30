using System;
using Godot;

/// <summary>
///   Handles drawing debug lines
/// </summary>
public class DebugDrawer : ControlWithInput
{
    /// <summary>
    ///   Needs to match what's defined in PhysicalWorld.hpp
    /// </summary>
    private const int MaxPhysicsDebugLevel = 7;

    /// <summary>
    ///   Assumption of what the vertex layout memory use is for immediate geometry (3 floats for position,
    ///   3 floats for normal, 2 floats for UVs, 4 floats for colour).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     It's really hard to find this in Godot source code so this is a pure assumption that has been tested to
    ///     work fine.
    ///   </para>
    /// </remarks>
    private const long MemoryUseOfIntermediateVertex = sizeof(float) * (3 + 3 + 2 + 4);

    // 2 vertices + space in index buffer
    private const long SingleLineDrawMemoryUse = MemoryUseOfIntermediateVertex * 2 + sizeof(uint);

    // 3 vertices
    private const long SingleTriangleDrawMemoryUse = MemoryUseOfIntermediateVertex * 3 + sizeof(uint);

    private static DebugDrawer? instance;

#pragma warning disable CA2213
    private ImmediateGeometry lineDrawer = null!;
    private ImmediateGeometry triangleDrawer = null!;
#pragma warning restore CA2213

    private int currentPhysicsDebugLevel;

    private bool physicsDebugSupported;
    private bool warnedAboutNotBeingSupported;
    private bool warnedAboutHittingMemoryLimit;

    // Note that only one debug draw geometry can be going on at once so drawing lines intermixed with triangles is
    // note very efficient
    private bool lineDrawStarted;
    private bool triangleDrawStarted;

    private long usedDrawMemory;
    private long drawMemoryLimit;
    private long extraNeededDrawMemory;

    private bool drawnThisFrame;

    private DebugDrawer()
    {
        instance = this;
    }

    public delegate void OnPhysicsDebugLevelChanged(int level);

    public delegate void OnPhysicsDebugCameraPositionChanged(Vector3 position);

    public event OnPhysicsDebugLevelChanged? OnPhysicsDebugLevelChangedHandler;
    public event OnPhysicsDebugCameraPositionChanged? OnPhysicsDebugCameraPositionChangedHandler;

    public static DebugDrawer Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int DebugLevel => currentPhysicsDebugLevel;
    public Vector3 DebugCameraLocation { get; private set; }

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
        lineDrawer = GetNode<ImmediateGeometry>("LineDrawer");
        triangleDrawer = GetNode<ImmediateGeometry>("TriangleDrawer");

        physicsDebugSupported = NativeInterop.RegisterDebugDrawer(DrawLine, DrawTriangle);

        // Make sure the debug stuff is always rendered
        lineDrawer.SetCustomAabb(new AABB(float.MinValue, float.MinValue, float.MinValue, float.MaxValue,
            float.MaxValue, float.MaxValue));
        triangleDrawer.SetCustomAabb(new AABB(float.MinValue, float.MinValue, float.MinValue, float.MaxValue,
            float.MaxValue, float.MaxValue));

        // TODO: implement debug text drawing (this is a Control to support that in the future)

        // Determine how much stuff we can draw before having all of the drawn stuff disappear
        var limit = ProjectSettings.Singleton.Get("rendering/limits/buffers/immediate_buffer_size_kb");

        if (limit == null)
        {
            GD.PrintErr("Unknown immediate geometry buffer size limit, can't draw debug lines");
        }
        else
        {
            drawMemoryLimit = (int)limit * 1024;
        }

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

        NativeInterop.RemoveDebugDrawer();
    }

    public override void _Process(float delta)
    {
        if (drawnThisFrame)
        {
            // Finish the geometry
            if (lineDrawStarted)
            {
                lineDrawStarted = false;
                lineDrawer.End();
            }

            if (triangleDrawStarted)
            {
                triangleDrawStarted = false;
                triangleDrawer.End();
            }

            lineDrawer.Visible = true;
            triangleDrawer.Visible = true;
            drawnThisFrame = false;

            // Send camera position to the debug draw for LOD purposes
            try
            {
                DebugCameraLocation = GetViewport().GetCamera().GlobalTranslation;

                OnPhysicsDebugCameraPositionChangedHandler?.Invoke(DebugCameraLocation);
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to send camera position to physics debug draw", e);
            }

            if (!warnedAboutHittingMemoryLimit && usedDrawMemory + SingleTriangleDrawMemoryUse * 100 >= drawMemoryLimit)
            {
                warnedAboutHittingMemoryLimit = true;

                // Put some extra buffer in the memory advice
                extraNeededDrawMemory += SingleTriangleDrawMemoryUse * 100;

                GD.PrintErr(
                    "Debug drawer hit immediate geometry memory limit (extra needed memory: " +
                    $"{extraNeededDrawMemory / 1024} KiB), some things were not rendered " +
                    "(this message won't repeat even if the problem occurs again)");
            }

            // This needs to reset here so that StartDrawingIfNotYetThisFrame gets called again
            usedDrawMemory = 0;
        }
        else if (currentPhysicsDebugLevel < 1)
        {
            lineDrawer.Visible = false;
            triangleDrawer.Visible = false;
        }
    }

    [RunOnKeyDown("d_physics_debug", Priority = -2)]
    public void IncrementPhysicsDebugLevel()
    {
        if (!physicsDebugSupported)
        {
            if (!warnedAboutNotBeingSupported)
            {
                GD.PrintErr("The version of the loaded native Thrive library doesn't support physics " +
                    "debug drawing, debug drawing will not be attempted");
                warnedAboutNotBeingSupported = true;
            }
        }
        else
        {
            currentPhysicsDebugLevel = (currentPhysicsDebugLevel + 1) % MaxPhysicsDebugLevel;

            GD.Print("Setting physics debug level to: ", currentPhysicsDebugLevel);

            OnPhysicsDebugLevelChangedHandler?.Invoke(currentPhysicsDebugLevel);
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, Color colour)
    {
        if (usedDrawMemory + SingleLineDrawMemoryUse >= drawMemoryLimit)
        {
            extraNeededDrawMemory += SingleLineDrawMemoryUse;
            return;
        }

        try
        {
            StartDrawingIfNotYetThisFrame();

            if (!lineDrawStarted)
            {
                if (triangleDrawStarted)
                {
                    triangleDrawStarted = false;
                    triangleDrawer.End();
                }

                lineDrawStarted = true;
                lineDrawer.Begin(Mesh.PrimitiveType.Lines);
            }

            lineDrawer.SetColor(colour);
            lineDrawer.AddVertex(from);
            lineDrawer.AddVertex(to);

            usedDrawMemory += SingleLineDrawMemoryUse;
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in debug drawing: ", e);
        }
    }

    private void DrawTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color colour)
    {
        if (usedDrawMemory + SingleTriangleDrawMemoryUse >= drawMemoryLimit)
        {
            extraNeededDrawMemory += SingleLineDrawMemoryUse;
            return;
        }

        try
        {
            StartDrawingIfNotYetThisFrame();

            if (!triangleDrawStarted)
            {
                if (lineDrawStarted)
                {
                    lineDrawStarted = false;
                    lineDrawer.End();
                }

                triangleDrawStarted = true;
                triangleDrawer.Begin(Mesh.PrimitiveType.Triangles);
            }

            triangleDrawer.SetColor(colour);

            triangleDrawer.AddVertex(vertex1);
            triangleDrawer.AddVertex(vertex2);
            triangleDrawer.AddVertex(vertex3);

            usedDrawMemory += SingleTriangleDrawMemoryUse;
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in debug drawing: ", e);
        }
    }

    private void StartDrawingIfNotYetThisFrame()
    {
        if (drawnThisFrame)
            return;

        lineDrawer.Clear();
        usedDrawMemory = 0;
        extraNeededDrawMemory = 0;
        lineDrawStarted = false;

        triangleDrawer.Clear();
        triangleDrawStarted = false;

        drawnThisFrame = true;
    }
}
