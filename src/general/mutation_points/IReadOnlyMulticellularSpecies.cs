using System.Collections.Generic;

public interface IReadOnlyMulticellularSpecies : IReadOnlySpecies
{
    /// <summary>
    ///   The simplified 1-hex cell layout used in the editor. If this doesn't exist, it is automatically created on
    ///   access from the <see cref="GameplayCells"/>.
    /// </summary>
    public IReadOnlyIndividualLayout<IReadOnlyCellTemplate> EditorCells { get; }

    /// <summary>
    ///   The full layout where cells are positioned at their real positions to remove overlap
    /// </summary>
    public IReadOnlyCellLayout<IReadOnlyCellTemplate> GameplayCells { get; }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes { get; }
}
