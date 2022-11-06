using Newtonsoft.Json;

/// <summary>
///   Definition for the day night cycle
/// </summary>
/// <remarks> Values for the DayNightCycle, as given in day_night_cycle.json </remarks>
public class DayNightConfiguration : IRegistryType
{
    public string InternalName { get; set; } = null!;

    [JsonProperty]
    public float HoursPerDay { get; private set; }

    /// <summary>
    ///   This is the percentage of the day that has sunlight
    /// </summary>
    [JsonProperty]
    public float DaytimePercentage { get; private set; }

    public void Check(string name)
    {
    }

    public void ApplyTranslations()
    {
    }
}
