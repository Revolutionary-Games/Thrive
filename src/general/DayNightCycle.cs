using System;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class DayNightCycle
{
    [JsonProperty]
    public DayNightConfiguration LightCycleConfig;

    [JsonProperty]
    public bool IsEnabled;

    [JsonProperty]
    public float PercentOfDayElapsed;

    /// <summary>
    ///   The multiplier used for calculating DayLightPercentage.
    /// </summary>
    /// <remarks>
    ///   This exists as it only needs to be calculated once and
    ///   the calculation for it is confusing.
    /// </remarks>
    [JsonIgnore]
    private float daytimeMultiplier;

    public DayNightCycle(bool isEnabled)
    {
        IsEnabled = isEnabled;

        LightCycleConfig = SimulationParameters.Instance.GetDayNightCycleConfiguration();

        // Start the game at noon
        PercentOfDayElapsed = 0.5f;

        float halfPercentage = LightCycleConfig.DaytimePercentage / 2;

        // This converts the percentage in DaytimePercentage to the power of two needed for DayLightPercentage
        daytimeMultiplier = Mathf.Pow(2, 1 / halfPercentage);

        if (IsEnabled)
        {
            AverageSunlight = CumulativeSunlightValue(0.5f + halfPercentage)
                - CumulativeSunlightValue(0.5f - halfPercentage);
        }
        else
        {
            AverageSunlight = 1;
        }
    }

    [JsonIgnore]
    public float AverageSunlight { get; private set; }

    /// <summary>
    ///   The percentage of daylight you should get.
    ///   light = max(-(PercentOfDayElapsed - 0.5)^2 * daytimeMultiplier + 1, 0)
    ///   desmos: https://www.desmos.com/calculator/vrrk1bkac2
    /// </summary>
    /// <remarks>
    ///   If this equation is changed EvaluateAverageSunlight needs to be updated.
    /// </remarks>
    [JsonIgnore]
    public float DayLightPercentage =>
        Math.Max(-Mathf.Pow(PercentOfDayElapsed - 0.5f, 2) * daytimeMultiplier + 1, 0);

    public void Process(float delta)
    {
        if (IsEnabled)
        {
            PercentOfDayElapsed = (PercentOfDayElapsed + delta / LightCycleConfig.RealTimePerDay) % 1;
        }
    }

    /// <summary>
    ///   Evaluates the DayLightPercentage Antiderivative to calculate AverageSunlight.
    /// </summary>
    /// <remarks>
    ///   This is based on DayLightPercentage equation so if somone wants to change it
    ///   they can do the calculus to fix this function.
    /// </remarks>
    private float CumulativeSunlightValue(float x)
    {
        return -daytimeMultiplier * (Mathf.Pow(x, 3) / 3 - Mathf.Pow(x, 2) / 2 + 0.25f * x) + x;
    }
}
