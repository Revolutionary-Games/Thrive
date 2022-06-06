using Godot;

public class GalleryCardAudio : GalleryCard
{
    [Export]
    public NodePath PlaybackBarPath = null!;

    private PlaybackBar? playbackBar;
    private AudioStreamPlayer ownPlayer = null!;

    [Signal]
    public delegate void OnAudioStarted();

    [Signal]
    public delegate void OnAudioStopped();

    public AudioStreamPlayer Player
    {
        get
        {
            EnsurePlayerExist();
            return ownPlayer;
        }
        set
        {
            ownPlayer = value;
            UpdatePlaybackBar();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        playbackBar = GetNode<PlaybackBar>(PlaybackBarPath);

        EnsurePlayerExist();
    }

    private void EnsurePlayerExist()
    {
        ownPlayer ??= new AudioStreamPlayer { Stream = GD.Load<AudioStream>(Asset.ResourcePath) };

        UpdatePlaybackBar();

        if (IsInsideTree() && !ownPlayer.IsInsideTree())
            AddChild(ownPlayer);
    }

    private void UpdatePlaybackBar()
    {
        if (playbackBar == null)
            return;

        playbackBar.AudioPlayer = ownPlayer;
    }

    private void OnStarted()
    {
        EmitSignal(nameof(OnAudioStarted));
    }

    private void OnStopped()
    {
        EmitSignal(nameof(OnAudioStopped));
    }
}
