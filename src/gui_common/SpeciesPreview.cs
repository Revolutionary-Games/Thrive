using Godot;

public class SpeciesPreview : Control
{
    [Export]
    public NodePath CameraPath = null!;

    [Export]
    public NodePath EntityParentPath = null!;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            previewSpecies = value;
            UpdatePreviewSpecies();
        }
    }

    private Camera camera = null!;
    private Node entityParent = null!;
    private Species? previewSpecies;
    private PackedScene microbeScene = null!;

    public override void _Ready()
    {
        camera = GetNode<Camera>(CameraPath);
        entityParent = GetNode(EntityParentPath);
        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    private void UpdatePreviewSpecies()
    {
        // Remove old ones
        foreach (Node child in entityParent.GetChildren())
        {
            child.DetachAndQueueFree();
        }

        switch (previewSpecies)
        {
            case MicrobeSpecies:
            {
                var displayedMicrobe = microbeScene.Instance<Microbe>();
                displayedMicrobe.IsForPreviewOnly = true;
                entityParent.AddChild(displayedMicrobe);
                displayedMicrobe.ApplySpecies(previewSpecies);
                break;
            }
        }
    }
}
