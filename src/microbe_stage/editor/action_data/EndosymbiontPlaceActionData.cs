using System;
using SharedBase.Archive;

public class EndosymbiontPlaceActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public OrganelleTemplate PlacedOrganelle;
    public bool PerformedUnlock;

    public Hex PlacementLocation;
    public int PlacementRotation;

    /// <summary>
    ///   The related endosymbiosis data. Required to be able to fully roll back the editor state
    /// </summary>
    public EndosymbiosisData.InProgressEndosymbiosis RelatedEndosymbiosisAction;

    /// <summary>
    ///   When not null, undoing this action required replacing the endosymbiosis action, which is stored here for redo
    ///   purposes
    /// </summary>
    public EndosymbiosisData.InProgressEndosymbiosis? OverriddenEndosymbiosisOnUndo;

    public EndosymbiontPlaceActionData(OrganelleTemplate placedOrganelle, Hex placementLocation, int placementRotation,
        EndosymbiosisData.InProgressEndosymbiosis relatedEndosymbiosisAction)
    {
        PlacedOrganelle = placedOrganelle;
        PlacementLocation = placementLocation;
        PlacementRotation = placementRotation;
        RelatedEndosymbiosisAction = relatedEndosymbiosisAction;
    }

    public EndosymbiontPlaceActionData(EndosymbiosisData.InProgressEndosymbiosis fromEndosymbiosisData) : this(
        new OrganelleTemplate(fromEndosymbiosisData.TargetOrganelle, new Hex(0, 0), 0, true),
        new Hex(0, 0), 0, fromEndosymbiosisData)
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EndosymbiontPlaceActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.EndosymbiontPlaceActionData)
            throw new NotSupportedException();

        writer.WriteObject((EndosymbiontPlaceActionData)obj);
    }

    public static EndosymbiontPlaceActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new EndosymbiontPlaceActionData(reader.ReadObject<OrganelleTemplate>(), reader.ReadHex(),
            reader.ReadInt32(), reader.ReadObject<EndosymbiosisData.InProgressEndosymbiosis>());

        // Base version is different
        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        instance.OverriddenEndosymbiosisOnUndo = reader.ReadObjectOrNull<EndosymbiosisData.InProgressEndosymbiosis>();
        instance.PerformedUnlock = reader.ReadBool();
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(PlacedOrganelle);
        writer.Write(PlacementLocation);
        writer.Write(PlacementRotation);
        writer.WriteObject(RelatedEndosymbiosisAction);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);

        writer.WriteObjectOrNull(OverriddenEndosymbiosisOnUndo);
        writer.Write(PerformedUnlock);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
