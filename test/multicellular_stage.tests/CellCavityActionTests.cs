using System.IO;
using System.Linq;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using static GdUnit4.Assertions;

/// <summary>
///   Checks cavity action replay without mutating the species used as the facade's baseline.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class CellCavityActionTests
{
    [TestCase]
    public void CavityChangesCanBeReplayedAndCleared()
    {
        var (species, cell) = CreateSpecies();
        var facade = new MulticellularEditsFacade(species);
        var change = new CellCavityActionData(cell, false, true);

        facade.AppendAction(change);
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsTrue();
        AssertThat(facade.GetByExactElementRootPosition(cell.Position)!.Data!.IsCavity).IsTrue();
        AssertThat(cell.Data!.IsCavity).IsFalse();

        facade.SetActiveActions([change, new CellCavityActionData(cell, true, false)]);
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsFalse();

        facade.SetActiveActions([change]);
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsTrue();

        facade.ClearActiveActions();
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsFalse();
    }

    [TestCase]
    public void MovingACavityPreservesItsOverride()
    {
        var (species, cell) = CreateSpecies();
        var facade = new MulticellularEditsFacade(species);
        var destination = new Hex(1, 0);
        facade.SetActiveActions([
            new CellCavityActionData(cell, false, true),
            new CellMoveActionData(cell, cell.Position, destination, 0, 1),
        ]);

        var moved = facade.EditorCells.Single();
        AssertThat(moved.Position).IsEqual(destination);
        AssertThat(moved.Data!.IsCavity).IsTrue();
        AssertThat(cell.Data!.IsCavity).IsFalse();

        // Resolving again reuses cached wrappers, which must retain the action's result.
        facade.SetActiveActions([
            new CellCavityActionData(cell, false, true),
            new CellMoveActionData(cell, cell.Position, destination, 0, 1),
        ]);
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsTrue();
    }

    [TestCase]
    public void CavityActionUsesCapturedPositionAfterCellMoves()
    {
        var (species, original) = CreateSpecies();
        var editedCell = new HexWithData<CellTemplate>((CellTemplate)original.Data!.Clone(), original.Position, 0);
        var change = new CellCavityActionData(editedCell, false, true);
        var destination = new Hex(1, 0);
        editedCell.Position = destination;
        editedCell.Data!.Position = destination;

        var facade = new MulticellularEditsFacade(species);
        facade.SetActiveActions([
            change,
            new CellMoveActionData(editedCell, original.Position, destination, 0, 0),
        ]);

        AssertThat(facade.EditorCells.Single().Position).IsEqual(destination);
        AssertThat(facade.EditorCells.Single().Data!.IsCavity).IsTrue();
    }

    [TestCase]
    public void PlacedCellsCanBecomeCavitiesAndBeRemoved()
    {
        var (species, original) = CreateSpecies();
        var template = new CellTemplate(original.Data!.ModifiableCellType, new Hex(1, 0), 0);
        var cell = new HexWithData<CellTemplate>(template, template.Position, 0);
        var place = new CellPlacementActionData(cell, cell.Position, cell.Orientation);
        var change = new CellCavityActionData(cell, false, true);
        var facade = new MulticellularEditsFacade(species);
        facade.SetActiveActions([place, change]);

        AssertThat(facade.GetByExactElementRootPosition(cell.Position)!.Data!.IsCavity).IsTrue();
        AssertThat(template.IsCavity).IsFalse();
        AssertThat(facade.EditorCells.Count).IsEqual(2);

        facade.SetActiveActions([place, change, new CellRemoveActionData(cell)]);
        AssertThat(facade.EditorCells.Count).IsEqual(1);
        AssertThat(facade.GetByExactElementRootPosition(cell.Position)).IsNull();
    }

    [TestCase]
    public void SavingPreservesTheTargetAndCapturedCavityChange()
    {
        var (_, cell) = CreateSpecies();
        cell.Data!.MembraneType = SimulationParameters.Instance.GetMembrane("single");
        var change = new CellCavityActionData(cell, false, true);
        var capturedPosition = cell.Position;
        cell.Position = new Hex(1, 0);
        cell.Data.IsCavity = true;

        var manager = new ThriveArchiveManager();
        using var data = new MemoryStream();
        var writer = new SArchiveMemoryWriter(data, manager, false);
        manager.OnStartNewWrite(writer);
        writer.WriteObject(change);
        manager.OnFinishWrite(writer);

        data.Position = 0;
        var reader = new SArchiveMemoryReader(data, manager);
        manager.OnStartNewRead(reader);
        var loaded = reader.ReadObjectOrNull<CellCavityActionData>()!;
        manager.OnFinishRead(reader);

        AssertThat(loaded.Location).IsEqual(capturedPosition);
        AssertThat(loaded.Cell.Position).IsEqual(cell.Position);
        AssertThat(loaded.OldIsCavity).IsFalse();
        AssertThat(loaded.NewIsCavity).IsTrue();
        AssertThat(loaded.Cell.Data!.IsCavity).IsTrue();
    }

    [TestCase]
    public void CellPopupReflectsSelectionAndEmitsCavityChanges()
    {
        var (_, cell) = CreateSpecies();
        var menu = GD.Load<PackedScene>("res://src/multicellular_stage/editor/CellPopupMenu.tscn")
            .Instantiate<CellPopupMenu>();
        try
        {
            menu.Hide();
            menu.GetActionPrice = _ => 0;
            menu.SelectedCells = [cell];
            menu._Ready();
            var checkBox = menu.GetNode<CheckBox>("PanelContainer/VBoxContainer/Cavity");
            bool? requestedCavity = null;
            menu.Connect(CellPopupMenu.SignalName.CavityToggled,
                Callable.From<bool>(value => requestedCavity = value));

            AssertThat(checkBox.ButtonPressed).IsFalse();
            checkBox.ButtonPressed = true;
            AssertThat(requestedCavity == true).IsTrue();

            cell.Data!.IsCavity = true;
            menu.SelectedCells = [cell];
            AssertThat(checkBox.ButtonPressed).IsTrue();

            var other = new HexWithData<CellTemplate>(new CellTemplate(cell.Data.ModifiableCellType, new Hex(1, 0), 0),
                new Hex(1, 0), 0);
            requestedCavity = null;
            menu.SelectedCells = [cell, other];
            AssertThat(checkBox.ButtonPressed).IsFalse();
            AssertThat(requestedCavity == null).IsTrue();
        }
        finally
        {
            menu.Free();
        }
    }

    private static (MulticellularSpecies Species, HexWithData<CellTemplate> Cell) CreateSpecies()
    {
        var type = new CellType(new MembraneType { Name = "Dummy" }) { CellTypeName = "Test" };
        var cell = new HexWithData<CellTemplate>(new CellTemplate(type, new Hex(0, 0), 0), new Hex(0, 0), 0);
        var layout = new IndividualHexLayout<CellTemplate>();
        layout.AddFast(cell, [], []);
        var species = new MulticellularSpecies(1, "Test", "species")
        {
            ModifiableEditorCells = layout,
        };
        species.ModifiableCellTypes.Add(type);
        return (species, cell);
    }
}
