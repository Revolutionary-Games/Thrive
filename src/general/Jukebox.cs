using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages playing music. Autoload singleton
/// </summary>
public class Jukebox : Node
{
    private const float FADE_TIME = 0.6f;
    private const float FADE_LOW_VOLUME = -24.0f;
    private const float NORMAL_VOLUME = 0.0f;

    private const float FADE_PER_TIME_UNIT = (FADE_LOW_VOLUME - NORMAL_VOLUME) / FADE_TIME;

    private static Jukebox instance;

    /// <summary>
    ///   Lists of music
    /// </summary>
    private readonly Dictionary<string, MusicCategory> categories;

    private readonly List<AudioPlayer> audioPlayers = new List<AudioPlayer>();

    private readonly Queue<Operation> operations = new Queue<Operation>();

    private bool paused = true;

    private string playingCategory;

    /// <summary>
    ///   Used to lookup the transitions to go away from a category
    /// </summary>
    private MusicCategory previouslyPlayedCategory;

    /// <summary>
    ///   Loads the music categories and prepares to play them
    /// </summary>
    private Jukebox()
    {
        instance = this;

        categories = SimulationParameters.Instance.GetMusicCategories();

        PauseMode = PauseModeEnum.Process;
    }

    public static Jukebox Instance
    {
        get
        {
            return instance;
        }
    }

    /// <summary>
    ///   The category to play music tracks from
    /// </summary>
    public string PlayingCategory
    {
        get
        {
            return playingCategory;
        }
        set
        {
            if (playingCategory == value)
                return;

            GD.Print("Jukebox now playing from: ", value);
            playingCategory = value;
            OnCategoryChanged();
        }
    }

    public override void _Ready()
    {
        // Preallocate one audio stream player, due to the dynamic number of simultaneous tracks to play this is a list
        audioPlayers.Add(NewPlayer());
    }

    public void Resume()
    {
        if (!paused)
            return;

        paused = false;
        SetStreamsPauseStatus(paused);
    }

    public void Pause()
    {
        if (paused)
            return;

        paused = true;
        SetStreamsPauseStatus(paused);
    }

    public void Stop()
    {
        Pause();
        StopStreams();
    }

    public override void _Process(float delta)
    {
        if (paused)
            return;

        // Process actions
        if (operations.Count > 0)
        {
            if (operations.Peek().Action(delta))
                operations.Dequeue();
        }

        // // Check if a stream has ended
        // foreach (var player in audioPlayers)
        // {
        //     if (!player.Playing)
        //     {
        //         OnSomeTrackEnded();
        //         break;
        //     }
        // }
    }

    private void SetStreamsPauseStatus(bool paused)
    {
        foreach (var player in audioPlayers)
        {
            player.StreamPaused = paused;
        }
    }

    private void StopStreams()
    {
        foreach (var player in audioPlayers)
        {
            player.Player.Stop();
        }
    }

    private void AdjustVolume(float adjustement)
    {
        foreach (var player in audioPlayers)
        {
            player.Player.VolumeDb += adjustement;
        }
    }

    private void SetVolume(float volume)
    {
        foreach (var player in audioPlayers)
        {
            player.Player.VolumeDb = volume;
        }
    }

    private AudioPlayer NewPlayer()
    {
        var player = new AudioStreamPlayer();

        AddChild(player);

        player.Bus = "Music";

        // TODO: should MIX_TARGET_SURROUND be used here?

        player.Connect("finished", this, "OnSomeTrackEnded");

        return new AudioPlayer(player);
    }

    private AudioPlayer GetNextPlayer(int index)
    {
        if (audioPlayers.Count <= index)
            audioPlayers.Add(NewPlayer());

        return audioPlayers[index];
    }

    private void PlayTrack(AudioPlayer player, TrackList.Track track)
    {
        if (player.CurrentTrack != track.ResourcePath)
        {
            var stream = GD.Load<AudioStream>(track.ResourcePath);

            player.Player.Stream = stream;
            player.CurrentTrack = track.ResourcePath;
        }

        player.Player.Play();
    }

