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
    ///   Pressure this species likes to be in. The value is in Pa (pascals).
    /// </summary>
    public float PreferredPressure = 101325;

    /// <summary>
    ///   Minimum pressure this species likes. This is not just a single range as the range needs to be lopsided
    ///   towards surviving higher pressures.
    /// </summary>
    public float PressureToleranceMin = 760;

    public float PressureToleranceMax = 3000;

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
        PreferredPressure = tolerancesToCopy.PreferredPressure;
        PressureToleranceMin = tolerancesToCopy.PressureToleranceMin;
        PressureToleranceMax = tolerancesToCopy.PressureToleranceMax;
        UVResistance = tolerancesToCopy.UVResistance;
        OxygenResistance = tolerancesToCopy.OxygenResistance;
    }

    public EnvironmentalTolerances Clone()
    {
        var newTolerances = new EnvironmentalTolerances();
        newTolerances.CopyFrom(this);
        return newTolerances;
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

    public override int GetHashCode()
    {
        return HashCode.Combine(PreferredTemperature, TemperatureTolerance, PreferredPressure, PressureToleranceMin,
            PressureToleranceMax, UVResistance, OxygenResistance);
    }

    protected bool Equals(EnvironmentalTolerances other)
    {
        return PreferredTemperature.Equals(other.PreferredTemperature) &&
            TemperatureTolerance.Equals(other.TemperatureTolerance) &&
            PreferredPressure.Equals(other.PreferredPressure) &&
            PressureToleranceMin.Equals(other.PressureToleranceMin) &&
            PressureToleranceMax.Equals(other.PressureToleranceMax) && UVResistance.Equals(other.UVResistance) &&
            OxygenResistance.Equals(other.OxygenResistance);
    }
}
