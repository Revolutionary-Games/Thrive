using Godot;

/// <summary>
///   A window dialog for tutorials with custom behaviors such as show (pop-up) delay and animation.
/// </summary>
/// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
/// [Tool]
public class TutorialDialog : CustomDialog
{
    private Tween tween = new();

    /// <summary>
    ///   Tweakable delay to make tutorial sequences flow more naturally.
    /// </summary>
    [Export]
    public float ShowDelay { get; set; } = 0.7f;

    public override void _Ready()
    {
        AddChild(tween);
    }

    public override void CustomShow()
    {
        // Don't animate if currently running inside the editor
        if (Engine.EditorHint)
            return;

        RectPivotOffset = RectSize / 2;
        RectScale = Vector2.Zero;

        tween.InterpolateProperty(
            this, "rect_scale", Vector2.Zero, Vector2.One, 0.3f, Tween.TransitionType.Expo,
            Tween.EaseType.Out, ShowDelay);
        tween.Start();
    }
}
