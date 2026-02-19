namespace Components;

using System;
using System.Diagnostics;
using SharedBase.Archive;

/// <summary>
///   Has most of the variables controlling environmental effects on gameplay microbes
/// </summary>
[ComponentIsReadByDefault]
public struct MicrobeEnvironmentalEffects : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float OsmoregulationMultiplier;

    public float HealthMultiplier;

    /// <summary>
    ///   This is a copy of <see cref="BioProcesses.OverallSpeedModifier"/> to be able to transfer this data along
    ///   easily
    /// </summary>
    public float ProcessSpeedModifier;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeEnvironmentalEffects;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(OsmoregulationMultiplier);
        writer.Write(HealthMultiplier);
        writer.Write(ProcessSpeedModifier);
    }
}

public static class MicrobeEnvironmentalEffectsHelpers
{
    public static MicrobeEnvironmentalEffects ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeEnvironmentalEffects.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeEnvironmentalEffects.SERIALIZATION_VERSION);

        return new MicrobeEnvironmentalEffects
        {
            OsmoregulationMultiplier = reader.ReadFloat(),
            HealthMultiplier = reader.ReadFloat(),
            ProcessSpeedModifier = reader.ReadFloat(),
        };
    }

    /// <summary>
    ///   Applies data from calculated tolerance data to components. Note that this doesn't refresh maximum health in
    ///   case that changes (if it has been initialized already). Also applies specialization bonus as these both want
    ///   to write to the same thing. So that's why this needs that info.
    /// </summary>
    /// <param name="effects">Where to put the primary data on the effects</param>
    /// <param name="toleRanceData">Resolved tolerance data</param>
    /// <param name="specializationFactor">
    ///   Specialization factor for this cell. <see cref="MicrobeInternalCalculations.CalculateSpecializationBonus"/>
    /// </param>
    /// <param name="bioProcesses">Additional state is applied here</param>
    public static void ApplyEffects(this ref MicrobeEnvironmentalEffects effects,
        in ResolvedMicrobeTolerances toleRanceData, float specializationFactor, ref BioProcesses bioProcesses)
    {
        if (specializationFactor <= 0)
        {
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();

            throw new ArgumentException("Specialization factor must be positive", nameof(specializationFactor));
#else
            GD.PrintErr("Specialization factor must be positive, uninitialized species?");
#endif
        }

        effects.OsmoregulationMultiplier = toleRanceData.OsmoregulationModifier;

        effects.HealthMultiplier = toleRanceData.HealthModifier;

        effects.ProcessSpeedModifier = toleRanceData.ProcessSpeedModifier;

        bioProcesses.OverallSpeedModifier = toleRanceData.ProcessSpeedModifier * specializationFactor;
    }
}
