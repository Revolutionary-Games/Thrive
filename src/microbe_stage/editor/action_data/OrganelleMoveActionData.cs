using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class OrganelleMoveActionData : HexMoveActionData<OrganelleTemplate, CellType>
{
    public OrganelleMoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_HEX;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.OrganelleMoveActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.OrganelleMoveActionData)
            throw new NotSupportedException();

        writer.WriteObject((OrganelleMoveActionData)obj);
    }

    public static OrganelleMoveActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        var instance = new OrganelleMoveActionData(reader.ReadObject<OrganelleTemplate>(), reader.ReadHex(),
            reader.ReadHex(), reader.ReadInt32(), reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, version);

        return instance;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // Endosymbionts can be moved for free after placing
            if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData &&
                MatchesContext(endosymbiontPlaceActionData))
            {
                // If moved after placing
                if (MovedHex == endosymbiontPlaceActionData.PlacedOrganelle &&
                    OldLocation == endosymbiontPlaceActionData.PlacementLocation &&
                    OldRotation == endosymbiontPlaceActionData.PlacementRotation)
                {
                    return (0, 0);
                }
            }
        }

        return base.CalculateCostInternal(history, insertPosition);
    }
}
