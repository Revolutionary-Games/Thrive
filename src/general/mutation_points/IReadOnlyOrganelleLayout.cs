using System.Collections.Generic;

public interface IReadOnlyOrganelleLayout<T> : IReadOnlyHexLayout<T>
    where T : class, IPositionedOrganelle
{
}

public interface IReadOnlyHexLayout<T> : IReadOnlyCollection<T>
    where T : class, IPositionedHex
{
    public T? GetElementAt(Hex location, List<Hex> temporaryHexesStorage);
}
