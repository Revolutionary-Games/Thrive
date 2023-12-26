using Newtonsoft.Json;

/// <summary>
///   Relays statistics about the world and the player to the organelle unlocks system (and later achievements)
/// </summary>
[UseThriveSerializer]
public class WorldStatsTracker
{
    [JsonProperty]
    public SimpleStatistic TotalEngulfedByPlayer { get; private set; } = new(StatsTrackerEvent.PlayerEngulfedOther);

    [JsonProperty]
    public SimpleStatistic TotalPlayerDeaths { get; private set; } = new(StatsTrackerEvent.PlayerDied);

    [JsonProperty]
    public ReproductionStatistic PlayerReproductionStatistic { get; private set; } = new();

    public IStatistic[] CollectStatistics()
    {
        return new IStatistic[]
        {
            TotalEngulfedByPlayer,
            TotalPlayerDeaths,
            PlayerReproductionStatistic,
        };
    }
}
