using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Manages playing of music. Autoload singleton
/// </summary>
[GodotAutoload]
public partial class Jukebox : Node
{
    private const float FADE_TIME = 1.0f;
    private const float FADE_LOW_VOLUME = 0.0f;
    private const float NORMAL_VOLUME = 1.0f;

    private static Jukebox? instance;

    private readonly List<AudioPlayer> audioPlayers = new();

    private readonly Queue<Operation> operations = new();

    /// <summary>
    ///   Lists of music
    /// </summary>
    private IReadOnlyDictionary<string, MusicCategory> categories = null!;

    /// <summary>
    ///   The current jukebox volume level in linear volume range 0-1.0f
    /// </summary>
    private float linearVolume = 1.0f;

    private bool paused = true;
    private bool pausing;

    private string? playingCategory;

    /// <summary>
    ///   Used to look up the transitions to go away from a category
    /// </summary>
    private MusicCategory? previouslyPlayedCategory;

    private MusicContext[]? activeContexts;

    /// <summary>
    ///   Loads the music categories and prepares to play them
    /// </summary>
    private Jukebox()
    {
        if (Engine.IsEditorHint())
            return;

        instance = this;
    }

    internal enum JukeboxCommandParameters
    {
        Status,
        Next,
    }

    public static Jukebox Instance => instance ?? throw new InstanceNotLoadedYetException();

    public static bool HasInstance => instance != null;

    /// <summary>
    ///   The category to play music tracks from
    /// </summary>
    public string PlayingCategory
    {
        get => playingCategory ?? throw new InvalidOperationException("Not yet playing any category");

        set
        {
            if (playingCategory == value)
                return;

            if (value == null)
                throw new ArgumentException("Playing category can't be set to null");

            GD.Print("Jukebox now playing from: ", value);

            playingCategory = value;
            OnCategoryChanged();
        }
    }

    public JukeboxPlaybackState PlaybackState
    {
        get => CreatePlaybackState();
        set
        {
            // We call this to not end the current category, but just to save and remember the positions of tracks
            // that were playing right now
            OnCategoryEnded();

            ApplyPlaybackState(value);
        }
    }

