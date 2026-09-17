using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoEvo;
using GdUnit4;
using Godot;
using Systems;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCacheResultIsolationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EnergyBalanceReadsReuseCachedResult(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var biome = CreatePatch().Biome;
        var cache = CreateCache();
        var expected = EnergyValues(CreateCache().GetEnergyBalanceForSpecies(species, biome));
        var first = cache.GetEnergyBalanceForSpecies(species, biome);
        var second = cache.GetEnergyBalanceForSpecies(species, biome);

        AssertThat(EnergyValues(second).SequenceEqual(expected)).IsTrue();
        AssertThat(ReferenceEquals(first, second)).IsTrue();

        cache.Clear();
        var afterClear = cache.GetEnergyBalanceForSpecies(species, biome);
        AssertThat(ReferenceEquals(first, afterClear)).IsFalse();
        AssertThat(EnergyValues(first).SequenceEqual(expected)).IsTrue();
        AssertThat(EnergyValues(afterClear).SequenceEqual(expected)).IsTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ProcessSpeedReadsReuseCachedResult(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var biome = CreatePatch().Biome;
        var cache = CreateCache();
        var processes = cache.GetActiveProcessList(species);
        AssertThat(processes.Count > 0).IsTrue();

        for (var i = 0; i < processes.Count; ++i)
        {
            var process = processes[i];
            var expected = ProcessSystem.CalculateProcessMaximumSpeed(process, 1, biome,
                CompoundAmountType.Average, true);
            var first = cache.GetProcessMaximumSpeed(process, 1, biome);
            var second = cache.GetProcessMaximumSpeed(process, 1, biome);
            AssertProcessEqual(expected, second);
            AssertThat(ReferenceEquals(first, second)).IsTrue();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActiveProcessReadsReuseCachedListAndReturnElementsByValue(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var first = cache.GetActiveProcessList(species);
        var expected = first.ToArray();
        AssertThat(expected.Length > 0).IsTrue();

        var element = first[0];
        element.Rate = float.NaN;
        element.SpeedMultiplier = float.NaN;

        var second = cache.GetActiveProcessList(species);
        AssertThat(ReferenceEquals(first, second)).IsTrue();
        AssertThat(second.Count).IsEqual(expected.Length);
        for (var i = 0; i < expected.Length; ++i)
        {
            AssertThat(ReferenceEquals(second[i].Process, expected[i].Process)).IsTrue();
            AssertThat(second[i].Rate).IsEqual(expected[i].Rate);
            AssertThat(second[i].SpeedMultiplier).IsEqual(expected[i].SpeedMultiplier);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WarmConsumersDoNotAddAllocations(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var patch = CreatePatch();
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var pressure = new MaintainCompoundPressure(Compound.ATP, 1);

        // Multicellular tolerance resolution already allocates independently of result access.
        var toleranceAllocation =
            MeasureAllocations(() => cache.GetEnvironmentalTolerances(species, patch.Biome).ProcessSpeedModifier);

        Measure("IndividualCost", multicellular,
            () => MichePopulation.CalculateIndividualCost(species, patch.Biome, cache));
        Measure("MaintainCompound", multicellular, () => pressure.Score(species, patch, cache), toleranceAllocation);
        Measure("Conversion", multicellular,
            () => cache.GetCompoundConversionScoreForSpecies(glucose, atp, species));
        Measure("GeneratedFrom", multicellular,
            () => cache.GetCompoundGeneratedFrom(glucose, atp, species, patch.Biome), toleranceAllocation);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CachedResultsRemainValidAfterClear(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var energy = cache.GetEnergyBalanceForSpecies(species, biome);
        var active = cache.GetActiveProcessList(species);
        var speed = cache.GetProcessMaximumSpeed(active[0], 1, biome);
        var expectedEnergy = EnergyValues(energy);
        var expectedSpeed = ProcessSystem.CalculateProcessMaximumSpeed(active[0], 1, biome,
            CompoundAmountType.Average, true);
        var expectedProcesses = active.ToArray();

        cache.Clear();

        // Recreate the entries as well: retained results must not refer to reused scratch storage.
        var newEnergy = cache.GetEnergyBalanceForSpecies(species, biome);
        var newSpeed = cache.GetProcessMaximumSpeed(expectedProcesses[0], 1, biome);
        var newActive = cache.GetActiveProcessList(species);
        AssertThat(ReferenceEquals(energy, newEnergy)).IsFalse();
        AssertThat(ReferenceEquals(speed, newSpeed)).IsFalse();
        AssertThat(ReferenceEquals(active, newActive)).IsFalse();
        AssertThat(EnergyValues(energy).SequenceEqual(expectedEnergy)).IsTrue();
        AssertProcessEqual(expectedSpeed, speed);
        AssertProcessEqual(expectedSpeed, newSpeed);
        AssertThat(active.Count).IsEqual(expectedProcesses.Length);
        AssertThat(newActive.Count).IsEqual(expectedProcesses.Length);
        for (var i = 0; i < active.Count; ++i)
        {
            AssertThat(ReferenceEquals(active[i].Process, expectedProcesses[i].Process)).IsTrue();
            AssertThat(active[i].Rate).IsEqual(expectedProcesses[i].Rate);
            AssertThat(active[i].SpeedMultiplier).IsEqual(expectedProcesses[i].SpeedMultiplier);
            AssertThat(ReferenceEquals(newActive[i].Process, expectedProcesses[i].Process)).IsTrue();
            AssertThat(newActive[i].Rate).IsEqual(expectedProcesses[i].Rate);
            AssertThat(newActive[i].SpeedMultiplier).IsEqual(expectedProcesses[i].SpeedMultiplier);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WarmReadOnlyResultsAndEnumerationAllocateNothing(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var process = cache.GetActiveProcessList(species)[0];
        Measure("ReadOnlyResultsAndEnumeration", multicellular, () =>
        {
            var total = cache.GetEnergyBalanceForSpecies(species, biome).TotalProduction;
            var speed = cache.GetProcessMaximumSpeed(process, 1, biome);
            total += speed.CurrentSpeed + speed.ATPConsumption + speed.ATPProduction;
            foreach (var input in speed.AllInputs)
                total += input.Value;

            var active = cache.GetActiveProcessList(species);
            for (var i = 0; i < active.Count; ++i)
                total += active[i].Rate + active[i].SpeedMultiplier;

            return total;
        });
    }

    [TestCase]
    public void ReadOnlyProcessInputsIncludeEnvironmentalCompounds()
    {
        var species = CreateSpecies(false);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var processes = cache.GetActiveProcessList(species);
        var foundEnvironmentalInput = false;

        for (var i = 0; i < processes.Count; ++i)
        {
            var process = processes[i];
            var expected = ProcessSystem.CalculateProcessMaximumSpeed(process, 1, biome,
                CompoundAmountType.Average, true);
            var actual = cache.GetProcessMaximumSpeed(process, 1, biome);
            AssertProcessEqual(expected, actual);

            foreach (var input in actual.AllInputs)
            {
                if (SimulationParameters.GetCompound(input.Key).IsEnvironmental)
                    foundEnvironmentalInput = true;
            }
        }

        AssertThat(foundEnvironmentalInput).IsTrue();
    }

    [TestCase]
    public void CachedAndUncachedATPTrackingMatch()
    {
        var species = (MicrobeSpecies)CreateSpecies(false);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var tolerances = cache.GetEnvironmentalTolerances(species, biome);
        var expected = new EnergyBalanceInfoFull();
        var actual = new EnergyBalanceInfoFull();
        expected.SetupTrackingForRequiredCompounds();
        actual.SetupTrackingForRequiredCompounds();

        foreach (var organelle in species.Organelles)
        {
            var uncached = ProcessSystem.CalculateOrganelleProcessATPBalance(organelle, biome,
                CompoundAmountType.Average, tolerances.ProcessSpeedModifier, null);
            var cached = ProcessSystem.CalculateOrganelleProcessATPBalance(organelle, biome, CompoundAmountType.Average,
                tolerances.ProcessSpeedModifier, cache);
            AssertThat(cached.Production).IsEqual(uncached.Production);
            AssertThat(cached.Consumption).IsEqual(uncached.Consumption);

            ProcessSystem.AddOrganelleATPTracking(organelle, biome, tolerances, 1, CompoundAmountType.Average, null,
                expected);
            ProcessSystem.AddOrganelleATPTracking(organelle, biome, tolerances, 1, CompoundAmountType.Average, cache,
                actual);
        }

        AssertThat(actual.Consumption.SequenceEqual(expected.Consumption)).IsTrue();
        AssertThat(actual.Production.SequenceEqual(expected.Production)).IsTrue();
        AssertThat(actual.ProductionRequiresCompounds!.Count).IsEqual(expected.ProductionRequiresCompounds!.Count);
        foreach (var group in expected.ProductionRequiresCompounds)
            AssertThat(actual.ProductionRequiresCompounds[group.Key].SequenceEqual(group.Value)).IsTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PublicReadOnlyResultsDoNotAllocate(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var process = cache.GetActiveProcessList(species)[0];
        var energyBytes = MeasureAllocations(() => cache.GetEnergyBalanceForSpecies(species, biome).TotalProduction);
        var speedBytes = MeasureAllocations(() => cache.GetProcessMaximumSpeed(process, 1, biome).CurrentSpeed);
        var activeBytes = MeasureAllocations(() => cache.GetActiveProcessList(species).Count);
        GD.Print($"CACHE_PUBLIC_READ multicellular={multicellular} iterations=100000 " +
            $"energyBytes={energyBytes} speedBytes={speedBytes} activeBytes={activeBytes}");
        AssertThat(energyBytes).IsEqual(0L);
        AssertThat(speedBytes).IsEqual(0L);
        AssertThat(activeBytes).IsEqual(0L);
    }

    private static long MeasureAllocations(Func<float> operation)
    {
        for (var i = 0; i < 100000; ++i)
            operation();

        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100000; ++i)
            operation();

        return GC.GetAllocatedBytesForCurrentThread() - allocated;
    }

    private static void Measure(string name, bool multicellular, Func<float> operation, long baselineAllocation = 0)
    {
        const int iterations = 100000;
        var expected = operation();
        AssertThat(float.IsFinite(expected)).IsTrue();
        for (var i = 0; i < iterations; ++i)
            operation();

        var minimumAllocation = long.MaxValue;
        for (var round = 0; round < 7; ++round)
        {
            float total = 0;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            for (var i = 0; i < iterations; ++i)
                total += operation();

            var elapsed = Stopwatch.GetTimestamp() - started;
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            GD.Print($"CACHE_BENCH {name} multicellular={multicellular} round={round} " +
                $"iterations={iterations} bytes={allocated} baselineBytes={baselineAllocation} " +
                $"ns/op={elapsed * 1e9 / Stopwatch.Frequency / iterations} " +
                $"value={expected} total={total}");
            AssertThat(float.IsFinite(total)).IsTrue();
            minimumAllocation = Math.Min(minimumAllocation, allocated);
        }

        AssertThat(minimumAllocation - baselineAllocation).IsEqual(0L);
    }

    private static void AssertProcessEqual(ProcessSpeedInformation expected, IReadOnlyProcessSpeedInfo actual)
    {
        AssertThat(actual.CurrentSpeed).IsEqual(expected.CurrentSpeed);
        AssertThat(actual.ATPProduction).IsEqual(expected.ATPProduction);
        AssertThat(actual.ATPConsumption).IsEqual(expected.ATPConsumption);
        var actualInputs = new List<KeyValuePair<Compound, float>>();
        foreach (var input in actual.AllInputs)
            actualInputs.Add(input);

        AssertThat(actualInputs.SequenceEqual(expected.WritableInputs)).IsTrue();
    }

    private static float[] EnergyValues(IReadOnlyEnergyBalanceInfo value)
    {
        return
        [
            value.BaseMovement, value.Flagella, value.Actomyosin, value.Cilia, value.TotalMovement,
            value.Osmoregulation,
            value.TotalProduction, value.TotalConsumption, value.TotalConsumptionStationary, value.FinalBalance,
            value.FinalBalanceStationary,
        ];
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings { Seed = 1 });
    }

    private static Patch CreatePatch()
    {
        return new Patch(new LocalizedString("Isolation"), 1,
            SimulationParameters.Instance.GetBiome("aavolcanic_vent"), BiomeType.Vents,
            new PatchRegion(0, "Isolation", PatchRegion.RegionType.Sea, Vector2.Zero), 1);
    }

    private static Species CreateSpecies(bool multicellular)
    {
        var parameters = SimulationParameters.Instance;
        var membrane = parameters.GetMembrane("single");
        var organelles = new[] { "cytoplasm", "rusticyanin", "chloroplast" };
        if (!multicellular)
        {
            var species = new MicrobeSpecies(1, "Isolation", "Microbe")
            {
                IsBacteria = true,
                MembraneType = membrane,
            };
            for (var i = 0; i < organelles.Length; ++i)
            {
                species.Organelles.Add(new OrganelleTemplate(parameters.GetOrganelleType(organelles[i]),
                    new Hex(i * 4, 0), 0));
            }

            species.OnEdited();
            return species;
        }

        var cell = new CellType(membrane) { CellTypeName = "Isolation" };
        for (var i = 0; i < organelles.Length; ++i)
        {
            cell.ModifiableOrganelles.Add(new OrganelleTemplate(parameters.GetOrganelleType(organelles[i]),
                new Hex(i * 4, 0), 0));
        }

        var colony = new MulticellularSpecies(2, "Isolation", "Multicellular");
        colony.ModifiableCellTypes.Add(cell);
        colony.ModifiableGameplayCells.AddFast(new CellTemplate(cell, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        colony.OnEdited();
        return colony;
    }
}
