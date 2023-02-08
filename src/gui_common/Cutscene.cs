using Godot;

/// <summary>
///   Controls a video cutscene
/// </summary>
public class Cutscene : Control, ITransition
{
#pragma warning disable CA2213
    private VideoPlayer? cutsceneVideoPlayer;
#pragma warning restore CA2213

    private VideoStream? stream;
    private float volume;

    [Signal]
    public delegate void OnFinishedSignal();

    public bool Finished { get; private set; }

    public VideoStream? Stream
    {
        get => stream;
        set
        {
            stream = value;
            UpdateVideoPlayer();
        }
    }

    /// <summary>
    ///   The video player's volume in linear value.
    /// </summary>
    public float Volume
    {
        get => volume;
        set
        {
            volume = value;
            UpdateVideoPlayer();
        }
    }

    public override void _Ready()
    {
        cutsceneVideoPlayer = GetNode<VideoPlayer>("VideoPlayer");

        cutsceneVideoPlayer.Connect("finished", this, nameof(OnFinished));

        UpdateVideoPlayer();
        Hide();
    }

    public void Begin()
    {
        if (cutsceneVideoPlayer == null)
        {
            GD.PrintErr("Video player missing, can't play cutscene");
            return;
        }

        Show();
        cutsceneVideoPlayer.Play();
    }

    public void Skip()
    {
        OnFinished();
    }

    public void Clear()
    {
        this.DetachAndQueueFree();
    }

    private void UpdateVideoPlayer()
    {
        if (cutsceneVideoPlayer == null)
            return;

        cutsceneVideoPlayer.Stream = stream;
        cutsceneVideoPlayer.Volume = volume;
    }

    private void OnFinished()
    {
        Finished = true;
    }
}
