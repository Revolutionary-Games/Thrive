using Godot;

public class EvolutionaryTreeNode : TextureButton
{
    public int Generation { get; set; }

    public Species Species { get; set; } = null!;

    public bool LastGeneration { get; set; }

    public EvolutionaryTreeNode? ParentNode { get; set; }

    public Vector2 Center => RectPosition + RectSize / 2;
}
