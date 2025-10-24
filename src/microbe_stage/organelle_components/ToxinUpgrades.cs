﻿using System;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Upgrades for toxin firing organelles
/// </summary>
/// <remarks>
///   <para>
///     This is in a separate files as there isn't a toxin organelle component file to put this into
///   </para>
/// </remarks>
[JSONDynamicTypeAllowed]
public class ToxinUpgrades : IComponentSpecificUpgrades
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ToxinUpgrades(ToxinType baseType, float toxicity)
    {
        BaseType = baseType;
        Toxicity = toxicity;
    }

    /// <summary>
    ///   Category of the selected toxin to fire. Note that this doesn't *really* need to be here as the toxin type
    ///   is actually determined by the unlocked features in the base upgrades class, but for now this is here for
    ///   completeness’s sake. It is hopefully not possible for this to get out of sync with the other data.
    /// </summary>
    public ToxinType BaseType { get; set; }

    /// <summary>
    ///   Toxicity / speed of firing of the toxin. Range is -1 to 1, with 0 being the default. 1 is the maximum potency
    ///   and -1 is the maximum firerate with minimum potency.
    /// </summary>
    public float Toxicity { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ToxinUpgrades;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static ToxinUpgrades ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new ToxinUpgrades((ToxinType)reader.ReadInt32(), reader.ReadFloat());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)BaseType);
        writer.Write(Toxicity);
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is ToxinUpgrades toxinUpgrades)
        {
            return toxinUpgrades.BaseType == BaseType &&
                Math.Abs(Toxicity - toxinUpgrades.Toxicity) < MathUtils.EPSILON;
        }

        return false;
    }

    public object Clone()
    {
        return new ToxinUpgrades(BaseType, Toxicity);
    }

    public override int GetHashCode()
    {
        return int.RotateLeft(Toxicity.GetHashCode(), 3) ^ BaseType.GetHashCode();
    }

    public ulong GetVisualHashCode()
    {
        // Upgrades don't affect the visuals
        return 3L << 32;
    }
}
