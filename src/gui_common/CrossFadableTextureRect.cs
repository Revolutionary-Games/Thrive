using Godot;

/// <summary>
///   Displays image that can be changed smoothly with fade.
/// </summary>
public partial class CrossFadableTextureRect : TextureRect
{
    private readonly NodePath modulationReference = new("modulate");

    // This class takes in external textures.
#pragma warning disable CA2213
    private Texture2D? image;
#pragma warning restore CA2213

    [Signal]
    public delegate void FadedEventHandler();

    /// <summary>
    ///   Image to be displayed. This fades the texture rect. To change the image without fading use
    ///   <see cref="TextureRect.Texture"/>. Note that this needs to be a texture managed elsewhere for destroying.
    /// </summary>
    public Texture2D? Image
    {
        get => image;
        set
        {
            image = value;
            UpdateImage();
        }
    }

    [Export]
    public double FadeDuration { get; set; } = 0.5;

    public override void _Ready()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            modulationReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateImage()
    {
        // Initial image display shouldn't fade
        if (Texture == null)
        {
            Texture = Image;
            return;
        }

        var tween = CreateTween();

        tween.TweenProperty(this, modulationReference, Colors.Black, FadeDuration);

        tween.TweenCallback(new Callable(this, nameof(OnFaded)));
    }

    private void OnFaded()
    {
        Texture = Image;
        EmitSignal(SignalName.Faded);

        var tween = CreateTween();
        tween.TweenProperty(this, modulationReference, Colors.White, FadeDuration);
    }
}
