using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Saving.Serializers;
using SharedBase.Archive;

/// <summary>
///   Allows placing individual hexes with data in a layout
/// </summary>
/// <typeparam name="TData">The type of data to hold in hexes</typeparam>
public class IndividualHexLayout<TData> : HexLayout<HexWithData<TData>>, IReadOnlyIndividualLayout<TData>, IArchivable
    where TData : class, IActionHex, IArchivable, IReadOnlyPositionedHex, ICloneable
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

    public new IReadOnlyHexWithData<TData> this[int index]
    {
        get => existingHexes[index];
        set => existingHexes[index] = (HexWithData<TData>)value;
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ExtendedIndividualHexLayout)
            throw new NotSupportedException();

        writer.WriteObject((IArchivable)obj);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // We ensure that this organism is up-to-date and has an isolated layout for safety.
        if (existingHexes.Shared)
        {
            if (!existingHexes.Commit(false))
                existingHexes.IsolateIfShared();
        }

        writer.WriteObject(existingHexes.MainHexes);
        writer.WriteDelegateOrNull(onAdded);
        writer.WriteDelegateOrNull(onRemoved);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HexLayout<HexWithData<TData>> AsModifiable()
    {
        return this;
    }

    public new ReadOnlyEnumerator GetEnumerator()
    {
        return new ReadOnlyEnumerator(((HexLayout<HexWithData<TData>>)this).GetEnumerator());
    }

    IEnumerator<IReadOnlyHexWithData<TData>> IEnumerable<IReadOnlyHexWithData<TData>>.GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; ++i)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<IReadOnlyHexWithData<TData>>)this).GetEnumerator();
    }

    public new IReadOnlyHexWithData<TData>? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        return ((HexLayout<HexWithData<TData>>)this).GetElementAt(location, temporaryHexesStorage);
    }

    public new IReadOnlyHexWithData<TData>? GetByExactElementRootPosition(Hex location)
    {
        return ((HexLayout<HexWithData<TData>>)this).GetByExactElementRootPosition(location);
    }

    protected override void GetHexComponentPositions(HexWithData<TData> hex, List<Hex> result)
    {
        result.Clear();

        // The single hex is always at 0,0 as it's at the exact position the hex's overall position is
        result.Add(new Hex(0, 0));
    }

    public ref struct ReadOnlyEnumerator(HexLayoutView.Enumerator baseEnumerator)
    {
        private HexLayoutView.Enumerator baseEnumerator = baseEnumerator;

        public IReadOnlyHexWithData<TData> Current => baseEnumerator.Current;

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }
    }
}
