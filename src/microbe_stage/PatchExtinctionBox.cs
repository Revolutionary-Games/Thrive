using Godot;

public class PatchExtinctionBox : PanelContainer
{
    [Export]
    public NodePath PatchMapDrawerPath;

    private PatchMapDrawer patchMapDrawer;

    public PatchMap Map { get; set; }

    public override void _Ready()
    {
        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchMapDrawer.Map = Map;
    }
}
