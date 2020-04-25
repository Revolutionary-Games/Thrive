using System;
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
    public enum RETURN_TYPE
    {
        Continue,
    }

    public enum TRANSITION
    {
        Fade,
    }

    public enum TRACK_TRANSITION
    {
        None,
    }

    public RETURN_TYPE Return { get; set; } = RETURN_TYPE.Continue;

    public TRANSITION CategoryTransition { get; set; } = TRANSITION.Fade;

    public TRACK_TRANSITION TrackTransition { get; set; } = TRACK_TRANSITION.None;

    /// <summary>
    ///   List of track lists. When the mode is concurrent one track from each list is played at once
    /// </summary>
    /// <value>The track lists.</value>
    public List<TrackList> TrackLists { get; set; }

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (TrackLists == null || TrackLists.Count < 1)
            throw new InvalidRegistryData(name, this.GetType().Name, "missing track lists");

        foreach (var list in TrackLists)
            list.Check();
    }
}

/// <summary>
///   Track list within a category
/// </summary>
public class TrackList
{
    public enum TYPE
    {
        Concurrent,
    }

    public enum ORDER
    {
        Random,
        Sequential,
    }

    public TYPE ListType { get; set; } = TYPE.Concurrent;

    public ORDER TrackOrder { get; set; } = ORDER.Random;

    public List<Track> Tracks { get; set; }

    [JsonIgnore]
    public int LastPlayedIndex { get; set; } = -1;

    public void Check()
    {
        if (Tracks == null || Tracks.Count < 1)
            throw new InvalidRegistryData("track list", this.GetType().Name, "missing Tracks");

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
                throw new InvalidRegistryData("track", this.GetType().Name, "ResourcePath missing for track");
            }
        }
    }
}
