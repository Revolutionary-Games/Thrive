using System;
using System.Collections.Generic;
using System.Linq;
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

    private List<string> PlayingTracks
    {
        get
        {
            return audioPlayers.Where((player) => player.Playing).
                Select((player) => player.CurrentTrack).ToList();
        }
    }

    public override void _Ready()
    {
        // Preallocate one audio stream player, due to the dynamic number of simultaneous tracks to play this is a list
        NewPlayer();
    }

    /// <summary>
    ///   Unpauses currently playing songs
    /// </summary>
    public void Resume()
    {
        if (!paused)
            return;

        paused = false;
        SetStreamsPauseStatus(paused);
    }

    /// <summary>
    ///   Pause the currently playing songs
    /// </summary>
    public void Pause()
    {
        if (paused)
            return;

        paused = true;
        SetStreamsPauseStatus(paused);
    }

    /// <summary>
    ///   Stops the currently playing music (doesn't preserve positions for when Resume is called)
    /// </summary>
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
            player.Stop();
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

        var created = new AudioPlayer(player);

        audioPlayers.Add(created);
        return created;
    }

    private AudioPlayer GetNextPlayer(int index)
    {
        if (audioPlayers.Count <= index)
            NewPlayer();

        return audioPlayers[index];
    }

    private void PlayTrack(AudioPlayer player, TrackList.Track track, float fromPosition = 0)
    {
        if (player.CurrentTrack != track.ResourcePath)
        {
            var stream = GD.Load<AudioStream>(track.ResourcePath);

            player.Player.Stream = stream;
            player.CurrentTrack = track.ResourcePath;

            player.Player.Play(fromPosition);

            GD.Print("Jukebox: starting track: ", track.ResourcePath, " position: ", fromPosition);
        }
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
        // Check that a stream has actually ended, as we get this callback when also purposefully stopping
        bool actuallyEnded = false;

        foreach (var player in audioPlayers)
        {
            if (!player.Playing && !string.IsNullOrEmpty(player.CurrentTrack))
                actuallyEnded = true;
        }

        if (!actuallyEnded)
            return;

        StartPlayingFromMissingLists(categories[PlayingCategory]);
    }

    private void SetupStreamsFromCategory()
    {
        OnCategoryEnded();

        // Stop all players to not let them play anymore
        StopStreams();

        var target = categories[PlayingCategory];
        previouslyPlayedCategory = target;

        int nextPlayerToUse = 0;

        // Resume tracks
        if (target.Return == MusicCategory.RETURN_TYPE.Continue)
        {
            foreach (var list in target.TrackLists)
            {
                foreach (var track in list.Tracks)
                {
                    // Resume track (but only one per list)
                    if (track.WasPlaying)
                    {
                        PlayTrack(GetNextPlayer(nextPlayerToUse++), track, track.PreviousPlayedPosition);
                        break;
                    }
                }
            }
        }

        StartPlayingFromMissingLists(target);
    }

    private void StartPlayingFromMissingLists(MusicCategory target)
    {
        // Find track lists that don't have a playing track in them and reallocate players for those
        var needToStartFrom = new List<TrackList>();

        var activeTracks = PlayingTracks;
        var usablePlayers = audioPlayers.Where((player) => !player.Playing).ToList();

        foreach (var list in target.TrackLists)
        {
            var trackResources = list.Tracks.Select((track) => track.ResourcePath);
            if (activeTracks.Any((track) => trackResources.Contains(track)))
                continue;

            needToStartFrom.Add(list);
        }

        int nextPlayerToUse = 0;

        foreach (var list in needToStartFrom)
        {
            PlayNextTrackFromList(list, (index) =>
            {
                if (index < usablePlayers.Count)
                {
                    return usablePlayers[index];
                }
                else
                {
                    return NewPlayer();
                }
            }, nextPlayerToUse++);
        }

        // Set pause status for any new streams
        SetStreamsPauseStatus(paused);
    }

    private void PlayNextTrackFromList(TrackList list, Func<int, AudioPlayer> getPlayer, int playerToUse)
    {
        var mode = list.TrackOrder;

        if (mode == TrackList.ORDER.Sequential)
        {
            list.LastPlayedIndex = (list.LastPlayedIndex + 1) % list.Tracks.Count;

            PlayTrack(getPlayer(playerToUse), list.Tracks[list.LastPlayedIndex]);
        }
        else
        {
            var random = new Random();

            // Make sure same random track is not played twice in a row
            int nextIndex;

            do
            {
                nextIndex = random.Next(0, list.Tracks.Count);
            }
            while (nextIndex == list.LastPlayedIndex && list.Tracks.Count > 1);

            PlayTrack(getPlayer(playerToUse), list.Tracks[nextIndex]);
            list.LastPlayedIndex = nextIndex;
        }
    }

    private void OnCategoryEnded()
    {
        if (previouslyPlayedCategory == null)
            return;

        var category = previouslyPlayedCategory;

        // Store continue positions
        if (category.Return == MusicCategory.RETURN_TYPE.Continue)
        {
            var activeTracks = PlayingTracks;

            foreach (var list in category.TrackLists)
            {
                foreach (var track in list.Tracks)
                {
                    if (activeTracks.Contains(track.ResourcePath))
                    {
                        track.WasPlaying = true;

                        // Store the position to resume from
                        track.PreviousPlayedPosition = audioPlayers.Where(
                            (player) => player.Playing && player.CurrentTrack == track.ResourcePath).Select(
                                (player) => player.Player.GetPlaybackPosition()).First();
                    }
                    else
                    {
                        track.WasPlaying = false;
                    }
                }
            }
        }
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

        public void Stop()
        {
            CurrentTrack = null;
            Player.Stop();
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
