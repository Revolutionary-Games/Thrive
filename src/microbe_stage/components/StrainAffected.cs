namespace Components;

using System;
using SharedBase.Archive;

/// <summary>
///   Contains variables related to strain
/// </summary>
public struct StrainAffected : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The current amount of strain
    /// </summary>
    public float CurrentStrain;

    /// <summary>
    ///   The amount of time the organism has to wait before <see cref="CurrentStrain"/> sarts to fall
    /// </summary>
    public float StrainDecreaseCooldown;

    /// <summary>
    ///   True when sprinting or when strain is supposed to be otherwise generated
    /// </summary>
    public bool IsUnderStrain;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentStrainAffected;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(CurrentStrain);
        writer.Write(StrainDecreaseCooldown);
        writer.Write(IsUnderStrain);
    }
}

public static class StrainAffectedHelpers
{
    public static StrainAffected ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > StrainAffected.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, StrainAffected.SERIALIZATION_VERSION);

        return new StrainAffected
        {
            CurrentStrain = reader.ReadFloat(),
            StrainDecreaseCooldown = reader.ReadFloat(),
            IsUnderStrain = reader.ReadBool(),
        };
    }

    public static float CalculateStrainFraction(this ref StrainAffected affected)
    {
        return Math.Max(0, affected.CurrentStrain - Constants.CANCELED_STRAIN) / Constants.MAX_STRAIN_PER_ENTITY;
    }
}
