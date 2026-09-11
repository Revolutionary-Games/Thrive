using System;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Verifies that auto-evo compound throughput includes each cell's specialisation exactly once.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class SimulationCacheProcessSpeedTests
{
    [TestCase]
    public void GeneratedCompound_IncludesMicrobeSpecializationAcrossCacheLifetimes()
    {
        var parameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(1001, "Process", "Microbe")
        {
            IsBacteria = true,
            MembraneType = parameters.GetMembrane("single"),
        };
        species.Organelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0));
        species.Organelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"), new Hex(1, 0), 0));
        species.OnEdited();
        AssertThat(species.CellTypeSpecializationBonus).IsGreater(1.0f);

        var rawRate = CytoplasmRate() * 2;
        VerifyThroughput(species, rawRate * species.CellTypeSpecializationBonus);

        // Reading the auto-evo list must not change shared organelle process definitions.
        AssertThat(CytoplasmRate()).IsEqual(rawRate / 2);
    }

    [TestCase]
    public void GeneratedCompound_WeightsDifferentCellTypesAndRepeatedAdjacentCells()
    {
        var specialised = SimulationCacheTestFixtures.CreateCellType("Specialised", "single",
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(0, 0)),
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(1, 0)));
        var ordinary = SimulationCacheTestFixtures.CreateCellType("Ordinary", "single",
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(0, 0)));
        var species = SimulationCacheTestFixtures.CreateMulticellular(1002, "Processes",
            (specialised, new Hex(0, 0)), (specialised, new Hex(0, 1)), (ordinary, new Hex(-1, 0)));

        AssertThat(specialised.CellTypeSpecializationBonus).IsGreater(ordinary.CellTypeSpecializationBonus);
        var adjacency = 1 + Constants.CELL_ADJACENCY_SPECIALIZATION_BONUS;
        var expectedRate = CytoplasmRate() *
            (2 * 2 * specialised.CellTypeSpecializationBonus * adjacency + ordinary.CellTypeSpecializationBonus);
        VerifyThroughput(species, expectedRate);
    }

    [TestCase]
    public void MaintainCompoundPressure_UniformMicrobeSpecializationCancelsInRatio()
    {
        var species = SimulationCacheTestFixtures.CreateMicrobe(1003, "Maintenance", "single",
            "cytoplasm", "chloroplast");
        AssertThat(species.CellTypeSpecializationBonus).IsGreater(1.0f);
        var patch = CreateMaintenancePatch();
        var expected = PhotosynthesisGlucoseProduction() / CytoplasmGlucoseConsumption();
        var pressure = new MaintainCompoundPressure(Compound.Glucose, 1);
        var actual = pressure.Score(species, patch, SimulationCacheTestFixtures.CreateCache());

        AssertThat(actual).IsGreater(0.0f);
        AssertThat(actual).IsLess(1.0f);
        AssertThat(MathF.Abs(actual - expected) < 1e-5f).IsTrue();
    }

    [TestCase]
    public void MaintainCompoundPressure_WeightsProductionAndConsumptionByCellSpecialization()
    {
        var producer = SimulationCacheTestFixtures.CreateCellType("Producer", "single",
            SimulationCacheTestFixtures.CreateOrganelle("chloroplast", new Hex(0, 0)));
        var consumer = SimulationCacheTestFixtures.CreateCellType("Consumer", "single",
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(0, 0)),
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(1, 0)));
        var species = SimulationCacheTestFixtures.CreateMulticellular(1004, "Maintenance",
            (producer, new Hex(0, 0)), (consumer, new Hex(1, 0)));
        AssertThat(producer.CellTypeSpecializationBonus).IsNotEqual(consumer.CellTypeSpecializationBonus);
        var unweighted = PhotosynthesisGlucoseProduction() / (2 * CytoplasmGlucoseConsumption());
        var expected = unweighted * producer.CellTypeSpecializationBonus / consumer.CellTypeSpecializationBonus;
        var pressure = new MaintainCompoundPressure(Compound.Glucose, 1);
        var cache = SimulationCacheTestFixtures.CreateCache();
        var patch = CreateMaintenancePatch();
        var actual = pressure.Score(species, patch, cache);

        AssertThat(actual).IsGreater(0.0f);
        AssertThat(actual).IsLess(1.0f);
        AssertThat(MathF.Abs(actual - expected) < 1e-5f).IsTrue();
        AssertThat(MathF.Abs(actual - unweighted) > 1e-5f).IsTrue();
        AssertThat(pressure.Score(species, patch, cache)).IsEqual(actual);
    }

    private static Patch CreateMaintenancePatch()
    {
        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var conditions = SimulationCacheTestFixtures.CreateBiome();
        conditions.ChangeableCompounds[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0.01f };
        conditions.ChangeableCompounds[Compound.Carbondioxide] = new BiomeCompoundProperties { Ambient = 0.15f };
        conditions.AverageCompounds[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0.01f };
        conditions.AverageCompounds[Compound.Carbondioxide] = new BiomeCompoundProperties { Ambient = 0.15f };
        return new Patch(new LocalizedString("Maintenance"), 1, template, BiomeType.Vents,
            new PatchSnapshot(conditions, template.Background), 1);
    }

    private static float PhotosynthesisGlucoseProduction()
    {
        var process = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var rate = SimulationParameters.Instance.GetOrganelleType("chloroplast").RunnableProcesses[0].Rate;
        return process.Outputs[SimulationParameters.GetCompound(Compound.Glucose)] * rate * 0.01f;
    }

    private static float CytoplasmGlucoseConsumption()
    {
        var process = SimulationParameters.Instance.GetBioProcess("glycolysis_cytoplasm");
        return process.Inputs[SimulationParameters.GetCompound(Compound.Glucose)] * CytoplasmRate();
    }

    private static void VerifyThroughput(Species species, float expectedRate)
    {
        var cache = SimulationCacheTestFixtures.CreateCache();
        var biome = SimulationCacheTestFixtures.CreateBiome();
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var process = SimulationParameters.Instance.GetBioProcess("glycolysis_cytoplasm");
        var tolerance = cache.GetEnvironmentalTolerances(species, biome).ProcessSpeedModifier;
        AssertThat(tolerance).IsGreater(0.0f);
        var expected = process.Outputs[atp] * (expectedRate * tolerance);

        for (int iteration = 0; iteration < 3; ++iteration)
        {
            if (iteration == 2)
                cache.Clear();

            var actual = cache.GetCompoundGeneratedFrom(glucose, atp, species, biome);
            AssertThat(MathF.Abs(actual - expected) <= MathF.Abs(expected) * 1e-5f).IsTrue();
            var active = cache.GetActiveProcessList(species);
            AssertThat(active).HasSize(1);
            AssertThat(MathF.Abs(active[0].Rate - expectedRate) <= expectedRate * 1e-5f).IsTrue();

            // This consumer scores reaction efficiency, which does not depend on speed or specialisation.
            AssertThat(cache.GetCompoundConversionScoreForSpecies(glucose, atp, species))
                .IsEqual(process.Outputs[atp] / process.Inputs[glucose]);
        }
    }

    private static float CytoplasmRate()
    {
        var processes = SimulationParameters.Instance.GetOrganelleType("cytoplasm").RunnableProcesses;
        AssertThat(processes).HasSize(1);
        return processes[0].Rate;
    }
}
