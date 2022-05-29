using Godot;

public class PlaybackBar : HBoxContainer
{
    private HSlider? playbackSlider;
    private PlayButton playButton = null!;
    private Button stopButton = null!;

    private float playbackProgress;
    private bool dragging;
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

    public override void _ExitTree()
    {
        base._ExitTree();
        StopPlayback();
    }

    public override void _Ready()
    {
        playbackSlider = GetNode<HSlider>("PlaybackSlider");
        playButton = GetNode<PlayButton>("PlayButton");
        stopButton = GetNode<Button>("StopButton");

        UpdatePlaybackState();
        UpdateSlider();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (AudioPlayer?.Stream == null)
            return;

        PlaybackProgress = ProgressFromPlaybackPos(AudioPlayer.GetPlaybackPosition()).GetValueOrDefault();
        UpdatePlaybackState();
    }

    public void StartPlayback()
    {
        if (AudioPlayer == null)
            return;

        AudioPlayer.StreamPaused = false;
        AudioPlayer.Playing = true;
    }

    public void StopPlayback()
    {
        if (AudioPlayer == null)
            return;

        AudioPlayer.StreamPaused = true;
        AudioPlayer.Playing = false;
        AudioPlayer.Seek(0);
    }

    private float? ProgressFromPlaybackPos(float value)
    {
        return (value / AudioPlayer?.Stream.GetLength()) * (float?)playbackSlider?.MaxValue;
    }

    private float? PlaybackPosFromProgress(float value)
    {
        return (value * AudioPlayer?.Stream.GetLength()) / (float?)playbackSlider?.MaxValue;
    }

    private void UpdatePlaybackState()
    {
        playButton.Paused = !Playing;

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

        playbackSlider.Value = playbackProgress;
    }

    private void OnPlayButtonPressed(bool paused)
    {
        if (AudioPlayer?.Stream == null)
            return;

        AudioPlayer.StreamPaused = paused;

        if (!paused && !Playing)
            StartPlayback();
    }

    private void OnSliderInput(InputEvent @event)
    {
        // TODO: Explain this
        if (@event is InputEventMouseButton button && button.ButtonIndex == (int)ButtonList.Left)
            dragging = button.Pressed;
    }

    private void OnSliderChanged(float value)
    {
        // Only set the playback position if we really are dragging the slider
        if (dragging)
        {
            playbackProgress = value;
            AudioPlayer?.Seek(PlaybackPosFromProgress(value).GetValueOrDefault());
        }
        else if (!dragging && Playing && value == playbackSlider!.MaxValue)
        {
            StopPlayback();
        }
    }

    private void OnStopPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        StopPlayback();
    }
}
