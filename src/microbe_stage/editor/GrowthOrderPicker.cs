using Godot;

/// <summary>
///   Allows picking the growth order of things in a GUI
/// </summary>
public partial class GrowthOrderPicker : Control
{
#pragma warning disable CA2213
    private PackedScene draggableItemScene = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        draggableItemScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/DraggableItem.tscn");
    }

    public override void _Process(double delta)
    {
    }
}
