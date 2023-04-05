using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Storage of a society's resources
/// </summary>
public class SocietyResourceStorage : IResourceContainer
{
    [JsonProperty]
    private readonly Dictionary<WorldResource, float> resources = new();

    /// <summary>
    ///   Capacity that limits how much of each resource type can be added
    /// </summary>
    [JsonIgnore]
    public float Capacity { get; set; }

    public float Add(WorldResource resource, float amount)
    {
        if (amount < 0)
            throw new ArgumentException("Can't add negative resource");

        resources.TryGetValue(resource, out var existing);

        float newAmount = Mathf.Clamp(existing + amount, 0, Capacity);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (newAmount == existing)
            return amount;

        resources[resource] = newAmount;

        return amount - (newAmount - existing);
    }

    public float GetAvailableAmount(WorldResource resource)
    {
        if (!resources.TryGetValue(resource, out var amount))
            return 0;

        return amount;
    }

    public IEnumerable<KeyValuePair<WorldResource, float>> GetAllResources()
    {
        return resources;
    }
}
