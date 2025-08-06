public interface IAchievementStatStore
{
    // This config must match what is in the Steamworks backend
    public const int STAT_MICROBE_KILLS = 1;
    public const string STAT_MICROBE_KILLS_NAME = "microbe_kills";

    public const string MICROBIAL_MASSACRE_ID = "MICROBIAL_MASSACRE";

    public static bool IsValidStatistic(int statId)
    {
        return GetStatName(statId) != null;
    }

    public static string? GetStatName(int statId)
    {
        switch (statId)
        {
            case STAT_MICROBE_KILLS:
                return STAT_MICROBE_KILLS_NAME;
        }

        return null;
    }

    public int GetIntStat(int statId);
    public int IncrementIntStat(int statId);

    public void Reset();
}
