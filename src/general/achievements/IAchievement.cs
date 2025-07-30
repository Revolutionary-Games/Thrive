public interface IAchievement
{
    /// <summary>
    ///   A unique identifier that each achievement is identified by
    /// </summary>
    public int Identifier { get; }

    /// <summary>
    ///   Internal name of the achievement. This must match what is configured in the Steam backend.
    /// </summary>
    public string InternalName { get; }

    public LocalizedString Name { get; }

    /// <summary>
    ///   Description of how to achieve this achievement. For including info about the current progress in this text,
    ///   see <see cref="GetProgress"/>
    /// </summary>
    public LocalizedString Description { get; }

    public bool Achieved { get; }

    public bool HideIfNotAchieved { get; }

    /// <summary>
    ///   Called when a relevant change happens in the underlying statistics. Should unlock this achievement if the
    ///   conditions are fulfilled.
    /// </summary>
    /// <param name="updatedStats">Achievement data store with the updates</param>
    /// <returns>True if this is now unlocked</returns>
    public bool ProcessPotentialUnlock(AchievementStatStore updatedStats);

    /// <summary>
    ///   Locks this achievement (resets progress)
    /// </summary>
    public void Reset();

    /// <summary>
    ///   Checks if <see cref="GetProgress"/> would have anything meaningful to show
    /// </summary>
    /// <param name="stats">Stats to check in</param>
    /// <returns>
    ///   True if there is any progress towards this achievement. Note that many achievements are just on / off.
    /// </returns>
    public bool HasAnyProgress(AchievementStatStore stats);

    /// <summary>
    ///   Gets text describing the current progress.
    /// </summary>
    /// <returns>Similar text to <see cref="Description"/> but has progress info</returns>
    public string GetProgress(AchievementStatStore stats);
}

public static class AchievementIds
{
    public const int MICROBIAL_MASSACRE = 1;
}
