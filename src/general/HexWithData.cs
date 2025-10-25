using Saving.Serializers;
using SharedBase.Archive;

public class HexWithData<T> : IPositionedHex, IActionHex, IArchivable
    where T : IActionHex, IArchivable
{
    public HexWithData(T? data, Hex position)
    {
        Data = data;
        Position = position;
    }

    public T? Data { get; set; }
    public Hex Position { get; set; }

    public ushort CurrentArchiveVersion => HexLayoutSerializer.SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ExtendedHexWithData;
    public bool CanBeReferencedInArchive => false;

    public bool MatchesDefinition(IActionHex other)
    {
        if (other is HexWithData<T> casted)
        {
            if (ReferenceEquals(casted.Data, Data))
                return true;

            if (ReferenceEquals(null, casted.Data) || ReferenceEquals(null, Data))
                return false;

            return Data.MatchesDefinition(casted.Data);
        }

        return false;
    }

    // Read method is in HexLayoutSerializer.cs
    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(Data);
        writer.Write(Position);
    }
}
