using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Tracks damage per source type
/// </summary>
public class DamageStatistic : IStatistic
{
    [JsonProperty]
    public Dictionary<string, float> DamageByType { get; set; } = new();

    public float GetDamageBySource(string sourceName)
    {
        DamageByType.TryGetValue(sourceName, out var damage);
        return damage;
    }

    public void IncrementDamage(string sourceName, float amount)
    {
        DamageByType.TryGetValue(sourceName, out var damage);
        DamageByType[sourceName] = damage + amount;
    }
}
