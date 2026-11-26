using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using Test.Utils;
using static GdUnit4.Assertions;

/// <summary>
///   Checks cavity actions, editor source mappings, and cavity cell behaviour in gameplay.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class CellCavityActionTests
{
    [TestCase]
    public void PreviewSourcesSurviveGrowthOrderChangesAndCopying()
    {
        var (species, first) = CreateSpecies();
        var second = new HexWithData<CellTemplate>(
            new CellTemplate(first.Data!.ModifiableCellType, new Hex(1, 0), 0, true), new Hex(1, 0), 0);
        species.ModifiableEditorCells.AddFast(second, [], []);
        var editor = new CellBodyPlanEditorComponent();
        var picker = new GrowthOrderPicker();
        var firstItem = new DraggableItem { UserData = first.Data };
        var secondItem = new DraggableItem { UserData = second.Data };
        try
        {
            SetPrivateField(editor, "editedMicrobeCells", species.ModifiableEditorCells);
            SetPrivateField(editor, "growthOrderGUI", picker);

            // The picker has changed order while a calculation using [first, second] was running.
            GetPrivateField<List<DraggableItem>>(picker, "itemControls").AddRange([secondItem, firstItem]);
            var snapshots = new List<CellTemplate>
            {
                new(first.Data.ModifiableCellType.Clone(true), first.Position, 0),
                new(second.Data!.ModifiableCellType.Clone(true), second.Position, 0, true),
            };
            var preview = snapshots.Select(cell =>
                new HexWithData<CellTemplate>((CellTemplate)cell.Clone(), cell.Position, 0)).ToList();
            var resultType = typeof(CellBodyPlanEditorComponent).GetNestedType("LayoutCalculationResult",
                BindingFlags.NonPublic)!;
            var result = Activator.CreateInstance(resultType, true)!;
            resultType.GetProperty("GrowthOrderSources")!.SetValue(result, snapshots);
            resultType.GetProperty("CompactSources")!.SetValue(result,
                new List<HexWithData<CellTemplate>> { first, second });

            InvokePrivate(editor, "RebuildFullLayoutGrowthOrderSources", preview, result);
            var sources =
                GetPrivateField<Dictionary<HexWithData<CellTemplate>, HexWithData<CellTemplate>>>(editor,
                    "fullLayoutGrowthOrderSources");
            AssertThat(ReferenceEquals(sources[preview[0]], first)).IsTrue();
            AssertThat(ReferenceEquals(sources[preview[1]], second)).IsTrue();

            var manual = new List<HexWithData<CellTemplate>>();
            InvokePrivate(editor, "CopyLayout", preview, manual);
            var manualSources =
                GetPrivateField<Dictionary<HexWithData<CellTemplate>, HexWithData<CellTemplate>>>(editor,
                    "manualLayoutSources");
            AssertThat(ReferenceEquals(manualSources[manual[0]], first)).IsTrue();
            AssertThat(ReferenceEquals(manualSources[manual[1]], second)).IsTrue();

            AssertThat((bool)InvokePrivate(editor, "LayoutCalculationSourcesAreValid", result)!).IsTrue();
            species.ModifiableEditorCells.Clear();
            species.ModifiableEditorCells.AddFast(first.Clone(), [], []);
            species.ModifiableEditorCells.AddFast(second, [], []);
            AssertThat((bool)InvokePrivate(editor, "LayoutCalculationSourcesAreValid", result)!).IsFalse();
        }
        finally
        {
            firstItem.Free();
            secondItem.Free();
            picker.Free();
            editor.Free();
        }
    }

    [TestCase]
    public void FullLayoutPreviewAcceptsDistinctFootprintsWithSharedOriginAndClearsReplacedCells()
    {
        var simulationParameters = SimulationParameters.Instance;
        var cytoplasm = simulationParameters.GetOrganelleType("cytoplasm");
        var membrane = simulationParameters.GetMembrane("single");
        var firstType = new CellType(membrane) { CellTypeName = "First" };
        firstType.ModifiableOrganelles.Add(new OrganelleTemplate(cytoplasm, new Hex(-10, 0), 0));
        var secondType = new CellType(membrane) { CellTypeName = "Second" };
        secondType.ModifiableOrganelles.Add(new OrganelleTemplate(cytoplasm, new Hex(10, 0), 0));

        var firstFootprint = firstType.ModifiableOrganelles.SelectMany(organelle =>
            organelle.Definition.GetRotatedHexes(organelle.Orientation)
                .Select(hex => hex + organelle.Position)).ToHashSet();
        var secondFootprint = secondType.ModifiableOrganelles.SelectMany(organelle =>
            organelle.Definition.GetRotatedHexes(organelle.Orientation)
                .Select(hex => hex + organelle.Position)).ToHashSet();
        AssertThat(firstFootprint.Intersect(secondFootprint).Any()).IsFalse();

        var origin = new Hex(0, 0);
        var first = new HexWithData<CellTemplate>(new CellTemplate(firstType, origin, 0), origin, 0);
        var second = new HexWithData<CellTemplate>(new CellTemplate(secondType, origin, 0), origin, 0);
        var editor = new CellBodyPlanEditorComponent();
        try
        {
            var preview = GetPrivateField<IReadOnlyList<HexWithData<CellTemplate>>>(editor, "fullLayoutPreview");
            InvokePrivate(editor, "SetFullLayoutPreview", new List<HexWithData<CellTemplate>> { first, second });

            AssertThat(first.Position).IsEqual(second.Position);
            AssertThat(preview.Count).IsEqual(2);
            AssertThat(ReferenceEquals(preview[0], first)).IsTrue();
            AssertThat(ReferenceEquals(preview[1], second)).IsTrue();

            InvokePrivate(editor, "SetFullLayoutPreview", new List<HexWithData<CellTemplate>> { second });
            AssertThat(preview.Count).IsEqual(1);
            AssertThat(ReferenceEquals(preview[0], second)).IsTrue();

            InvokePrivate(editor, "SetFullLayoutPreview", new List<HexWithData<CellTemplate>>());
            AssertThat(preview.Count).IsEqual(0);
        }
        finally
        {
            editor.Free();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CavityEditsInvalidateCompletedCalculationsAndExistingPreviews(bool undo)
    {
        var (species, cell) = CreateSpecies();
        var editor = new CellBodyPlanEditorComponent();
        var owningEditor = new MulticellularEditor();
        try
        {
            species.ModifiableGameplayCells.AddFast(cell.Data!, [], []);
            owningEditor.CurrentGame = GameProperties.StartNewMulticellularGame(new WorldGenerationSettings(),
                startingSpecies: species);
            owningEditor.TutorialState.Enabled = true;
            var cavityTutorial = owningEditor.TutorialState.MulticellularCellCavityTutorial;
            cavityTutorial.Show();
            typeof(EditorComponentBase<MulticellularEditor>).GetField("editor",
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, owningEditor);

            SetPrivateField(editor, "editedMicrobeCells", species.ModifiableEditorCells);
            var preview = GetPrivateField<List<HexWithData<CellTemplate>>>(editor, "fullLayoutPreview");
            preview.Add(cell.Clone());
            var resultType = typeof(CellBodyPlanEditorComponent).GetNestedType("LayoutCalculationResult",
                BindingFlags.NonPublic)!;
            var result = Activator.CreateInstance(resultType, true)!;
            var completedCalculation = typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType).Invoke(null, [result])!;
            SetPrivateField(editor, "pendingLayoutCalculation", completedCalculation);
            SetPrivateField(editor, "cellDataDirty", false);

            // The result is already complete, but _Process has not applied it yet.
            cell.Data!.IsCavity = undo;
            InvokePrivate(editor, undo ? "UndoCellCavityAction" : "DoCellCavityAction",
                new CellCavityActionData(cell, false, true));

            AssertThat(cell.Data.IsCavity).IsEqual(!undo);
            AssertThat(preview.Count).IsEqual(0);
            AssertThat(GetPrivateField<bool>(editor, "layoutCalculationRequested")).IsTrue();
            AssertThat(GetPrivateField<bool>(editor, "cellDataDirty")).IsTrue();
            AssertThat(cavityTutorial.Complete).IsEqual(!undo);
            AssertThat(cavityTutorial.ShownCurrently).IsEqual(undo);
        }
        finally
        {
            editor.Free();
            owningEditor.Free();
        }
    }

    [TestCase]
    public void ManualFirstCellValidationRebuildsMissingGrowthOrderMapping()
    {
        var (species, first) = CreateSpecies();
        var editor = new CellBodyPlanEditorComponent();
        var picker = new GrowthOrderPicker();
        try
        {
            SetPrivateField(editor, "editedMicrobeCells", species.ModifiableEditorCells);
            SetPrivateField(editor, "growthOrderGUI", picker);
            var manual = new HexWithData<CellTemplate>((CellTemplate)first.Data!.Clone(), new Hex(1, 0), 0);
            SetPrivateField(editor, "manualFullLayout", new List<HexWithData<CellTemplate>> { manual });
            InvokePrivate(editor, "SetManualLayoutSource", manual, first);
            var cavity = new HexWithData<CellTemplate>(
                new CellTemplate(first.Data.ModifiableCellType, new Hex(1, 0), 0, true), new Hex(1, 0), 0);
            species.ModifiableEditorCells.AddFast(cavity, [], []);
            var manualCavity = new HexWithData<CellTemplate>((CellTemplate)cavity.Data!.Clone(), new Hex(0, 0), 0);

            // Moving the leader puts the cavity first in storage order, with the growth-order cache missing.
            SetPrivateField(editor, "manualFullLayout", new List<HexWithData<CellTemplate>> { manualCavity, manual });
            InvokePrivate(editor, "SetManualLayoutSource", manualCavity, cavity);

            AssertThat((bool)InvokePrivate(editor, "ManualLayoutHasValidFirstCell")!).IsTrue();
            manual.Data!.IsCavity = true;
            AssertThat((bool)InvokePrivate(editor, "ManualLayoutHasValidFirstCell")!).IsFalse();
            manual.Data.IsCavity = false;
            GetPrivateField<Dictionary<HexWithData<CellTemplate>, HexWithData<CellTemplate>>>(editor,
                "manualLayoutSources").Clear();
            AssertThat((bool)InvokePrivate(editor, "ManualLayoutHasValidFirstCell")!).IsFalse();
        }
        finally
        {
            picker.Free();
            editor.Free();
        }
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void DetachedCavityCellsDie(bool isCavity, bool removeAttachedComponent)
    {
        using var world = ThriveWorld.Create();
        using var recorder = new CommandBuffer();
        var (species, cell) = CreateSpecies();
        cell.Data!.IsCavity = isCavity;
        species.ModifiableGameplayCells.AddFast(cell.Data, [], []);

        var leader = world.Create(default(MicrobeControl), default(CellProperties), default(Physics));
        var member = world.Create(default(MicrobeControl), default(Physics),
            new Health { CurrentHealth = 100, MaxHealth = 100 },
            new MulticellularSpeciesMember(species, cell.Data.ModifiableCellType, 0),
            new MicrobeColonyMember(leader),
            new AttachedToEntity(leader, new Vector3(0, Constants.CAVITY_CELL_Y_OFFSET, 0), Quaternion.Identity));
        leader.Add(new MicrobeColony(leader, MicrobeState.Normal, leader, member));

        AssertThat(leader.Get<MicrobeColony>().RemoveFromColonyAndDisbandIfEmpty(leader, member, recorder,
            removeAttachedComponent)).IsFalse();
        recorder.Playback(world);

        AssertThat(member.Get<Health>().Dead).IsEqual(isCavity);
        AssertThat(member.Has<MicrobeColonyMember>()).IsFalse();
        AssertThat(member.Has<AttachedToEntity>()).IsEqual(!removeAttachedComponent);
    }

    [TestCase]
    public void GameteLaunchedFromCavityCellSpawnsOnGameplayPlane()
    {
        using var world = new TestWorldSimulation();
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        var (species, first) = CreateSpecies();
        var simulationParameters = SimulationParameters.Instance;
        var type = first.Data!.ModifiableCellType;
        type.MembraneType = simulationParameters.GetMembrane("single");
        type.ModifiableOrganelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0));
        var cavityHex = new Hex(1, 0);
        var cavityTemplate = new CellTemplate(type, cavityHex, 0, true);
        species.ModifiableEditorCells.AddFast(new HexWithData<CellTemplate>(cavityTemplate, cavityHex, 0), [], []);
        species.ModifiableGameplayCells.AddFast(first.Data, [], []);
        species.ModifiableGameplayCells.AddFast(cavityTemplate, [], []);
        species.ReproductionMethod = MulticellularReproductionMethod.SexualIsogamy;
        var gameteType = type.Clone(true);
        gameteType.CellTypeName = "Gamete";
        species.ModifiableCellTypes.Add(gameteType);
        species.ModifiableGameteTypeA = gameteType;
        species.OnEdited();

        var leader = world.EntitySystem.Create(new WorldPosition(Vector3.Zero), new MicrobeSex { Sex = GameteType.A });

        // Place the cavity ahead of the leader so it is picked as the launch position.
        var cavityPosition = new Vector3(2, Constants.CAVITY_CELL_Y_OFFSET, -10);
        var cavity = world.EntitySystem.Create(new WorldPosition(cavityPosition));
        var colony = new MicrobeColony(leader, MicrobeState.Normal, leader, cavity);
        var growth = new MulticellularGrowth(species);

        growth.ShootGamete(ref colony, leader, species, world, spawnEnvironment, new DummySpawnSystem(), true);
        world.ProcessAll(0.1f);

        var gamete = new FirstEntityGrabber(new QueryDescription().WithAll<GameteCell>(), world.EntitySystem).Found;
        AssertThat(gamete.Get<WorldPosition>().Position).IsEqual(new Vector3(cavityPosition.X, 0, cavityPosition.Z));
    }

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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void CavityChangesAfterPlacementDoNotAddMutationPointCost(int cavityCellIndex)
    {
        var (species, first) = CreateSpecies();
        var type = first.Data!.ModifiableCellType;
        foreach (var position in new[] { new Hex(0, 1), new Hex(-1, 1) })
        {
            species.ModifiableEditorCells.AddFast(
                new HexWithData<CellTemplate>(new CellTemplate(type, position, 0), position, 0), [], []);
        }

        var facade = new MulticellularEditsFacade(species);
        var comparer = new MulticellularSpeciesComparer();
        var placedPosition = new Hex(0, -1);
        var placed = new HexWithData<CellTemplate>(new CellTemplate(type, placedPosition, 0), placedPosition, 0);
        var placement = new CellPlacementActionData(placed, placedPosition, 0);
        var original = species.ModifiableEditorCells.AsModifiable().ElementAt(cavityCellIndex);
        var cavity = new CellCavityActionData(original, false, true);

        facade.SetActiveActions([placement]);
        var placementCost = comparer.Compare(species, facade, double.MaxValue);
        AssertThat(placementCost).IsEqual(type.MPCost);

        facade.SetActiveActions([placement, cavity]);
        AssertThat(comparer.Compare(species, facade, double.MaxValue)).IsEqual(placementCost);

        facade.SetActiveActions([placement, cavity, new CellCavityActionData(original, true, false)]);
        AssertThat(comparer.Compare(species, facade, double.MaxValue)).IsEqual(placementCost);

        facade.SetActiveActions([cavity]);
        AssertThat(comparer.Compare(species, facade, double.MaxValue)).IsEqual(0.0);
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

    private static void SetPrivateField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    }

    private static object? InvokePrivate(object target, string name, params object[] arguments)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(target, arguments);
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
