using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Line helping the player by showing a direction
/// </summary>
public partial class GuidanceLine : MeshInstance3D
{
    private Vector3 lineStart;

    private Vector3 lineEnd;

    private Color colour = Colors.White;

    private float lineWidth = 0.3f;

    private bool dirty = true;

    private XoShiRo128plus rng = new();

    private float yOffset = 0.0f;

    // Assigned as a child resource so this should be disposed automatically
#pragma warning disable CA2213
    private ImmediateMesh mesh = null!;
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

            dirty = true;
            colour = value;
        }
    }

    [Export]
    public float LineWidth
    {
        get => lineWidth;
        set
        {
            if (lineWidth == value)
                return;

            dirty = true;
            lineWidth = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // ImmediateMesh is no longer a node type so this just needs to have one as a child
        mesh = new ImmediateMesh();
        Mesh = mesh;

        // Make the line update after any possible code that might update our parameters
        ProcessPriority = 800;
        ProcessMode = ProcessModeEnum.Always;

        // This material is needed for SetColor to work at all
        var material = new StandardMaterial3D();
        material.VertexColorUseAsAlbedo = true;
        MaterialOverride = material;
        yOffset = (float)rng.NextDouble() - 2.0f;
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

        mesh.SurfaceSetColor(colour);

        // To form quad, we want it in 'orgin + vector' form, not 'start + end' form
        // Be sure to flatten the Y-axis of the vector, so it's all on a 2D plane
        Vector3 lineVector = lineEnd - lineStart;
        lineVector[1] = 0.0f;

        // To get a vector that is at a right angle to the line in 2D
        // swap the coords and negate one term, then normalize.
        Vector3 lineNormal = new Vector3(-lineVector[2], 0.0f, lineVector[0]).Normalized();

        Vector3 yOffsetVector = new Vector3(0.0f, yOffset, 0.0f);

        mesh.SurfaceAddVertex(LineEnd + yOffsetVector);
        mesh.SurfaceAddVertex(LineStart + lineNormal * lineWidth + yOffsetVector);
        mesh.SurfaceAddVertex(LineStart - lineNormal * lineWidth + yOffsetVector);

        mesh.SurfaceEnd();
    }
}
