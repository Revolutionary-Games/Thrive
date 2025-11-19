using System;
using System.Collections.Generic;

public class MulticellularEditsFacade : SpeciesEditsFacade, IReadOnlyMulticellularSpecies
{
    // private readonly IReadOnlyMulticellularSpecies multicellularSpecies;

    public MulticellularEditsFacade(IReadOnlyMulticellularSpecies species) : base(species)
    {
        // TODO: implement
        // multicellularSpecies = species;
    }

    public IReadOnlyCellLayout<IReadOnlyCellTemplate> Cells => throw new NotSupportedException("not implemented yet");
    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes => throw new NotSupportedException("not implemented yet");
}
