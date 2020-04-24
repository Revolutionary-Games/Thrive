using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages playing music. Autoload singleton
/// </summary>
public class Jukebox : Node
{
    private static Jukebox instance;

    /// <summary>
    ///   Lists of music
    /// </summary>
    private readonly Dictionary<string, MusicCategory> categories;

    private readonly List<AudioStreamPlayer> audioPlayers = new List<AudioStreamPlayer>();

    private bool paused = true;

    private string playingCategory;

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

    public override void _Process(float delta)
    {
        if (paused)
            return;

        // Process actions

        // Check if a stream has ended
        foreach (var player in audioPlayers)
        {
            if (!player.Playing)
            {
                OnSomeTrackEnded();
                break;
            }
        }
    }

    private void SetStreamsPauseStatus(bool paused)
    {
        foreach (var player in audioPlayers)
        {
            player.StreamPaused = paused;
        }
    }

    private AudioStreamPlayer NewPlayer()
    {
        var player = new AudioStreamPlayer();

        AddChild(player);

        player.Bus = "Music";

        return player;
    }

    private AudioStreamPlayer GetNextPlayer(int index)
    {
        if (audioPlayers.Count <= index)
            audioPlayers.Add(NewPlayer());

        return audioPlayers[index];
    }

    private void PlayTrack(AudioStreamPlayer player, TrackList.Track track)
    {
        var stream = GD.Load<AudioStream>(track.ResourcePath);

        player.Stream = stream;
        player.Play();
    }

    private void OnCategoryChanged()
    {
        var target = categories[PlayingCategory];

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

        // TODO:
        // Add transition

        // Set pause status for any new streams
        SetStreamsPauseStatus(paused);
    }

    private void OnSomeTrackEnded()
    {
    }
}
