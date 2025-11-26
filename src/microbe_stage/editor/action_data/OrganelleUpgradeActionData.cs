using System;
using SharedBase.Archive;

public class OrganelleUpgradeActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 2;

    public OrganelleUpgrades NewUpgrades;
    public OrganelleUpgrades OldUpgrades;

    /// <summary>
    ///   Position of the organelle when upgraded. Needed for facades to in all cases be able to identify the right
    ///   organelle to upgrade.
    /// </summary>
    public Hex Position;

    // TODO: make the upgrade not cost MP if a new organelle of the same type is placed at the same location and then
    // upgraded in the same way
    public OrganelleTemplate UpgradedOrganelle;

    public OrganelleUpgradeActionData(OrganelleUpgrades oldUpgrades, OrganelleUpgrades newUpgrades,
        OrganelleTemplate upgradedOrganelle)
    {
        OldUpgrades = oldUpgrades;
        NewUpgrades = newUpgrades;
        UpgradedOrganelle = upgradedOrganelle;

        // Store position in case the upgraded organelle is moved
        Position = upgradedOrganelle.Position;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData)
            throw new NotSupportedException();

        writer.WriteObject((OrganelleUpgradeActionData)obj);
    }

    public static OrganelleUpgradeActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new OrganelleUpgradeActionData(reader.ReadObject<OrganelleUpgrades>(),
            reader.ReadObject<OrganelleUpgrades>(), reader.ReadObject<OrganelleTemplate>());

        if (version > 1)
        {
            instance.Position = reader.ReadHex();
        }

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(OldUpgrades);
        writer.WriteObject(NewUpgrades);
        writer.WriteObject(UpgradedOrganelle);
        writer.Write(Position);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        // Doesn't need to merge as organelle upgrades are applied when hitting "ok" in the GUI and not for each slider
        // step
        return false;
    }
}
