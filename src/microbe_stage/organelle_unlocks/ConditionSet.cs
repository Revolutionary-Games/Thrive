namespace UnlockConstraints
{
    using System.Linq;
    using Newtonsoft.Json;

    [JsonConverter(typeof(ConditionSetConverter))]
    public class ConditionSet
    {
        public ConditionSet(IUnlockCondition[] conditions)
        {
            Requirements = conditions;
        }

        public IUnlockCondition[] Requirements { get; }

        public bool Satisfied(WorldAndPlayerDataSource worldAndPlayerData)
        {
            return Requirements.All(c =>
                IsSatisfied(c, worldAndPlayerData, worldAndPlayerData.World.StatisticsTracker));
        }

        public void GenerateTooltip(LocalizedStringBuilder builder, WorldAndPlayerDataSource worldAndPlayerData)
        {
            var tracker = worldAndPlayerData.World.StatisticsTracker;

            var first = true;
            foreach (var entry in Requirements)
            {
                if (!first)
                {
                    builder.Append(new LocalizedString("AND_UNLOCK_CONDITION"));
                    builder.Append(" ");
                }

                var colour = IsSatisfied(entry, worldAndPlayerData, tracker) ?
                    Constants.CONDITION_GREEN_COLOUR :
                    Constants.CONDITION_RED_COLOUR;
                builder.Append($"[color={colour}]");

                entry.GenerateTooltip(builder,
                    entry is WorldBasedUnlockCondition ? worldAndPlayerData : tracker);

                builder.Append("[/color]");

                first = false;
            }
        }

        public void Check(string name)
        {
            foreach (var requirement in Requirements)
                requirement.Check(name);
        }

        public void Resolve(SimulationParameters parameters)
        {
            foreach (var requirement in Requirements)
                requirement.Resolve(parameters);
        }

        private bool IsSatisfied(IUnlockCondition condition, WorldAndPlayerDataSource worldAndPlayerData,
            WorldStatsTracker worldStatistics)
        {
            if (condition is WorldBasedUnlockCondition)
            {
                return condition.Satisfied(worldAndPlayerData);
            }

            return condition.Satisfied(worldStatistics);
        }
    }
}
