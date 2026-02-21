using System;
using SharedBase.Archive;

/// <summary>
///   Environmental tolerances of a species
/// </summary>
public class EnvironmentalTolerances : IArchiveUpdatable, IReadOnlyEnvironmentalTolerances
{
    public const ushort SERIALIZATION_VERSION = 2;

    [Flags]
    public enum ToleranceChangedStats
    {
        Temperature = 1,
        Pressure = 2,
        UVResistance = 4,
        OxygenResistance = 8,
    }

    /// <summary>
    ///   Temperature (in C) that this species likes to be in
    /// </summary>
    public float PreferredTemperature { get; set; } = 15;

    /// <summary>
    ///   How wide a temperature range this species can stay in effectively. The range of temperatures is
    ///   <c>PreferredTemperature - TemperatureTolerance</c> to <c>PreferredTemperature + TemperatureTolerance</c>
    /// </summary>
    public float TemperatureTolerance { get; set; } = 21;

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
    public float PressureMinimum { get; set; } = 71325;

    public float PressureTolerance { get; set; } = 2000000;

    public float UVResistance { get; set; }
    public float OxygenResistance { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalTolerances;

    public static bool operator ==(EnvironmentalTolerances? left, EnvironmentalTolerances? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EnvironmentalTolerances? left, EnvironmentalTolerances? right)
    {
        return !Equals(left, right);
    }

    public void CopyFrom(IReadOnlyEnvironmentalTolerances tolerancesToCopy)
    {
        PreferredTemperature = tolerancesToCopy.PreferredTemperature;
        TemperatureTolerance = tolerancesToCopy.TemperatureTolerance;
        PressureMinimum = tolerancesToCopy.PressureMinimum;
        PressureTolerance = tolerancesToCopy.PressureTolerance;
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
        if (PressureTolerance < 0)
            return false;

        return true;
    }

    public ToleranceChangedStats GetChangedStats(EnvironmentalTolerances other)
    {
        ToleranceChangedStats changes = 0;

        if (Math.Abs(PreferredTemperature - other.PreferredTemperature) > 0.01f ||
            Math.Abs(TemperatureTolerance - other.TemperatureTolerance) > 0.01f)
        {
            changes |= ToleranceChangedStats.Temperature;
        }

        if (Math.Abs(PressureMinimum - other.PressureMinimum) > 0.01f ||
            Math.Abs(PressureTolerance - other.PressureTolerance) > 0.01f)
        {
            changes |= ToleranceChangedStats.Pressure;
        }

        if (Math.Abs(UVResistance - other.UVResistance) > 0.01f)
            changes |= ToleranceChangedStats.UVResistance;

        if (Math.Abs(OxygenResistance - other.OxygenResistance) > 0.01f)
            changes |= ToleranceChangedStats.OxygenResistance;

        return changes;
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(PreferredTemperature);
        writer.Write(TemperatureTolerance);
        writer.Write(PressureMinimum);
        writer.Write(PressureTolerance);
        writer.Write(UVResistance);
        writer.Write(OxygenResistance);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        PreferredTemperature = reader.ReadFloat();
        TemperatureTolerance = reader.ReadFloat();
        PressureMinimum = reader.ReadFloat();

        if (version <= 1)
        {
            PressureTolerance = Math.Min(reader.ReadFloat() - PressureMinimum, Constants.TOLERANCE_PRESSURE_RANGE_MAX);
        }
        else
        {
            PressureTolerance = reader.ReadFloat();
        }

        UVResistance = reader.ReadFloat();
        OxygenResistance = reader.ReadFloat();
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
            Math.Abs(PressureTolerance - other.PressureTolerance) < MathUtils.EPSILON &&
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
            PressureTolerance, UVResistance, OxygenResistance);
    }

    protected bool Equals(EnvironmentalTolerances other)
    {
        return PreferredTemperature.Equals(other.PreferredTemperature) &&
            TemperatureTolerance.Equals(other.TemperatureTolerance) &&
            PressureMinimum.Equals(other.PressureMinimum) &&
            PressureTolerance.Equals(other.PressureTolerance) && UVResistance.Equals(other.UVResistance) &&
            OxygenResistance.Equals(other.OxygenResistance);
    }
}
