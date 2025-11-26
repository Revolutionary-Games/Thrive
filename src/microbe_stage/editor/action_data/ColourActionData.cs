using System;
using Godot;
using SharedBase.Archive;

public class ColourActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Color NewColour;
    public Color PreviousColour;

    public ColourActionData(Color newColour, Color previousColour)
    {
        NewColour = newColour;
        PreviousColour = previousColour;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ColourActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ColourActionData)
            throw new NotSupportedException();

        writer.WriteObject((ColourActionData)obj);
    }

    public static ColourActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new ColourActionData(reader.ReadColor(), reader.ReadColor());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(NewColour);
        writer.Write(PreviousColour);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is ColourActionData;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var colourChangeActionData = (ColourActionData)other;

        if (PreviousColour.IsEqualApprox(colourChangeActionData.NewColour))
        {
            // Handle cancels out
            if (NewColour.IsEqualApprox(colourChangeActionData.PreviousColour))
            {
                NewColour = colourChangeActionData.NewColour;
                return;
            }

            PreviousColour = colourChangeActionData.PreviousColour;
            return;
        }

        NewColour = colourChangeActionData.NewColour;
    }
}
