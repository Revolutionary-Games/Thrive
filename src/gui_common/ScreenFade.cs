using Godot;

/// <summary>
///   Controls the screen fade transition
/// </summary>
public class ScreenFade : CanvasLayer, ITransition
{
    private ColorRect rect;
    private Tween fader;
    private Control controlNode;

    private FadeType currentFadeType;

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

    public bool Skippable { get; set; } = true;

    public bool Visible
    {
        get => controlNode.Visible;
        set => controlNode.Visible = value;
    }

    public float FadeDuration { get; set; }

    public FadeType CurrentFadeType
    {
        get => currentFadeType;
        set
        {
            currentFadeType = value;

            // Apply initial colors
            if (currentFadeType == FadeType.FadeIn)
            {
                rect.Color = new Color(0, 0, 0, 0);
            }
            else if (currentFadeType == FadeType.FadeOut)
            {
                rect.Color = new Color(0, 0, 0, 1);
            }
        }
    }

    public override void _Ready()
    {
        controlNode = GetNode<Control>("Control");
        rect = GetNode<ColorRect>("Control/Rect");
        fader = GetNode<Tween>("Control/Fader");

        fader.Connect("tween_all_completed", this, "OnFinished");

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
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
        rect.Color = initial;

        fader.InterpolateProperty(rect, "color", initial, final, FadeDuration);

        fader.Start();
    }

    public void OnStarted()
    {
        switch (CurrentFadeType)
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

        this.DetachAndQueueFree();
    }
}
