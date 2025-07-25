using Godot;

/// <summary>
///   Stores stats needed to track all achievement states
/// </summary>
public class AchievementStatStore
{
    public const int STAT_MICROBE_KILLS = 1;

    private int statMicrobeKills;

    public int GetIntStat(int statId)
    {
        switch (statId)
        {
            case STAT_MICROBE_KILLS:
                return statMicrobeKills;
        }

        GD.PrintErr("Unknown stat ID requested: ", statId);
        return 0;
    }

    public int IncrementIntStat(int statId)
    {
        switch (statId)
        {
            case STAT_MICROBE_KILLS:
                ++statMicrobeKills;
                return statMicrobeKills;
        }

        GD.PrintErr("Unknown stat ID tried to be incremented: ", statId);
        return 0;
    }
}
