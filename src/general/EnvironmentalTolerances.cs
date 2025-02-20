using System;

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
    ///   Minimum pressure this species likes. The value is in Pa (pascals). This is not just a single range as
    ///   the range needs to be lopsided towards surviving higher pressures.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The difference between the defaults may not be over Constants.TOLERANCE_PRESSURE_RANGE_MAX, otherwise the
    ///     GUI will break when this data is fed in.
    ///   </para>
    /// </remarks>
    public float PressureMinimum = 71325;

    public float PressureMaximum = 301325;

    public float UVResistance;
    public float OxygenResistance;

    public static bool operator ==(EnvironmentalTolerances? left, EnvironmentalTolerances? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EnvironmentalTolerances? left, EnvironmentalTolerances? right)
    {
        return !Equals(left, right);
    }

    public void CopyFrom(EnvironmentalTolerances tolerancesToCopy)
    {
        PreferredTemperature = tolerancesToCopy.PreferredTemperature;
        TemperatureTolerance = tolerancesToCopy.TemperatureTolerance;
        PressureMinimum = tolerancesToCopy.PressureMinimum;
        PressureMaximum = tolerancesToCopy.PressureMaximum;
        UVResistance = tolerancesToCopy.UVResistance;
        OxygenResistance = tolerancesToCopy.OxygenResistance;
    }

    public void SanityCheck()
    {
        if (!SanityCheckNoThrow())
            throw new Exception("Tolerances are not valid (pressure is out of range)");
    }

    public bool SanityCheckNoThrow()
    {
        if (PressureMinimum > PressureMaximum)
            return false;

        if (PressureMaximum < 0)
            return false;

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((EnvironmentalTolerances)obj);
    }

    public bool EqualsApprox(EnvironmentalTolerances other)
    {
        return Math.Abs(PreferredTemperature - other.PreferredTemperature) < MathUtils.EPSILON &&
            Math.Abs(TemperatureTolerance - other.TemperatureTolerance) < MathUtils.EPSILON &&
            Math.Abs(PressureMinimum - other.PressureMinimum) < MathUtils.EPSILON &&
            Math.Abs(PressureMaximum - other.PressureMaximum) < MathUtils.EPSILON &&
            Math.Abs(UVResistance - other.UVResistance) < MathUtils.EPSILON &&
            Math.Abs(OxygenResistance - other.OxygenResistance) < MathUtils.EPSILON;
    }

    public EnvironmentalTolerances Clone()
    {
        var newTolerances = new EnvironmentalTolerances();
        newTolerances.CopyFrom(this);
        return newTolerances;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PreferredTemperature, TemperatureTolerance, PressureMinimum,
            PressureMaximum, UVResistance, OxygenResistance);
    }

    protected bool Equals(EnvironmentalTolerances other)
    {
        return PreferredTemperature.Equals(other.PreferredTemperature) &&
            TemperatureTolerance.Equals(other.TemperatureTolerance) &&
            PressureMinimum.Equals(other.PressureMinimum) &&
            PressureMaximum.Equals(other.PressureMaximum) && UVResistance.Equals(other.UVResistance) &&
            OxygenResistance.Equals(other.OxygenResistance);
    }
}
