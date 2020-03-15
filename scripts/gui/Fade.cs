using System;
using Godot;

/// <summary>
///   Controls the screen fading
/// </summary>
public class Fade : CanvasLayer
{
    public ColorRect Rect;
    public Tween Fader;

    [Signal]
    public delegate void FadeFinished();

    public override void _Ready()
    {
        Rect = GetNode<ColorRect>("Rect");
        Fader = GetNode<Tween>("Fader");
        Fader.Connect("tween_all_completed", this, "OnTweenCompleted");
    }

    public void FadeToBlack(float fadeDuration)
    {
        Fader.InterpolateProperty(Rect, "color", new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 1), fadeDuration);

        Fader.Start();
    }

    public void FadeToWhite(float fadeDuration)
    {
        Fader.InterpolateProperty(Rect, "color", new Color(0, 0, 0, 1),
            new Color(0, 0, 0, 0), fadeDuration);

        Fader.Start();
    }

    public void OnTweenCompleted()
    {
        EmitSignal(nameof(FadeFinished));
        QueueFree();
    }
}
