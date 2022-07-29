using Godot;

/// <summary>
///   Base class for displaying photograph tasks
/// </summary>
public abstract class PhotographPreview : Control
{
    [Export]
    public NodePath TextureRectPath = null!;

    private TextureRect textureRect = null!;
    private ImageTask? task;
    private Texture loadingTexture = null!;

    public override void _Ready()
    {
        base._Ready();

        textureRect = GetNode<TextureRect>(TextureRectPath);
        loadingTexture = textureRect.Texture;
        textureRect.Texture = null;
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

    protected void UpdatePreview()
    {
        textureRect.Texture = loadingTexture;
        task = SetupImageTask();
    }

    protected abstract ImageTask? SetupImageTask();
}
