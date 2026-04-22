namespace ThriveTest.MulticellularStage.Tests;

using System.Collections.Generic;
using Xunit;

public class MulticellularSpeciesTests
{
    private readonly MembraneType dummyMembrane = new()
    {
        Name = "Dummy",
    };

    private readonly OrganelleDefinition singleHexOrganelle = new()
    {
        Name = "DummyCytoplasm",
        Hexes = [new Hex(0, 0)],
    };

    [Fact]
    public void MulticellularSpecies_SameCellTypeAdjacencyIncreasesSpecialization()
    {
        var species = new MulticellularSpecies(1, "Test", "species");
        var type1 = CreateCellType(2);
        var type2 = CreateCellType(1.5f);

        species.ModifiableCellTypes.Add(type1);
        species.ModifiableCellTypes.Add(type2);

        var temporaryMemory1 = new List<Hex>();
        var temporaryMemory2 = new List<Hex>();

        species.ModifiableGameplayCells.AddFast(new CellTemplate(type1, new Hex(0, 0), 0), temporaryMemory1,
            temporaryMemory2);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(type1, new Hex(1, 0), 0), temporaryMemory1,
            temporaryMemory2);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(type2, new Hex(0, 1), 0), temporaryMemory1,
            temporaryMemory2);

        Assert.Equal(1.05f, species.GetAdjacencySpecializationBonus(0), 0.0001f);
        Assert.Equal(1.05f, species.GetAdjacencySpecializationBonus(1), 0.0001f);
        Assert.Equal(1, species.GetAdjacencySpecializationBonus(2));
        Assert.Equal(1.9f, species.CalculateAverageSpecialization(), 0.0001f);
    }

    private CellType CreateCellType(float specializationBonus)
    {
        var organelles = new OrganelleLayout<OrganelleTemplate>();
        var temporaryMemory1 = new List<Hex>();
        var temporaryMemory2 = new List<Hex>();

        organelles.AddFast(new OrganelleTemplate(singleHexOrganelle, new Hex(0, 0), 0), temporaryMemory1,
            temporaryMemory2);

        return new CellType(organelles, dummyMembrane)
        {
            SpecializationBonus = specializationBonus,
        };
    }
}
