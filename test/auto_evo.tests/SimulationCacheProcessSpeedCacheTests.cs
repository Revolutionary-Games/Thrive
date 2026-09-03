using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using Systems;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCacheProcessSpeedCacheTests
{
    [TestCase]
    public void RateAndModifierAreDistinctCacheIdentityInBothInsertionOrders()
    {
        var process = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var biome = SimulationParameters.Instance.GetBiome("default").Conditions;
        var first = new ProcessRequest(new TweakedProcess(process, 4.0f), 1.0f, biome);
        var second = new ProcessRequest(new TweakedProcess(process, 5.0f), 0.8f, biome);

        AssertThat(BitConverter.SingleToInt32Bits(first.Process.Rate * first.SpeedModifier))
            .IsEqual(BitConverter.SingleToInt32Bits(second.Process.Rate * second.SpeedModifier));

        AssertIndependentCacheEntriesInBothInsertionOrders(first, second);
    }

    [TestCase]
    public void OverlappingBiomeHashBitsDoNotAliasCacheEntriesInBothInsertionOrders()
    {
        var process = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var biome = SimulationParameters.Instance.GetBiome("default").Conditions;
        var firstBiome = new HashControlledBiomeConditions(biome, 0x00000000);
        var secondBiome = new HashControlledBiomeConditions(biome, 0x00007f80);
        var first = new ProcessRequest(new TweakedProcess(process, 1.0f), 1.0f, firstBiome);
        var second = new ProcessRequest(new TweakedProcess(process, 2.0f), 1.0f, secondBiome);

        AssertIndependentCacheEntriesInBothInsertionOrders(first, second);
    }

    [TestCase]
    public void BiomeIdentitySeparatesEqualHashCodesInBothInsertionOrders()
    {
        var process = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var firstBiome = new HashControlledBiomeConditions(
            SimulationParameters.Instance.GetBiome("default").Conditions, 12345);
        var secondBiome = new HashControlledBiomeConditions(
            SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions, 12345);
        var first = new ProcessRequest(new TweakedProcess(process), 1.0f, firstBiome);
        var second = new ProcessRequest(new TweakedProcess(process), 1.0f, secondBiome);

        AssertIndependentCacheEntriesInBothInsertionOrders(first, second);
    }

    [TestCase]
    public void ProcessIdentitySeparatesEqualProcessIdsInBothInsertionOrders()
    {
        var source = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var biome = SimulationParameters.Instance.GetBiome("default").Conditions;
        var first = new ProcessRequest(new TweakedProcess(CloneProcess(source, source.ProcessId)), 1.0f, biome);
        var second = new ProcessRequest(new TweakedProcess(CloneProcess(source, source.ProcessId)), 1.0f, biome);

        AssertIndependentCacheEntriesInBothInsertionOrders(first, second);
    }

    private static void AssertIndependentCacheEntriesInBothInsertionOrders(ProcessRequest first,
        ProcessRequest second)
    {
        AssertIndependentCacheEntries(first, second);
        AssertIndependentCacheEntries(second, first);
    }

    private static void AssertIndependentCacheEntries(ProcessRequest first, ProcessRequest second)
    {
        var firstOracle = CalculateDirect(first);
        var secondOracle = CalculateDirect(second);

        AssertThat(ObservedResultsDiffer(firstOracle, secondOracle)).IsTrue();

        var cache = CreateCache();
        var firstCached = cache.GetProcessMaximumSpeed(first.Process, first.SpeedModifier, first.BiomeConditions);
        var secondCached = cache.GetProcessMaximumSpeed(second.Process, second.SpeedModifier, second.BiomeConditions);

        AssertMatchesDirectCalculation(firstCached, firstOracle);
        AssertMatchesDirectCalculation(secondCached, secondOracle);
        AssertThat(ReferenceEquals(firstCached, secondCached)).IsFalse();
        AssertThat(cache.GetProcessMaximumSpeed(first.Process, first.SpeedModifier, first.BiomeConditions))
            .IsSame(firstCached);
        AssertThat(cache.GetProcessMaximumSpeed(second.Process, second.SpeedModifier, second.BiomeConditions))
            .IsSame(secondCached);
    }

    private static ProcessSpeedInformation CalculateDirect(ProcessRequest request)
    {
        return ProcessSystem.CalculateProcessMaximumSpeed(request.Process, request.SpeedModifier,
            request.BiomeConditions, CompoundAmountType.Average, true);
    }

    private static void AssertMatchesDirectCalculation(ProcessSpeedInformation actual,
        ProcessSpeedInformation expected)
    {
        AssertThat(actual.Process).IsSame(expected.Process);
        AssertFloatBitsEqual(actual.CurrentSpeed, expected.CurrentSpeed);
        AssertFloatBitsEqual(actual.AvailableRates[Compound.Sunlight], expected.AvailableRates[Compound.Sunlight]);
        AssertFloatBitsEqual(actual.WritableOutputs[Compound.Glucose], expected.WritableOutputs[Compound.Glucose]);
    }

    private static bool ObservedResultsDiffer(ProcessSpeedInformation first, ProcessSpeedInformation second)
    {
        return !ReferenceEquals(first.Process, second.Process) ||
            BitConverter.SingleToInt32Bits(first.CurrentSpeed) !=
            BitConverter.SingleToInt32Bits(second.CurrentSpeed) ||
            BitConverter.SingleToInt32Bits(first.AvailableRates[Compound.Sunlight]) !=
            BitConverter.SingleToInt32Bits(second.AvailableRates[Compound.Sunlight]) ||
            BitConverter.SingleToInt32Bits(first.WritableOutputs[Compound.Glucose]) !=
            BitConverter.SingleToInt32Bits(second.WritableOutputs[Compound.Glucose]);
    }

    private static void AssertFloatBitsEqual(float actual, float expected)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(BitConverter.SingleToInt32Bits(expected));
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
    }

    private static BioProcess CloneProcess(BioProcess source, ushort processId)
    {
        var result = new BioProcess
        {
            Name = source.Name,
            ProcessId = processId,
            IsMetabolismProcess = source.IsMetabolismProcess,
        };

        foreach (var input in source.Inputs)
            result.Inputs.Add(input.Key, input.Value);

        foreach (var output in source.Outputs)
            result.Outputs.Add(output.Key, output.Value);

        return result;
    }

    private sealed class HashControlledBiomeConditions : IBiomeConditions
    {
        private readonly IBiomeConditions inner;
        private readonly int hashCode;

        public HashControlledBiomeConditions(IBiomeConditions inner, int hashCode)
        {
            this.inner = inner;
            this.hashCode = hashCode;
        }

        public Dictionary<string, ChunkConfiguration> Chunks => inner.Chunks;
        public float Pressure => inner.Pressure;

        public BiomeCompoundProperties GetCompound(Compound compound, CompoundAmountType amountType)
        {
            return inner.GetCompound(compound, amountType);
        }

        public bool TryGetCompound(Compound compound, CompoundAmountType amountType,
            out BiomeCompoundProperties result)
        {
            return inner.TryGetCompound(compound, amountType, out result);
        }

        public IEnumerable<Compound> GetAmbientCompoundsThatVary()
        {
            return inner.GetAmbientCompoundsThatVary();
        }

        public bool HasCompoundsThatVary()
        {
            return inner.HasCompoundsThatVary();
        }

        public bool IsVaryingCompound(Compound compound)
        {
            return inner.IsVaryingCompound(compound);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }

    private sealed class ProcessRequest
    {
        public readonly TweakedProcess Process;
        public readonly float SpeedModifier;
        public readonly IBiomeConditions BiomeConditions;

        public ProcessRequest(TweakedProcess process, float speedModifier, IBiomeConditions biomeConditions)
        {
            Process = process;
            SpeedModifier = speedModifier;
            BiomeConditions = biomeConditions;
        }
    }
}
