namespace UnlockConstraints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ConditionSet : IUnlockCondition
    {
        [JsonConstructor]
        public ConditionSet(Dictionary<string, JObject> conditions)
        {
            // TODO: This could probably be done with a json converter
            Requirements = new IUnlockCondition[conditions.Count];

            int i = 0;
            foreach (var entry in conditions)
            {
                var name = entry.Key;
                var classType = Type.GetType($"{nameof(UnlockConstraints)}.{name}") ??
                    throw new InvalidRegistryDataException($"Unlock condition {name} does not exist");

                var jsonObject = entry.Value;

                if (jsonObject.ToObject(classType) is not IUnlockCondition unlockCondition)
                    throw new InvalidRegistryDataException("Failed to parse unlock condition");

                Requirements[i] = unlockCondition;

                i++;
            }
        }

        [JsonIgnore]
        public IUnlockCondition[] Requirements { get; private set; }

        public bool Satisfied()
        {
            return Requirements.All(c => c.Satisfied());
        }

        public void GenerateTooltip(LocalizedStringBuilder builder)
        {
            var first = true;
            foreach (var entry in Requirements)
            {
                if (!first)
                {
                    builder.Append(new LocalizedString("UNLOCK_AND"));
                    builder.Append(" ");
                }

                var color = entry.Satisfied() ? "green" : "red";

                builder.Append($"[color={color}]");

                entry.GenerateTooltip(builder);

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
            {
                if (requirement is StatisticBasedUnlockCondition statBased)
                    statBased.Resolve(parameters);
            }
        }
    }
}
