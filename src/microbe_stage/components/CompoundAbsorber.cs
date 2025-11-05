namespace Components;

using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Entity that can absorb compounds from <see cref="CompoundCloudSystem"/>. Requires <see cref="WorldPosition"/>
///   and <see cref="CompoundStorage"/> components as well.
/// </summary>
public struct CompoundAbsorber : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   If not null, then this tracks the total absorbed compounds
    /// </summary>
    public Dictionary<Compound, float>? TotalAbsorbedCompounds;

    /// <summary>
    ///   How big the radius for absorption is
    /// </summary>
    public float AbsorbRadius;

    /// <summary>
    ///   How fast this can absorb things. If 0 then the absorption speed is not limited.
    /// </summary>
    public float AbsorbSpeed;

    /// <summary>
    ///   The effectiveness (ratio of gained vs compounds taken from the clouds) of absorption
    /// </summary>
    public float AbsorptionRatio;

    /// <summary>
    ///   When true, then the <see cref="CompoundBag"/> that we put things in must have useful compounds set and
    ///   only those will be absorbed
    /// </summary>
    public bool OnlyAbsorbUseful;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCompoundAbsorber;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        if (TotalAbsorbedCompounds != null)
        {
            writer.WriteObject(TotalAbsorbedCompounds);
        }
        else
        {
            writer.WriteNullObject();
        }

        writer.Write(AbsorbRadius);
        writer.Write(AbsorbSpeed);
        writer.Write(AbsorptionRatio);
        writer.Write(OnlyAbsorbUseful);
    }
}

public static class CompoundAbsorberHelpers
{
    public static CompoundAbsorber ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CompoundAbsorber.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CompoundAbsorber.SERIALIZATION_VERSION);

        return new CompoundAbsorber
        {
            TotalAbsorbedCompounds = reader.ReadObject<Dictionary<Compound, float>>(),
            AbsorbRadius = reader.ReadFloat(),
            AbsorbSpeed = reader.ReadFloat(),
            AbsorptionRatio = reader.ReadFloat(),
            OnlyAbsorbUseful = reader.ReadBool(),
        };
    }
}
