using System;
using Godot;

/// <summary>
///   Controls the cutscene
/// </summary>
public class Cutscene : CanvasLayer, ITransition
{
    public VideoPlayer CutsceneVideoPlayer;
    public Vector2 FrameSize;

    [Signal]
    public delegate void OnFinishedSignal();

    public Control ControlNode { get; private set; }

    public bool Skippable { get; set; } = true;

    public override void _Ready()
    {
        CutsceneVideoPlayer = GetNode<VideoPlayer>("Control/VideoPlayer");
        ControlNode = GetNode<Control>("Control");

        FrameSize = CutsceneVideoPlayer.RectSize;

        CutsceneVideoPlayer.Connect("finished", this, nameof(OnFinished));
        GetViewport().Connect("size_changed", this, nameof(OnCutsceneResized));

        // Initially adjust video player frame size
        OnCutsceneResized();

        ControlNode.Hide();
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

    public void OnStarted()
    {
        ControlNode.Show();
        CutsceneVideoPlayer.Play();
    }

    public void OnFinished()
    {
        EmitSignal(nameof(OnFinishedSignal));
        QueueFree();
    }
}
