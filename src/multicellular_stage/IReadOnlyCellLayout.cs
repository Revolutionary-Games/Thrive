using System;
using System.Collections;
using System.Collections.Generic;

public interface IReadOnlyCellLayout<T> : IReadOnlyHexLayout<T>
    where T : class, IReadOnlyCellTemplate
{
}

public class ReadonlyCellLayoutAdapter<TReadOnly, TUnderlying> : IReadOnlyCellLayout<TReadOnly>
    where TReadOnly : class, IReadOnlyCellTemplate
    where TUnderlying : CellTemplate, TReadOnly
{
    private readonly CellLayout<TUnderlying> wrappedLayout;

    public ReadonlyCellLayoutAdapter(CellLayout<TUnderlying> wrappedLayout)
    {
        this.wrappedLayout = wrappedLayout;
    }

    public int Count => wrappedLayout.Count;

    public TReadOnly this[int index]
    {
        get => wrappedLayout[index];
        set => throw new NotSupportedException();
    }

    public IEnumerator<TReadOnly> GetEnumerator()
    {
        return wrappedLayout.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TReadOnly? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        return wrappedLayout.GetElementAt(location, temporaryHexesStorage);
    }

    public TReadOnly? GetByExactElementRootPosition(Hex location)
    {
        return wrappedLayout.GetByExactElementRootPosition(location);
    }
}
