using Godot;

/// <summary>
///   Controls the screen fade
/// </summary>
public class Fade : CanvasLayer, ITransition
{
    public ColorRect Rect;
    public Tween Fader;

    public float FadeDuration;
    public FadeType FadeTransition;

    [Signal]
    public delegate void OnFinishedSignal();

    public enum FadeType
    {
        /// <summary>
        ///   Screen fades in
        /// </summary>
        FadeIn,

        /// <summary>
        ///   Screen fades out
        /// </summary>
        FadeOut,
    }

    public Control ControlNode { get; private set; }

    public bool Skippable { get; set; } = true;

    public override void _Ready()
    {
        ControlNode = GetNode<Control>("Control");
        Rect = GetNode<ColorRect>("Control/Rect");
        Fader = GetNode<Tween>("Control/Fader");
        Fader.Connect("tween_all_completed", this, "OnFinished");

        // Keep this node running even while paused
        PauseMode = PauseModeEnum.Process;

        ControlNode.Hide();
    }

    public void FadeToBlack()
    {
        FadeTo(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1));
    }

    public void FadeToWhite()
    {
        FadeTo(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0));
    }

    public void FadeTo(Color initial, Color final)
    {
        Rect.Color = initial;

        Fader.InterpolateProperty(Rect, "color", initial, final, FadeDuration);

        Fader.Start();
    }

    public void OnStarted()
    {
        ControlNode.Show();

        switch (FadeTransition)
        {
            case FadeType.FadeIn:
                FadeToBlack();
                break;
            case FadeType.FadeOut:
                FadeToWhite();
                break;
        }
    }

    public void OnFinished()
    {
        EmitSignal(nameof(OnFinishedSignal));

        QueueFree();
    }
}
