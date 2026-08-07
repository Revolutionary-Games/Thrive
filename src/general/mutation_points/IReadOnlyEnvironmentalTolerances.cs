public interface IReadOnlyEnvironmentalTolerances
{
    /// <summary>
    ///   Temperature (in C) that this species likes to be in
    /// </summary>
    public float PreferredTemperature { get; }

    /// <summary>
    ///   How wide a temperature range this species can stay in effectively. The range of temperatures is
    ///   <c>PreferredTemperature - TemperatureTolerance</c> to <c>PreferredTemperature + TemperatureTolerance</c>
    /// </summary>
    public float TemperatureTolerance { get; }

    /// <summary>
    ///   Minimum pressure this species likes. The value is in Pa (pascals). This is not just a single range as
    ///   the range needs to be lopsided towards surviving higher pressures.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The difference between the defaults may not be over Constants.TOLERANCE_PRESSURE_RANGE_MAX, otherwise the
    ///     GUI will break when this data is fed in.
    ///   </para>
    /// </remarks>
    public float PressureMinimum { get; }
    public float PressureTolerance { get; }

    /// <summary>
    ///   UV Resistance ranged in the unit interval.
    /// </summary>
    public float UVResistance { get; }

    /// <summary>
    ///   Oxygen Resistance ranged in the unit interval.
    /// </summary>
    public float OxygenResistance { get; }

    public EnvironmentalTolerances Clone()
    {
        var newTolerances = new EnvironmentalTolerances();
        newTolerances.CopyFrom(this);
        return newTolerances;
    }
}
