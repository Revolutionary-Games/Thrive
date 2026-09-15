using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MicrobeEditorTests
{
    [TestCase]
    public void EditorAction_HistoryResetKeepsActionsAfterResetWhenPreviousActionIsCombined()
    {
        var history = new EditorActionHistory<EditorAction>();

        var oldPlacement1 = new OrganellePlacementActionData(
            new OrganelleTemplate(new OrganelleDefinition { Hexes = [new Hex(0, 0)] }, new Hex(0, 0), 0),
            new Hex(0, 0), 0);
        var oldPlacement2 = new OrganellePlacementActionData(
            new OrganelleTemplate(new OrganelleDefinition { Hexes = [new Hex(0, 0)] }, new Hex(1, 0), 0),
            new Hex(1, 0), 0);
        var oldPlacement3 = new OrganellePlacementActionData(
            new OrganelleTemplate(new OrganelleDefinition { Hexes = [new Hex(0, 0)] }, new Hex(2, 0), 0),
            new Hex(2, 0), 0);

        history.AddAction(new CombinedEditorAction(
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, oldPlacement1),
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, oldPlacement2),
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, oldPlacement3)));

        var newCell = new NewMicrobeActionData(new OrganelleLayout<OrganelleTemplate>(), new MembraneType(), 0,
            Colors.White, null, null);
        history.AddAction(new SingleEditorAction<NewMicrobeActionData>(_ => { }, _ => { }, newCell));

        var newPlacement = new OrganellePlacementActionData(
            new OrganelleTemplate(new OrganelleDefinition { Hexes = [new Hex(0, 0)] }, new Hex(0, 0), 0),
            new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, newPlacement));

        AssertThat(history.HexPlacedThisSession<OrganelleTemplate, CellType>(oldPlacement1.PlacedHex)).IsFalse();
        AssertThat(history.HexPlacedThisSession<OrganelleTemplate, CellType>(oldPlacement2.PlacedHex)).IsFalse();
        AssertThat(history.HexPlacedThisSession<OrganelleTemplate, CellType>(oldPlacement3.PlacedHex)).IsFalse();
        AssertThat(history.HexPlacedThisSession<OrganelleTemplate, CellType>(newPlacement.PlacedHex)).IsTrue();
    }
}
