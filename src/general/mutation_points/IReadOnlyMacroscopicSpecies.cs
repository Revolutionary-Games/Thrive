using System.Collections.Generic;

public interface IReadOnlyMacroscopicSpecies : IReadOnlySpecies
{
    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes { get; }

    public IReadOnlyMacroscopicMetaballLayout BodyLayout { get; }
}
