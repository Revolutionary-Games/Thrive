using System;
using System.Collections;
using System.Collections.Generic;
using SharedBase.Archive;

public interface IReadOnlyIndividualLayout<T> : IReadOnlyHexLayout<IReadOnlyHexWithData<T>>
    where T : class, IReadOnlyPositionedHex, IActionHex
{
}

public class ReadonlyIndividualLayoutAdapter<T, T2> : IReadOnlyIndividualLayout<T2>
    where T : class, IReadOnlyPositionedHex, IActionHex, IArchivable, IReadOnlyHexWithData<T2>, ICloneable
    where T2 : class, IReadOnlyPositionedHex, IActionHex
{
    private readonly IndividualHexLayout<T> wrappedLayout;

    public ReadonlyIndividualLayoutAdapter(IndividualHexLayout<T> wrappedLayout)
    {
        this.wrappedLayout = wrappedLayout;
    }

    public int Count => wrappedLayout.Count;

    public IReadOnlyHexWithData<T2> this[int index]
    {
        get => wrappedLayout[index].Data ?? throw new InvalidOperationException("No data with this hex");
        set => throw new NotSupportedException();
    }

    public IEnumerator<IReadOnlyHexWithData<T2>> GetEnumerator()
    {
        return new LayoutEnumeratorConverter(wrappedLayout);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IReadOnlyHexWithData<T2>? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        return wrappedLayout.GetElementAt(location, temporaryHexesStorage)?.Data;
    }

    public IReadOnlyHexWithData<T2>? GetByExactElementRootPosition(Hex location)
    {
        return wrappedLayout.GetByExactElementRootPosition(location)?.Data;
    }

    private class LayoutEnumeratorConverter : IEnumerator<IReadOnlyHexWithData<T2>>
    {
        private readonly IEnumerator<IReadOnlyHexWithData<T>> wrappedEnumerator;

        public LayoutEnumeratorConverter(IndividualHexLayout<T> wrappedLayout)
        {
            wrappedEnumerator = wrappedLayout.GetEnumerator();
        }

        IReadOnlyHexWithData<T2> IEnumerator<IReadOnlyHexWithData<T2>>.Current =>
            wrappedEnumerator.Current.Data ?? throw new Exception("No data with this hex");

        object? IEnumerator.Current => wrappedEnumerator.Current.Data;

        public bool MoveNext()
        {
            return wrappedEnumerator.MoveNext();
        }

        public void Reset()
        {
            wrappedEnumerator.Reset();
        }

        public void Dispose()
        {
            wrappedEnumerator.Dispose();
        }
    }
}
