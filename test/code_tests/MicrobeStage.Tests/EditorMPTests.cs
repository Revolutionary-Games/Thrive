namespace ThriveTest.MicrobeStage;

using System.Collections.Generic;
using Utils;
using Xunit;

public class EditorMPTests
{
    private const int TEST_UPGRADE_COST = 11;
    private const int TEST_UPGRADE_COST_2 = 26;

    private readonly OrganelleDefinition cheapOrganelle = new()
    {
        MPCost = 20,
        Name = "CheapOrganelle",
        InternalName = "cheapOrganelle",
        AvailableUpgrades = new Dictionary<string, AvailableUpgrade>
        {
            {
                "normal", new TestOrganelleUpgrade("Test2", 0, true)
            },
            {
                "test", new TestOrganelleUpgrade("Test", TEST_UPGRADE_COST)
            },
            {
                "test2", new TestOrganelleUpgrade("Test2", TEST_UPGRADE_COST_2)
            },
        },
    };

    private readonly OrganelleDefinition dummyCytoplasm = new()
    {
        MPCost = 18,
        Name = "Cytoplasm",
        InternalName = "cytoplasm",
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

    [Fact]
    public void EditorMPTests_RemoveCancelsAddMoveUpgradeCosts()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), new OrganelleUpgrades
        {
            UnlockedFeatures = ["test"],
        }, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost - TEST_UPGRADE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsMoveCost()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RemovingEndosymbiontIsFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var endosymbiontData = new EndosymbiontPlaceActionData(template, new Hex(0, 0), 0,
            new EndosymbiosisData.InProgressEndosymbiosis(new MicrobeSpecies(1, "test", "test"), 1, cheapOrganelle));

