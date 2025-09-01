namespace ThriveTest.MicrobeStage;

using Xunit;

public class EditorMPTests
{
    private readonly OrganelleDefinition cheapOrganelle = new()
    {
        MPCost = 20,
        Name = "CheapOrganelle",
        InternalName = "cheapOrganelle",
    };

    [Fact]
    public void EditorMPTests_EmptyIsFullMP()
    {
        var history = new EditorActionHistory<EditorAction>();

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_SimpleAdd()
    {
        var history = new EditorActionHistory<EditorAction>();

        var actionData =
            new OrganellePlacementActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0), new Hex(0, 0), 0);

        bool undo = false;
        bool redo = false;

        history.AddAction(
            new SingleEditorAction<OrganellePlacementActionData>(_ => redo = true, _ => undo = true, actionData));

        Assert.True(redo);
        Assert.False(undo);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_SimpleRemove()
    {
        var history = new EditorActionHistory<EditorAction>();

        var actionData =
            new OrganelleRemoveActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0), new Hex(0, 0), 0);

        bool undo = false;
        bool redo = false;

        history.AddAction(
            new SingleEditorAction<OrganelleRemoveActionData>(_ => redo = true, _ => undo = true, actionData));

        Assert.True(redo);
        Assert.False(undo);

        Assert.NotEqual(Constants.ORGANELLE_REMOVE_COST, cheapOrganelle.MPCost);
        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_SimpleMove()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        bool undo = false;
        bool redo = false;

        history.AddAction(
            new SingleEditorAction<OrganelleMoveActionData>(_ => redo = true, _ => undo = true, moveData));

        Assert.True(redo);
        Assert.False(undo);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MoveIsFreeAfterAdd()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        bool undo = false;
        bool redo = false;

        var actionData =
            new OrganellePlacementActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0), new Hex(0, 0), 0);

        history.AddAction(
            new SingleEditorAction<OrganellePlacementActionData>(_ => redo = true, _ => undo = true, actionData));

        Assert.True(redo);
        Assert.False(undo);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(
            new SingleEditorAction<OrganelleMoveActionData>(_ => redo = true, _ => undo = true, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsAddCost()
    {
        var history = new EditorActionHistory<EditorAction>();

        var organelle = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(organelle, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(organelle, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsAddMoveCosts()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }
}
