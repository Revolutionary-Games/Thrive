using System;
using SharedBase.Archive;

public class MulticellularReproductionActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MulticellularReproductionMethod OldReproductionMethod;
    public MulticellularReproductionMethod NewReproductionMethod;

    public MulticellularReproductionActionData(MulticellularReproductionMethod oldReproduction,
        MulticellularReproductionMethod newReproduction)
    {
        OldReproductionMethod = oldReproduction;
        NewReproductionMethod = newReproduction;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
    (ArchiveObjectType)ThriveArchiveObjectType.MulticellularReproductionActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MulticellularReproductionActionData)
            throw new NotSupportedException();

        writer.WriteObject((MulticellularReproductionActionData)obj);
    }

    public static MulticellularReproductionActionData ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MulticellularReproductionActionData((MulticellularReproductionMethod)reader.ReadInt32(),
            (MulticellularReproductionMethod)reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)OldReproductionMethod);
        writer.Write((int)NewReproductionMethod);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
