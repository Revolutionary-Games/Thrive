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
}

public static class AchievementIds
{
    public const int MICROBIAL_MASSACRE = 1;
}
