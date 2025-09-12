namespace ThriveTest.MacroscopicStage.Tests;

using Godot;
using Xunit;

public class MetaballEditorTests
{
    private readonly CellType simpleType = new(new MembraneType());

    [Fact]
    public void MetaballEditor_DeleteRefundsResize()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rootMetaball = new MacroscopicMetaball(simpleType)
        {
            Size = 1,
        };

        var metaball = new MacroscopicMetaball(simpleType)
        {
            Parent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        var resizeActionData = new MetaballResizeActionData<MacroscopicMetaball>(metaball, 1, 1.2f);

        history.AddAction(new SingleEditorAction<MetaballResizeActionData<MacroscopicMetaball>>(
            data => metaball.Size = data.NewSize, _ => { }, resizeActionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.METABALL_RESIZE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new MetaballRemoveActionData<MacroscopicMetaball>(metaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.METABALL_REMOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void MetaballEditor_DeleteRefundsMove()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rootMetaball = new MacroscopicMetaball(simpleType)
        {
            Size = 1,
        };

        var metaball = new MacroscopicMetaball(simpleType)
        {
            Parent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        var moveActionData = new MetaballMoveActionData<MacroscopicMetaball>(metaball, metaball.Position,
            new Vector3(-1, 0, 0), rootMetaball, rootMetaball, null);

        history.AddAction(new SingleEditorAction<MetaballMoveActionData<MacroscopicMetaball>>(_ => { }, _ => { },
            moveActionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.METABALL_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new MetaballRemoveActionData<MacroscopicMetaball>(metaball, new Vector3(-1, 0, 0), rootMetaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.METABALL_REMOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void MetaballEditor_DeleteRefundsAdd()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rootMetaball = new MacroscopicMetaball(simpleType)
        {
            Size = 1,
        };

        var metaball = new MacroscopicMetaball(simpleType)
        {
            Parent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        var placementData = new MetaballPlacementActionData<MacroscopicMetaball>(metaball);

        history.AddAction(new SingleEditorAction<MetaballPlacementActionData<MacroscopicMetaball>>(_ => { }, _ => { },
            placementData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.METABALL_ADD_COST,
            history.CalculateMutationPointsLeft());

        var removeData = new MetaballRemoveActionData<MacroscopicMetaball>(metaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }
}
