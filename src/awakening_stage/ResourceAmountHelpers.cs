using System.Collections.Generic;
using System.Text;

public static class ResourceAmountHelpers
{
    /// <summary>
    ///   Find the first (if any) missing resource
    /// </summary>
    /// <param name="availableResources">The resources that are available</param>
    /// <param name="requiredResources">What resources are needed</param>
    /// <returns>The first resource in <see cref="requiredResources"/> that isn't available</returns>
    public static WorldResource? CalculateMissingResource(IReadOnlyDictionary<WorldResource, int> availableResources,
        IEnumerable<KeyValuePair<WorldResource, int>> requiredResources)
    {
        foreach (var required in requiredResources)
        {
            if (!availableResources.TryGetValue(required.Key, out var amount))
                return required.Key;

            if (amount < required.Value)
                return required.Key;
        }

        return null;
    }

    public static bool HasEnoughResource(WorldResource resource, int availableAmount,
        IReadOnlyDictionary<WorldResource, int> requiredResources)
    {
        if (!requiredResources.TryGetValue(resource, out var requiredAmount))
            return false;

        return availableAmount >= requiredAmount;
    }

    public static void CreateRichTextForResourceAmounts(IReadOnlyDictionary<WorldResource, int> requiredResources,
        IReadOnlyDictionary<WorldResource, int> availableResources, StringBuilder stringBuilder,
        bool requirementMetIconFirst = false)
    {
        bool first = true;

        foreach (var tuple in requiredResources)
        {
            if (!first)
            {
                stringBuilder.Append(", ");
            }

            availableResources.TryGetValue(tuple.Key, out var availableAmount);

            bool enough = HasEnoughResource(tuple.Key, availableAmount, requiredResources);

            if (requirementMetIconFirst)
                AddRequirementConditionFulfillIcon(stringBuilder, enough);

            stringBuilder.Append(tuple.Value);

            // Icon for this material
            // TODO: make these clickable to show what the required material is
            stringBuilder.Append($"[thrive:resource type=\"{tuple.Key.InternalName}\"][/thrive:resource]");

            if (!requirementMetIconFirst)
                AddRequirementConditionFulfillIcon(stringBuilder, enough);

            first = false;
        }
    }

    private static void AddRequirementConditionFulfillIcon(StringBuilder stringBuilder, bool enough)
    {
        stringBuilder.Append(GUICommon.RequirementFulfillmentIconRichText(enough));
    }
}
