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

        [JsonIgnore]
        public IUnlockCondition[] Requirements { get; set; }

        public bool Satisfied(WorldAndPlayerEventArgs worldAndPlayerArgs)
        {
            var statisticArgs = new StatisticTrackerEventArgs(worldAndPlayerArgs.World.StatisticsTracker);

            return Requirements.All(c => IsSatisfied(c, worldAndPlayerArgs, statisticArgs));
        }

        public void GenerateTooltip(LocalizedStringBuilder builder, WorldAndPlayerEventArgs worldAndPlayerArgs)
        {
            var statisticArgs = new StatisticTrackerEventArgs(worldAndPlayerArgs.World.StatisticsTracker);

            var first = true;
            foreach (var entry in Requirements)
            {
                if (!first)
                {
                    builder.Append(new LocalizedString("AND_UNLOCK_CONDITION"));
                    builder.Append(" ");
                }

                var color = IsSatisfied(entry, worldAndPlayerArgs, statisticArgs) ? "green" : "red";
                builder.Append($"[color={color}]");

                entry.GenerateTooltip(builder,
                    entry is WorldBasedUnlockCondition ? worldAndPlayerArgs : statisticArgs);

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

        private bool IsSatisfied(IUnlockCondition condition, WorldAndPlayerEventArgs worldAndPlayerArgs,
            StatisticTrackerEventArgs statisticArgs)
        {
            if (condition is WorldBasedUnlockCondition)
            {
                return condition.Satisfied(worldAndPlayerArgs);
            }

            return condition.Satisfied(statisticArgs);
        }
    }
}
