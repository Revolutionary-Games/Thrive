using System;

/// <summary>
///   Stores info about a startup attempt for use with running using different settings on subsequent launch to get
///   around launch problems
/// </summary>
public class StartupAttemptInfo
{
    /// <summary>
    ///   When the start attempt happened
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///   True when the attempt was made with one or more enabled mods
    /// </summary>
    public bool ModsEnabled { get; set; }

    public bool VideosEnabled { get; set; }
}
