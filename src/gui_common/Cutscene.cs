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

        ControlNode.Hide();
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
