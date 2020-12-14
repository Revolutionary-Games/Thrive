using Godot;

/// <summary>
///   Line helping the player by showing a direction
/// </summary>
public class GuidanceLine : ImmediateGeometry
{
    private Vector3 lineStart;

    private Vector3 lineEnd;

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

    public override void _Process(float delta)
    {
        if (!dirty)
            return;

        dirty = false;
        Clear();
        Begin(Mesh.PrimitiveType.Lines);

        AddVertex(LineStart);
        AddVertex(LineEnd);

        End();
    }
}
