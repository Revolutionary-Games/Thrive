using Godot;

/// <summary>
///   Line helping the player by showing a direction
/// </summary>
public class GuidanceLine : ImmediateGeometry
{
    private Vector3 lineStart;

    private Vector3 lineEnd;

    private Color colour = Colors.White;

    private bool dirty = true;

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

    public override void _Ready()
    {
        base._Ready();

        // This material is needed for SetColor to work at all
        var material = new SpatialMaterial();
        material.VertexColorUseAsAlbedo = true;
        MaterialOverride = material;
    }

    public override void _Process(float delta)
    {
        if (!dirty)
            return;

        dirty = false;
        Clear();
        Begin(Mesh.PrimitiveType.Lines);

        SetColor(colour);
        AddVertex(LineStart);
        AddVertex(LineEnd);

        // TODO: if we want to have line thickness, we need to generate a quad here with the wanted *width* around the
        // points (we need to figure out the right rotation for the line at both ends for where to place those points
        // that are slightly off from the positions)

        End();
    }
}
