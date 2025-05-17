using System;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Line helping the player by showing a direction
/// </summary>
public partial class GuidanceLine : MeshInstance3D
{
    private readonly XoShiRo128plus random = new();

    private Vector3 lineStart;

    private Vector3 lineEnd;

    private Color colour = Colors.White;

    private float lineWidth = 0.3f;

    private bool dirty = true;

    private float yOffset;

    // Assigned as a child resource so this should be disposed of automatically
#pragma warning disable CA2213
    private ImmediateMesh mesh = null!;

    /// <summary>
    ///   The line material that needs to be set through the editor and made to use vertex colours
    /// </summary>
    [Export]
    private StandardMaterial3D lineMaterial = null!;
#pragma warning restore CA2213

    [Export]
    public Vector3 LineStart
    {
        get => lineStart;
        set
        {
            if (lineStart == value)
                return;

            dirty = true;
            lineStart = value;
        }
    }

    [Export]
    public Vector3 LineEnd
    {
        get => lineEnd;
        set
        {
            if (lineEnd == value)
                return;

            dirty = true;
            lineEnd = value;
        }
    }

    [Export]
    public Color Colour
    {
        get => colour;
        set
        {
            if (colour == value)
                return;

            colour = value;
            dirty = true;
        }
    }

    [Export]
    public float LineWidth
    {
        get => lineWidth;
        set
        {
            if (Math.Abs(lineWidth - value) < MathUtils.EPSILON)
                return;

            dirty = true;
            lineWidth = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // ImmediateMesh is no longer a node type, so this just needs to have one as a child
        mesh = new ImmediateMesh();
        Mesh = mesh;

        // Make the line update after any possible code that might update our parameters
        ProcessPriority = 800;
        ProcessMode = ProcessModeEnum.Always;

        // Maybe a separate resource that is just loaded here and then use the old vertex colour trick?
        // Should have preloading and displaying the standard material for the microbe stage to make sure there isn't
        // a lag spike when the line appears

        yOffset = random.NextFloat() - 2.0f;
    }

    public override void _Process(double delta)
    {
        if (!dirty)
            return;

        dirty = false;
        mesh.ClearSurfaces();

        // If there is no line to be drawn, don't draw one
        if (lineStart.IsEqualApprox(lineEnd))
            return;

        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        if (FeatureInformation.GetVideoDriver() != OS.RenderingDriver.Opengl3)
        {
            // This needs to be darkened a ton here to make up for the unlit rendering mode now used on the material
            // (which was changed to make exported game properly display the material)
            mesh.SurfaceSetColor(colour.Darkened(0.94f));
        }
        else
        {
            // Visibility is seriously bad with this mode, so don't adjust colour
            mesh.SurfaceSetColor(colour);
        }

        // To form quad, we want it in 'origin + vector' form, not 'start + end' form
        // Be sure to flatten the Y-axis of the vector, so it's all on a 2D plane
        Vector3 lineVector = lineEnd - lineStart;
        lineVector[1] = 0.0f;

        // To get a vector at a right angle to the line in 2D, swap the coords and negate one term, then normalise.
        Vector3 lineNormal = new Vector3(-lineVector[2], 0.0f, lineVector[0]).Normalized();

        Vector3 yOffsetVector = new Vector3(0.0f, yOffset, 0.0f);

        mesh.SurfaceAddVertex(LineEnd + yOffsetVector);
        mesh.SurfaceAddVertex(LineStart + lineNormal * lineWidth + yOffsetVector);
        mesh.SurfaceAddVertex(LineStart - lineNormal * lineWidth + yOffsetVector);

        mesh.SurfaceEnd();
        mesh.SurfaceSetMaterial(0, lineMaterial);
    }
}
