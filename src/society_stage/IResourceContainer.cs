using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An abstract resource container that can hold some amount of resources
/// </summary>
[JsonObject(IsReference = true)]
public interface IResourceContainer
{
    /// <summary>
    ///   Adds some resources to this container
    /// </summary>
    /// <param name="resource">The resource to add</param>
    /// <param name="amount">The amount to add</param>
    /// <returns>
    ///   The amount that did NOT fit, if 0 then all the <see cref="amount"/> fit and is now in the container
    /// </returns>
    public float Add(WorldResource resource, float amount);

    /// <summary>
    ///   Gets the currently stored amount
    /// </summary>
    /// <param name="resource">Resource type to get</param>
    /// <returns>The amount of available resource of the specified type in the container</returns>
    public float GetAvailableAmount(WorldResource resource);

    /// <summary>
    ///   Takes some resource from this container
    /// </summary>
    /// <param name="resource">Resource type to take</param>
    /// <param name="wantedAmount">The amount of resource wanted</param>
    /// <param name="takePartial">If false then a partial amount is not taken</param>
    /// <returns>The amount taken, may be less than <see cref="wantedAmount"/> if not enough is available</returns>
    public float Take(WorldResource resource, float wantedAmount, bool takePartial = false);

    public IEnumerable<WorldResource> GetAvailableResources();

    /// <summary>
    ///   Takes all resources and adds them from the other resources
    /// </summary>
    /// <param name="otherResources">Where to take resources</param>
    public void TransferFrom(IResourceContainer otherResources);
}

public static class ResourceContainerHelpers
{
    public static WorldResource? CalculateMissingResource(this IResourceContainer resourceContainer,
        IEnumerable<KeyValuePair<WorldResource, int>> requiredResources)
    {
        foreach (var required in requiredResources)
        {
            if (resourceContainer.GetAvailableAmount(required.Key) < required.Value)
                return required.Key;
        }

        return null;
    }

    public static bool TakeResourcesIfPossible(this IResourceContainer resourceContainer,
        IReadOnlyCollection<KeyValuePair<WorldResource, int>> neededResources)
    {
        // First check that everything is available to not take partial resources
        foreach (var tuple in neededResources)
        {
            if (resourceContainer.GetAvailableAmount(tuple.Key) < tuple.Value)
                return false;
        }

        // Then take the resources
        foreach (var tuple in neededResources)
        {
            if (resourceContainer.Take(tuple.Key, tuple.Value) < tuple.Value)
                GD.PrintErr($"Failed to take enough of resource: {tuple.Key.InternalName}");
        }

        return true;
    }

    public static bool TakeResourcesIfPossible(this IResourceContainer resourceContainer,
        IReadOnlyCollection<KeyValuePair<WorldResource, float>> neededResources)
    {
        // First check that everything is available to not take partial resources
        foreach (var tuple in neededResources)
        {
            if (resourceContainer.GetAvailableAmount(tuple.Key) < tuple.Value)
                return false;
        }

        // Then take the resources
        foreach (var tuple in neededResources)
        {
            if (resourceContainer.Take(tuple.Key, tuple.Value) < tuple.Value)
                GD.PrintErr($"Failed to take enough of resource: {tuple.Key.InternalName}");
        }

        return true;
    }
}
