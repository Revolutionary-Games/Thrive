using Godot;

/// <summary>
///   Handles fade effects in the gallery's model viewer. For generic fades, see
///   <see cref="CrossFadableTextureRect"/> or <see cref="ScreenFade"/>.
/// </summary>
public partial class CrossFadableGalleryViewport : SubViewportContainer
{
    private NodePath modulationReference = new("modulate");

    [Signal]
    public delegate void FadedEventHandler();

    [Export]
    public double FadeDuration { get; set; } = 0.5;

    public override void _Ready()
    {
    }

    public void BeginFade()
    {
        var tween = CreateTween();

        tween.TweenProperty(this, modulationReference, Colors.Black, FadeDuration);

        tween.TweenCallback(new Callable(this, nameof(OnFaded)));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            modulationReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnFaded(GodotObject @object, NodePath key)
    {
        _ = @object;
        _ = key;

        EmitSignal(SignalName.Faded);

        var tween = CreateTween();
        tween.TweenProperty(this, modulationReference, Colors.White, FadeDuration);
    }
}
