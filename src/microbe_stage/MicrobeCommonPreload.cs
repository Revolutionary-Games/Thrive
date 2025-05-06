using Godot;

/// <summary>
///   Handles loading common resources for preloading their graphics and shaders in the microbe stage
/// </summary>
public partial class MicrobeCommonPreload : Node3D
{
#pragma warning disable CA2213
    private GuidanceLine guidance = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        guidance = GetNode<GuidanceLine>("GuidanceLine");
    }

    public override void _Process(double delta)
    {
        var pos = GlobalPosition;
        guidance.LineStart = pos + Vector3.Left * 2;
        guidance.LineEnd = pos + Vector3.Right * 2;
    }
}
