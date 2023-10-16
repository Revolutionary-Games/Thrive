using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Music category to be loaded from json
/// </summary>
/// <remarks>
///   <para>
///     The Jukebox stores temporary playback data in this class. This is despite this being registry type, but this is
///     fine as there will only ever be a single Jukebox so it modifying the data doesn't break things.
///   </para>
/// </remarks>
public class MusicCategory : IRegistryType
{
    public enum ReturnType
    {
        /// <summary>
        ///   Previous tracks are reset.
        /// </summary>
        Reset,

        /// <summary>
        ///   Previous tracks are continued
        /// </summary>
        Continue,
    }

    public enum Transition
    {
        /// <summary>
        ///   No transition between tracks/categories
        /// </summary>
        None,

        /// <summary>
        ///   There is a crossfade between the tracks/categories
        /// </summary>
        Crossfade,
    }

    public ReturnType Return { get; set; } = ReturnType.Continue;

    public Transition CategoryTransition { get; set; } = Transition.Crossfade;

    public Transition TrackTransition { get; set; } = Transition.Crossfade;

    /// <summary>
    ///   List of track lists. When the mode is concurrent one track from each list is played at once
    /// </summary>
    /// <value>The track lists.</value>
    public List<TrackList> TrackLists { get; set; } = null!;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (TrackLists == null || TrackLists.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "missing track lists");

        foreach (var list in TrackLists)
            list.Check();
    }

    public void ApplyTranslations()
    {
    }
}

/// <summary>
///   Track list within a category
/// </summary>
public class TrackList
{
    public enum Type
    {
        /// <summary>
        ///   Tracks from all categories are played
        /// </summary>
        Concurrent,
    }

    public enum Order
    {
        /// <summary>
        ///   Track order is random
        /// </summary>
        Random,

        /// <summary>
        ///   Track order is as specified in the file
        /// </summary>
        Sequential,

        /// <summary>
        ///   Track order is random (fully random, same item can be played multiple times in a row)
        /// </summary>
        EntirelyRandom,
    }

    public Type ListType { get; set; } = Type.Concurrent;

    public Order TrackOrder { get; set; } = Order.Random;

    public string TrackBus { get; set; } = "Music";

    /// <summary>
    ///   Repeat this track list if all tracks has been played at least once.
    /// </summary>
    public bool Repeat { get; set; } = true;

    [JsonIgnore]
    public int LastPlayedIndex { get; set; } = -1;

    [JsonProperty]
    private List<Track> Tracks { get; set; } = null!;

    public void Check()
    {
        if (Tracks == null || Tracks.Count < 1)
            throw new InvalidRegistryDataException("track list", GetType().Name, "missing Tracks");

        foreach (var track in Tracks)
            track.Check();
    }

    public IEnumerable<Track> GetTracksForContexts(MusicContext[]? contexts)
    {
        return Tracks.Where(t => CheckIfTrackValidInContext(t, contexts));
    }

    private bool CheckIfTrackValidInContext(Track track, MusicContext[]? contexts)
    {
        if (track.PlayOnlyWithoutContext)
            return contexts == null;

        if (track.ExclusiveToContexts != null)
            return track.ExclusiveToContexts.Any(contexts.Contains);

        if (track.DisallowInContexts != null)
            return !track.DisallowInContexts.Any(contexts.Contains);

        return true;
    }

    /// <summary>
    ///   A single track within a track list
    /// </summary>
    public class Track
    {
        /// <summary>
        ///   The track's base volume level in linear volume range 0-1.0f
        /// </summary>
        public float Volume { get; set; } = 1.0f;

        public string ResourcePath { get; set; } = null!;

        public MusicContext[]? ExclusiveToContexts { get; set; }

        public MusicContext[]? DisallowInContexts { get; set; }

        public bool PlayOnlyWithoutContext { get; set; } = false;

        [JsonIgnore]
        public bool WasPlaying { get; set; } = false;

        [JsonIgnore]
        public float PreviousPlayedPosition { get; set; } = 0;

        [JsonIgnore]
        public bool PlayedOnce { get; set; } = false;

        public void Check()
        {
            if (string.IsNullOrEmpty(ResourcePath))
            {
                throw new InvalidRegistryDataException("track", GetType().Name, "ResourcePath missing for track");
            }
        }
    }
}
