namespace UnlockConstraints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ConditionSet : IUnlockCondition
    {
        [JsonIgnore]
        public IUnlockCondition[] Requirements { get; private set; }

        [JsonConstructor]
        public ConditionSet(Dictionary<string, JObject> conditions)
        {
            // TODO: This could probably be done with a json converter
            Requirements = new IUnlockCondition[conditions.Count];

            int i = 0;
            foreach (var entry in conditions)
            {
                var name = entry.Key;
                var classType = Type.GetType($"{nameof(UnlockConstraints)}.{name}");

                if (classType == null)
                    throw new InvalidRegistryDataException($"Unlock condition {name} does not exist");

                var jsonObject = entry.Value;

                var unlockCondition = jsonObject.ToObject(classType) as IUnlockCondition;

                if (unlockCondition == null)
                    throw new InvalidRegistryDataException("Failed to parse unlock condition");

                Requirements[i] = unlockCondition;

                i++;
            }
        }

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
                    builder.Append(new LocalizedString("UNLOCK_AND"));

                var color = entry.Satisfied() ? "green" : "red";

                builder.Append($"[color={color}]");

                entry.GenerateTooltip(builder);

                builder.Append("[/color]");

                first = false;
            }
        }
    }
}