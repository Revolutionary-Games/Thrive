using System;
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
        Rect.Color = new Color(0, 0, 0, 0);

        Fader.InterpolateProperty(Rect, "color", new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 1), FadeDuration);

        Fader.Start();
    }

    public void FadeToWhite()
    {
        Rect.Color = new Color(0, 0, 0, 1);

        Fader.InterpolateProperty(Rect, "color", new Color(0, 0, 0, 1),
            new Color(0, 0, 0, 0), FadeDuration);

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
            default:
                break;
        }
    }

    public void OnFinished()
    {
        // TODO: find a better solution
        EmitSignal(nameof(OnFinishedSignal));

        QueueFree();
    }
}
