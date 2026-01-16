using System;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;

public class HexWithData<T> : IPositionedHex, IReadOnlyHexWithData<T>, IArchivable
    where T : IActionHex, IArchivable, ICloneable
{
    public HexWithData(T? data, Hex position, int orientation)
    {
        // This is before the if-check to apply the custom value setter
        Orientation = orientation;

#if DEBUG
        if (data is IPositionedHex posHex)
        {
            if (posHex.Position != position || posHex.Orientation != Orientation)
            {
                GD.PrintErr("HexWithData position and orientation mismatch with data object");
            }
        }
#endif

        Data = data;
        Position = position;
    }

    public T? Data { get; set; }
    public Hex Position { get; set; }

    public int Orientation
    {
        get;

        // Normalize rotations to work similarly to CellTemplate, otherwise this will break everything by not matching
        // rotations
        set => field = value % 6;
    }

    public ushort CurrentArchiveVersion => HexLayoutSerializer.SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ExtendedHexWithData;

    /// <summary>
    ///   Has to be referenceable for the editor action history to work
    /// </summary>
    public bool CanBeReferencedInArchive => true;

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
        writer.Write(Orientation);
    }

    public HexWithData<T> Clone()
    {
        return new HexWithData<T>((T?)Data?.Clone(), Position, Orientation);
    }

    public override string ToString()
    {
        return $"Hex<{typeof(T).Name}> at {Position} with orientation {Orientation}, with data {Data}";
    }
}
