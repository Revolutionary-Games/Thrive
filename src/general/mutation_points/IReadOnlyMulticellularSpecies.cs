using System.Collections.Generic;

public interface IReadOnlyMulticellularSpecies : IReadOnlySpecies
{
    public IReadOnlyCellLayout<IReadOnlyCellTemplate> Cells { get; }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes { get; }
}
