using System;
using System.Collections;
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
    public void EnergyBalanceMutationDoesNotPoisonCache(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var biome = CreatePatch().Biome;
        var cache = CreateCache();
        var expected = EnergyValues(CreateCache().GetEnergyBalanceForSpecies(species, biome));
        var first = cache.GetEnergyBalanceForSpecies(species, biome);
        first.Clear();
        first.BaseMovement = float.NaN;
        first.Flagella = float.NaN;
        first.Actomyosin = float.NaN;
        first.Cilia = float.NaN;
        first.TotalMovement = float.NaN;
        first.Osmoregulation = float.NaN;
        first.TotalProduction = float.NaN;
        first.TotalConsumption = float.NaN;
        first.TotalConsumptionStationary = float.NaN;
        first.FinalBalance = float.NaN;
        first.FinalBalanceStationary = float.NaN;

        var second = cache.GetEnergyBalanceForSpecies(species, biome);
        AssertThat(EnergyValues(second).SequenceEqual(expected)).IsTrue();
        AssertThat(ReferenceEquals(first, second)).IsFalse();
    }

    [TestCase]
    public void ProcessSpeedMutationDoesNotPoisonCache()
    {
        var species = CreateSpecies(false);
        var biome = CreatePatch().Biome;
        var cache = CreateCache();
        var processes = cache.GetActiveProcessList(species);
        AssertThat(processes.Count > 0).IsTrue();

        foreach (var process in processes)
        {
            var expected = CreateCache().GetProcessMaximumSpeed(process, 1, biome);
            var first = cache.GetProcessMaximumSpeed(process, 1, biome);
            first.ScaleSpeed(0, null);
            first.CurrentSpeed = float.NaN;
            first.Efficiency = float.NaN;
            first.ATPProduction = float.NaN;
            first.ATPConsumption = float.NaN;
            foreach (var dictionary in ProcessDictionaries(first))
            {
                dictionary.Clear();
                dictionary[Compound.ATP] = float.NaN;
            }

            first.WritableLimitingCompounds.Clear();
            first.WritableLimitingCompounds.Add(Compound.ATP);

            var second = cache.GetProcessMaximumSpeed(process, 1, biome);
            AssertProcessEqual(expected, second);
            AssertThat(ReferenceEquals(first, second)).IsFalse();
            var firstDictionaries = ProcessDictionaries(first);
            var secondDictionaries = ProcessDictionaries(second);
            for (var i = 0; i < firstDictionaries.Length; ++i)
                AssertThat(ReferenceEquals(firstDictionaries[i], secondDictionaries[i])).IsFalse();

            AssertThat(ReferenceEquals(first.WritableLimitingCompounds, second.WritableLimitingCompounds)).IsFalse();
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ActiveProcessMutationDoesNotPoisonCache(bool multicellular, bool clear)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var first = cache.GetActiveProcessList(species);
        var expected = first.ToArray();
        AssertThat(expected.Length > 0).IsTrue();
        if (clear)
        {
            first.Clear();
        }
        else
        {
            var changed = first[0];
            changed.Rate = float.NaN;
            changed.SpeedMultiplier = float.NaN;
            first[0] = changed;
        }

        var second = cache.GetActiveProcessList(species);
        AssertThat(ReferenceEquals(first, second)).IsFalse();
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
    public void SafeViewsDoNotExposeBackingAndSurviveClear(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var energy = cache.GetEnergyBalanceForSpeciesView(species, biome);
        var active = cache.GetActiveProcessListView(species);
        var speed = cache.GetProcessMaximumSpeedView(active[0], 1, biome);
        var expectedEnergy = EnergyValues(energy.ToMutableCopy());
        var expectedSpeed = speed.ToMutableCopy();
        var expectedProcesses = active.ToMutableCopy();

        AssertThat((object)energy is EnergyBalanceInfoSimple).IsFalse();
        AssertThat((object)speed is ProcessSpeedInformation).IsFalse();
        AssertThat((object)speed is IProcessDisplayInfo).IsFalse();
        AssertThat((object)active is List<TweakedProcess>).IsFalse();
        AssertThat((object)active is ICollection<TweakedProcess>).IsFalse();
        AssertThat((object)active is IList).IsFalse();
        AssertThat((object)speed.Inputs is Dictionary<Compound, float>).IsFalse();
        AssertThat((object)speed.Inputs is IDictionary<Compound, float>).IsFalse();
        AssertThat((object)speed.Inputs is ICollection<KeyValuePair<Compound, float>>).IsFalse();
        AssertThat((object)speed.Inputs is IDictionary).IsFalse();

        var element = active[0];
        element.Rate = float.NaN;
        element.SpeedMultiplier = float.NaN;
        AssertThat(active[0].Rate).IsEqual(expectedProcesses[0].Rate);
        AssertThat(active[0].SpeedMultiplier).IsEqual(expectedProcesses[0].SpeedMultiplier);

        energy.ToMutableCopy().Clear();
        speed.ToMutableCopy().WritableInputs.Clear();
        active.ToMutableCopy().Clear();
        cache.Clear();

        // Recreate the entries as well: a retained view must not refer to reused scratch storage.
        cache.GetEnergyBalanceForSpecies(species, biome).Clear();
        cache.GetProcessMaximumSpeed(expectedProcesses[0], 1, biome).WritableInputs.Clear();
        cache.GetActiveProcessList(species).Clear();
        AssertThat(EnergyValues(energy.ToMutableCopy()).SequenceEqual(expectedEnergy)).IsTrue();
        AssertProcessEqual(expectedSpeed, speed.ToMutableCopy());
        AssertThat(active.Count).IsEqual(expectedProcesses.Count);
        for (var i = 0; i < active.Count; ++i)
        {
            AssertThat(ReferenceEquals(active[i].Process, expectedProcesses[i].Process)).IsTrue();
            AssertThat(active[i].Rate).IsEqual(expectedProcesses[i].Rate);
            AssertThat(active[i].SpeedMultiplier).IsEqual(expectedProcesses[i].SpeedMultiplier);
        }

        AssertThat(speed.Inputs.GetEnumerator().GetType().IsValueType).IsTrue();
        var inputs = new List<KeyValuePair<Compound, float>>();
        foreach (var input in speed.Inputs)
            inputs.Add(input);

        AssertThat(inputs.SequenceEqual(expectedSpeed.WritableInputs)).IsTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WarmViewsAndEnumerationAllocateNothing(bool multicellular)
    {
        var species = CreateSpecies(multicellular);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var process = cache.GetActiveProcessListView(species)[0];
        Measure("ViewsAndEnumeration", multicellular, () =>
        {
            var total = cache.GetEnergyBalanceForSpeciesView(species, biome).TotalProduction;
            var speed = cache.GetProcessMaximumSpeedView(process, 1, biome);
            total += speed.CurrentSpeed + speed.ATPConsumption + speed.ATPProduction;
            foreach (var input in speed.Inputs)
                total += input.Value;

            foreach (var active in cache.GetActiveProcessListView(species))
                total += active.Rate + active.SpeedMultiplier;

            return total;
        });
    }

    [TestCase]
    public void EnergyCompatibilityCopyIncludesAllFields()
    {
        var builder = new EnergyBalanceInfoSimple
        {
            BaseMovement = 1,
            Flagella = 2,
            Actomyosin = 3,
            Cilia = 4,
            TotalMovement = 5,
            Osmoregulation = 6,
            TotalProduction = 7,
            TotalConsumption = 8,
            TotalConsumptionStationary = 9,
            FinalBalance = 10,
            FinalBalanceStationary = 11,
        };
        var view = new EnergyBalanceView(builder);
        var copy = view.ToMutableCopy();
        AssertThat(EnergyValues(copy).SequenceEqual(EnergyValues(builder))).IsTrue();
        copy.Clear();
        AssertThat(EnergyValues(view.ToMutableCopy()).SequenceEqual(EnergyValues(builder))).IsTrue();
    }

    [TestCase]
    public void ProcessCompatibilityCopyIncludesAllState()
    {
        var process = CreateCache().GetActiveProcessListView(CreateSpecies(false))[0].Process;
        var builder = new ProcessSpeedInformation(process)
        {
            CurrentSpeed = 2.5f,
            Efficiency = 3.5f,
            ATPProduction = 4.5f,
            ATPConsumption = 5.5f,
        };
        var dictionaries = ProcessDictionaries(builder);
        for (var i = 0; i < dictionaries.Length; ++i)
        {
            dictionaries[i].Add(Compound.Glucose, i + 0.5f);
            dictionaries[i].Add(Compound.ATP, i + 1.5f);
        }

        builder.WritableLimitingCompounds.AddRange([Compound.Glucose, Compound.ATP]);
        var view = new ProcessSpeedView(builder);
        var copy = view.ToMutableCopy();
        AssertProcessEqual(builder, copy);
        copy.ScaleSpeed(0, null);
        foreach (var dictionary in ProcessDictionaries(copy))
            dictionary.Clear();

        copy.WritableLimitingCompounds.Clear();
        AssertProcessEqual(builder, view.ToMutableCopy());
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

    [TestCase]
    public void PublicCompatibilityAllocationIsSeparateFromWarmViews()
    {
        var species = CreateSpecies(false);
        var cache = CreateCache();
        var biome = CreatePatch().Biome;
        var process = cache.GetActiveProcessListView(species)[0];
        var energyBytes = MeasureAllocations(() => cache.GetEnergyBalanceForSpecies(species, biome).TotalProduction);
        var speedBytes = MeasureAllocations(() => cache.GetProcessMaximumSpeed(process, 1, biome).CurrentSpeed);
        var activeBytes = MeasureAllocations(() => cache.GetActiveProcessList(species).Count);
        GD.Print($"CACHE_COPY iterations=100000 energyBytes={energyBytes} speedBytes={speedBytes} " +
            $"activeBytes={activeBytes}");
        AssertThat(energyBytes > 0).IsTrue();
        AssertThat(speedBytes > 0).IsTrue();
        AssertThat(activeBytes > 0).IsTrue();
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

    private static void AssertProcessEqual(ProcessSpeedInformation expected, ProcessSpeedInformation actual)
    {
        AssertThat(ReferenceEquals(expected.Process, actual.Process)).IsTrue();
        AssertThat(actual.CurrentSpeed).IsEqual(expected.CurrentSpeed);
        AssertThat(actual.Efficiency).IsEqual(expected.Efficiency);
        AssertThat(actual.ATPProduction).IsEqual(expected.ATPProduction);
        AssertThat(actual.ATPConsumption).IsEqual(expected.ATPConsumption);
        var expectedDictionaries = ProcessDictionaries(expected);
        var actualDictionaries = ProcessDictionaries(actual);
        for (var i = 0; i < expectedDictionaries.Length; ++i)
            AssertThat(actualDictionaries[i].SequenceEqual(expectedDictionaries[i])).IsTrue();

        AssertThat(actual.WritableLimitingCompounds.SequenceEqual(expected.WritableLimitingCompounds)).IsTrue();
    }

    private static Dictionary<Compound, float>[] ProcessDictionaries(ProcessSpeedInformation value)
    {
        return
        [
            value.WritableInputs, value.WritableOutputs, value.WritableFullSpeedRequiredEnvironmentalInputs,
            value.AvailableAmounts, value.AvailableRates,
        ];
    }

    private static float[] EnergyValues(EnergyBalanceInfoSimple value)
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
