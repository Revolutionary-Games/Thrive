using Newtonsoft.Json;

/// <summary>
///   Definition for the day night cycle.
/// </summary>
/// <remarks>
///   <para>
///     Values for the <see cref="DayNightCycle"/>, as given in day_night_cycle.json
///   </para>
/// </remarks>
public class DayNightConfiguration : IRegistryType
{
    public string InternalName { get; set; } = null!;

    /// <summary>
    ///   Number of in-game hours per in-game day.
    /// </summary>
    [JsonProperty]
    public float HoursPerDay { get; private set; }

    /// <summary>
    ///   Fraction of the in-game day which has sunlight.
    /// </summary>
    [JsonProperty]
    public float DaytimeFraction { get; private set; }

    /// <summary>
    ///   Fraction of the in-game day which does not has sunlight.
    /// </summary>
    [JsonIgnore]
    public float NighttimeFraction { get; private set; }


    public void Check(string name)
    {
        if (HoursPerDay < 0.0f)
            throw new InvalidRegistryDataException(name, GetType().Name, "Hours per day must be non-negative");

        if (DaytimeFraction is < 0.0f or > 1.0f)
            throw new InvalidRegistryDataException(name, GetType().Name, "Daytime fraction must be between 0 and 1");
    }

    public void ApplyTranslations()
    {
    }
}
