using System.Collections.Generic;

public class MulticellularEditsFacade : SpeciesEditsFacade, IReadOnlyMulticellularSpecies
{
    private readonly IReadOnlyMulticellularSpecies multicellularSpecies;

    public MulticellularEditsFacade(IReadOnlyMulticellularSpecies species) : base(species)
    {
        multicellularSpecies = species;
    }

    public IReadOnlyCellLayout<IReadOnlyCellTemplate> Cells { get; set; }
    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes { get; set; }
}
