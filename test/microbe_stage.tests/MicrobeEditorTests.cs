using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MicrobeEditorTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PlacementRemovalAndRepeatedHistoryTraversalPreserveEveryIntermediateLayout(
        bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.Perform(fixture.Movement(3, 6, 4));
        fixture.Perform(fixture.Removal(6));

        for (int cycle = 0; cycle < 3; ++cycle)
        {
            fixture.AssertLayout();
            fixture.Undo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 6, rotation: 4));
            fixture.Undo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
            fixture.Undo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 0));
            fixture.Undo();
            fixture.AssertLayout();
            AssertThat(fixture.History.Undo()).IsFalse();
            fixture.Redo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 0));
            fixture.Redo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
            fixture.Redo();
            fixture.AssertLayout(fixture.Template("cytoplasm", 6, rotation: 4));
            fixture.Redo();
            fixture.AssertLayout();
            AssertThat(fixture.History.Redo()).IsFalse();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RotationWithoutTranslationCanBeUndoneAndRedone(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("mitochondrion", 0));
        fixture.Perform(fixture.Movement(0, 0, 2));
        fixture.Perform(fixture.Movement(0, 0, 5));
        fixture.AssertLayout(fixture.Template("mitochondrion", 0, rotation: 5));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("mitochondrion", 0, rotation: 2));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("mitochondrion", 0));
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("mitochondrion", 0, rotation: 5));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MoveBackToPlacementOriginThenRemoveRestoresTheRecordedRotation(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Perform(fixture.Placement("cytoplasm", 0, rotation: 1));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.Perform(fixture.Movement(3, 0, 4));
        fixture.Perform(fixture.Removal(0));
        fixture.Undo(3);
        fixture.AssertLayout(fixture.Template("cytoplasm", 0, rotation: 1));
        fixture.Undo();
        fixture.AssertLayout();
        fixture.Redo(4);
        fixture.AssertLayout();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MovingOntoCytoplasmDoesNotMakePlacementUndoRemoveTheRestoredCytoplasm(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("cytoplasm", 3, rotation: 2));
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Removal(3), fixture.Movement(0, 3, 2));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("cytoplasm", 0), fixture.Template("cytoplasm", 3, rotation: 2));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemovingThenReplacingWithDifferentDefinitionAtSameOriginRestoresTheRightOrganelle(
        bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("mitochondrion", 0, rotation: 2));
        fixture.Perform(fixture.Removal(0));
        fixture.Perform(fixture.Placement("cytoplasm", 0, rotation: 4));
        fixture.Perform(fixture.Movement(0, 3, 1));
        fixture.Undo(2);
        fixture.AssertLayout();
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("mitochondrion", 0, rotation: 2));
        fixture.Redo();
        fixture.AssertLayout();
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MultiHexPlacementRestoresAllReplacedCytoplasmOnUndo(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        var replacement = fixture.Template("mitochondrion", 0, rotation: 2);
        var cytoplasm = replacement.RotatedHexes.Select(hex =>
            fixture.Template("cytoplasm", hex.Q, hex.R)).ToArray();
        fixture.Seed(cytoplasm);
        fixture.Perform(fixture.Placement("mitochondrion", 0, rotation: 2));
        fixture.AssertLayout(replacement);
        fixture.Perform(fixture.Movement(0, 5, 4));
        fixture.Undo(2);
        fixture.AssertLayout(cytoplasm);
        fixture.Redo();
        fixture.AssertLayout(replacement);
        fixture.Redo();
        fixture.AssertLayout(fixture.Template("mitochondrion", 5, rotation: 4));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NestedCombinedPlacementsAndMovesUndoAsSingleSteps(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Perform(new CombinedEditorAction(fixture.Placement("cytoplasm", -3),
            fixture.Placement("cytoplasm", 3)), fixture.Placement("cytoplasm", 0, 3));
        fixture.Perform(fixture.Movement(-3, -6, 1), fixture.Movement(3, 6, 2),
            fixture.Movement(0, 0, 3, fromR: 3, toR: 6));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("cytoplasm", -3), fixture.Template("cytoplasm", 3),
            fixture.Template("cytoplasm", 0, 3));
        fixture.Undo();
        fixture.AssertLayout();
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", -6, rotation: 1),
            fixture.Template("cytoplasm", 6, rotation: 2), fixture.Template("cytoplasm", 0, 6, 3));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CombinedRemovalAndReplacementAtSameLocationRespectsReverseUndoOrder(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("mitochondrion", 0, rotation: 3));
        fixture.Perform(fixture.Removal(0), fixture.Placement("cytoplasm", 0, rotation: 2));
        fixture.AssertLayout(fixture.Template("cytoplasm", 0, rotation: 2));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("mitochondrion", 0, rotation: 3));
        fixture.Redo();
        fixture.AssertLayout(fixture.Template("cytoplasm", 0, rotation: 2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EditingAfterUndoDiscardsOnlyTheRedoBranch(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.Perform(fixture.Removal(3));
        fixture.Undo(2);
        fixture.Perform(fixture.Movement(0, -3, 4));
        AssertThat(fixture.History.CanRedo()).IsFalse();
        fixture.AssertLayout(fixture.Template("cytoplasm", -3, rotation: 4));
        fixture.Undo(2);
        fixture.AssertLayout();
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", -3, rotation: 4));
        AssertThat(fixture.History.Redo()).IsFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemovingMovedPreexistingOrganelleCanBeRedoneAfterUndoingAndRedoingTheMove(
        bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(multicellular: cloneLayoutBetweenActions,
            reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.Perform(fixture.Removal(3));
        fixture.Undo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
        fixture.Redo();
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
        fixture.Redo();
        fixture.AssertLayout();
    }

    [TestCase]
    public void PlacedAndMovedOrganelleCanBeUndoneAfterReopeningCellType()
    {
        using var fixture = new MicrobeEditorTestFixture();
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.ReopenCellType();
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
        fixture.Undo();
        fixture.AssertLayout();
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NewCellBetweenMovesSupportsUndoAndRedoAcrossTheReset(bool cloneLayoutBetweenActions)
    {
        using var fixture = new MicrobeEditorTestFixture(reopenBetweenActions: cloneLayoutBetweenActions);
        fixture.Seed(fixture.Template("cytoplasm", -3));
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.Perform(fixture.NewCell());
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
        fixture.Perform(fixture.Placement("cytoplasm", 6));
        fixture.Perform(fixture.Movement(6, 9, 4));
        fixture.Undo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
        fixture.Undo();
        fixture.AssertLayout(fixture.Template("cytoplasm", -3), fixture.Template("cytoplasm", 3, rotation: 2));
        fixture.Undo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", -3));
        fixture.Redo(5);
        fixture.AssertLayout(fixture.Template("cytoplasm", 0), fixture.Template("cytoplasm", 9, rotation: 4));
    }

    [TestCase]
    public void UndoAndRedoAcrossCellTypesKeepTheirLayoutsAndActionContextsSeparate()
    {
        using var fixture = new MicrobeEditorTestFixture();
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.SelectCellType("Second");
        fixture.Perform(fixture.Placement("mitochondrion", 0));
        fixture.Perform(fixture.Movement(0, 3, 4));
        fixture.SelectCellType("First");

        fixture.Undo();
        AssertThat(fixture.SelectedCellTypeName).IsEqual("Second");
        fixture.AssertLayout(fixture.Template("mitochondrion", 0));
        fixture.Undo();
        fixture.AssertLayout();
        fixture.Undo();
        AssertThat(fixture.SelectedCellTypeName).IsEqual("First");
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
        fixture.Undo();
        fixture.AssertLayout();
        fixture.Redo(4);
        AssertThat(fixture.SelectedCellTypeName).IsEqual("Second");
        fixture.AssertLayout(fixture.Template("mitochondrion", 3, rotation: 4));
        fixture.SelectCellType("First");
        fixture.AssertLayout(fixture.Template("cytoplasm", 3, rotation: 2));
    }

    [TestCase]
    public void NewActionInAnotherCellTypeAfterUndoDoesNotRedoDiscardedEdits()
    {
        using var fixture = new MicrobeEditorTestFixture();
        fixture.Perform(fixture.Placement("cytoplasm", 0));
        fixture.Perform(fixture.Movement(0, 3, 2));
        fixture.SelectCellType("Second");
        fixture.Perform(fixture.Placement("mitochondrion", 0));
        fixture.Undo(2);
        fixture.SelectCellType("Second");
        fixture.Perform(fixture.Placement("cytoplasm", -3));

        AssertThat(fixture.History.CanRedo()).IsFalse();
        fixture.Undo(2);
        fixture.AssertLayout();
        fixture.Redo(2);
        fixture.AssertLayout(fixture.Template("cytoplasm", -3));
        fixture.SelectCellType("First");
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void OrganelleRemovalRecoversCloneAtRecordedLocation(bool redoRemoval, bool stalePositionOccupied)
    {
        using var fixture = new MicrobeEditorTestFixture();
        var originalLocation = new Hex(0, 0);
        var original = fixture.Template("cytoplasm", 0);
        EditorCombinableActionData data = redoRemoval ?
            new OrganelleRemoveActionData(original) :
            new OrganellePlacementActionData(original, originalLocation, 0);

        // The action retains the old instance after a move, clone, and undo of the move.
        original.Position = new Hex(2, 0);
        original.Orientation = 1;
        fixture.Seed(fixture.Template("cytoplasm", 0));
        if (stalePositionOccupied)
            fixture.Seed(fixture.Template("cytoplasm", 2, rotation: 1));

        fixture.Invoke(redoRemoval ? "DoOrganelleRemoveAction" : "UndoOrganellePlaceAction", data);
        fixture.AssertLayout(stalePositionOccupied ? [fixture.Template("cytoplasm", 2, rotation: 1)] : []);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OrganelleInsertionRestoresRecordedTransform(bool undoRemoval)
    {
        using var fixture = new MicrobeEditorTestFixture();
        var organelle = fixture.Template("cytoplasm", 0);
        EditorCombinableActionData data = undoRemoval ?
            new OrganelleRemoveActionData(organelle) :
            new OrganellePlacementActionData(organelle, new Hex(0, 0), 0);
        organelle.Position = new Hex(2, 0);
        organelle.Orientation = 1;

        fixture.Invoke(undoRemoval ? "UndoOrganelleRemoveAction" : "DoOrganellePlaceAction", data);
        fixture.AssertLayout(fixture.Template("cytoplasm", 0));
    }

    [TestCase]
    public void EditorAction_HistoryResetKeepsActionsAfterResetWhenPreviousActionIsCombined()
    {
        using var fixture = new MicrobeEditorTestFixture();
        fixture.Perform(fixture.Placement("cytoplasm", 0), fixture.Placement("cytoplasm", 3),
            fixture.Placement("cytoplasm", 6));
        var oldOrganelles = fixture.Layout.ToArray();
        fixture.Perform(fixture.NewCell());
        fixture.Perform(fixture.Placement("cytoplasm", 9));
        var newOrganelle = fixture.At(9);

        foreach (var organelle in oldOrganelles)
            AssertThat(fixture.History.HexPlacedThisSession<OrganelleTemplate, CellType>(organelle)).IsFalse();
        AssertThat(fixture.History.HexPlacedThisSession<OrganelleTemplate, CellType>(newOrganelle)).IsTrue();

        fixture.Undo(2);
        fixture.AssertLayout(oldOrganelles);
        foreach (var organelle in oldOrganelles)
            AssertThat(fixture.History.HexPlacedThisSession<OrganelleTemplate, CellType>(organelle)).IsTrue();
        AssertThat(fixture.History.HexPlacedThisSession<OrganelleTemplate, CellType>(newOrganelle)).IsFalse();
    }
}
