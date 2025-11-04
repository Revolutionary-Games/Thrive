using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SharedBase.Archive;

/// <summary>
///   Upgrades for a placed or template organelle
/// </summary>
public class OrganelleUpgrades : ICloneable, IEquatable<OrganelleUpgrades>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   A list of "feature" names that have been unlocked for this organelle. Depends on the organelle components
    ///   what names they look for.
    /// </summary>
    public List<string> UnlockedFeatures { get; set; } = new();

    /// <summary>
    ///   Organelle type specific upgrade data. Null if not configured
    /// </summary>
    public IComponentSpecificUpgrades? CustomUpgradeData { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgrades;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static OrganelleUpgrades ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new OrganelleUpgrades
        {
            UnlockedFeatures = reader.ReadObject<List<string>>(),
            CustomUpgradeData = reader.ReadObjectOrNull<IComponentSpecificUpgrades>(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(UnlockedFeatures);
        writer.WriteObjectOrNull(CustomUpgradeData);
    }

    public bool Equals(OrganelleUpgrades? other)
    {
        // TODO: allow default value to equal null, see: https://github.com/Revolutionary-Games/Thrive/issues/4091
        if (other == null)
            return false;

        if (!UnlockedFeatures.SequenceEqual(other.UnlockedFeatures))
            return false;

        if (CustomUpgradeData == null)
            return other.CustomUpgradeData == null;

        if (other.CustomUpgradeData == null)
            return false;

        return CustomUpgradeData.Equals(other.CustomUpgradeData);
    }

    public object Clone()
    {
        return new OrganelleUpgrades
        {
            UnlockedFeatures = new List<string>(UnlockedFeatures),
            CustomUpgradeData = (IComponentSpecificUpgrades?)CustomUpgradeData?.Clone(),
        };
    }

    public override int GetHashCode()
    {
        return UnlockedFeatures.GetHashCode() * 3 ^
            (CustomUpgradeData != null ? CustomUpgradeData.GetHashCode() : 1) * 1151;
    }

    public ulong GetVisualHashCode()
    {
        // This assumes all unlocks affect the visuals, but that isn't really true
        return ulong.RotateRight(PersistentStringHash.GetHash(UnlockedFeatures), 5) ^
            (CustomUpgradeData != null ? CustomUpgradeData.GetVisualHashCode() : 1) * 1151;
    }
}
