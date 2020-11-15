using System.Collections.Generic;
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
        ///   Previous tracks are continued
        /// </summary>
        Continue,
    }

    public enum Transition
    {
        /// <summary>
        ///   There is a fade between the categories
        /// </summary>
        Fade,
    }

    public enum TrackTransitionType
    {
        /// <summary>
        ///   No transition between tracks
        /// </summary>
        None,
    }

    public ReturnType Return { get; set; } = ReturnType.Continue;

    public Transition CategoryTransition { get; set; } = Transition.Fade;

    public TrackTransitionType TrackTransition { get; set; } = TrackTransitionType.None;

    /// <summary>
    ///   List of track lists. When the mode is concurrent one track from each list is played at once
    /// </summary>
    /// <value>The track lists.</value>
    public List<TrackList> TrackLists { get; set; }

    public string InternalName { get; set; }

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
    }

    public Type ListType { get; set; } = Type.Concurrent;

    public Order TrackOrder { get; set; } = Order.Random;

    public string TrackBus { get; set; } = "Music";

    public List<Track> Tracks { get; set; }

    [JsonIgnore]
    public int LastPlayedIndex { get; set; } = -1;

    public void Check()
    {
        if (Tracks == null || Tracks.Count < 1)
            throw new InvalidRegistryDataException("track list", GetType().Name, "missing Tracks");

        foreach (var track in Tracks)
            track.Check();
    }

    /// <summary>
    ///   A single track within a track list
    /// </summary>
    public class Track
    {
        public string ResourcePath { get; set; }

        [JsonIgnore]
        public bool WasPlaying { get; set; } = false;

        [JsonIgnore]
        public float PreviousPlayedPosition { get; set; } = 0;

        public void Check()
        {
            if (string.IsNullOrEmpty(ResourcePath))
            {
                throw new InvalidRegistryDataException("track", GetType().Name, "ResourcePath missing for track");
            }
        }
    }
}
