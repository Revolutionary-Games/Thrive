using System;
using System.Collections;
using System.Collections.Generic;

public interface IReadOnlyOrganelleLayout<T> : IReadOnlyHexLayout<T>
    where T : class, IReadOnlyPositionedOrganelle;

public interface IReadOnlyHexLayout<T> : IReadOnlyCollection<T>
    where T : class, IReadOnlyPositionedHex
{
    public T? GetElementAt(Hex location, List<Hex> temporaryHexesStorage);

    public T? GetByExactElementRootPosition(Hex location);
}

public class ReadonlyOrganelleLayoutAdapter<TReadOnly, TUnderlying> : IReadOnlyOrganelleLayout<TReadOnly>
    where TReadOnly : class, IReadOnlyPositionedOrganelle
    where TUnderlying : class, IPositionedOrganelle, ICloneable, TReadOnly
{
    private readonly OrganelleLayout<TUnderlying> wrappedLayout;

    public ReadonlyOrganelleLayoutAdapter(OrganelleLayout<TUnderlying> wrappedLayout)
    {
        this.wrappedLayout = wrappedLayout;
    }

    public int Count => wrappedLayout.Count;

    public TReadOnly this[int index]
    {
        get => wrappedLayout[index];
        set => throw new NotSupportedException();
    }

    public TReadOnly? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        return wrappedLayout.GetElementAt(location, temporaryHexesStorage);
    }

    public TReadOnly? GetByExactElementRootPosition(Hex location)
    {
        return wrappedLayout.GetByExactElementRootPosition(location);
    }

    public IEnumerator<TReadOnly> GetEnumerator()
    {
        return wrappedLayout.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
