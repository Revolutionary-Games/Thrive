using Godot;

/// <summary>
///   Accesses achievement's backing statistics through Steam
/// </summary>
public class SteamStatStore : IAchievementStatStore
{
    private readonly ISteamClient steamClient;

    public SteamStatStore(ISteamClient steamClient)
    {
        this.steamClient = steamClient;
    }

    public int GetIntStat(int statId)
    {
        var stat = IAchievementStatStore.GetStatName(statId);

        if (stat == null)
        {
            GD.PrintErr("Requesting invalid statistic from Steam: ", statId);
            return -1;
        }

        if (!steamClient.GetSteamStatistic(stat, out int data))
        {
            GD.PrintErr("Failed to get statistic from Steam: ", statId);
            return -1;
        }

        return data;
    }

    public int IncrementIntStat(int statId)
    {
        // TODO: could maybe load all stats and keep them in local fields here for updates
        var stat = IAchievementStatStore.GetStatName(statId);

        if (stat == null)
        {
            GD.PrintErr("Updating invalid statistic to Steam: ", statId);
            return -1;
        }

        var value = GetIntStat(statId);

        ++value;

        if (!steamClient.SetSteamStatistic(stat, value))
        {
            GD.PrintErr("Failed to write updated statistic to Steam: ", statId);
            return -1;
        }

        return value;
    }

    public bool SetIntStat(int statId, int value)
    {
        var stat = IAchievementStatStore.GetStatName(statId);

        if (stat == null)
        {
            GD.PrintErr("Updating invalid statistic to Steam: ", statId);
            return false;
        }

        if (!steamClient.SetSteamStatistic(stat, value))
        {
            GD.PrintErr("Failed to write updated statistic to Steam: ", statId);
            return false;
        }

        return true;
    }

    public void Reset()
    {
        GD.Print("Resetting Steam stats and achievements");
        if (!steamClient.ResetAllSteamAchievements())
        {
            GD.PrintErr("Failed to reset Steam achievements");
        }
    }
}
