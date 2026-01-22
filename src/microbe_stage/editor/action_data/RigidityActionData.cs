using System;
using SharedBase.Archive;

public class RigidityActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.RigidityActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.RigidityActionData)
            throw new NotSupportedException();

        writer.WriteObject((RigidityActionData)obj);
    }

    public static RigidityActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new RigidityActionData(reader.ReadFloat(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(NewRigidity);
        writer.Write(PreviousRigidity);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is RigidityActionData;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var rigidityChangeActionData = (RigidityActionData)other;

        if (Math.Abs(PreviousRigidity - rigidityChangeActionData.NewRigidity) < MathUtils.EPSILON)
        {
            // Handle cancels out
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON)
            {
                NewRigidity = rigidityChangeActionData.NewRigidity;
                return;
            }

            PreviousRigidity = rigidityChangeActionData.PreviousRigidity;
            return;
        }

        NewRigidity = rigidityChangeActionData.NewRigidity;
    }
}
