using Godot;

/// <summary>
///   Line helping the player by showing a direction
/// </summary>
public class GuidanceLine : ImmediateGeometry
{
    private Vector2 lineStart;

    private Vector2 lineEnd;

    private bool dirty = true;

    [Export]
    public Vector2 LineStart
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
    public Vector2 LineEnd
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

        AddVertex(LineStart.ToVector3());
        AddVertex(LineEnd.ToVector3());

        End();
    }
}
