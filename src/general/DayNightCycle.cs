using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Controller for variation in sunlight during an in-game day for the current game.
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class DayNightCycle
{
    [JsonProperty]
    private bool isEnabled;

    /// <summary>
    ///   Configuration details for the day/night cycle.
    /// </summary>
    [JsonProperty]
    private DayNightConfiguration lightCycleConfig;

    /// <summary>
    ///   Number of real-time seconds per in-game day.
    /// </summary>
    [JsonProperty]
    private int realTimePerDay;

    /// <summary>
    ///   Current position in the day/night cycle, expressed as a percentage of the day elapsed so far.
    /// </summary>
    [JsonProperty]
    private float percentOfDayElapsed;

    /// <summary>
    ///   Multiplier used for calculating DayLightPercentage.
    /// </summary>
    /// <remarks>
    ///   This exists as it only needs to be calculated once and the calculation for it is confusing.
    /// </remarks>
    [JsonIgnore]
    private float daytimeMultiplier;

    /// <summary>
    ///   Controller for variation in sunlight during an in-game day for the current game.
    /// </summary>
    /// <param name="isEnabled">Whether the day/night cycle is enabled for this game</param>
    /// <param name="realTimePerDay">Player configured length in real-time seconds of an in-game day</param>
    public DayNightCycle(bool isEnabled, int realTimePerDay)
    {
        this.isEnabled = isEnabled;
        this.realTimePerDay = realTimePerDay;

        lightCycleConfig = SimulationParameters.Instance.GetDayNightCycleConfiguration();

        // Start the game at noon
        percentOfDayElapsed = 0.5f;

        // This converts the percentage in DaytimePercentage to the power of two needed for DayLightPercentage
        daytimeMultiplier = Mathf.Pow(2, 2 / lightCycleConfig.DaytimeFraction);

        AverageSunlight = this.isEnabled ? CalculateAverageSunlight() : 1.0f;
    }

    [JsonIgnore]
    public float AverageSunlight { get; private set; }

    /// <summary>
    ///   The percentage of daylight you should get.
    ///   light = max(-(PercentOfDayElapsed - 0.5)^2 * daytimeMultiplier + 1, 0)
    ///   desmos: https://www.desmos.com/calculator/vrrk1bkac2
    /// </summary>
    [JsonIgnore]
    public float DayLightPercentage => isEnabled ? CalculatePointwiseSunlight(percentOfDayElapsed) : 1.0f;

    public void Process(float delta)
    {
        if (isEnabled)
        {
            percentOfDayElapsed = (percentOfDayElapsed + delta / realTimePerDay) % 1;
        }
    }

    /// <summary>
    ///   Calculates sunlight value (on a scale from 0 to 1) at a given point during the day.
    /// </summary>
    /// <remarks>
    ///   If this equation is changed, <see cref="IntegratePointwiseSunlight"/> must also be updated.
    /// </remarks>
    /// <param name="x">Percentage of the day completed</param>
    private float CalculatePointwiseSunlight(float x)
    {
        return Math.Max(1 - daytimeMultiplier * Mathf.Pow(x - 0.5f, 2), 0);
    }

    /// <summary>
    ///   Calculates average sunlight over the course of a day. A relatively expensive operation so should be used
    ///   sparingly.
    /// </summary>
    private float CalculateAverageSunlight()
    {
        // Average is the integral across the interval divided by length of the interval. Since the interval is
        // [0, 1] and hence has length 1, we just return the integral. The current function is only non-zero in the
        // interval [0.5 - 1 / squareRoot(daytimeMultiplier), 0.5 + 1 / squareRoot(daytimeMultiplier)], so we can
        // reduce to only integrating over this interval.
        var daytimeMultiplierRootReciprocal = 1.0f / Mathf.Sqrt(daytimeMultiplier);
        var start = 0.5f - daytimeMultiplierRootReciprocal;
        var end = 0.5f + daytimeMultiplierRootReciprocal;
        return IntegratePointwiseSunlight(end) - IntegratePointwiseSunlight(start);
    }

    /// <summary>
    ///   Calculates the antiderivative of the sunlight function at a given point.
    /// </summary>
    /// <remarks>
    ///   Based on <see cref="CalculatePointwiseSunlight"/>, so must be updated if that is updated.
    /// </remarks>
    /// <param name="x">Percentage of the day completed</param>
    private float IntegratePointwiseSunlight(float x)
    {
        return x - daytimeMultiplier * (Mathf.Pow(x, 3) / 3 - Mathf.Pow(x, 2) / 2 + 0.25f * x);
    }
}
