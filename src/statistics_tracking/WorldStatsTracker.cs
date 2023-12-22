using System.Collections.Generic;
using System.Linq;
using UnlockConstraints;

/// <summary>
///   Relays statistics about the world and the player to the organelle unlocks system (and later achievements)
/// </summary>
[UseThriveSerializer]
public class WorldStatsTracker
{
    public SimpleStatistic TotalEngulfedByPlayer { get; private set; } = new(StatsTrackerEvent.PlayerEngulfedOther);

    public SimpleStatistic TotalPlayerDeaths { get; private set; } = new(StatsTrackerEvent.PlayerDied);

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
