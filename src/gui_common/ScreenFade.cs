using Godot;

/// <summary>
///   Controls the screen fade transition
/// </summary>
public class ScreenFade : Control, ITransition
{
    private ColorRect rect = null!;
    private Tween fader = null!;

    private FadeType currentFadeType;

    [Signal]
    public delegate void OnFinishedSignal();

    public enum FadeType
    {
        /// <summary>
        ///   Screen fades to white (transparent)
        /// </summary>
        FadeIn,

        /// <summary>
        ///   Screen fades to black
        /// </summary>
        FadeOut,
    }

    public bool Skippable { get; set; } = true;

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
                rect.Color = new Color(0, 0, 0, 1);
            }
            else if (currentFadeType == FadeType.FadeOut)
            {
                rect.Color = new Color(0, 0, 0, 0);
            }
        }
    }

    public override void _Ready()
    {
        rect = GetNode<ColorRect>("Rect");
        fader = GetNode<Tween>("Fader");

        fader.Connect("tween_all_completed", this, nameof(OnFinished));

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
                FadeToWhite();
                break;
            case FadeType.FadeOut:
                FadeToBlack();
                break;
        }
    }

    public void OnFinished()
    {
        EmitSignal(nameof(OnFinishedSignal));

        this.DetachAndQueueFree();
    }
}
