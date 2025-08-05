using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores stats needed to track all achievement states
/// </summary>
public class AchievementStatStore
{
    public const int STAT_MICROBE_KILLS = 1;
    public const string STAT_MICROBE_KILLS_NAME = "microbe_kills";

    private int statMicrobeKills;

    public static bool IsValidStatistic(int statId)
    {
        switch (statId)
        {
            case STAT_MICROBE_KILLS:
                return true;
        }

        return false;
    }

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

    /// <summary>
    ///   Resets ALL stats to initial values, losing all progress towards achievements.
    /// </summary>
    public void Reset()
    {
        GD.Print("Resetting tracked stats");
        statMicrobeKills = 0;
    }

    public void Save(Dictionary<int, int> intValues)
    {
        intValues[STAT_MICROBE_KILLS] = statMicrobeKills;
    }

    public void Load(Dictionary<int, int> intValues)
    {
        if (intValues.TryGetValue(STAT_MICROBE_KILLS, out var value))
            statMicrobeKills = value;
    }
}
