using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Controller for variation in sunlight during an in-game day for the current game.
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class DayNightCycle : IDaylightInfo
{
    [JsonProperty]
    private bool isEnabled;

    /// <summary>
    ///   Number of real-time seconds per in-game day.
    /// </summary>
    [JsonProperty]
    private int realTimePerDay;

    /// <summary>
    ///   Multiplier used for calculating DayLightFraction.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This exists as it only needs to be calculated once and the calculation for it is confusing.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    private float daytimeMultiplier;

    /// <summary>
    ///   How big part of the day is considered to be the day. Defaults to 0.5f.
    /// </summary>
    [JsonIgnore]
    private float daytimeFraction;

    /// <summary>
    ///   Controller for variation in sunlight during an in-game day for the current game.
    /// </summary>
    public DayNightCycle()
    {
        isEnabled = false;
        AverageSunlight = 1.0f;

        // Start the game at noon
        FractionOfDayElapsed = 0.5f;
    }

    /// <summary>
    ///   Current position in the day/night cycle, expressed as a fraction of the day elapsed so far.
    /// </summary>
    [JsonProperty]
    public float FractionOfDayElapsed { get; set; }

    [JsonIgnore]
    public float AverageSunlight { get; private set; }

    /// <summary>
    ///   The fraction of daylight you should get.
    ///   light = max(-(FractionOfDayElapsed - 0.5)^2 * daytimeMultiplier + 1, 0)
    ///   desmos: https://www.desmos.com/calculator/vrrk1bkac2
    /// </summary>
    [JsonIgnore]
    public float DayLightFraction =>
        isEnabled ? CalculatePointwiseSunlight(FractionOfDayElapsed, daytimeMultiplier) : 1.0f;

    /// <summary>
    ///   How long a single day/night cycle lasts in realtime seconds of gameplay
    /// </summary>
    [JsonIgnore]
    public float DayLengthRealtimeSeconds => realTimePerDay;

    /// <summary>
    ///   How long until the night starts. When negative it is currently night.
    /// </summary>
    [JsonIgnore]
    public float DayFractionUntilNightStart => FractionOfDayElapsed > daytimeFraction * 0.5f ?
        1 - daytimeFraction * 0.5f - FractionOfDayElapsed :
        -FractionOfDayElapsed - daytimeFraction * 0.5f;

    [JsonIgnore]
    public float SecondsUntilNightStart => DayFractionUntilNightStart * DayLengthRealtimeSeconds;

    public static float CalculateDayTimeMultiplier(float daytimeFraction)
    {
        // This converts the fraction in daytimeFraction to the power of two needed for DayLightFraction
        return Mathf.Pow(2, 2 / daytimeFraction);
    }

    public static float CalculateAverageSunlight(float daytimeMultiplier,
        WorldGenerationSettings worldGenerationSettings)
    {
        if (!worldGenerationSettings.DayNightCycleEnabled)
            return 1;

        return CalculateAverageSunlight(daytimeMultiplier);
    }

    /// <summary>
    ///   Applies the world settings. This needs to be called when this object is created (and not loaded from JSON)
    /// </summary>
    /// <param name="worldSettings">
    ///   The settings to apply. Note that changes to the settings object won't apply before calling this method again
    /// </param>
    public void ApplyWorldSettings(WorldGenerationSettings worldSettings)
    {
        isEnabled = worldSettings.DayNightCycleEnabled;
        realTimePerDay = worldSettings.DayLength;

        CalculateDependentLightData(worldSettings);
    }

    /// <summary>
    ///   Calculates some dependent values that are not saved, this is public to allow recomputing these after loading
    ///   a save
    /// </summary>
    public void CalculateDependentLightData(WorldGenerationSettings worldSettings)
    {
        daytimeFraction = worldSettings.DaytimeFraction;
        daytimeMultiplier = CalculateDayTimeMultiplier(daytimeFraction);

        AverageSunlight = isEnabled ? CalculateAverageSunlight(daytimeMultiplier) : 1.0f;
    }

    public void Process(float delta)
    {
        if (isEnabled && !CheatManager.LockTime)
        {
            FractionOfDayElapsed = (FractionOfDayElapsed + delta / realTimePerDay) % 1;
        }
    }

    /// <summary>
    ///   Calculates sunlight value (on a scale from 0 to 1) at a given point during the day.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If this equation is changed, <see cref="IntegratePointwiseSunlight"/> must also be updated.
    ///   </para>
    /// </remarks>
    /// <param name="x">Fraction of the day completed, between 0-1</param>
    /// <param name="daytimeMultiplier">Converted daylight fraction to usable form</param>
    private static float CalculatePointwiseSunlight(float x, float daytimeMultiplier)
    {
        return Math.Max(1 - daytimeMultiplier * Mathf.Pow(x - 0.5f, 2), 0);
    }

    /// <summary>
    ///   Calculates average sunlight over the course of a day. A relatively expensive operation so should be used
    ///   sparingly.
    /// </summary>
    private static float CalculateAverageSunlight(float daytimeMultiplier)
    {
        // Average is the integral across the interval divided by length of the interval. Since the interval is
        // [0, 1] and hence has length 1, we just return the integral. The current function is only non-zero in the
        // interval [0.5 - 1 / squareRoot(daytimeMultiplier), 0.5 + 1 / squareRoot(daytimeMultiplier)], so we can
        // reduce to only integrating over this interval.
        var daytimeMultiplierRootReciprocal = 1.0f / Mathf.Sqrt(daytimeMultiplier);
        var start = 0.5f - daytimeMultiplierRootReciprocal;
        var end = 0.5f + daytimeMultiplierRootReciprocal;
        return IntegratePointwiseSunlight(end, daytimeMultiplier) -
            IntegratePointwiseSunlight(start, daytimeMultiplier);
    }

    /// <summary>
    ///   Calculates the antiderivative of the sunlight function at a given point.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Based on <see cref="CalculatePointwiseSunlight"/>, so must be updated if that is updated.
    ///   </para>
    /// </remarks>
    /// <param name="x">Fraction of the day completed</param>
    /// <param name="daytimeMultiplier">Converted daylight fraction to usable form</param>
    private static float IntegratePointwiseSunlight(float x, float daytimeMultiplier)
    {
        return x - daytimeMultiplier * (Mathf.Pow(x, 3) / 3 - Mathf.Pow(x, 2) / 2 + 0.25f * x);
    }
}
