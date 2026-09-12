using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using GdUnit4;
using Systems;
using Test.Utils;
using static GdUnit4.Assertions;

/// <summary>
///   Checks that estimated throughput agrees with actual process execution and compound accounting.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ProcessSpeedTests
{
    [TestCase("glycolysis")]
    [TestCase("respiration")]
    [TestCase("photosynthesis")]
    public void MaximumSpeed_AgreesWithRuntimeAndStoichiometry(string processName)
    {
        var process = SimulationParameters.Instance.GetBioProcess(processName);
        foreach (var ambientFactor in new[] { 0.5f, 1.0f, 1.5f })
        {
            var biome = CreateBiome(process, ambientFactor);
            var environmentModifier =
                MathF.Pow(ambientFactor, process.Inputs.Count(input => input.Key.IsEnvironmental));
            foreach (var (rate, modifier, delta) in new[]
                     {
                         (1.0f, 1.0f, 1.0f), (2, 1, 0.5f), (1, 2, 0.5f), (1, 0.64f, 1), (5, 0.8f, 0.25f),
                     })
            {
                var expected = environmentModifier * (rate * modifier);
                var predicted = ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(process, rate),
                    modifier, biome, CompoundAmountType.Current, false);
                var actual = Run(process, biome, rate, modifier, delta);

                AssertClose(predicted.CurrentSpeed, expected);
                AssertClose(actual.Speed, expected);
                foreach (var input in process.Inputs.Where(input => !input.Key.IsEnvironmental))
                    AssertClose(predicted.WritableInputs[input.Key.ID], input.Value * expected);

                foreach (var output in process.Outputs)
                    AssertClose(predicted.WritableOutputs[output.Key.ID], output.Value * expected);

                foreach (var compound in process.Inputs.Keys.Concat(process.Outputs.Keys)
                             .Where(compound => !compound.IsEnvironmental).Distinct())
                {
                    var netCoefficient = process.Outputs.GetValueOrDefault(compound) -
                        process.Inputs.GetValueOrDefault(compound);
                    AssertClose(actual.After.GetValueOrDefault(compound.ID) -
                        actual.Before.GetValueOrDefault(compound.ID), netCoefficient * expected * delta);
                }
            }
        }
    }

    [TestCase("glycolysis")]
    [TestCase("respiration")]
    [TestCase("photosynthesis")]
    public void MaximumSpeed_EqualRateProductsHaveIdenticalResults(string processName)
    {
        var process = SimulationParameters.Instance.GetBioProcess(processName);
        var biome = CreateBiome(process, 0.7f);
        var first = ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(process, 4), 1,
            biome, CompoundAmountType.Current, false);
        var second = ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(process, 5), 0.8f,
            biome, CompoundAmountType.Current, false);

        AssertThat(first.CurrentSpeed).IsEqual(second.CurrentSpeed);
        AssertThat(first.ATPProduction).IsEqual(second.ATPProduction);
        AssertThat(first.ATPConsumption).IsEqual(second.ATPConsumption);
        AssertThat(first.Efficiency).IsEqual(second.Efficiency);
        AssertThat(first.WritableInputs).IsEqual(second.WritableInputs);
        AssertThat(first.WritableOutputs).IsEqual(second.WritableOutputs);
        AssertThat(first.AvailableAmounts).IsEqual(second.AvailableAmounts);
        AssertThat(first.AvailableRates).IsEqual(second.AvailableRates);
        AssertThat(first.FullSpeedRequiredEnvironmentalInputs).IsEqual(second.FullSpeedRequiredEnvironmentalInputs);
        AssertThat(first.LimitingCompounds).IsEqual(second.LimitingCompounds);
    }

    [TestCase("glycolysis")]
    [TestCase("respiration")]
    [TestCase("photosynthesis")]
    [TestCase("cytotoxinSynthesis")]
    public void ScaleSpeed_KeepsFlowsAndDerivedFieldsConsistent(string processName)
    {
        var process = SimulationParameters.Instance.GetBioProcess(processName);
        var biome = CreateBiome(process);
        var info = ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(process, 2), 0.8f,
            biome, CompoundAmountType.Current, false);
        var available = new Dictionary<Compound, float>(info.AvailableAmounts);
        var rates = new Dictionary<Compound, float>(info.AvailableRates);
        var workMemory = new Dictionary<Compound, float>();

        foreach (var modifier in new[] { 0.5f, 0.0f })
        {
            info.ScaleSpeed(modifier, workMemory);
            var expectedSpeed = modifier == 0 ? 0 : 0.8f;
            AssertClose(info.CurrentSpeed, expectedSpeed);
            AssertClose(info.ATPProduction,
                process.Outputs.GetValueOrDefault(SimulationParameters.GetCompound(Compound.ATP)) * expectedSpeed);
            AssertClose(info.ATPConsumption,
                process.Inputs.GetValueOrDefault(SimulationParameters.GetCompound(Compound.ATP)) * expectedSpeed);

            foreach (var input in process.Inputs)
            {
                if (input.Key.IsEnvironmental)
                {
                    AssertThat(info.FullSpeedRequiredEnvironmentalInputs.ContainsKey(input.Key.ID)).IsTrue();
                    AssertThat(info.FullSpeedRequiredEnvironmentalInputs[input.Key.ID]).IsEqual(input.Value);
                }
                else
                {
                    AssertClose(info.WritableInputs[input.Key.ID], input.Value * expectedSpeed);
                }
            }

            foreach (var output in process.Outputs)
                AssertClose(info.WritableOutputs[output.Key.ID], output.Value * expectedSpeed);

            AssertThat(info.AvailableAmounts).IsEqual(available);
            AssertThat(info.AvailableRates).IsEqual(rates);
        }
    }

    [TestCase]
    public void Runtime_AccumulatesInventoryAndCapacityLimits()
    {
        var process = SimulationParameters.Instance.GetBioProcess("glycolysis");
        var actual = Run(process, CreateBiome(process), 1, 1, 1, 0.003f, 0.5f);

        AssertClose(actual.Speed, 0.25f);
        AssertClose(actual.After[Compound.Glucose], 0.0015f);
        AssertClose(actual.After[Compound.ATP], 0.5f);
    }

    [TestCase]
    public void Runtime_StopsAtOrBelowCumulativeMinimumFraction()
    {
        var process = SimulationParameters.Instance.GetBioProcess("glycolysis");
        var minimum = Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION;
        foreach (var fraction in new[] { minimum * 0.75f, minimum, float.BitIncrement(minimum) })
        {
            var actual = Run(process, CreateBiome(process), 1, 1, 1, 0.003f, fraction * 2);
            if (fraction <= minimum)
            {
                AssertThat(actual.Speed).IsEqual(0.0f);
                AssertThat(actual.After.GetValueOrDefault(Compound.Glucose)).IsEqual(0.003f);
                AssertThat(actual.After.GetValueOrDefault(Compound.ATP)).IsEqual(0.0f);
            }
            else
            {
                AssertThat(actual.Speed).IsEqual(fraction);
                AssertThat(actual.After[Compound.Glucose]).IsLess(0.003f);
                AssertThat(actual.After[Compound.ATP]).IsGreater(0.0f);
            }
        }
    }

    [TestCase("glycolysis")]
    [TestCase("photosynthesis")]
    public void Runtime_PreservesManualAndToxinControls(string processName)
    {
        var process = SimulationParameters.Instance.GetBioProcess(processName);
        foreach (var (manual, toxin) in new[] { (0.0f, 0.0f), (0.5f, 0), (2, 0), (1, 0.5f), (1, -1) })
        {
            var actual = Run(process, CreateBiome(process), 1, 1, 1, manual: manual, toxin: toxin);
            float toxinModifier = 1;
            if (process.Outputs.ContainsKey(SimulationParameters.GetCompound(Compound.ATP)))
            {
                if (toxin < 0)
                {
                    toxinModifier = 0;
                }
                else if (toxin > 0)
                {
                    toxinModifier = toxin;
                }
            }

            AssertClose(actual.Speed, MathF.Min(manual, 1) * toxinModifier);
        }
    }

    [TestCase("respiration")]
    [TestCase("photosynthesis")]
    public void WorldEffect_UsesEffectiveSpeedAndIndependentBalance(string processName)
    {
        var process = SimulationParameters.Instance.GetBioProcess(processName);
        var actual = ProcessSystem.CalculateEffectiveProcessSpeedForEffect(new TweakedProcess(process, 2),
            0.5f, CreateBiome(process), 0.8f, 1.05f);
        var expected = 2 * (0.8f * 1.05f) * (process.IsMetabolismProcess ? 0.5f : 1);
        AssertClose(actual, expected);
    }

    [TestCase]
    public void MaximumSpeed_DistinguishesBiomePresenceFromStoredInputs()
    {
        var process = SimulationParameters.Instance.GetBioProcess("glycolysis");
        var biome = CreateBiome(process);
        biome.ChangeableCompounds.Remove(Compound.Glucose);
        var tweaked = new TweakedProcess(process);
        AssertThat(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 1, biome, CompoundAmountType.Current, true)
            .CurrentSpeed).IsEqual(0.0f);
        AssertThat(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 1, biome, CompoundAmountType.Current, false)
            .CurrentSpeed).IsEqual(1.0f);
        AssertThat(Run(process, biome, 1, 1, 1).Speed).IsEqual(1.0f);

        biome.Chunks["food"] = new ChunkConfiguration
        {
            Density = 1,
            Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>
            {
                [Compound.Glucose] = new() { Amount = 1 },
            },
        };
        AssertThat(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 1, biome, CompoundAmountType.Current, true)
            .CurrentSpeed).IsEqual(1.0f);

        var toxin = SimulationParameters.Instance.GetBioProcess("cytotoxinSynthesis");
        var empty = new BiomeConditions(new Dictionary<Compound, BiomeCompoundProperties>(), null, null, null, null)
        {
            Chunks = new Dictionary<string, ChunkConfiguration>(),
        };
        AssertThat(ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(toxin), 1,
            empty, CompoundAmountType.Current, true).CurrentSpeed).IsEqual(1.0f);
    }

    [TestCase]
    public void MaximumSpeed_SelectsRequestedTimeWhileRuntimeUsesCurrent()
    {
        var process = SimulationParameters.Instance.GetBioProcess("photosynthesis");
        var biome = CreateBiome(process);
        biome.CurrentCompoundAmounts[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0.5f };
        biome.AverageCompounds[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0.25f };
        var tweaked = new TweakedProcess(process, 2);
        AssertClose(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 0.8f, biome,
            CompoundAmountType.Current, false).CurrentSpeed, 0.8f);
        AssertClose(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 0.8f, biome,
            CompoundAmountType.Average, false).CurrentSpeed, 0.4f);
        AssertClose(Run(process, biome, 2, 0.8f, 1).Speed, 0.8f);

        biome.CurrentCompoundAmounts[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0 };
        AssertThat(ProcessSystem.CalculateProcessMaximumSpeed(tweaked, 0.8f, biome,
            CompoundAmountType.Current, false).CurrentSpeed).IsEqual(0.0f);
        AssertThat(Run(process, biome, 2, 0.8f, 1).Speed).IsEqual(0.0f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Runtime_MultipleConstraintsPreserveStoichiometryInEitherOrder(bool reverse)
    {
        var inputs = new[] { Compound.Glucose, Compound.Ammonia };
        var outputs = new[] { Compound.ATP, Compound.Oxytoxy };
        if (reverse)
        {
            Array.Reverse(inputs);
            Array.Reverse(outputs);
        }

        var process = CreateSyntheticProcess(inputs, outputs);
        var bag = new CompoundBag(0);
        bag.Compounds[Compound.Glucose] = 0.5f;
        bag.Compounds[Compound.Ammonia] = 0.25f;
        bag.AddSpecificCapacityForCompound(Compound.ATP, 0.125f);
        bag.AddSpecificCapacityForCompound(Compound.Oxytoxy, 0.0625f);
        var tweaked = new TweakedProcess(process);
        var statistics = RunProcesses(new List<TweakedProcess> { tweaked }, bag, CreateBiome(process), 1, 1);

        AssertThat(statistics.Processes[tweaked].CurrentSpeed).IsEqual(0.0625f);
        AssertThat(bag.GetCompoundAmount(Compound.Glucose)).IsEqual(0.4375f);
        AssertThat(bag.GetCompoundAmount(Compound.Ammonia)).IsEqual(0.1875f);
        AssertThat(bag.GetCompoundAmount(Compound.ATP)).IsEqual(0.0625f);
        AssertThat(bag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(0.0625f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Runtime_CumulativeMinimumAppliesAcrossInputsOrOutputs(bool limitOutputs)
    {
        var process = CreateSyntheticProcess(new[] { Compound.Glucose, Compound.Ammonia },
            new[] { Compound.ATP, Compound.Oxytoxy });
        var minimum = Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION;
        foreach (var fraction in new[] { minimum * 0.75f, minimum, float.BitIncrement(minimum) })
        {
            var bag = new CompoundBag(0);
            if (limitOutputs)
            {
                bag.Compounds[Compound.Glucose] = 1;
                bag.Compounds[Compound.Ammonia] = 1;
                bag.AddSpecificCapacityForCompound(Compound.ATP, 0.5f);
                bag.AddSpecificCapacityForCompound(Compound.Oxytoxy, fraction);
            }
            else
            {
                bag.Compounds[Compound.Glucose] = 0.5f;
                bag.Compounds[Compound.Ammonia] = fraction;
                bag.AddSpecificCapacityForCompound(Compound.ATP, 10);
                bag.AddSpecificCapacityForCompound(Compound.Oxytoxy, 10);
            }

            var before = new Dictionary<Compound, float>(bag.Compounds);
            var tweaked = new TweakedProcess(process);
            var statistics = RunProcesses(new List<TweakedProcess> { tweaked }, bag, CreateBiome(process), 1, 1);
            if (fraction <= minimum)
            {
                AssertThat(statistics.Processes[tweaked].CurrentSpeed).IsEqual(0.0f);
                AssertThat(bag.GetCompoundAmount(Compound.Glucose)).IsEqual(before[Compound.Glucose]);
                AssertThat(bag.GetCompoundAmount(Compound.Ammonia)).IsEqual(before[Compound.Ammonia]);
                AssertThat(bag.GetCompoundAmount(Compound.ATP)).IsEqual(0.0f);
                AssertThat(bag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(0.0f);
            }
            else
            {
                AssertThat(statistics.Processes[tweaked].CurrentSpeed).IsEqual(fraction);
                AssertThat(bag.GetCompoundAmount(Compound.ATP)).IsEqual(fraction);
                AssertThat(bag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(fraction);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Runtime_SharedInventoryIsConsumedInProcessOrder(bool reverse)
    {
        var first = new TweakedProcess(SimulationParameters.Instance.GetBioProcess("glycolysis"));
        var second = new TweakedProcess(SimulationParameters.Instance.GetBioProcess("glycolysis_cytoplasm"));
        if (reverse)
            (first, second) = (second, first);

        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var bag = new CompoundBag(10);
        bag.Compounds[Compound.Glucose] = first.Process.Inputs[glucose];
        var statistics =
            RunProcesses(new List<TweakedProcess> { first, second }, bag, CreateBiome(first.Process), 1, 1);
        AssertThat(statistics.Processes[first].CurrentSpeed).IsEqual(1.0f);
        AssertThat(statistics.Processes[second].CurrentSpeed).IsEqual(0.0f);
        AssertThat(bag.GetCompoundAmount(Compound.Glucose)).IsEqual(0.0f);
        AssertThat(bag.GetCompoundAmount(Compound.ATP)).IsEqual(first.Process.Outputs[atp]);
    }

    /// <summary>
    ///   Synthetic stoichiometry exercises multiple ordinary inputs/outputs through the real ECS system.
    /// </summary>
    private static BioProcess CreateSyntheticProcess(IEnumerable<Compound> inputs, IEnumerable<Compound> outputs)
    {
        var process = new BioProcess { Name = "Multiple constraints", InternalName = "testMultipleConstraints" };
        foreach (var input in inputs)
            process.Inputs.Add(SimulationParameters.GetCompound(input), 1);

        foreach (var output in outputs)
            process.Outputs.Add(SimulationParameters.GetCompound(output), 1);

        return process;
    }

    private static BiomeConditions CreateBiome(BioProcess process, float ambientFactor = 1)
    {
        var compounds = new Dictionary<Compound, BiomeCompoundProperties>();
        foreach (var compound in SimulationParameters.Instance.GetAllCompounds().Values)
        {
            compounds[compound.ID] = new BiomeCompoundProperties
            {
                Ambient = compound.ID == Compound.Temperature ? 15 : 0,
                Amount = 1000,
                Density = 1,
            };
        }

        foreach (var input in process.Inputs.Where(input => input.Key.IsEnvironmental))
        {
            compounds[input.Key.ID] = new BiomeCompoundProperties
            {
                Ambient = input.Value * ambientFactor,
                Amount = 1000,
                Density = 1,
            };
        }

        return new BiomeConditions(compounds, null, null, null, null)
        {
            Chunks = new Dictionary<string, ChunkConfiguration>(),
        };
    }

    private static RunResult Run(BioProcess process, BiomeConditions biome, float rate, float modifier, float delta,
        float? glucose = null, float? atpCapacity = null, float manual = 1, float toxin = 0)
    {
        var bag = new CompoundBag(0);
        foreach (var compound in SimulationParameters.Instance.GetAllCompounds().Values)
        {
            bag.AddSpecificCapacityForCompound(compound.ID,
                compound.ID == Compound.ATP && atpCapacity.HasValue ? atpCapacity.Value : 1000000);
        }

        foreach (var input in process.Inputs.Where(input => !input.Key.IsEnvironmental))
            bag.Compounds[input.Key.ID] = input.Value * 100;

        if (glucose.HasValue)
            bag.Compounds[Compound.Glucose] = glucose.Value;

        var before = new Dictionary<Compound, float>(bag.Compounds);
        var tweaked = new TweakedProcess(process, rate) { SpeedMultiplier = manual };
        var statistics = RunProcesses(new List<TweakedProcess> { tweaked }, bag, biome, modifier, delta, toxin);
        var speed = statistics.Processes.TryGetValue(tweaked, out var processStatistics) ?
            processStatistics.CurrentSpeed :
            0;
        return new RunResult(before, new Dictionary<Compound, float>(bag.Compounds), speed);
    }

    private static ProcessStatistics RunProcesses(List<TweakedProcess> processes, CompoundBag bag,
        BiomeConditions biome, float modifier, float delta, float toxin = 0)
    {
        using var world = new TestWorldSimulation();
        var statistics = new ProcessStatistics();
        world.EntitySystem.Create(new CompoundStorage { Compounds = bag },
            new BioProcesses
            {
                ActiveProcesses = processes,
                ProcessStatistics = statistics,
                ATPProductionSpeedModifier = toxin,
            },
            new MicrobeEnvironmentalEffects { ProcessSpeedModifier = modifier });
        var system = new ProcessSystem(world.EntitySystem);
        system.SetBiome(biome);
        system.BeforeUpdate(delta);
        system.Update(delta);
        system.AfterUpdate(delta);
        return statistics;
    }

    private static void AssertClose(float actual, float expected)
    {
        AssertThat(float.IsFinite(actual)).IsTrue();
        AssertThat(MathF.Abs(actual - expected) <= 1e-7f + MathF.Abs(expected) * 1e-5f).IsTrue();
    }

    private sealed record RunResult(Dictionary<Compound, float> Before, Dictionary<Compound, float> After, float Speed);
}
