using Godot;

public class GalleryCardAudio : GalleryCard
{
    [Export]
    public NodePath PlaybackBarPath = null!;

    [Signal]
    public delegate void OnAudioStarted();

    [Signal]
    public delegate void OnAudioStopped();

    private PlaybackBar? playbackBar;
    private AudioStreamPlayer ownPlayer = null!;

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
