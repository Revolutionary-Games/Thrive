/// <summary>
///   Finalized tolerance calculation results. This is a struct as these need to be created a *ton*
/// </summary>
public struct ToleranceResult
{
    // The scores are doubles to avoid rounding problems where the score is 1 but something is missing
    public double OverallScore;

    public double TemperatureScore;

    /// <summary>
    ///   How to adjust the preferred temperature to get to the exact value in the biome
    /// </summary>
    public float PerfectTemperatureAdjustment;

    /// <summary>
    ///   How to adjust the tolerance range of temperature to qualify as perfectly adapted
    /// </summary>
    public float TemperatureRangeSizeAdjustment;

    public double PressureScore;
    public float PerfectPressureAdjustment;
    public float PressureRangeSizeAdjustment;

    public double OxygenScore;
    public float PerfectOxygenAdjustment;

    public double UVScore;
    public float PerfectUVAdjustment;
}
