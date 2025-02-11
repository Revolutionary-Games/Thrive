/// <summary>
///   Environmental tolerances of a species
/// </summary>
public class EnvironmentalTolerances
{
    /// <summary>
    ///   Temperature (in C) that this species likes to be in
    /// </summary>
    public float PreferredTemperature = 15;

    /// <summary>
    ///   How wide a temperature range this species can stay in effectively. The range of temperatures is
    ///   <c>PreferredTemperature - TemperatureTolerance</c> to <c>PreferredTemperature + TemperatureTolerance</c>
    /// </summary>
    public float TemperatureTolerance = 21;

    /// <summary>
    ///   Pressure this species likes to be in. The value is in hPa (hectopascals).
    /// </summary>
    public float PreferredPressure = 1013.25f;

    /// <summary>
    ///   Minimum pressure this species likes. This is not just a single range as the range needs to be lopsided
    ///   towards surviving higher pressures.
    /// </summary>
    public float PressureToleranceMin = 760;

    public float PressureToleranceMax = 3000;

    public float UVResistance;
    public float OxygenResistance;
}
