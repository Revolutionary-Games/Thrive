﻿using System;
using Godot;

/// <summary>
///   Handles drawing debug lines
/// </summary>
public class DebugDrawer : ControlWithInput
{
#pragma warning disable CA2213
    [Export]
    public Material LineMaterial = null!;

    [Export]
    public Material TriangleMaterial = null!;
#pragma warning restore CA2213

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

    /// <summary>
    ///   Hides the debug drawing after this time of inactivity. Makes sure debug draw is still visible after a while
    ///   the game is paused, but will eventually clear up (for example if going to a part of the game that doesn't
    ///   use debug drawing)
    /// </summary>
    private const float HideAfterInactiveFor = 15.0f;

    private static DebugDrawer? instance;

#pragma warning disable CA2213
    private MeshInstance lineDrawer = null!;
    private MeshInstance triangleDrawer = null!;
#pragma warning restore CA2213

    private CustomImmediateMesh? lineMesh;
    private CustomImmediateMesh triangleMesh = null!;

    private int currentPhysicsDebugLevel;

    private bool physicsDebugSupported;
    private bool warnedAboutNotBeingSupported;
    private bool warnedAboutHittingMemoryLimit;

    private long usedDrawMemory;
    private long drawMemoryLimit;
    private long extraNeededDrawMemory;

    private bool drawnThisFrame;

    // As the data is not drawn each frame, there's a delay before hiding the draw result
    private float timeInactive;

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
    public bool PhysicsDebugDrawAvailable => physicsDebugSupported;

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
        lineDrawer = GetNode<MeshInstance>("LineDrawer");
        triangleDrawer = GetNode<MeshInstance>("TriangleDrawer");

        lineMesh = new CustomImmediateMesh(LineMaterial);
        triangleMesh = new CustomImmediateMesh(TriangleMaterial);

        // Make sure the debug stuff is always rendered
        float halfVisibility = Constants.DEBUG_DRAW_MAX_DISTANCE_ORIGIN * 0.5f;

        var quiteBigAABB = new AABB(-halfVisibility, -halfVisibility, -halfVisibility, halfVisibility * 2,
            halfVisibility * 2, halfVisibility * 2);

        lineMesh.CustomBoundingBox = quiteBigAABB;
        triangleMesh.CustomBoundingBox = quiteBigAABB;

        physicsDebugSupported = NativeInterop.RegisterDebugDrawer(DrawLine, DrawTriangle);

        lineDrawer.Mesh = lineMesh.Mesh;
        lineDrawer.Visible = false;

        triangleDrawer.Mesh = triangleMesh.Mesh;
        triangleDrawer.Visible = false;

        // TODO: implement debug text drawing (this is a Control to support that in the future)

        // Set a max limit to not draw way too much stuff and slow down things a ton
        drawMemoryLimit = Constants.MEBIBYTE * 4;

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
            timeInactive = 0;

            // Finish the geometry
            lineMesh!.Finish();
            triangleMesh.Finish();

            lineDrawer.Visible = true;
            triangleDrawer.Visible = true;
            drawnThisFrame = false;

            // Send camera position to the debug draw for LOD purposes
            try
            {
                var camera = GetViewport().GetCamera();

                if (camera != null)
                {
                    DebugCameraLocation = camera.GlobalTranslation;

                    OnPhysicsDebugCameraPositionChangedHandler?.Invoke(DebugCameraLocation);
                }
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

                GD.PrintErr("Debug drawer hit immediate geometry memory limit (extra needed memory: " +
                    $"{extraNeededDrawMemory / 1024} KiB), some things were not rendered " +
                    "(this message won't repeat even if the problem occurs again)");
            }

            // This needs to reset here so that StartDrawingIfNotYetThisFrame gets called again
            usedDrawMemory = 0;
            return;
        }

        timeInactive += delta;

        if (currentPhysicsDebugLevel < 1 || timeInactive > HideAfterInactiveFor)
        {
            lineDrawer.Visible = false;
            triangleDrawer.Visible = false;
        }
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
            currentPhysicsDebugLevel = (currentPhysicsDebugLevel + 1) % MaxPhysicsDebugLevel;

            GD.Print("Setting physics debug level to: ", currentPhysicsDebugLevel);

            OnPhysicsDebugLevelChangedHandler?.Invoke(currentPhysicsDebugLevel);
        }
    }

    public void EnablePhysicsDebug()
    {
        if (currentPhysicsDebugLevel == 0)
            IncrementPhysicsDebugLevel();
    }

    public void DisablePhysicsDebugLevel()
    {
        if (currentPhysicsDebugLevel == 0)
            return;

        currentPhysicsDebugLevel = 0;

        GD.Print("Disabling physics debug");

        OnPhysicsDebugLevelChangedHandler?.Invoke(currentPhysicsDebugLevel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (lineMesh != null)
            {
                lineMesh.Dispose();
                triangleMesh.Dispose();
            }
        }

        base.Dispose(disposing);
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

            lineMesh!.StartIfNeeded(Mesh.PrimitiveType.Lines);
            lineMesh.AddLine(from, to, colour);

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

            triangleMesh.StartIfNeeded(Mesh.PrimitiveType.Triangles);
            triangleMesh.AddTriangle(vertex1, vertex2, vertex3, colour);

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

        usedDrawMemory = 0;
        extraNeededDrawMemory = 0;

        drawnThisFrame = true;
    }
}
