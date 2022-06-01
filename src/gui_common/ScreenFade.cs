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

    public bool Finished { get; private set; }

    public float FadeDuration { get; set; }

    public FadeType CurrentFadeType
    {
        get => currentFadeType;
        set
        {
            currentFadeType = value;
            SetInitialColours();
        }
    }

    public override void _Ready()
    {
        rect = GetNode<ColorRect>("Rect");
        fader = GetNode<Tween>("Fader");

        fader.Connect("tween_all_completed", this, nameof(OnFinished));

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;

        SetInitialColours();
        Hide();
    }

    public void FadeToBlack()
    {
        FadeTo(new Color(0, 0, 0, 1));
    }

    public void FadeToWhite()
    {
        FadeTo(new Color(0, 0, 0, 0));
    }

    public void FadeTo(Color final)
    {
        fader.InterpolateProperty(rect, "color", null, final, FadeDuration);

        fader.Start();
    }

    public void Begin()
    {
        Show();

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

    public void Skip()
    {
        OnFinished();
    }

    public void Clear()
    {
        this.DetachAndQueueFree();
    }

    private void SetInitialColours()
    {
        if (rect == null)
            return;

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

    private void OnFinished()
    {
        Finished = true;
    }
}
