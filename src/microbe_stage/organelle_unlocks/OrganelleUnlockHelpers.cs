using System.Collections.Generic;
using System.Linq;
using UnlockConstraints;

public static class OrganelleUnlockHelpers
{
    private static IEnumerable<IUnlockCondition>? allUnlockConditions;

    public static void InitConditionTracking(GameWorld world)
    {
        GetAllUnlockConditions();

        var statistics = world.StatisticsTracker.CollectStatistics();

        foreach (var constraint in allUnlockConditions!)
        {
            if (constraint is StatisticBasedUnlockCondition condition)
                condition.RelevantStatistic = statistics.First(s => s.LinkedEvent == condition.RelevantEvent);
        }
    }

    public static void UpdateUnlockConditionWorldData(GameWorld? gameWorld, ICellProperties? data,
        EnergyBalanceInfo? energyBalance)
    {
        GetAllUnlockConditions();

        foreach (var constraint in allUnlockConditions!)
        {
            if (constraint is WorldBasedUnlockCondition condition)
                condition.UpdateData(gameWorld, data, energyBalance);
        }
    }

    private static void GetAllUnlockConditions()
    {
        allUnlockConditions ??= SimulationParameters.Instance.GetAllOrganelles()
            .Where(o => o.UnlockConditions != null)
            .Select(o => o.UnlockConditions)
            .SelectMany(c => c)
            .Select(c => c.Requirements)
            .SelectMany(c => c);
    }
}