    private void OnCategoryChanged()
    {
        var target = categories[PlayingCategory];

        bool faded = false;

        // Add transitions
        if (previouslyPlayedCategory != null)
        {
            if (previouslyPlayedCategory.CategoryTransition == MusicCategory.TRANSITION.Fade)
            {
                AddFadeOut();
                faded = true;
            }
        }

        operations.Enqueue(new Operation((delta) =>
        {
            SetupStreamsFromCategory();
            return true;
        }));

        if (target.CategoryTransition == MusicCategory.TRANSITION.Fade)
        {
            if (!faded)
            {
                AddVolumeRemove();
            }

            AddFadeIn();
        }
        else if (faded)
        {
            AddVolumeRestore();
        }
    }

    private void AddFadeOut()
    {
        var data = new TimedOperationData(FADE_TIME);
        operations.Enqueue(new Operation((delta) =>
        {
            data.TimeLeft -= delta;

            bool finished = data.TimeLeft <= 0;

            if (finished)
            {
                AdjustVolume(FADE_PER_TIME_UNIT * delta);
            }
            else
            {
                SetVolume(FADE_LOW_VOLUME);
            }

            return finished;
        }));
    }

    private void AddFadeIn()
    {
        var data = new TimedOperationData(FADE_TIME);
        operations.Enqueue(new Operation((delta) =>
        {
            data.TimeLeft -= delta;

            bool finished = data.TimeLeft <= 0;

            if (finished)
            {
                AdjustVolume(-1 * FADE_PER_TIME_UNIT * delta);
            }
            else
            {
                SetVolume(NORMAL_VOLUME);
            }

            return finished;
        }));
    }

    private void AddVolumeRestore()
    {
        operations.Enqueue(new Operation((delta) =>
        {
            SetVolume(NORMAL_VOLUME);
            return true;
        }));
    }

    private void AddVolumeRemove()
    {
        operations.Enqueue(new Operation((delta) =>
        {
            SetVolume(FADE_LOW_VOLUME);
            return true;
        }));
    }

    private void OnSomeTrackEnded()
    {
        GD.Print("Jukebox: some track finished");

        // TODO:
        // Find track lists that don't have a playing track in them and reallocate players for those
    }

    private void SetupStreamsFromCategory()
    {
        var target = categories[PlayingCategory];
        previouslyPlayedCategory = target;

        int nextPlayerToUse = 0;

        foreach (var list in target.TrackLists)
        {
            var mode = list.TrackOrder;

            if (mode == TrackList.ORDER.Sequential)
            {
                list.LastPlayedIndex = (list.LastPlayedIndex + 1) % list.Tracks.Count;

                PlayTrack(GetNextPlayer(nextPlayerToUse++), list.Tracks[list.LastPlayedIndex]);
            }
            else
            {
                PlayTrack(GetNextPlayer(nextPlayerToUse++), list.Tracks.Random(new Random()));
            }
        }

        // Set pause status for any new streams
        SetStreamsPauseStatus(paused);
    }

    private class AudioPlayer
    {
        public AudioStreamPlayer Player;
        public string CurrentTrack;

        public AudioPlayer(AudioStreamPlayer player)
        {
            Player = player;
        }

        public bool StreamPaused
        {
            get
            {
                return Player.StreamPaused;
            }
            set
            {
                Player.StreamPaused = value;
            }
        }

        public bool Playing
        {
            get
            {
                return Player.Playing;
            }
            set
            {
                Player.Playing = value;
            }
        }
    }

    private class Operation
    {
        public Func<float, bool> Action;

        public Operation(Func<float, bool> action)
        {
            Action = action;
        }
    }

    private class TimedOperationData
    {
        public float TimeLeft;

        public TimedOperationData(float time)
        {
            TimeLeft = time;
        }
    }
}
