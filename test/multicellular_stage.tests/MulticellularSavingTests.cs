using System;
using System.Collections.Generic;
using System.IO;
using AutoEvo;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MulticellularSavingTests
{
    [TestCase]
    public void TestSavingMulticellularChemoreceptorWithSelfReference()
    {
        var species = new MulticellularSpecies(1, "Test", "Species");
        var organelleType = SimulationParameters.Instance.GetOrganelleType("cytoplasm");
        var chemoreceptor = SimulationParameters.Instance.GetOrganelleType("chemoreceptor");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(organelleType, new Hex(0, 0), 0));
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(chemoreceptor, new Hex(1, 0), 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                // Self-reference here which blows up saving in multicellular
                CustomUpgradeData = new ChemoreceptorUpgrades(Compound.Invalid, species, 1000, 1, Colors.White),
            },
        });

        species.ModifiableCellTypes.Add(cellType);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var cellTemplate = new CellTemplate(cellType, new Hex(0, 0), 0);
        species.ModifiableGameplayCells.AddFast(cellTemplate, workMemory1, workMemory2);

        // Ensure editor cells are generated
        var editorCells = species.ModifiableEditorCells;
        AssertThat(editorCells.Count).IsEqual(1);

        var playerSpecies = new MicrobeSpecies(2, "Player", "Player")
        {
            MembraneType = membraneType,
        };
        playerSpecies.Organelles.AddFast(new OrganelleTemplate(organelleType, new Hex(0, 0), 0), workMemory1,
            workMemory2);
        playerSpecies.OnEdited();

        // It needs to be wrapped in a GameWorld to trigger the problem this test is made against
        var gameWorld = new GameWorld(new WorldGenerationSettings(), playerSpecies);
        var firstPatch = gameWorld.Map.CurrentPatch ?? throw new Exception("No patch");
        firstPatch.AddSpecies(playerSpecies, 10);
        firstPatch.AddSpecies(species, 50);
        gameWorld.RegisterAutoEvoCreatedSpecies(species);

        var runResults = new RunResults();
        runResults.AddPopulationResultForSpecies(playerSpecies, firstPatch, 100);
        runResults.AddPopulationResultForSpecies(species, firstPatch, 60);

        // Simulate auto-evo has changed the multicellular species. This is key to triggering the bug.
        runResults.AddMutationResultForSpecies(species, (Species)species.Clone(),
            new KeyValuePair<Patch, long>(firstPatch, 0));

        gameWorld.AddCurrentGenerationToHistory();

        // For reference, these kinds of auto-evo results fail the test. This is because species are cloned when the
        // properties changed so that they stay steady in history.
        gameWorld.GenerationHistory.Add(1, new GenerationRecord(1,
            runResults.GetSpeciesRecords()));

        var manager = new ThriveArchiveManager();
        var data = new MemoryStream();
        var writer = new SArchiveMemoryWriter(data, manager, false);

        manager.OnStartNewWrite(writer);
        writer.WriteObject(gameWorld);
        manager.OnFinishWrite(writer);

        var reader = new SArchiveMemoryReader(data, manager);
        data.Position = 0;

        manager.OnStartNewRead(reader);
        var loadedWorld = reader.ReadObjectOrNull<GameWorld>();
        manager.OnFinishRead(reader);

        AssertThat(loadedWorld).IsNotNull();
        var loadedSpecies = loadedWorld!.Species[species.ID];

        AssertThat(loadedSpecies).IsNotNull();
        AssertThat(loadedSpecies).IsInstanceOf<MulticellularSpecies>();

        AssertThat(((MulticellularSpecies)loadedSpecies).ModifiableCellTypes.Count).IsEqual(1);
    }
}
