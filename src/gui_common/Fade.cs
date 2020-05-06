using System;
using Godot;

/// <summary>
///   Controls the screen fading
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
        FadeIn,
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

        if (FadeTransition == FadeType.FadeIn)
        {
            FadeToBlack();
        }
        else if (FadeTransition == FadeType.FadeOut)
        {
            FadeToWhite();
        }
    }

    public void OnFinished()
    {
        EmitSignal(nameof(OnFinishedSignal));
        QueueFree();
    }
}
