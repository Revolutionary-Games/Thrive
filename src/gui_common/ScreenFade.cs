using System;
using Godot;

/// <summary>
///   Controls the screen fade transition
/// </summary>
public partial class ScreenFade : Control, ITransition
{
    private readonly NodePath colorReference = new("color");

    private readonly Callable finishCallable;

#pragma warning disable CA2213
    private Tween? tween;
    private ColorRect? rect;
#pragma warning restore CA2213

    private FadeType currentFadeType;

    public ScreenFade()
    {
        finishCallable = new Callable(this, nameof(OnFinished));
    }

    [Signal]
    public delegate void OnFinishedSignalEventHandler();

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

    public double FadeDuration { get; set; }

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

        // Keep this node running while paused
        ProcessMode = ProcessModeEnum.Always;

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
        StopTween();

        tween = CreateTween();

        tween.TweenProperty(rect, colorReference, final, FadeDuration);

        tween.TweenCallback(finishCallable);
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

        SetToEndState();
    }

    public void Clear()
    {
        this.DetachAndQueueFree();
    }

    public void SetToEndState()
    {
        if (rect == null)
            throw new InvalidOperationException("Instance not initialized yet");

        StopTween();

        switch (CurrentFadeType)
        {
            case FadeType.FadeIn:
                rect.Color = new Color(0, 0, 0, 0);
                break;
            case FadeType.FadeOut:
                rect.Color = new Color(0, 0, 0, 1);
                break;
            default:
                GD.PrintErr("Unknown fade type to reach end state with");
                break;
        }

        Finished = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            colorReference.Dispose();
        }

        base.Dispose(disposing);
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

    private void StopTween()
    {
        if (tween != null)
        {
            if (tween.IsValid())
                tween.Kill();

            tween = null;
        }
    }
}
