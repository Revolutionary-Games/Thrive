using Godot;

public class SpeciesPreview : Control
{
    [Export]
    public NodePath TextureRectPath = null!;

    private TextureRect textureRect = null!;
    private Species? previewSpecies;
    private ImageTask? task;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            if (previewSpecies == value)
                return;

            previewSpecies = value;

            if (previewSpecies != null)
                UpdatePreviewSpecies();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        textureRect = GetNode<TextureRect>(TextureRectPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (task?.Finished == true)
        {
            textureRect.Texture = task.FinalImage;
            task = null;
        }
    }

    private void UpdatePreviewSpecies()
    {
        if (previewSpecies is MicrobeSpecies microbeSpecies)
        {
            task = new ImageTask(microbeSpecies);
            PhotoStudio.Instance.SubmitTask(task);
        }
        else
        {
            GD.PrintErr("Unknown species type to preview: ", previewSpecies);
        }
    }
}
