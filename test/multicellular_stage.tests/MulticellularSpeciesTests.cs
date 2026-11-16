using System.Collections.Generic;
using GdUnit4;
using Godot;
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

    [TestCase]
    public void TestColonyHexPhotoBuilderCavityMaterial()
    {
        var species = new MulticellularSpecies(1, "Test", "Species");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        species.ModifiableCellTypes.Add(cellType);

        var normalCell = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0);
        var cavityCell = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(1, 0), 0, true), new Hex(1, 0), 0);

        species.ModifiableEditorCells.AddFast(normalCell, [], []);
        species.ModifiableEditorCells.AddFast(cavityCell, [], []);

        var builder = new ColonyHexPhotoBuilder
        {
            Species = species,
        };

        try
        {
            builder.ApplySceneParameters(builder);

            AssertThat(builder.GetChildCount()).IsEqual(2);

            var child1 = builder.GetChild<MeshInstance3D>(0);
            var child2 = builder.GetChild<MeshInstance3D>(1);

            var validHexMaterial =
                GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
            var cavityHexMaterial =
                GD.Load<Material>("res://src/multicellular_stage/editor/CavityHex.tres");

            AssertThat(child1.MaterialOverride).IsEqual(validHexMaterial);
            AssertThat(child2.MaterialOverride).IsEqual(cavityHexMaterial);
        }
        finally
        {
            builder.Free();
        }
    }

    [TestCase]
    public void TestColonyHexPhotoBuilderHexesHashCavities()
    {
        var species1 = new MulticellularSpecies(1, "Test", "Species1");
        var species2 = new MulticellularSpecies(2, "Test", "Species2");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        species1.ModifiableCellTypes.Add(cellType);
        species2.ModifiableCellTypes.Add(cellType);

        var normalCellA = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0);
        var normalCellB = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(1, 0), 0, false), new Hex(1, 0), 0);

        var cavityCellA = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0);
        var cavityCellB = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(1, 0), 0, true), new Hex(1, 0), 0);

        species1.ModifiableEditorCells.AddFast(normalCellA, [], []);
        species1.ModifiableEditorCells.AddFast(normalCellB, [], []);

        species2.ModifiableEditorCells.AddFast(cavityCellA, [], []);
        species2.ModifiableEditorCells.AddFast(cavityCellB, [], []);

        var hash1 = ColonyHexPhotoBuilder.GetVisualHash(species1);
        var hash2 = ColonyHexPhotoBuilder.GetVisualHash(species2);

        AssertThat(hash1).IsNotEqual(hash2);

        // Species with identical layout and cavity status must produce equal hashes
        var species3 = new MulticellularSpecies(3, "Test", "Species3");
        species3.ModifiableCellTypes.Add(cellType);
        var cavityCell3A = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0);
        var cavityCell3B = new HexWithData<CellTemplate>(
            new CellTemplate(cellType, new Hex(1, 0), 0, true), new Hex(1, 0), 0);

        species3.ModifiableEditorCells.AddFast(cavityCell3A, [], []);
        species3.ModifiableEditorCells.AddFast(cavityCell3B, [], []);

        var hash3 = ColonyHexPhotoBuilder.GetVisualHash(species3);
        AssertThat(hash2).IsEqual(hash3);
    }

    [TestCase]
    public void TestColonyHexPhotoBuilderHexesHashCavitySwapping()
    {
        var species1 = new MulticellularSpecies(1, "Test", "Species1");
        var species2 = new MulticellularSpecies(2, "Test", "Species2");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        species1.ModifiableCellTypes.Add(cellType);
        species2.ModifiableCellTypes.Add(cellType);

        // Layout with normal at (0, 0), cavity at (-2, 1), and normal at (-1, 0)
        species1.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0), [],
            []);
        species1.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(-2, 1), 0, true), new Hex(-2, 1), 0), [],
            []);
        species1.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(-1, 0), 0, false), new Hex(-1, 0), 0), [],
            []);

        // Layout with normal at (0, 0), normal at (-2, 1), and cavity at (-1, 0)
        species2.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(0, 0), 0, false), new Hex(0, 0), 0), [],
            []);
        species2.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(-2, 1), 0, false), new Hex(-2, 1), 0), [],
            []);
        species2.ModifiableEditorCells.AddFast(
            new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(-1, 0), 0, true), new Hex(-1, 0), 0), [],
            []);

        var hash1 = ColonyHexPhotoBuilder.GetVisualHash(species1);
        var hash2 = ColonyHexPhotoBuilder.GetVisualHash(species2);

        AssertThat(hash1).IsNotEqual(hash2);
    }
}
