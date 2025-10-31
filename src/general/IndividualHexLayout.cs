using System;
using System.Collections.Generic;
using Saving.Serializers;
using SharedBase.Archive;

/// <summary>
///   Allows placing individual hexes with data in a layout
/// </summary>
/// <typeparam name="TData">The type of data to hold in hexes</typeparam>
public class IndividualHexLayout<TData> : HexLayout<HexWithData<TData>>, IArchivable
    where TData : IActionHex, IArchivable
{
    public IndividualHexLayout(Action<HexWithData<TData>> onAdded, Action<HexWithData<TData>>? onRemoved = null) : base(
        onAdded, onRemoved)
    {
    }

    public IndividualHexLayout()
    {
    }

    protected IndividualHexLayout(List<HexWithData<TData>> existingHexes, Action<HexWithData<TData>> onAdded,
        Action<HexWithData<TData>>? onRemoved = null) : base(existingHexes, onAdded, onRemoved)
    {
    }

    public ushort CurrentArchiveVersion => HexLayoutSerializer.SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ExtendedIndividualHexLayout;

    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ExtendedIndividualHexLayout)
            throw new NotSupportedException();

        writer.WriteObject((IArchivable)obj);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(existingHexes);
        writer.WriteDelegateOrNull(onAdded);
        writer.WriteDelegateOrNull(onRemoved);
    }

    protected override void GetHexComponentPositions(HexWithData<TData> hex, List<Hex> result)
    {
        result.Clear();

        // The single hex is always at 0,0 as it's at the exact position the hex's overall position is
        result.Add(new Hex(0, 0));
    }
}
