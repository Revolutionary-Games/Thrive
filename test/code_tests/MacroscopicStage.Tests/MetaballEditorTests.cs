namespace ThriveTest.MacroscopicStage.Tests;

using System.Collections.Generic;
using System.Linq;
using Godot;
using ThriveTest.Utils;
using Xunit;

public class MetaballEditorTests
{
    private readonly OrganelleDefinition dummyCytoplasm = new()
    {
        MPCost = 18,
        Name = "Cytoplasm",
        InternalName = "cytoplasm",
        Hexes = [new Hex(0, 0)],
    };

    private readonly CellType simpleType = new(new MembraneType
    {
        EditorCost = 50,
        Name = "Simple",
        InternalName = "simple",
    });

    private readonly MacroscopicSpeciesComparer speciesComparer;

    private readonly IReadOnlyMacroscopicSpecies speciesTemplate1;

    public MetaballEditorTests()
    {
        speciesComparer = new MacroscopicSpeciesComparer(dummyCytoplasm);

        var species1 = new MacroscopicSpecies(1, "test", "test");

        species1.ModifiableCellTypes.Add(simpleType);
        species1.ModifiableBodyLayout.Add(new MacroscopicMetaball(simpleType)
        {
            Size = 1,
            Position = new Vector3(0, 0, 0),
        });

        speciesTemplate1 = species1;
    }

    [Fact]
    public void MetaballEditor_DeleteRefundsResize()
    {
        var originalSpecies = (MacroscopicSpecies)((MacroscopicSpecies)speciesTemplate1).Clone();
        var editsFacade = new MacroscopicEditsFacade(originalSpecies);
        var history = new EditorActionHistory<EditorAction>();

        var rootMetaball = originalSpecies.ModifiableBodyLayout[0];

        var metaball = new MacroscopicMetaball(simpleType)
        {
            ModifiableParent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        originalSpecies.ModifiableBodyLayout.Add(metaball);

        var resizeActionData = new MetaballResizeActionData<MacroscopicMetaball>(metaball, 1, 1.2f);

        history.AddAction(new SingleEditorAction<MetaballResizeActionData<MacroscopicMetaball>>(
            _ => { }, _ => { }, resizeActionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.METABALL_RESIZE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new MetaballRemoveActionData<MacroscopicMetaball>(metaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.METABALL_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void MetaballEditor_DeleteRefundsMove()
    {
        var originalSpecies = (MacroscopicSpecies)((MacroscopicSpecies)speciesTemplate1).Clone();
        var editsFacade = new MacroscopicEditsFacade(originalSpecies);
        var history = new EditorActionHistory<EditorAction>();

        var rootMetaball = originalSpecies.ModifiableBodyLayout[0];

        var metaball = new MacroscopicMetaball(simpleType)
        {
            ModifiableParent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        originalSpecies.ModifiableBodyLayout.Add(metaball);

        var moveActionData = new MetaballMoveActionData<MacroscopicMetaball>(metaball, metaball.Position,
            new Vector3(-1, 0, 0), rootMetaball, rootMetaball, null);

        history.AddAction(new SingleEditorAction<MetaballMoveActionData<MacroscopicMetaball>>(_ => { }, _ => { },
            moveActionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.METABALL_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new MetaballRemoveActionData<MacroscopicMetaball>(metaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        // Deleting cancels the move cost, results in just removal cost
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.METABALL_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void MetaballEditor_DeleteRefundsAdd()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MacroscopicEditsFacade(originalSpecies);
        var history = new EditorActionHistory<EditorAction>();

        // Slight hack to get the root metaball
        var rootMetaball = (Metaball)originalSpecies.BodyLayout.First();

        // The metaball to be added
        var metaball = new MacroscopicMetaball(simpleType)
        {
            ModifiableParent = rootMetaball,
            Position = new Vector3(1, 0, 0),
            Size = 1,
        };

        var placementData = new MetaballPlacementActionData<MacroscopicMetaball>(metaball);

        history.AddAction(new SingleEditorAction<MetaballPlacementActionData<MacroscopicMetaball>>(_ => { }, _ => { },
            placementData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.METABALL_ADD_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData = new MetaballRemoveActionData<MacroscopicMetaball>(metaball, null);

        history.AddAction(
            new SingleEditorAction<MetaballRemoveActionData<MacroscopicMetaball>>(_ => { }, _ => { }, removeData));

        // Adding and then removing should result in 0 net cost (full refund)
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    private void ApplyFacadeEdits(MacroscopicEditsFacade facade, EditorActionHistory<EditorAction> history)
    {
        var actions = new List<EditorCombinableActionData>();
        history.GetPerformedActionData(actions);
        facade.SetActiveActions(actions);
    }
}
