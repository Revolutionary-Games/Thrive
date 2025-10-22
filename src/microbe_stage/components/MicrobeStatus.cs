namespace Components;

using SharedBase.Archive;
using Godot;

/// <summary>
///   A collection place for various microbe status flags and variables that don't have more sensible components
///   to put them in
/// </summary>
public struct MicrobeStatus : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Variables related to movement sound playing
    public Vector3 LastLinearVelocity;
    public Vector3 LastLinearAcceleration;
    public float MovementSoundCooldownTimer;

    public float LastCheckedATPDamage;

    public float LastCheckedOxytoxyDigestionDamage;

    // TODO: remove if rate limited reproduction is not needed
    // public float LastCheckedReproduction;

    public float TimeUntilChemoreceptionUpdate;

    /// <summary>
    ///   Flips every reproduction update. Used to make compound use for reproduction distribute more evenly between
    ///   the compound types.
    /// </summary>
    public bool ConsumeReproductionCompoundsReverse;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeStatus;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class MicrobeStatusHelpers
{
    public static MicrobeStatus ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeStatus.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeStatus.SERIALIZATION_VERSION);

        return new MicrobeStatus
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