        history.AddAction(new SingleEditorAction<EndosymbiontPlaceActionData>(_ => { }, _ => { }, endosymbiontData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MovingEndosymbiontIsFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var endosymbiontData = new EndosymbiontPlaceActionData(template, new Hex(0, 0), 0,
            new EndosymbiosisData.InProgressEndosymbiosis(new MicrobeSpecies(1, "test", "test"), 1, cheapOrganelle));

        history.AddAction(new SingleEditorAction<EndosymbiontPlaceActionData>(_ => { }, _ => { }, endosymbiontData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MultipleIndependentUpgrades()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var upgrades1 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test"],
        };

        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST,
            history.CalculateMutationPointsLeft());

        var upgrades2 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test", "test2"],
        };

        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST - TEST_UPGRADE_COST_2,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MultipleUpgradesCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var upgrades1 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test"],
        };

        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST,
            history.CalculateMutationPointsLeft());

        var upgrades2 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test2"],
        };

        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST_2,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MultipleUpgradesCombine3Step()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var upgrades1 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test"],
        };

        var noUpgrades = new OrganelleUpgrades();

        var upgradeData = new OrganelleUpgradeActionData(noUpgrades, upgrades1, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST,
            history.CalculateMutationPointsLeft());

        var upgradeData2 = new OrganelleUpgradeActionData(upgrades1, noUpgrades, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var upgrades2 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test2"],
        };

        var upgradeData3 = new OrganelleUpgradeActionData(noUpgrades, upgrades2, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData3));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST_2, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_AddAfterRemoveIsFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MoveAfterRemoveIsNotFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_PlacingTwiceWithRemoveIsNotFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var placementData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var placementData2 =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_DeleteBetweenUpgradesWorksCorrectly()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var upgrades1 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test"],
        };

        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());

        var placementData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var upgrades2 = new OrganelleUpgrades
        {
            UnlockedFeatures = ["test2"],
        };

        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);

        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - TEST_UPGRADE_COST_2,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_ReplacingCytoplasmRefundsIt()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(dummyCytoplasm, new Hex(0, 0), 0);

        var placementData = new OrganellePlacementActionData(template1, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - dummyCytoplasm.MPCost,
            history.CalculateMutationPointsLeft());

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template2, new Hex(0, 0), 0)
            {
                ReplacedCytoplasm = [template1],
            };

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_ReplacingMovedCytoplasmRefundsIt()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(dummyCytoplasm, new Hex(0, 0), 0);

        var placementData = new OrganellePlacementActionData(template1, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - dummyCytoplasm.MPCost,
            history.CalculateMutationPointsLeft());

        var moveData = new OrganelleMoveActionData(template1, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - dummyCytoplasm.MPCost,
            history.CalculateMutationPointsLeft());

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template2, new Hex(1, 0), 0)
            {
                ReplacedCytoplasm = [template1],
            };

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_ReplacingMovedCytoplasmWithoutPlacingIt()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(dummyCytoplasm, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template1, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template2, new Hex(1, 0), 0)
            {
                ReplacedCytoplasm = [template1],
            };

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MoveCanBeRefundedByPlacingNewOrganelle()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template1, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var actionData = new OrganellePlacementActionData(template2, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MovingBackRefundsTheCost()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var moveData2 = new OrganelleMoveActionData(template, new Hex(1, 0), new Hex(0, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_MoveBackDoesNotMakeFutureMovesFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var moveData2 = new OrganelleMoveActionData(template, new Hex(1, 0), new Hex(0, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData2));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        var moveData3 = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(2, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData3));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RigidityChangesCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rigidityAction1 = new RigidityActionData(0.2f, 0.0f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction1));

        var changeCost1 = RigidityActionData.CalculateRigidityCost(0.2f, 0);
        Assert.True(changeCost1 > 0.01f);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - changeCost1, history.CalculateMutationPointsLeft());

        var rigidityAction2 = new RigidityActionData(0.3f, 0.2f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction2));

        var totalCost = RigidityActionData.CalculateRigidityCost(0.3f, 0);

        Assert.True(totalCost > changeCost1);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - totalCost, history.CalculateMutationPointsLeft());

        // Then back to 0 to have full MP remaining
        var rigidityAction3 = new RigidityActionData(0.0f, 0.3f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction3));
        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_RigidityChangesCombineWithMoveInBetween()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rigidityAction1 = new RigidityActionData(0.2f, 0.0f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction1));

        var changeCost1 = RigidityActionData.CalculateRigidityCost(0.2f, 0);
        Assert.True(changeCost1 > 0.01f);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - changeCost1, history.CalculateMutationPointsLeft());

        var rigidityAction2 = new RigidityActionData(0.3f, 0.2f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction2));

        var totalCost = RigidityActionData.CalculateRigidityCost(0.3f, 0);

        Assert.True(totalCost > changeCost1);

        Assert.Equal(Constants.BASE_MUTATION_POINTS - totalCost, history.CalculateMutationPointsLeft());

        var moveAction = new OrganelleMoveActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0),
            new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveAction));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - totalCost - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        // Then back to 0 to have full MP remaining
        var rigidityAction3 = new RigidityActionData(0.0f, 0.3f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction3));
        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST, history.CalculateMutationPointsLeft());

        // Undo the move
        var moveAction2 = new OrganelleMoveActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0),
            new Hex(1, 0), new Hex(0, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveAction2));
        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        // And once more up to check a more complex combine
        var rigidityAction4 = new RigidityActionData(0.2f, 0.0f);

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction4));

        var changeCost4 = RigidityActionData.CalculateRigidityCost(0.2f, 0);
        Assert.Equal(Constants.BASE_MUTATION_POINTS - changeCost4, history.CalculateMutationPointsLeft());
    }

    // TODO: implement a test for custom upgrade data changing and having an MP cost once that is supported

    [Fact]
    public void EditorMPTests_MoveDeleteAndAddingBackIsFree()
    {
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_REMOVE_COST,
            history.CalculateMutationPointsLeft());

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template2, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());

        moveData = new OrganelleMoveActionData(template2, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - Constants.ORGANELLE_MOVE_COST,
            history.CalculateMutationPointsLeft());
    }

    [Fact]
    public void EditorMPTests_ReAddingOrganelleAfterRemoveAndMove()
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

        // Adding it back after a delete operation
        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        actionData =
            new OrganellePlacementActionData(template2, new Hex(1, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        moveData = new OrganelleMoveActionData(template2, new Hex(1, 0), new Hex(2, 0), 0, 0);

        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS - cheapOrganelle.MPCost, history.CalculateMutationPointsLeft());

        removeData =
            new OrganelleRemoveActionData(template2, new Hex(2, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        Assert.Equal(Constants.BASE_MUTATION_POINTS, history.CalculateMutationPointsLeft());
    }
}
