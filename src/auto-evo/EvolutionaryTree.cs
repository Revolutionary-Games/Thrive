using Godot;

public class EvolutionaryTree : Control
{
    private PackedScene treeNodeScene = null!;

    public override void _Ready()
    {
        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");
    }
}
