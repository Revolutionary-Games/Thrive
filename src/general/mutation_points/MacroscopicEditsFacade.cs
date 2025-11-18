using System;
using System.Collections.Generic;

public class MacroscopicEditsFacade : SpeciesEditsFacade, IReadOnlyMacroscopicSpecies
{
    // private readonly IReadOnlyMacroscopicSpecies macroscopicSpecies;

    public MacroscopicEditsFacade(MacroscopicSpecies species) : base(species)
    {
        // TODO: implement
        // macroscopicSpecies = species;
    }

    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes => throw new NotSupportedException("not implemented yet");
}
