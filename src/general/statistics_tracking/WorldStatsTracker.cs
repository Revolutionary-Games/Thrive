using System;
using Newtonsoft.Json;
using UnlockConstraints;

/// <summary>
///   Relays statistics about the world and the player to the organelle unlocks system (and later achievements)
/// </summary>
[UseThriveSerializer]
public class WorldStatsTracker : IUnlockStateDataSource
{
    public WorldStatsTracker()
    {
        AllStatistics = new Lazy<IStatistic[]>(() =>
        {
            return new IStatistic[]
            {
                TotalEngulfedByPlayer,
                TotalDigestedByPlayer,
                TotalPlayerDeaths,
                PlayerReproductionStatistic,
            };
        });
    }

    /// <summary>
    ///   All the statistics recorded in <see cref="WorldStatsTracker"/>
    /// </summary>
    [JsonProperty]
    public Lazy<IStatistic[]> AllStatistics { get; private set; }

    [JsonProperty]
    public SimpleStatistic TotalEngulfedByPlayer { get; private set; } = new();

    [JsonProperty]
    public SimpleStatistic TotalDigestedByPlayer { get; private set; } = new();

    [JsonProperty]
    public SimpleStatistic TotalPlayerDeaths { get; private set; } = new();

    [JsonProperty]
    public ReproductionStatistic PlayerReproductionStatistic { get; private set; } = new();
}
