using System;
using SharedBase.Archive;

public class ToleranceActionData : EditorCombinableActionData
{
    public const ushort SERIALIZATION_VERSION = 1;

    public EnvironmentalTolerances OldTolerances;
    public EnvironmentalTolerances NewTolerances;

    public ToleranceActionData(EnvironmentalTolerances oldTolerances, EnvironmentalTolerances newTolerances)
    {
        OldTolerances = oldTolerances;
        NewTolerances = newTolerances;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ToleranceActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ToleranceActionData)
            throw new NotSupportedException();

        writer.WriteObject((ToleranceActionData)obj);
    }

    public static ToleranceActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var tolerances1 = new EnvironmentalTolerances();
        var tolerances2 = new EnvironmentalTolerances();

        reader.ReadObjectProperties(tolerances1);
        reader.ReadObjectProperties(tolerances2);

        var instance = new ToleranceActionData(tolerances1, tolerances2);

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectProperties(OldTolerances);
        writer.WriteObjectProperties(NewTolerances);

        writer.Write(SERIALIZATION_VERSION_EDITOR);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        // These must always merge with other tolerance actions, because otherwise the undo history step count is going
        // to explore
        if (other is ToleranceActionData toleranceActionData)
        {
            var ourChanges = NewTolerances.GetChangedStats(OldTolerances);

            if (ourChanges == 0)
                return true;

            // Only allow combining if both actions changed the same stats only
            if (toleranceActionData.NewTolerances.GetChangedStats(toleranceActionData.OldTolerances) == ourChanges)
                return true;
        }

        return false;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var otherTolerance = (ToleranceActionData)other;

        if (OldTolerances.EqualsApprox(otherTolerance.NewTolerances))
        {
            // Handle cancels out
            if (NewTolerances.EqualsApprox(otherTolerance.OldTolerances))
            {
                NewTolerances = otherTolerance.NewTolerances;
                return;
            }

            OldTolerances = otherTolerance.OldTolerances;
            return;
        }

        NewTolerances = otherTolerance.NewTolerances;
    }
}
