using Godot;

public class MetaballTest : Node
{
    private MulticellularMetaballDisplayer metaballDisplayer = null!;

    public override void _Ready()
    {
        metaballDisplayer = GetNode<MulticellularMetaballDisplayer>("MulticellularMetaballDisplayer");
    }

    public override void _Process(float delta)
    {
    }
}
