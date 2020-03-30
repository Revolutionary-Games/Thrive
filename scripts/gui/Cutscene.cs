using System;
using Godot;

/// <summary>
///   Controls the cutscene
/// </summary>
public class Cutscene : CanvasLayer
{
    public VideoPlayer CutsceneVideoPlayer;
    public VideoStream CutsceneVideoStream;

    public Vector2 FrameSize;

    public bool AllowSkipping = true;

    [Signal]
    public delegate void CutsceneFinished();

    public override void _Ready()
    {
        CutsceneVideoPlayer = GetNode<VideoPlayer>("VideoPlayer");
        CutsceneVideoPlayer.Connect("finished", this, "OnStreamFinished");
        FrameSize = CutsceneVideoPlayer.RectSize;

        GetViewport().Connect("size_changed", this, "OnCutsceneResized");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") && AllowSkipping)
        {
            OnStreamFinished();
        }
    }

    /// <summary>
    ///   Keeps aspect ratio of the cutscene whenever
    ///   the window is being resized.
    /// </summary>
    public void OnCutsceneResized()
    {
        var currentSize = OS.WindowSize;

        // Scaling factors
        var scaleHeight = currentSize.x / FrameSize.x;
        var scaleWidth = currentSize.y / FrameSize.y;

        var scale = Math.Min(scaleHeight, scaleWidth);

        var newSize = new Vector2(FrameSize.x * scale,
            FrameSize.y * scale);

        // Adjust the cutscene size and center it
        CutsceneVideoPlayer.SetSize(newSize);
        CutsceneVideoPlayer.SetAnchorsAndMarginsPreset(
            Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);
    }

    public void OnStreamFinished()
    {
        EmitSignal(nameof(CutsceneFinished));
        QueueFree();
    }
}
