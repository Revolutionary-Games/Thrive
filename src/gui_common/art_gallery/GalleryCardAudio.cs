using Godot;

public class GalleryCardAudio : GalleryCard
{
    [Export]
    public NodePath PlaybackBarPath = null!;

    [Export]
    public NodePath PlayButtonPath = null!;

    private HSlider playbackBar = null!;
    private PlayButton playButton = null!;
    private AudioStreamPlayer audioPlayer = null!;

    private float scaledPlaybackPos;
    private bool dragging;

    public float ScaledPlaybackPosition => scaledPlaybackPos;

    public override void _Ready()
    {
        base._Ready();

        audioPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        playbackBar = GetNode<HSlider>(PlaybackBarPath);
        playButton = GetNode<PlayButton>(PlayButtonPath);

        audioPlayer.Stream = GD.Load<AudioStream>(Asset.ResourcePath);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (audioPlayer?.Stream == null)
            return;

        if (!audioPlayer.StreamPaused && audioPlayer.Playing)
        {
            scaledPlaybackPos = FixedScaleFromPlaybackPos(audioPlayer.GetPlaybackPosition());
            playbackBar.Value = scaledPlaybackPos;
        }
    }

    private float FixedScaleFromPlaybackPos(float value)
    {
        return (value / audioPlayer.Stream.GetLength()) * (float)playbackBar.MaxValue;
    }

    private float PlaybackPosFromFixedScale(float value)
    {
        return (value * audioPlayer.Stream.GetLength()) / (float)playbackBar.MaxValue;
    }

    private void OnPlayButtonPressed(bool paused)
    {
        if (audioPlayer?.Stream == null)
            return;

        if (paused)
        {
            audioPlayer.StreamPaused = true;
            scaledPlaybackPos = FixedScaleFromPlaybackPos(audioPlayer.GetPlaybackPosition());
            Jukebox.Instance.Resume();
        }
        else
        {
            audioPlayer.StreamPaused = false;
            audioPlayer.Play(PlaybackPosFromFixedScale(scaledPlaybackPos));
            Jukebox.Instance.Pause();
        }
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
            audioPlayer.Seek(PlaybackPosFromFixedScale(value));
    }

    private void OnAudioFinished()
    {
        playButton.Paused = true;
        audioPlayer.StreamPaused = false;
        audioPlayer.Stop();
        audioPlayer.Seek(0);
        scaledPlaybackPos = 0;
        playbackBar.Value = 0;
    }
}