    private List<string> PlayingTracks => audioPlayers.Where(p => p.Playing)
        .Select(p => p.CurrentTrack).WhereNotNull().ToList();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            paused = true;
            return;
        }

        categories = SimulationParameters.Instance.GetMusicCategories();

        ProcessMode = ProcessModeEnum.Always;

        // Preallocate one audio stream player, due to the dynamic number of simultaneous tracks to play this is a list
        NewPlayer();
    }

    public override void _Process(double delta)
    {
        if (paused)
            return;

        // Process actions
        if (operations.Count > 0)
        {
            if (operations.Peek().Action(delta))
                operations.Dequeue();
        }

        foreach (var player in audioPlayers)
        {
            if (player.Operations.Count > 0)
            {
                if (player.Operations.Peek().Action(delta))
                    player.Operations.Dequeue();
            }
        }
    }

    /// <summary>
    ///   Starts playing tracks from the provided category
    /// </summary>
    /// <param name="category">category from music_tracks.json</param>
    /// <param name="contexts">list of contexts to select only specific tracks</param>
    public void PlayCategory(string category, MusicContext[]? contexts = null)
    {
        activeContexts = contexts;
        PlayingCategory = category;
        Resume();
    }

    /// <summary>
    ///   Unpauses currently playing songs
    /// </summary>
    public void Resume(bool fade = false)
    {
        if (!paused && !pausing)
            return;

        pausing = false;
        paused = false;

        if (fade)
        {
            operations.Clear();
            AddFadeIn();
        }

        UpdateStreamsPauseStatus();
    }

    /// <summary>
    ///   Pause the currently playing songs
    /// </summary>
    public void Pause(bool fade = false)
    {
        if (paused)
            return;

        if (!fade)
        {
            paused = true;
            UpdateStreamsPauseStatus();
        }
        else if (fade && !pausing)
        {
            pausing = true;
            operations.Clear();
            AddFadeOut();
            operations.Enqueue(new Operation(_ =>
            {
                pausing = false;
                paused = true;
                UpdateStreamsPauseStatus();
                return true;
            }));
        }
    }

    /// <summary>
    ///   Stops the currently playing music (doesn't preserve positions for when Resume is called)
    /// </summary>
    public void Stop(bool fade = false)
    {
        if (!fade)
        {
            Pause();
            StopStreams();
            operations.Clear();
        }
        else
        {
            operations.Clear();
            AddFadeOut();
            operations.Enqueue(new Operation(_ =>
            {
                Pause();
                StopStreams();
                return true;
            }));
        }
    }

    [Command("jukebox", false,
        "`jukebox status` shows the currently playing Jukebox's active category, " +
        "current active audio players and their playing tracks.\n" +
        "`jukebox next i (default 0)` advances the i-th active audio player's playing track to the next selection. " +
        "i start from 0.")]
    private static bool CommandJukebox(CommandContext context, JukeboxCommandParameters param, int index = 0)
    {
        switch (param)
        {
            case JukeboxCommandParameters.Status when !HasInstance:
                context.PrintWarning("Jukebox has no active instance.");
                return false;
            case JukeboxCommandParameters.Status:
                return Instance.PrintPlaybackStatus(context);
            case JukeboxCommandParameters.Next:
                return CommandJukeboxNextTrack(context, index);
            default:
                context.PrintWarning("Illegal parameter");
                return false;
        }
    }

    private static bool CommandJukeboxNextTrack(CommandContext context, int index)
    {
        if (!HasInstance)
        {
            context.PrintWarning("Jukebox has no active instance.");
            return false;
        }

        if (Instance.operations.Count > 0)
        {
            context.PrintWarning("Jukebox is busy with a pending transition. Wait for it to finish before advancing.");
            return false;
        }

        if (Instance.playingCategory == null)
        {
            context.PrintWarning("Jukebox is not currently playing a category.");
            return false;
        }

        var activePlayers = Instance.GetActiveAudioPlayersForDebugCommands();

        if (activePlayers.Count <= 0)
        {
            context.PrintWarning("Jukebox has no active track to advance.");
            return false;
        }

        if (index < 0 || index >= activePlayers.Count)
        {
            context.PrintWarning(
                $"Jukebox audio player index {index} is invalid. Valid indexes are 0-{activePlayers.Count - 1}.");
            return false;
        }

        if (!Instance.AdvanceToNextTrack(activePlayers[index]))
        {
            context.PrintWarning("Jukebox has no active track to advance.");
            return false;
        }

        context.Print($"Jukebox advanced audio player {index} to the next track.");
        return true;
    }

    private bool PrintPlaybackStatus(CommandContext context)
    {
        if (playingCategory == null)
        {
            context.PrintWarning("Jukebox is not currently playing a category.");
            return false;
        }

        context.Print($"Jukebox category: {playingCategory}");

        var activePlayers = GetActiveAudioPlayersForDebugCommands();
        var count = activePlayers.Count;

        if (count <= 0)
        {
            context.PrintWarning("Jukebox has no active players.");
            return false;
        }

        for (var i = 0; i < count; ++i)
        {
            var player = activePlayers[i];
            context.Print($"audio player {i}, Bus: {player.Bus}, playing: {player.CurrentTrack}");
        }

        return true;
    }

    private List<AudioPlayer> GetActiveAudioPlayersForDebugCommands()
    {
        var activePlayers = new List<AudioPlayer>();

        foreach (var player in audioPlayers)
        {
            if (player is { Playing: true, CurrentTrack: not null })
                activePlayers.Add(player);
        }

        return activePlayers;
    }

    /// <summary>
    ///   Advances the selected currently playing track as if it had just naturally ended.
    /// </summary>
    /// <returns>
    ///   True if the selected active track was advanced; false if nothing is currently playing.
    /// </returns>
    private bool AdvanceToNextTrack(AudioPlayer player)
    {
        if (playingCategory == null)
            return false;

        if (!player.Playing || player.CurrentTrack == null)
            return false;

        player.Player.Stop();
        OnSomeTrackEnded();
        return true;
    }

    private void UpdateStreamsPauseStatus()
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

    private void SetVolume(float volume, AudioPlayer? audioPlayer = null)
    {
        if (audioPlayer == null)
        {
            linearVolume = volume;
        }
        else
        {
            audioPlayer.LinearVolume = volume;
        }

        ApplyLinearVolume(audioPlayer);
    }

    private void ApplyLinearVolume(AudioPlayer? audioPlayer = null)
    {
        if (audioPlayer == null)
        {
            foreach (var player in audioPlayers)
            {
                var dbValue = Mathf.LinearToDb(linearVolume * player.BaseVolume * player.LinearVolume);
                player.Player.VolumeDb = dbValue;
            }
        }
        else
        {
            var dbValue = Mathf.LinearToDb(linearVolume * audioPlayer.BaseVolume * audioPlayer.LinearVolume);
            audioPlayer.Player.VolumeDb = dbValue;
        }
    }

    private AudioPlayer NewPlayer()
    {
        var player = new AudioStreamPlayer();

        AddChild(player);

        player.Bus = "Music";

        // Set initial volume to be what the volume should be currently
        player.VolumeDb = Mathf.LinearToDb(linearVolume);

        // TODO: should MIX_TARGET_SURROUND be used here?

        player.Connect(AudioStreamPlayer.SignalName.Finished, new Callable(this, nameof(OnSomeTrackEnded)));

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

    private void PlayTrack(AudioPlayer player, TrackList.Track track, string trackBus, float fromPosition = 0)
    {
        bool changedTrack = false;

        if (player.CurrentTrack != track.ResourcePath)
        {
            var stream = GD.Load<AudioStream>(track.ResourcePath);

            player.Player.Stream = stream;
            player.CurrentTrack = track.ResourcePath;
            player.BaseVolume = track.Volume;

            changedTrack = true;
        }

        if (player.Bus != trackBus)
        {
            player.Bus = trackBus;
            changedTrack = true;
        }

        if (changedTrack || !player.Playing)
        {
            var target = categories[PlayingCategory];

            // TODO: It would be nice to skip the fade, if the same track is going to be played again.
            //       e.g. New transition type FadeIfTrackChanges. This needs a lookahead to know what track is
            //       played next. Microbe ambiance2 would sound better when it loops that it doesn't have the
            //       fade in the middle of it.

            if (target.TrackTransition == MusicCategory.Transition.Crossfade)
            {
                AddFadeIn(player);
                AddWait(player.Player.Stream.GetLength() - fromPosition - 2 * FADE_TIME, player);
                AddFadeOut(player);
            }

            player.Player.Play(fromPosition);
            GD.Print("Jukebox: starting track: ", track.ResourcePath, " position: ", fromPosition);

            track.PlayedOnce = true;
        }
    }

    private void OnCategoryChanged()
    {
        var target = categories[PlayingCategory];

        bool faded = false;

        operations.Clear();
        foreach (var player in audioPlayers)
        {
            player.Operations.Clear();
        }

        // Add transitions
        if (previouslyPlayedCategory?.CategoryTransition == MusicCategory.Transition.Crossfade)
        {
            AddFadeOut();
            faded = true;
        }

        operations.Enqueue(new Operation(_ =>
        {
            SetupStreamsFromCategory();
            return true;
        }));

        if (target.CategoryTransition == MusicCategory.Transition.Crossfade)
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

    private void AddFadeOut(AudioPlayer? audioPlayer = null)
    {
        AddVolumeChange(FADE_TIME, linearVolume, FADE_LOW_VOLUME, audioPlayer);
    }

    private void AddFadeIn(AudioPlayer? audioPlayer = null)
    {
        AddVolumeChange(FADE_TIME, 0, NORMAL_VOLUME, audioPlayer);
    }

    private void AddWait(double duration, AudioPlayer? audioPlayer = null)
    {
        var data = new TimedOperationData(duration, audioPlayer);

        var targetOperations = operations;
        if (audioPlayer != null)
        {
            targetOperations = audioPlayer.Operations;
        }

        targetOperations.Enqueue(new Operation(delta =>
        {
            data.TimeLeft -= delta;

            if (data.TimeLeft <= 0)
                return true;

            return false;
        }));
    }

    private void AddVolumeChange(double duration, float startVolume, float endVolume, AudioPlayer? audioPlayer = null)
    {
        var data = new TimedOperationData(duration, audioPlayer) { StartVolume = startVolume, EndVolume = endVolume };

        var targetOperations = operations;
        if (audioPlayer != null)
        {
            targetOperations = audioPlayer.Operations;
        }

        targetOperations.Enqueue(new Operation(delta =>
        {
            data.TimeLeft -= delta;

            if (data.TimeLeft < 0)
                data.TimeLeft = 0;

            var progress = (data.TotalDuration - data.TimeLeft) / data.TotalDuration;

            if (progress >= 1.0f)
            {
                SetVolume(data.EndVolume, data.AudioPlayer);
                return true;
            }

            var targetVolume = data.StartVolume + (data.EndVolume - data.StartVolume) * progress;

            SetVolume((float)targetVolume, data.AudioPlayer);

            return false;
        }));
    }

    private void AddVolumeRestore(AudioPlayer? audioPlayer = null)
    {
        var targetOperations = operations;
        if (audioPlayer != null)
        {
            targetOperations = audioPlayer.Operations;
        }

        targetOperations.Enqueue(new Operation(_ =>
        {
            SetVolume(NORMAL_VOLUME, audioPlayer);
            return true;
        }));
    }

    private void AddVolumeRemove(AudioPlayer? audioPlayer = null)
    {
        var targetOperations = operations;
        if (audioPlayer != null)
        {
            targetOperations = audioPlayer.Operations;
        }

        targetOperations.Enqueue(new Operation(_ =>
        {
            SetVolume(FADE_LOW_VOLUME, audioPlayer);
            return true;
        }));
    }

    // ReSharper disable once UnusedMember.Local
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
        if (target.Return == MusicCategory.ReturnType.Continue)
        {
            foreach (var list in target.TrackLists)
            {
                foreach (var track in list.GetTracksForContexts(activeContexts))
                {
                    // Resume track (but only one per list)
                    if (track.WasPlaying)
                    {
                        PlayTrack(GetNextPlayer(nextPlayerToUse++), track, list.TrackBus, track.PreviousPlayedPosition);
                        break;
                    }
                }
            }
        }

        StartPlayingFromMissingLists(target);
    }

    private JukeboxPlaybackState CreatePlaybackState()
    {
        var state = new JukeboxPlaybackState();

        foreach (var (categoryName, category) in categories)
        {
            // Skip categories that are general and not save-specific
            if (category.IsGlobalMenu)
                continue;

            var trackPositions = new Dictionary<string, float>();

            foreach (var list in category.TrackLists)
            {
                foreach (var track in list.GetAllTracks())
                {
                    if (track.WasPlaying)
                        trackPositions[track.ResourcePath] = track.PreviousPlayedPosition;
                }
            }

            if (categoryName == playingCategory)
            {
                foreach (var player in audioPlayers)
                {
                    if (!player.Playing || player.CurrentTrack == null)
                        continue;

                    trackPositions[player.CurrentTrack] = player.Player.GetPlaybackPosition();
                }
            }

            if (trackPositions.Count > 0)
                state.TrackPositionsByCategory[categoryName] = trackPositions;
        }

        return state;
    }

    private void ApplyPlaybackState(JukeboxPlaybackState state)
    {
        foreach (var (categoryName, trackPositions) in state.TrackPositionsByCategory)
        {
            if (!categories.TryGetValue(categoryName, out var category))
                continue;

            if (category.IsGlobalMenu)
            {
                GD.Print($"Ignoring Jukebox state in a save for global category: {categoryName}");
                continue;
            }

            foreach (var list in category.TrackLists)
            {
                foreach (var track in list.GetAllTracks())
                {
                    // Reset all tracks to be eligible to be played again after loading a save
                    track.PlayedOnce = false;

                    if (trackPositions.TryGetValue(track.ResourcePath, out var position))
                    {
                        track.WasPlaying = true;
                        track.PreviousPlayedPosition = position;
                    }
                    else
                    {
                        track.WasPlaying = false;
                        track.PreviousPlayedPosition = 0;
                    }
                }
            }
        }
    }

    private void StartPlayingFromMissingLists(MusicCategory target)
    {
        // Find track lists that don't have a playing track in them and reallocate players for those
        var needToStartFrom = new List<TrackList>();

        var activeTracks = PlayingTracks;
        var usablePlayers = audioPlayers.Where(p => !p.Playing).ToList();

        foreach (var list in target.TrackLists)
        {
            // Detecting playing tracks doesn't take context restrictions into account to allow context to change but
            // not then force 2 tracks to play at the same time from the same track list if the context switch didn't
            // force tracks to end
            var trackResources = list.GetAllTracks().Select(t => t.ResourcePath);
            if (activeTracks.Any(trackResources.Contains))
                continue;

            needToStartFrom.Add(list);
        }

        int nextPlayerToUse = 0;

        foreach (var list in needToStartFrom)
        {
            if (!list.Repeat && list.GetTracksForContexts(activeContexts).All(t => t.PlayedOnce))
                continue;

            PlayNextTrackFromList(list, index =>
            {
                if (index < usablePlayers.Count)
                {
                    return usablePlayers[index];
                }

                return NewPlayer();
            }, nextPlayerToUse++);
        }

        // Set pause status for any new streams
        UpdateStreamsPauseStatus();
    }

    private void PlayNextTrackFromList(TrackList list, Func<int, AudioPlayer> getPlayer, int playerToUse)
    {
        var mode = list.TrackOrder;
        TrackList.Track[]? tracks = null;

        var random = new XoShiRo128starstar();

        if (random.NextFloat() <= Constants.CONTEXTUAL_ONLY_MUSIC_CHANCE)
        {
            // TODO: avoid the temporary array and lambda allocations here somehow
            var contextMusicOnly = list.GetTracksForContexts(activeContexts)
                .Where(c => c.ExclusiveToContexts != null && c.ResourcePath != list.LastPlayedTrackPath)
                .ToArray();

            if (contextMusicOnly.Length > 0)
            {
                tracks = contextMusicOnly;
            }
        }

        tracks ??= list.GetTracksForContexts(activeContexts).ToArray();

        if (tracks.Length == 0)
            return;

        if (mode == TrackList.Order.Sequential)
        {
            var nextTrack = GetNextSequentialTrack(list, tracks);

            PlayTrack(getPlayer(playerToUse), nextTrack, list.TrackBus);
            list.LastPlayedTrackPath = nextTrack.ResourcePath;
        }
        else
        {
            int nextIndex;

            if (mode == TrackList.Order.Random)
            {
                // Make sure same random track is not played twice in a row
                do
                {
                    nextIndex = random.Next(0, tracks.Length);
                }
                while (tracks.Length > 1 && tracks[nextIndex].ResourcePath == list.LastPlayedTrackPath);
            }
            else if (mode == TrackList.Order.EntirelyRandom)
            {
                nextIndex = random.Next(0, tracks.Length);
            }
            else
            {
                throw new InvalidOperationException("Unknown track list order type");
            }

            PlayTrack(getPlayer(playerToUse), tracks[nextIndex], list.TrackBus);
            list.LastPlayedTrackPath = tracks[nextIndex].ResourcePath;
        }
    }

    private TrackList.Track GetNextSequentialTrack(TrackList list, TrackList.Track[] playableTracks)
    {
        if (playableTracks.Length == 1 || string.IsNullOrEmpty(list.LastPlayedTrackPath))
            return playableTracks[0];

        var orderedTracks = list.GetAllTracks();
        var lastIndex = -1;

        for (var i = 0; i < orderedTracks.Count; ++i)
        {
            if (orderedTracks[i].ResourcePath != list.LastPlayedTrackPath)
                continue;

            lastIndex = i;
            break;
        }

        if (lastIndex < 0)
            return playableTracks[0];

        for (var checkedCount = 1; checkedCount < orderedTracks.Count; ++checkedCount)
        {
            var candidate = orderedTracks[(lastIndex + checkedCount) % orderedTracks.Count];
            if (IsTrackPlayable(playableTracks, candidate))
                return candidate;
        }

        return playableTracks[0];
    }

    private bool IsTrackPlayable(TrackList.Track[] playableTracks, TrackList.Track candidate)
    {
        foreach (var track in playableTracks)
        {
            if (ReferenceEquals(track, candidate))
                return true;
        }

        return false;
    }

    private void OnCategoryEnded()
    {
        var category = previouslyPlayedCategory;

        if (category != null)
        {
            ResetPlayedOnceFlag(category);
        }

        // We don't have to do anything for the Reset return type here

        if (category?.Return == MusicCategory.ReturnType.Continue)
        {
            StoreContinuePositions(category);
        }
    }

    private void ResetPlayedOnceFlag(MusicCategory category)
    {
        foreach (var list in category.TrackLists)
        {
            foreach (var track in list.GetAllTracks())
            {
                track.PlayedOnce = false;
            }
        }
    }

    private void StoreContinuePositions(MusicCategory category)
    {
        foreach (var list in category.TrackLists)
        {
            // This doesn't restrict tracks to ones that can be played according to the context. This is done to
            // ensure that in the future if context can be changed while playing a category without immediately
            // stopping tracks, this will work correctly.
            foreach (var track in list.GetAllTracks())
            {
                track.WasPlaying = PlayingTracks.Contains(track.ResourcePath);
                if (track.WasPlaying)
                {
                    // Store the position to resume from
                    track.PreviousPlayedPosition = audioPlayers
                        .Where(p => p.Playing && p.CurrentTrack == track.ResourcePath)
                        .Select(p => p.Player.GetPlaybackPosition()).First();
                }
            }
        }
    }

    private class AudioPlayer
    {
        public readonly AudioStreamPlayer Player;
        public string? CurrentTrack;

        public AudioPlayer(AudioStreamPlayer player)
        {
            Player = player;
        }

        public Queue<Operation> Operations { get; } = new();

        /// <summary>
        ///   The current AudioPlayer volume level in linear volume range 0-1.0f
        /// </summary>
        public float LinearVolume { get; set; } = 1.0f;

        public float BaseVolume { get; set; } = 1.0f;

        public bool StreamPaused
        {
            set => Player.StreamPaused = value;
        }

        public bool Playing => Player.Playing;

        public string Bus
        {
            get => Player.Bus;
            set => Player.Bus = value;
        }

        public void Stop()
        {
            CurrentTrack = null;
            Player.Stop();
            Operations.Clear();
            LinearVolume = 1;
        }
    }

    private class Operation
    {
        public Func<double, bool> Action;

        public Operation(Func<double, bool> action)
        {
            Action = action;
        }
    }

    private class TimedOperationData
    {
        public readonly AudioPlayer? AudioPlayer;

        public readonly double TotalDuration;
        public double TimeLeft;

        // Data for timed operations dealing with volumes
        public float StartVolume;
        public float EndVolume;

        public TimedOperationData(double time, AudioPlayer? audioPlayer = null)
        {
            AudioPlayer = audioPlayer;

            TimeLeft = time;
            TotalDuration = time;
        }
    }
}
