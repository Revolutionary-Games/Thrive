namespace ThriveTest.MicrobeStage;

using System;
using System.Collections.Generic;
using Utils;
using Xunit;

public class EditorMPTests
{
    private const int TEST_UPGRADE_COST = 11;
    private const int TEST_UPGRADE_COST_2 = 26;

    private const int TEST_MEMBRANE_COST_1 = 50;
    private const int TEST_MEMBRANE_COST_2 = 55;
    private const int TEST_MEMBRANE_COST_3 = 60;

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
        Hexes = [new Hex(0, 0)],
    };

    private readonly OrganelleDefinition dummyCytoplasm = new()
    {
        MPCost = 18,
        Name = "Cytoplasm",
        InternalName = "cytoplasm",
        Hexes = [new Hex(0, 0)],
    };

    private readonly OrganelleDefinition dummyNucleus = new()
    {
        MPCost = 70,
        Name = "Nucleus",
        InternalName = "nucleus",
        Hexes = [new Hex(0, 0), new Hex(1, 0), new Hex(0, 1)],
    };

    private readonly MembraneType originalMembrane = new()
    {
        EditorCost = TEST_MEMBRANE_COST_1,
        Name = "OriginalMembrane",
    };

    private readonly MembraneType testMembrane1 = new()
    {
        EditorCost = TEST_MEMBRANE_COST_1,
        Name = "Test1",
    };

    private readonly MembraneType testMembrane2 = new()
    {
        EditorCost = TEST_MEMBRANE_COST_2,
        Name = "Test2",
    };

    private readonly MembraneType testMembrane3 = new()
    {
        EditorCost = TEST_MEMBRANE_COST_3,
        Name = "Test3",
    };

    private readonly IReadOnlyMicrobeSpecies speciesTemplate1;
    private readonly IReadOnlyMicrobeSpecies speciesTemplate2;
    private readonly IReadOnlyMicrobeSpecies speciesTemplate3;

    private readonly MicrobeSpeciesComparer speciesComparer;

    public EditorMPTests()
    {
        speciesComparer = new MicrobeSpeciesComparer(dummyCytoplasm);

        speciesTemplate1 = new MicrobeSpecies(1, "test1", "test1")
        {
            ModifiableOrganelles = { new OrganelleTemplate(dummyCytoplasm, new Hex(2, 1), 0) },
            MembraneType = originalMembrane,
        };

        speciesTemplate2 = new MicrobeSpecies(1, "test1", "test1")
        {
            ModifiableOrganelles = { new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0) },
            MembraneType = originalMembrane,
        };

        speciesTemplate3 = new MicrobeSpecies(1, "test1", "test1")
        {
            ModifiableOrganelles = { new OrganelleTemplate(dummyNucleus, new Hex(0, 0), 0) },
            MembraneType = originalMembrane,
        };
    }

    [Fact]
    public void EditorMPTests_EmptyIsFullMP()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_SimpleAdd()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var actionData =
            new OrganellePlacementActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0), new Hex(0, 0), 0);

        bool undo = false;
        bool redo = false;

        history.AddAction(
            new SingleEditorAction<OrganellePlacementActionData>(_ => redo = true, _ => undo = true, actionData));

        Assert.True(redo);
        Assert.False(undo);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_SimpleRemove()
    {
        var originalSpecies = speciesTemplate1.Clone(true);
        originalSpecies.ModifiableOrganelles.Add(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0));

        Assert.Equal(2, originalSpecies.ModifiableOrganelles.Count);

        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
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

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_SimpleMove()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        bool undo = false;
        bool redo = false;

        history.AddAction(
            new SingleEditorAction<OrganelleMoveActionData>(_ => redo = true, _ => undo = true, moveData));

        Assert.True(redo);
        Assert.False(undo);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MoveIsFreeAfterAdd()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
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

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);

        history.AddAction(
            new SingleEditorAction<OrganelleMoveActionData>(_ => redo = true, _ => undo = true, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsAddCost()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var organelle = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(organelle, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new OrganelleRemoveActionData(organelle, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsAddMoveCosts()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);

        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsAddMoveUpgradeCosts()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test"],
        }, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost + TEST_UPGRADE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RemoveCancelsMoveCost()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RemovingEndosymbiontIsFree()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0, true);

        var endosymbiontData = new EndosymbiontPlaceActionData(template, new Hex(0, 0), 0,
            new EndosymbiosisData.InProgressEndosymbiosis(new MicrobeSpecies(1, "test", "test"), 1, cheapOrganelle));

        history.AddAction(new SingleEditorAction<EndosymbiontPlaceActionData>(_ => { }, _ => { }, endosymbiontData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MovingEndosymbiontIsFree()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0, true);

        var endosymbiontData = new EndosymbiontPlaceActionData(template, new Hex(0, 0), 0,
            new EndosymbiosisData.InProgressEndosymbiosis(new MicrobeSpecies(1, "test", "test"), 1, cheapOrganelle));

        history.AddAction(new SingleEditorAction<EndosymbiontPlaceActionData>(_ => { }, _ => { }, endosymbiontData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData =
            new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MultipleIndependentUpgrades()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var upgrades1 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test"],
        };
        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var upgrades2 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test", "test2"],
        };
        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST + TEST_UPGRADE_COST_2, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MultipleUpgradesCombine()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var upgrades1 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test"],
        };
        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var upgrades2 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test2"],
        };
        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST_2, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MultipleUpgradesCombine3Step()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var upgrades1 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test"],
        };
        var noUpgrades = new OrganelleUpgrades();

        var upgradeData = new OrganelleUpgradeActionData(noUpgrades, upgrades1, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var upgradeData2 = new OrganelleUpgradeActionData(upgrades1, noUpgrades, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var upgrades2 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test2"],
        };
        var upgradeData3 = new OrganelleUpgradeActionData(noUpgrades, upgrades2, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData3));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST_2, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_AddAfterRemoveIsFree()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var removeData = new OrganelleRemoveActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var actionData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MoveAfterRemoveIsNotFree()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var removeData = new OrganelleRemoveActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var actionData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_PlacingTwiceWithRemoveIsNotFree()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var placementData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData = new OrganelleRemoveActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var placementData2 = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_DeleteBetweenUpgradesWorksCorrectly()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var upgrades1 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test"],
        };
        var upgradeData = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades1, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData = new OrganelleRemoveActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var placementData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var upgrades2 = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = ["test2"],
        };
        var upgradeData2 = new OrganelleUpgradeActionData(new OrganelleUpgrades(), upgrades2, template);
        history.AddAction(new SingleEditorAction<OrganelleUpgradeActionData>(_ => { }, _ => { }, upgradeData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_UPGRADE_COST_2, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_ReplacingCytoplasmRefundsIt()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(dummyCytoplasm, new Hex(0, 0), 0);
        var placementData = new OrganellePlacementActionData(template1, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(dummyCytoplasm.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var actionData =
            new OrganellePlacementActionData(template2, new Hex(0, 0), 0)
            {
                ReplacedCytoplasm = [template1],
            };

        // Cytoplasm needs to be deleted from below now with the new check system
        var deleteData = new OrganelleRemoveActionData(template1, new Hex(0, 0), 0);

        history.AddAction(new CombinedEditorAction(
            new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData),
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData)));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_ReplacingMovedCytoplasmRefundsIt()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(dummyCytoplasm, new Hex(0, 0), 0);
        var placementData = new OrganellePlacementActionData(template1, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(dummyCytoplasm.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template1, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(dummyCytoplasm.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        // Combined place and delete
        var actionData =
            new OrganellePlacementActionData(template2, new Hex(1, 0), 0)
            {
                ReplacedCytoplasm = [template1],
            };
        var deleteData = new OrganelleRemoveActionData(template1, new Hex(1, 0), 0);

        history.AddAction(new CombinedEditorAction(
            new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData),
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData)));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_ReplacingMovedCytoplasmWithoutPlacingIt()
    {
        var originalSpecies = speciesTemplate1.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template1 = originalSpecies.Organelles.GetElementAt(new Hex(2, 1), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var moveData = new OrganelleMoveActionData(template1, new Hex(2, 1), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(2, 1), 0);

        // Combined place and delete
        var actionData =
            new OrganellePlacementActionData(template2, new Hex(2, 1), 0)
            {
                ReplacedCytoplasm = [template1],
            };
        var deleteData = new OrganelleRemoveActionData(template1, new Hex(1, 0), 0);

        history.AddAction(new CombinedEditorAction(
            new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData),
            new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData)));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MoveCanBeRefundedByPlacingNewOrganelle()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template1 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var moveData = new OrganelleMoveActionData(template1, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var actionData = new OrganellePlacementActionData(template2, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MovingBackRefundsTheCost()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData2 = new OrganelleMoveActionData(template, new Hex(1, 0), new Hex(0, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_MoveBackDoesNotMakeFutureMovesFree()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData2 = new OrganelleMoveActionData(template, new Hex(1, 0), new Hex(0, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData3 = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(2, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData3));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RigidityChangesCombine()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var rigidityAction1 = new RigidityActionData(0.2f, 0.0f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction1));

        var changeCost1 = MicrobeSpeciesComparer.CalculateRigidityCost(0.2f, 0);
        Assert.True(changeCost1 > 0.01f);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(changeCost1, speciesComparer.Compare(originalSpecies, editsFacade));

        var rigidityAction2 = new RigidityActionData(0.3f, 0.2f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction2));

        var totalCost = MicrobeSpeciesComparer.CalculateRigidityCost(0.3f, 0);
        Assert.True(totalCost > changeCost1);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(totalCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var rigidityAction3 = new RigidityActionData(0.0f, 0.3f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction3));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RigidityChangesCombineWithMoveInBetween()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var rigidityAction1 = new RigidityActionData(0.2f, 0.0f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction1));

        var changeCost1 = MicrobeSpeciesComparer.CalculateRigidityCost(0.2f, 0);
        Assert.True(changeCost1 > 0.01f);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(changeCost1, speciesComparer.Compare(originalSpecies, editsFacade));

        var rigidityAction2 = new RigidityActionData(0.3f, 0.2f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction2));

        var totalCost = MicrobeSpeciesComparer.CalculateRigidityCost(0.3f, 0);
        Assert.True(totalCost > changeCost1);

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(totalCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveAction = new OrganelleMoveActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0),
            new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveAction));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(totalCost + Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        // Then back to 0 to have full MP remaining
        var rigidityAction3 = new RigidityActionData(0.0f, 0.3f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction3));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        // Undo the move
        var moveAction2 = new OrganelleMoveActionData(new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0),
            new Hex(1, 0), new Hex(0, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveAction2));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        // And once more up to check a more complex combine
        var rigidityAction4 = new RigidityActionData(0.2f, 0.0f);
        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => { }, _ => { }, rigidityAction4));

        var changeCost4 = MicrobeSpeciesComparer.CalculateRigidityCost(0.2f, 0);
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(changeCost4, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    // TODO: implement a test for custom upgrade data changing and having an MP cost once that is supported

    [Fact]
    public void EditorMPTests_MultipleMembraneChanges()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var membraneAction1 = new MembraneActionData(originalMembrane, testMembrane1);
        history.AddAction(new SingleEditorAction<MembraneActionData>(_ => { }, _ => { }, membraneAction1));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_MEMBRANE_COST_1, speciesComparer.Compare(originalSpecies, editsFacade));

        var membraneAction2 = new MembraneActionData(testMembrane1, testMembrane2);
        history.AddAction(new SingleEditorAction<MembraneActionData>(_ => { }, _ => { }, membraneAction2));
        ApplyFacadeEdits(editsFacade, history);
        Assert.True(TEST_MEMBRANE_COST_1 != TEST_MEMBRANE_COST_2);
        Assert.Equal(TEST_MEMBRANE_COST_2, speciesComparer.Compare(originalSpecies, editsFacade));

        var membraneAction3 = new MembraneActionData(testMembrane2, testMembrane3);
        history.AddAction(new SingleEditorAction<MembraneActionData>(_ => { }, _ => { }, membraneAction3));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(TEST_MEMBRANE_COST_3, speciesComparer.Compare(originalSpecies, editsFacade));

        // And back to the original
        var membraneAction4 = new MembraneActionData(testMembrane3, originalMembrane);
        history.AddAction(new SingleEditorAction<MembraneActionData>(_ => { }, _ => { }, membraneAction4));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        // Make sure they didn't all combine and cause that way the test to pass
        Assert.True(history.Undo());
        Assert.True(history.Undo());
        Assert.True(history.Undo());
    }

    [Fact]
    public void EditorMPTests_MoveDeleteAndAddingBackIsFree()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = originalSpecies.ModifiableOrganelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var removeData = new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_REMOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);
        var actionData = new OrganellePlacementActionData(template2, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        // Added back at the same position, so this is free with the new fully working refunds
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        moveData = new OrganelleMoveActionData(template2, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_ReAddingOrganelleAfterRemoveAndMove()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var actionData = new OrganellePlacementActionData(template, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var moveData = new OrganelleMoveActionData(template, new Hex(0, 0), new Hex(1, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        var removeData = new OrganelleRemoveActionData(template, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        // Adding it back after a delete operation
        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);
        actionData = new OrganellePlacementActionData(template2, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        moveData = new OrganelleMoveActionData(template2, new Hex(1, 0), new Hex(2, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        removeData = new OrganelleRemoveActionData(template2, new Hex(2, 0), 0);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, removeData));

        // Fully refunded with the new system
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_ToleranceChangeAllowsRevertingAt0()
    {
        var originalSpecies = speciesTemplate1;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var initialTolerances = new EnvironmentalTolerances();

        Assert.Equal(originalSpecies.Tolerances, initialTolerances);

        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        // Do some tolerance edits to get down to 0 MP left
        var changedTolerances1 = initialTolerances.Clone();
        changedTolerances1.TemperatureTolerance += 8;
        changedTolerances1.UVResistance = 0.01f;

        var toleranceData1 = new ToleranceActionData(initialTolerances, changedTolerances1);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData1));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances1),
            speciesComparer.Compare(originalSpecies, editsFacade));
        Assert.True(SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances1) > 5);

        var changedTolerances2 = changedTolerances1.Clone();
        changedTolerances2.TemperatureTolerance += 5;
        changedTolerances2.OxygenResistance = 0.1f;

        Assert.Equal(SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances2),
            SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances1) +
            SpeciesComparer.CalculateToleranceCost(changedTolerances1, changedTolerances2));

        var toleranceData2 = new ToleranceActionData(changedTolerances1, changedTolerances2);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData2));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances1) +
            SpeciesComparer.CalculateToleranceCost(changedTolerances1, changedTolerances2),
            speciesComparer.Compare(originalSpecies, editsFacade));

        var changedTolerances3 = changedTolerances2.Clone();
        changedTolerances3.TemperatureTolerance += 5;

        for (int i = 0; i < 1000; ++i)
        {
            if (SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances3) >=
                Constants.BASE_MUTATION_POINTS)
            {
                // If overshot
                if (SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances3) >
                    Constants.BASE_MUTATION_POINTS + 0.01)
                {
                    Assert.Fail("Logic overshoot in MP consuming action creation");
                }

                break;
            }

            changedTolerances3.TemperatureTolerance += 1;
        }

        // Ensure all mutation points taken
        var toleranceData3 = new ToleranceActionData(changedTolerances2, changedTolerances3);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData3));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(SpeciesComparer.CalculateToleranceCost(initialTolerances, changedTolerances3),
            speciesComparer.Compare(originalSpecies, editsFacade));
        Assert.Equal(Constants.BASE_MUTATION_POINTS, speciesComparer.Compare(originalSpecies, editsFacade));

        // And then it should be possible to go in the opposite direction to restore MP
        var changedTolerances4 = changedTolerances3.Clone();
        changedTolerances4.TemperatureTolerance -= 30;
        var toleranceData4 = new ToleranceActionData(changedTolerances3, changedTolerances4);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData4));
        ApplyFacadeEdits(editsFacade, history);
        Assert.True(Constants.BASE_MUTATION_POINTS - speciesComparer.Compare(originalSpecies, editsFacade) > 10);

        // As we used different stats, they should not all combine into a single action (this verifies the test is
        // actually testing what it is supposed to)
        Assert.True(history.Undo());
        Assert.True(history.Undo());
    }

    [Fact]
    public void EditorMPTests_DeletingOtherOrganelleAfterPlaceCountsAsAMove()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);

        var actionData =
            new OrganellePlacementActionData(new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0), new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, actionData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var deleteData = new OrganelleRemoveActionData(template);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        // Moving back to the original position then resets all costs
        var moveData = new OrganelleMoveActionData(template, new Hex(1, 0), new Hex(0, 0), 0, 0);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));

        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_DeletingDoesNotRefundTooMuchAfterMoveOfReplacedOldOrganelle()
    {
        var originalSpecies = speciesTemplate2;
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var template = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var placementData = new OrganellePlacementActionData(template2, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var deleteData = new OrganelleRemoveActionData(template);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var template3 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 1), 0);
        var placement2 = new OrganellePlacementActionData(template3, new Hex(0, 1), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placement2));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost + Constants.ORGANELLE_MOVE_COST,
            speciesComparer.Compare(originalSpecies, editsFacade));

        // Major infinite MP exploit reported for 0.9.0
        var deleteData2 = new OrganelleRemoveActionData(template3);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData2));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_FullRefundIsNotGivenAfterPlacingMultipleOrganelles()
    {
        var originalSpecies = speciesTemplate2.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var originalToRemove = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var template2 = new OrganelleTemplate(cheapOrganelle, new Hex(1, 0), 0);

        var placementData = new OrganellePlacementActionData(template2, new Hex(1, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placementData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));

        var deleteData = new OrganelleRemoveActionData(originalToRemove);
        history.AddAction(new SingleEditorAction<OrganelleRemoveActionData>(_ => { }, _ => { }, deleteData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));

        var template3 = new OrganelleTemplate(cheapOrganelle, new Hex(0, 0), 0);
        var placement2 = new OrganellePlacementActionData(template3, new Hex(0, 0), 0);
        history.AddAction(new SingleEditorAction<OrganellePlacementActionData>(_ => { }, _ => { }, placement2));
        ApplyFacadeEdits(editsFacade, history);

        Assert.Equal(cheapOrganelle.MPCost, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    [Fact]
    public void EditorMPTests_RotatingOrganelleIsFree()
    {
        var originalSpecies = speciesTemplate3.Clone(true);
        var editsFacade = new MicrobeEditsFacade(originalSpecies, dummyNucleus);
        var history = new EditorActionHistory<EditorAction>();

        var originalToMove = originalSpecies.Organelles.GetElementAt(new Hex(0, 0), new List<Hex>()) ??
            throw new Exception("Couldn't find organelle");

        var moveData = new OrganelleMoveActionData(originalToMove, new Hex(0, 0), new Hex(1, 0), 0, 2);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(0, speciesComparer.Compare(originalSpecies, editsFacade));

        moveData = new OrganelleMoveActionData(originalToMove, new Hex(1, 0), new Hex(2, 0), 2, 2);
        history.AddAction(new SingleEditorAction<OrganelleMoveActionData>(_ => { }, _ => { }, moveData));
        ApplyFacadeEdits(editsFacade, history);
        Assert.Equal(Constants.ORGANELLE_MOVE_COST, speciesComparer.Compare(originalSpecies, editsFacade));
    }

    private void ApplyFacadeEdits(MicrobeEditsFacade facade, EditorActionHistory<EditorAction> history)
    {
        var actions = new List<EditorCombinableActionData>();
        history.GetPerformedActionData(actions);
        facade.SetActiveActions(actions);
    }
}
