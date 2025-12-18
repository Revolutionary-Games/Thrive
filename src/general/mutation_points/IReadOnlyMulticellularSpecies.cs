using System.Collections.Generic;

public interface IReadOnlyMulticellularSpecies : IReadOnlySpecies
{
    /// <summary>
    ///   The full layout where cells are positioned at their real positions to remove overlap
    /// </summary>
    public IReadOnlyCellLayout<IReadOnlyCellTemplate> CellLayout { get; }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes { get; }
}
