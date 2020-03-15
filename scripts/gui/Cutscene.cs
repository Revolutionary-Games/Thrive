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

    [Signal]
    public delegate void CutsceneFinished();

    public override void _Ready()
    {
        CutsceneVideoPlayer = GetNode<VideoPlayer>("VideoPlayer");
        CutsceneVideoPlayer.Connect("finished", this, "OnStreamFinished");
        FrameSize = CutsceneVideoPlayer.RectSize;
    }

    public void OnStreamFinished()
    {
        EmitSignal(nameof(CutsceneFinished));
        QueueFree();
    }
}
