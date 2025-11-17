using System.Collections.Generic;

public class MacroscopicEditsFacade : SpeciesEditsFacade, IReadOnlyMacroscopicSpecies
{
    public MacroscopicEditsFacade(MacroscopicSpecies macroscopicSpecies) : base(macroscopicSpecies)
    {
    }

    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes { get; set; }
}
