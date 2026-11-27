using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using GdUnit4;
using SharedBase.Archive;
using static GdUnit4.Assertions;

/// <summary>
///   Tests complete generation application with owned population inputs and synchronous calculation.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AutoEvoGenerationApplicationTests
{
    /// <summary>
    ///   Verifies that a completed run advances time before applying controlled populations and recording history,
    ///   protecting the shared generation application order and the valid strict path.
    /// </summary>
    [TestCase]
    public void ApplyGeneration_CommitsTimePopulationGenerationAndHistoryInOrder()
    {
        var world = CreateWorld();
        var species = world.PlayerSpecies;
        var patch = world.Map.CurrentPatch!;
        var initialPopulation = patch.GetSpeciesSimulationPopulation(species);
        var initialGeneration = species.Generation;
        var initialHistory = world.GenerationHistory.Count;
        var expectedTime = world.CalculateNextTimeStep(1);
        bool observedBeforeApplication = false;
        world.TimedEffects.RegisterEffect("observe", new CallbackEffect(() =>
        {
            AssertThat(world.TotalPassedTime).IsEqual(expectedTime);
            AssertThat(patch.GetSpeciesSimulationPopulation(species)).IsEqual(initialPopulation);
            AssertThat(species.Generation).IsEqual(initialGeneration);
            AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory);
            observedBeforeApplication = true;
        }));
        var run = Complete(world, results => results.AddPopulationResultForSpecies(species, patch, 321));
        run.ExternalEffects.Add(new ExternalEffect(species, 7, 1, "test", patch));
        run.CalculateAndApplyFinalExternalEffectSizes(true);
        run.ApplyGeneration(true, true);

        AssertThat(observedBeforeApplication).IsTrue();
        AssertThat(patch.GetSpeciesSimulationPopulation(species)).IsEqual(328L);
        AssertThat(world.TotalPassedTime).IsEqual(expectedTime);
        AssertThat(species.Generation).IsEqual(initialGeneration + 1);
        AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory + 1);
        var totalPopulation = world.Map.Patches.Values.Sum(p => p.GetSpeciesSimulationPopulation(species));
        AssertThat(species.Population).IsEqual(totalPopulation);
        AssertThat(world.GenerationHistory[initialGeneration].AllSpeciesData[species.ID].Population)
            .IsEqual(totalPopulation);
    }

    /// <summary>
    ///   Verifies that unfinished and aborted runs cannot commit a strict generation,
    ///   protecting world time, generation and history from invalid application.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ApplyGeneration_RejectsUnfinishedOrAbortedRunBeforeChangingWorld(bool abort)
    {
        var world = CreateWorld();
        var originalTime = world.TotalPassedTime;
        var originalGeneration = world.PlayerSpecies.Generation;
        var originalHistory = world.GenerationHistory.Count;
        var run = new ControlledRun(world, _ => { });
        if (abort)
        {
            run.Abort();
            run.OneStep();
            AssertThat(run.Finished).IsTrue();
            AssertThat(run.WasSuccessful).IsFalse();
        }

        AssertThrown(() => run.ApplyGeneration(true, true)).IsInstanceOf<InvalidOperationException>();
        AssertThat(world.TotalPassedTime).IsEqual(originalTime);
        AssertThat(world.PlayerSpecies.Generation).IsEqual(originalGeneration);
        AssertThat(world.GenerationHistory.Count).IsEqual(originalHistory);
    }

    /// <summary>
    ///   Verifies that a missing population entry stops strict application but retains the default best-effort policy,
    ///   protecting error propagation without changing interactive callers.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void MissingPopulationSpecies_UsesRequestedErrorPolicy(bool strict)
    {
        var world = CreateWorld();
        var missingSpecies = world.NewMicrobeSpecies("Missing", "species");
        var patch = world.Map.CurrentPatch!;
        var initialGeneration = world.PlayerSpecies.Generation;
        var initialHistory = world.GenerationHistory.Count;
        var expectedTime = world.CalculateNextTimeStep(1);
        var run = Complete(world, results => results.AddPopulationResultForSpecies(missingSpecies, patch, 100));
        if (strict)
        {
            AssertThrown(() => run.ApplyGeneration(true, true)).IsInstanceOf<InvalidOperationException>();
            AssertThat(world.PlayerSpecies.Generation).IsEqual(initialGeneration);
            AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory);
        }
        else
        {
            run.ApplyGeneration(true);
            AssertThat(world.PlayerSpecies.Generation).IsEqual(initialGeneration + 1);
            AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory + 1);
        }

        AssertThat(world.TotalPassedTime).IsEqual(expectedTime);
        AssertThat(patch.GetSpeciesSimulationPopulation(missingSpecies)).IsEqual(0L);
    }

    /// <summary>
    ///   Verifies that a failing world effect stops strict application before populations and history change,
    ///   while preserving the existing default policy in each build configuration.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void WorldEffectFailure_UsesRequestedPolicyBeforePopulationAndHistory(bool strict)
    {
        var world = CreateWorld();
        var patch = world.Map.CurrentPatch!;
        var species = world.PlayerSpecies;
        var initialPopulation = patch.GetSpeciesSimulationPopulation(species);
        var initialGeneration = species.Generation;
        var initialHistory = world.GenerationHistory.Count;
        var expectedTime = world.CalculateNextTimeStep(1);
        bool laterEffectRan = false;
        world.TimedEffects.RegisterEffect("failure", new CallbackEffect(() =>
            throw new InvalidOperationException("controlled effect failure")));
        world.TimedEffects.RegisterEffect("later", new CallbackEffect(() => laterEffectRan = true));
        var run = Complete(world, results => results.AddPopulationResultForSpecies(species, patch, 321));

        bool expectFailure = strict;
#if DEBUG
        expectFailure = true;
#endif
        if (expectFailure)
        {
            AssertThrown(() => run.ApplyGeneration(true, strict)).IsInstanceOf<InvalidOperationException>();
            AssertThat(laterEffectRan).IsFalse();
            AssertThat(patch.GetSpeciesSimulationPopulation(species)).IsEqual(initialPopulation);
            AssertThat(species.Generation).IsEqual(initialGeneration);
            AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory);
        }
        else
        {
            run.ApplyGeneration(true);
            AssertThat(laterEffectRan).IsTrue();
            AssertThat(patch.GetSpeciesSimulationPopulation(species)).IsEqual(321L);
            AssertThat(species.Generation).IsEqual(initialGeneration + 1);
            AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory + 1);
        }

        AssertThat(world.TotalPassedTime).IsEqual(expectedTime);
    }

    /// <summary>
    ///   Verifies that overflowing external effects throw without changing results in strict mode,
    ///   while the default path retains its clamped population adjustment.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ExternalEffectOverflow_StrictThrowsAndDefaultClamps(bool strict)
    {
        var world = CreateWorld();
        var patch = world.Map.CurrentPatch!;
        var species = world.PlayerSpecies;
        var run = Complete(world, results => results.AddPopulationResultForSpecies(species, patch, int.MaxValue));
        run.ExternalEffects.Add(new ExternalEffect(species, 0, 4, "overflow", patch));
        if (strict)
        {
            AssertThrown(() => run.CalculateAndApplyFinalExternalEffectSizes(true))
                .IsInstanceOf<OverflowException>();
            AssertThat(run.Results!.GetPopulationInPatch(species, patch)).IsEqual(int.MaxValue);
        }
        else
        {
            run.CalculateAndApplyFinalExternalEffectSizes();
            AssertThat(run.ExternalEffects[0].Constant).IsEqual(int.MaxValue);
            AssertThat(run.Results!.GetPopulationInPatch(species, patch)).IsEqual(2L * int.MaxValue);
        }
    }

    private static GameWorld CreateWorld()
    {
        return new GameWorld(new WorldGenerationSettings
        {
            Seed = 12345,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
    }

    private static ControlledRun Complete(GameWorld world, Action<RunResults> action)
    {
        var run = new ControlledRun(world, action);
        for (int i = 0; i < 8 && !run.Finished; ++i)
            run.OneStep();

        AssertThat(run.WasSuccessful).IsTrue();
        return run;
    }

    private sealed class ControlledRun : AutoEvoRun
    {
        private readonly Action<RunResults> action;

        public ControlledRun(GameWorld world, Action<RunResults> action) : base(world, world.AutoEvoGlobalCache)
        {
            this.action = action;
        }

        protected override void GatherInfo(Queue<IRunStep> steps)
        {
            steps.Enqueue(new ActionStep(action));
        }
    }

    private sealed class ActionStep : IRunStep
    {
        private readonly Action<RunResults> action;

        public ActionStep(Action<RunResults> action)
        {
            this.action = action;
        }

        public int TotalSteps => 1;
        public bool CanRunConcurrently => false;

        public bool RunStep(RunResults results)
        {
            action(results);
            return true;
        }
    }

    private sealed class CallbackEffect : IWorldEffect
    {
        private readonly Action action;

        public CallbackEffect(Action action)
        {
            this.action = action;
        }

        public ushort CurrentArchiveVersion => 1;
        public ArchiveObjectType ArchiveObjectType => throw new NotSupportedException();
        public bool CanBeReferencedInArchive => false;

        public void WriteToArchive(ISArchiveWriter writer)
        {
            throw new NotSupportedException();
        }

        public void OnRegisterToWorld()
        {
        }

        public void OnTimePassed(double elapsed, double totalTimePassed)
        {
            action();
        }
    }
}
