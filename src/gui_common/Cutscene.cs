using Godot;

/// <summary>
///   Controls a video cutscene
/// </summary>
public class Cutscene : CanvasLayer, ITransition
{
    private VideoPlayer cutsceneVideoPlayer;
    private Control controlNode;

    [Signal]
    public delegate void OnFinishedSignal();

    public bool Skippable { get; set; } = true;

    public bool Visible
    {
        get => controlNode.Visible;
        set => controlNode.Visible = value;
    }

    public VideoStream Stream
    {
        get => cutsceneVideoPlayer.Stream;
        set => cutsceneVideoPlayer.Stream = value;
    }

    /// <summary>
    ///   The video player's volume in linear value.
    /// </summary>
    public float Volume
    {
        get => cutsceneVideoPlayer.Volume;
        set => cutsceneVideoPlayer.Volume = value;
    }

    public override void _Ready()
    {
        controlNode = GetNode<Control>("Control");
        cutsceneVideoPlayer = GetNode<VideoPlayer>("Control/VideoPlayer");

        cutsceneVideoPlayer.Connect("finished", this, nameof(OnFinished));
    }

    public void OnStarted()
    {
        cutsceneVideoPlayer.Play();
    }

    public void OnFinished()
    {
        EmitSignal(nameof(OnFinishedSignal));
        this.DetachAndQueueFree();
    }
}
