using Newtonsoft.Json;

/// <summary>
///   Definition for the day night cycle.
/// </summary>
/// <remarks>Values for the DayNightCycle, as given in day_night_cycle.json</remarks>
public class DayNightConfiguration : IRegistryType
{
    public string InternalName { get; set; } = null!;

    /// <summary>
    ///   Number of in-game hours per in-game day.
    /// </summary>
    [JsonProperty]
    public float HoursPerDay { get; private set; }

    /// <summary>
    ///   Percentage of the in-game day which has sunlight.
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
