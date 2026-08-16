using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using Test.Utils;

[TestSuite]
[RequireGodotRuntime]
public class MulticellularSpeciesTests
{
    [TestCase]
    public void TestCloneSharingCellTypes()
    {
        var species = new MulticellularSpecies(1, "Test", "Species");
        var organelleType = SimulationParameters.Instance.GetOrganelleType("cytoplasm");
        var membraneType = SimulationParameters.Instance.GetMembrane("single");

        var cellType = new CellType(membraneType)
        {
            CellTypeName = "TestType",
        };

        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(organelleType, new Hex(0, 0), 0));

        species.ModifiableCellTypes.Add(cellType);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var cellTemplate = new CellTemplate(cellType, new Hex(0, 0), 0);
        species.ModifiableGameplayCells.AddFast(cellTemplate, workMemory1, workMemory2);

        // Ensure editor cells are generated
        var editorCells = species.ModifiableEditorCells;
        AssertThat(editorCells.Count).IsEqual(1);

        species.ModifiableSporeCellType = cellType;

        var cloned = (MulticellularSpecies)species.Clone();

        AssertThat(cloned.ModifiableCellTypes).HasSize(1);
        AssertThat(cloned.ModifiableGameplayCells.Count).IsEqual(1);
        AssertThat(cloned.ModifiableEditorCells.Count).IsEqual(1);

        var clonedCellType = cloned.ModifiableCellTypes[0];
        var clonedGameplayCellType = cloned.ModifiableGameplayCells[0].ModifiableCellType;
        var clonedEditorCellType = cloned.ModifiableEditorCells[0].Data?.ModifiableCellType;

        AssertThat(clonedGameplayCellType).IsSame(clonedCellType);
        AssertThat(clonedEditorCellType).IsNotNull().IsSame(clonedCellType);
        AssertThat(cloned.ModifiableSporeCellType).IsSame(clonedCellType);
    }

    [TestCase]
    public void TestMicrobeConversionUsesColonyRootCellType()
    {
        using var worldSimulation = new TestWorldSimulation();
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        var parameters = SimulationParameters.Instance;
        var workData1 = new List<Hex>();
        var workData2 = new List<Hex>();

        var microbeSpecies = new MicrobeSpecies(1, "Test", "microbe")
        {
            MembraneType = parameters.GetMembrane("single"),
            Colour = new Color(1, 1, 1),
        };
        microbeSpecies.Organelles.AddFast(
            new OrganelleTemplate(parameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0), workData1, workData2);
        microbeSpecies.Organelles.AddFast(
            new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"), new Hex(3, 0), 0), workData1, workData2);
        microbeSpecies.Organelles.AddFast(
            new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"), new Hex(4, 0), 0), workData1, workData2);
        microbeSpecies.OnEdited();

        var multicellularSpecies = new MulticellularSpecies(1, "Test", "multicellular");
        var rootCellType = new CellType(parameters.GetMembrane("single"));
        rootCellType.ModifiableOrganelles.Add(
            new OrganelleTemplate(parameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0));
        multicellularSpecies.ModifiableCellTypes.Add(rootCellType);
        multicellularSpecies.ModifiableGameplayCells.AddFast(new CellTemplate(rootCellType, new Hex(0, 0), 0),
            workData1, workData2);
        multicellularSpecies.OnEdited();

        SpawnHelpers.SpawnMicrobe(worldSimulation, spawnEnvironment, microbeSpecies, Vector3.Zero, false,
            GameteType.All);
        worldSimulation.ProcessAll(0.1f);

        var microbe =
            new FirstEntityGrabber(new QueryDescription().WithAll<PlayerMarker>(), worldSimulation.EntitySystem).Found;
        ref var organelles = ref microbe.Get<OrganelleContainer>();
        organelles.AllOrganellesDivided = true;

        AssertThat(organelles.Organelles).IsNotNull();
        AssertThat(organelles.Organelles!.Count).IsEqual(3);

        var totalSpecializationBonus = rootCellType.CellTypeSpecializationBonus *
            multicellularSpecies.GetAdjacencySpecializationBonus(0);
        var resolvedTolerances = new ResolvedMicrobeTolerances
        {
            ProcessSpeedModifier = 1,
            OsmoregulationModifier = 1,
            HealthModifier = 1,
        };

        MicrobeStage.ConvertMicrobeToMulticellular(microbe, multicellularSpecies, rootCellType, resolvedTolerances,
            totalSpecializationBonus, worldSimulation, workData1, workData2);

        AssertThat(microbe.Has<MicrobeSpeciesMember>()).IsFalse();
        AssertThat(microbe.Get<MulticellularSpeciesMember>().MulticellularCellType).IsSame(rootCellType);

        ref var convertedOrganelles = ref microbe.Get<OrganelleContainer>();
        AssertThat(convertedOrganelles.Organelles!.Count).IsEqual(rootCellType.ModifiableOrganelles.Count);
        AssertThat(convertedOrganelles.AllOrganellesDivided).IsFalse();
        AssertThat(microbe.Get<MulticellularGrowth>().NextBodyPlanCellToGrowIndex).IsEqual(1);
    }
}
