using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MulticellularSpeciesTests
{
    [TestCase]
    public void TestCloneSharingCellTypes()
    {
        var species = new MulticellularSpecies(1, "Test", "Species");
        var organelleType = SimulationParameters.Instance.GetOrganelleType("cytoplasm");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(organelleType, new Hex(0, 0), 0));

        species.ModifiableCellTypes.Add(cellType);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var cellTemplate = new CellTemplate(cellType, new Hex(0, 0), 0);
        species.ModifiableGameplayCells.AddFast(cellTemplate, workMemory1, workMemory2);

        // Ensure editor cells are generated
        var editorCells = species.ModifiableEditorCells;
        AssertThat(editorCells.Count).IsEqual(1);

        species.ModifiableSporeCellType = cellType;

        var cloned = (MulticellularSpecies)species.Clone();

        AssertThat(cloned.ModifiableCellTypes).HasSize(1);
        AssertThat(cloned.ModifiableGameplayCells.Count).IsEqual(1);
        AssertThat(cloned.ModifiableEditorCells.Count).IsEqual(1);

        var clonedCellType = cloned.ModifiableCellTypes[0];
        var clonedGameplayCellType = cloned.ModifiableGameplayCells[0].ModifiableCellType;
        var clonedEditorCellType = cloned.ModifiableEditorCells[0].Data?.ModifiableCellType;

        AssertThat(clonedGameplayCellType).IsSame(clonedCellType);
        AssertThat(clonedEditorCellType).IsNotNull().IsSame(clonedCellType);
        AssertThat(cloned.ModifiableSporeCellType).IsSame(clonedCellType);
    }
}
