using System.Collections.Generic;

public interface IReadOnlyMacroscopicSpecies : IReadOnlySpecies
{
    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes { get; }

    public IReadOnlyMacroscopicMetaballLayout BodyLayout { get; }
}
