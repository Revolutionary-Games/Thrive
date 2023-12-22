using System.Collections.Generic;
using System.Linq;
using UnlockConstraints;

public static class OrganelleUnlockHelpers
{
    private static IEnumerable<IUnlockCondition> allUnlockConditions = null!;

    public static void InitConditionTracking(GameWorld world)
    {
        allUnlockConditions = SimulationParameters.Instance.GetAllOrganelles()
            .Where(o => o.UnlockConditions != null)
            .Select(o => o.UnlockConditions)
            .SelectMany(c => c)
            .Select(c => c.Requirements)
            .SelectMany(c => c);

        var statistics = world.StatisticsTracker.CollectStatistics();

        foreach (var constraint in allUnlockConditions)
        {
            if (constraint is StatisticBasedUnlockCondition condition)
            {
                condition.RelevantStatistic = statistics.First(s => s.Event == condition.RelevantEvent);
                condition.OnInit();
            }
        }
    }

    public static void UpdateUnlockConditionWorldData(GameWorld? gameWorld, ICellProperties? data,
        EnergyBalanceInfo? energyBalance)
    {
        foreach (var constraint in allUnlockConditions)
        {
            if (constraint is WorldBasedUnlockCondition condition)
                condition.UpdateData(gameWorld, data, energyBalance);
        }
    }
}