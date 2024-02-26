using Godot;

/// <summary>
///   Handles fade effects in the gallery's model viewer. For generic fades, see
///   <see cref="CrossFadableTextureRect"/> or <see cref="ScreenFade"/>.
/// </summary>
public partial class CrossFadableGalleryViewport : SubViewportContainer
{
#pragma warning disable CA2213
    private Tween tween = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void FadedEventHandler();

    [Export]
    public float FadeDuration { get; set; } = 0.5f;

    public override void _Ready()
    {
        tween = GetNode<Tween>("Tween");
    }

    public void BeginFade()
    {
        tween.InterpolateProperty(this, "modulate", null, Colors.Black, FadeDuration);
        tween.Start();

        tween.CheckAndConnect("tween_completed", new Callable(this, nameof(OnFaded)), null, (uint)ConnectFlags.OneShot);
    }

    private void OnFaded(GodotObject @object, NodePath key)
    {
        _ = @object;
        _ = key;

        EmitSignal(nameof(FadedEventHandler));

        tween.InterpolateProperty(this, "modulate", null, Colors.White, FadeDuration);
        tween.Start();
    }
}
