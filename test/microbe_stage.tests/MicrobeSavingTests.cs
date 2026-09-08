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
public class MicrobeSavingTests
{
    [TestCase]
    public void TestSavingMicrobeChemoreceptorSpeciesReferenceCycle()
    {
        var membraneType = SimulationParameters.Instance.GetMembrane("single");
        var cytoplasm = SimulationParameters.Instance.GetOrganelleType("cytoplasm");
        var chemoreceptor = SimulationParameters.Instance.GetOrganelleType("chemoreceptor");

        var speciesA = CreateSpecies(1, "A", membraneType, cytoplasm, chemoreceptor);
        var speciesB = CreateSpecies(2, "B", membraneType, cytoplasm, chemoreceptor);
        var speciesC = CreateSpecies(3, "C", membraneType, cytoplasm, chemoreceptor);

        // This is the important shape: while reading a species' organelle list, its upgrade data reads another
        // species, which in turn reads another organelle list. The final reference points back to the first species.
        SetChemoreceptorTarget(speciesA, speciesB);
        SetChemoreceptorTarget(speciesB, speciesC);
        SetChemoreceptorTarget(speciesC, speciesA);

        speciesA.BecomePlayerSpecies();
        var gameWorld = new GameWorld(new WorldGenerationSettings(), speciesA);
        var firstPatch = gameWorld.Map.CurrentPatch ?? throw new Exception("No patch");
        firstPatch.AddSpecies(speciesA, 10);
        firstPatch.AddSpecies(speciesB, 20);
        firstPatch.AddSpecies(speciesC, 30);
        gameWorld.RegisterAutoEvoCreatedSpecies(speciesB);
        gameWorld.RegisterAutoEvoCreatedSpecies(speciesC);

        var records = new Dictionary<uint, SpeciesRecordLite>
        {
            { speciesA.ID, new SpeciesRecordLite(speciesA.Population, (Species)speciesA.Clone()) },
            { speciesB.ID, new SpeciesRecordLite(speciesB.Population, (Species)speciesB.Clone()) },
            { speciesC.ID, new SpeciesRecordLite(speciesC.Population, (Species)speciesC.Clone()) },
        };
        gameWorld.GenerationHistory.Clear();
        gameWorld.GenerationHistory.Add(0, new GenerationRecord(0, records));

        var manager = new ThriveArchiveManager();
        using var data = new MemoryStream();
        using var writer = new SArchiveMemoryWriter(data, manager, false);

        manager.OnStartNewWrite(writer);
        writer.WriteObject(gameWorld);
        manager.OnFinishWrite(writer);

        using var reader = new SArchiveMemoryReader(data, manager);
        data.Position = 0;

        manager.OnStartNewRead(reader);
        var loadedWorld = reader.ReadObjectOrNull<GameWorld>();
        manager.OnFinishRead(reader);

        AssertThat(loadedWorld).IsNotNull();
        AssertThat(loadedWorld!.Species.Count).IsEqual(3);

        var loadedA = (MicrobeSpecies)loadedWorld.Species[speciesA.ID];
        var loadedB = (MicrobeSpecies)loadedWorld.Species[speciesB.ID];
        var loadedC = (MicrobeSpecies)loadedWorld.Species[speciesC.ID];
        AssertThat(loadedA.Organelles.Count).IsEqual(2);
        AssertThat(loadedB.Organelles.Count).IsEqual(2);
        AssertThat(loadedC.Organelles.Count).IsEqual(2);
        AssertThat(((ChemoreceptorUpgrades)loadedA.Organelles.Organelles[1].ModifiableUpgrades!.CustomUpgradeData!)
            .TargetSpecies).IsSame(loadedB);
        AssertThat(((ChemoreceptorUpgrades)loadedB.Organelles.Organelles[1].ModifiableUpgrades!.CustomUpgradeData!)
            .TargetSpecies).IsSame(loadedC);
        AssertThat(((ChemoreceptorUpgrades)loadedC.Organelles.Organelles[1].ModifiableUpgrades!.CustomUpgradeData!)
            .TargetSpecies).IsSame(loadedA);
    }

    private static MicrobeSpecies CreateSpecies(uint id, string name, MembraneType membraneType,
        OrganelleDefinition cytoplasm, OrganelleDefinition chemoreceptor)
    {
        var species = new MicrobeSpecies(id, name, "Species")
        {
            IsBacteria = true,
            MembraneType = membraneType,
        };
        species.Organelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
        species.Organelles.Add(new OrganelleTemplate(chemoreceptor, new Hex(1, 0), 0));
        return species;
    }

    private static void SetChemoreceptorTarget(MicrobeSpecies species, MicrobeSpecies target)
    {
        species.Organelles.Organelles[1].ModifiableUpgrades = new OrganelleUpgrades
        {
            CustomUpgradeData = new ChemoreceptorUpgrades(Compound.Invalid, target, 1000, 1, Colors.White),
        };
    }
}
