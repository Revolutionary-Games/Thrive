using Godot;

/// <summary>
///   Handles fade effects in the gallery's model viewer. For generic fades, see
///   <see cref="CrossFadableTextureRect"/> or <see cref="ScreenFade"/>.
/// </summary>
public class CrossFadableGalleryViewport : ViewportContainer
{
    private bool fadeRequested;

#pragma warning disable CA2213
    private Tween tween = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void Faded();

    /// <summary>
    ///   Triggers the gallery model viewer's crossfade effect.
    /// </summary>
    public bool FadeRequested
    {
        set
        {
            fadeRequested = value;
            if (fadeRequested == false)
                return;

            BeginFade();
        }
    }

    [Export]
    public float FadeDuration { get; set; } = 0.5f;

    public override void _Ready()
    {
        tween = GetNode<Tween>("Tween");
    }

    private void BeginFade()
    {
        tween.InterpolateProperty(this, "modulate", null, Colors.Black, FadeDuration);
        tween.Start();

        tween.CheckAndConnect(
            "tween_completed", this, nameof(OnFaded), null, (uint)ConnectFlags.Oneshot);
    }

    private void OnFaded(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        EmitSignal(nameof(Faded));

        tween.InterpolateProperty(this, "modulate", null, Colors.White, FadeDuration);
        tween.Start();

        fadeRequested = false;
    }
}
