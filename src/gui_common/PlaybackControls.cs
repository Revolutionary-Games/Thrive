using Godot;

/// <summary>
///   Controls for manipulating <see cref="AudioStreamPlayer"/>'s playback.
/// </summary>
public class PlaybackControls : HBoxContainer
{
#pragma warning disable CA2213
    private HSlider? playbackSlider;
    private PlayButton? playButton;
    private Button? stopButton;
#pragma warning restore CA2213

    private float playbackProgress;
    private bool sliderAutoUpdate;
    private bool? lastState;

    [Signal]
    public delegate void Started();

    [Signal]
    public delegate void Stopped();

    public AudioStreamPlayer? AudioPlayer { get; set; }

    public float PlaybackProgress
    {
        get => playbackProgress;
        private set
        {
            if (playbackProgress == value)
                return;

            playbackProgress = value;
            UpdateSlider();
        }
    }

    public bool Playing
    {
        get
        {
            if (AudioPlayer == null)
                return false;

            return AudioPlayer.Playing && !AudioPlayer.StreamPaused;
        }
    }

    public override void _Ready()
    {
        playbackSlider = GetNode<HSlider>("PlaybackSlider");
        playButton = GetNode<PlayButton>("PlayButton");
        stopButton = GetNode<Button>("StopButton");

        UpdatePlaybackState();
        UpdateSlider();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        StopPlayback();
    }

    public override void _Process(float delta)
    {
        if (AudioPlayer?.Stream == null)
            return;

        PlaybackProgress = ProgressFromPlaybackPos(AudioPlayer.GetPlaybackPosition());
        UpdatePlaybackState();
    }

    public void StartPlayback()
    {
        if (Playing)
            return;

        if (AudioPlayer == null)
        {
            GD.PrintErr("Playback is requested to be started but audio player is missing");
            return;
        }

        AudioPlayer.StreamPaused = false;
        AudioPlayer.Play(PlaybackPosFromProgress(playbackProgress));
    }

    public void StopPlayback()
    {
        if (!Playing)
            return;

        if (AudioPlayer == null)
        {
            GD.PrintErr("Playback is requested to be stopped but audio player is missing");
            return;
        }

        AudioPlayer.StreamPaused = true;
        AudioPlayer.Playing = false;
        AudioPlayer.Seek(0);
    }

    private float ProgressFromPlaybackPos(float value)
    {
        if (AudioPlayer == null || playbackSlider == null)
        {
            GD.PrintErr("Elements are missing to correctly convert playback position to playback progress");
            return 0;
        }

        return (float)((value / AudioPlayer.Stream.GetLength()) * playbackSlider.MaxValue);
    }

    private float PlaybackPosFromProgress(float value)
    {
        if (AudioPlayer == null || playbackSlider == null)
        {
            GD.PrintErr("Elements are missing to correctly convert playback progress to playback position");
            return 0;
        }

        return (float)((value * AudioPlayer.Stream.GetLength()) / playbackSlider.MaxValue);
    }

    private void UpdatePlaybackState()
    {
        if (playButton == null || stopButton == null)
            return;

        playButton.Paused = !Playing;
        stopButton.Disabled = !Playing;

        if (Playing != lastState)
        {
            if (!Playing)
            {
                EmitSignal(nameof(Stopped));
            }
            else
            {
                EmitSignal(nameof(Started));
            }

            lastState = Playing;
        }
    }

    private void UpdateSlider()
    {
        if (playbackSlider == null)
            return;

        sliderAutoUpdate = true;
        playbackSlider.Value = playbackProgress;
        sliderAutoUpdate = false;
    }

    private void OnPlayButtonPressed(bool paused)
    {
        if (AudioPlayer?.Stream == null)
            return;

        AudioPlayer.StreamPaused = paused;

        if (!paused && !Playing)
            StartPlayback();
    }

    private void OnSliderChanged(float value)
    {
        if (sliderAutoUpdate)
            return;

        playbackProgress = value;
        AudioPlayer?.Seek(PlaybackPosFromProgress(value));
    }

    private void OnStopPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        StopPlayback();
    }
}
