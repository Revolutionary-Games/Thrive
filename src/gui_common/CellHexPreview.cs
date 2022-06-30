using Godot;

public class CellHexPreview : Control
{
    [Export]
    public NodePath TextureRectPath = null!;

    private TextureRect textureRect = null!;
    private MicrobeSpecies? microbeSpecies;
    private ImageTask? task;

    public MicrobeSpecies? PreviewSpecies
    {
        get => microbeSpecies;
        set
        {
            microbeSpecies = value;
            UpdateHexPreview();
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

    private void UpdateHexPreview()
    {
        task = new ImageTask(new CellHexPhotoBuilder { Species = microbeSpecies });
        PhotoStudio.Instance.SubmitTask(task);
    }
}
