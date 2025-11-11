using System;
using SharedBase.Archive;

public class PatchEventProperties : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public string CustomTooltip = string.Empty;
    public float SunlightAmbientMultiplier = 1.0f;
    public float TemperatureAmbientChange;
    public float TemperatureAmbientFixedValue = float.NaN;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.PatchEventProperties;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.PatchEventProperties)
            throw new NotSupportedException();

        writer.WriteObject((PatchEventProperties)obj);
    }

    public static PatchEventProperties ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new PatchEventProperties
        {
            CustomTooltip = reader.ReadString() ?? throw new NullArchiveObjectException(),
            SunlightAmbientMultiplier = reader.ReadFloat(),
            TemperatureAmbientChange = reader.ReadFloat(),
            TemperatureAmbientFixedValue = reader.ReadFloat(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(CustomTooltip);
        writer.Write(SunlightAmbientMultiplier);
        writer.Write(TemperatureAmbientChange);
        writer.Write(TemperatureAmbientFixedValue);
    }

    public override string ToString()
    {
        return
            $"SunlightAmbientMultiplier: {SunlightAmbientMultiplier}, " +
            $"TemperatureAmbientChange: {TemperatureAmbientChange}, " +
            $"TemperatureAmbientFixedValue: {TemperatureAmbientFixedValue}";
    }
}
