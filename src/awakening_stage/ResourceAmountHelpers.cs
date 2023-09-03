using System.Collections.Generic;
using System.Globalization;
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

    public static bool HasEnoughResource(WorldResource resource, float availableAmount,
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

            WriteResourceText(stringBuilder, tuple.Value.ToString(CultureInfo.CurrentCulture), tuple.Key.InternalName,
                enough, requirementMetIconFirst);

            first = false;
        }
    }

    /// <summary>
    ///   Variant of this method that allows resource container to be used as data source. This is in this class to
    ///   make this be next to the other method doing basically the same thing.
    /// </summary>
    public static void CreateRichTextForResourceAmounts(IReadOnlyDictionary<WorldResource, int> requiredResources,
        IResourceContainer availableResources, StringBuilder stringBuilder,
        bool requirementMetIconFirst = false)
    {
        bool first = true;

        foreach (var tuple in requiredResources)
        {
            if (!first)
            {
                stringBuilder.Append(", ");
            }

            bool enough = HasEnoughResource(tuple.Key, availableResources.GetAvailableAmount(tuple.Key),
                requiredResources);

            WriteResourceText(stringBuilder, tuple.Value.ToString(CultureInfo.CurrentCulture), tuple.Key.InternalName,
                enough, requirementMetIconFirst);

            first = false;
        }
    }

    private static void WriteResourceText(StringBuilder stringBuilder, string amount, string resourceType, bool enough,
        bool requirementMetIconFirst)
    {
        if (requirementMetIconFirst)
            AddRequirementConditionFulfillIcon(stringBuilder, enough);

        stringBuilder.Append(amount);

        // Icon for this material
        // TODO: make these clickable to show what the required material is
        stringBuilder.Append($"[thrive:resource type=\"{resourceType}\"][/thrive:resource]");

        if (!requirementMetIconFirst)
            AddRequirementConditionFulfillIcon(stringBuilder, enough);
    }

    private static void AddRequirementConditionFulfillIcon(StringBuilder stringBuilder, bool enough)
    {
        stringBuilder.Append(GUICommon.RequirementFulfillmentIconRichText(enough));
    }
}
