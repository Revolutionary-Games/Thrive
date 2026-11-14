using System;
using System.Threading;
using GdUnit4;
using Newtonsoft.Json;
using SharedBase.Archive;
using static GdUnit4.Assertions;

/// <summary>
///   Tests consecutive generation commits, cancellation and terminal partial application.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AutoEvoSimulationTests
{
    /// <summary>
    ///   Verifies that two successful generations advance the same world and chain their recorded input and output,
    ///   protecting consecutive commits and reporting the completed run duration.
    /// </summary>
    [TestCase]
    public void TwoGenerations_ApplyTimeAndHistoryInTheSameWorld()
    {
        var world = CreateWorld();

        // Keep this sequencing test focused on generation commits without stochastic migration or speciation.
        world.WorldSettings.AutoEvoConfiguration = new AutoEvoConfiguration
        {
            MutationsPerSpecies = 0,
            MoveAttemptsPerSpecies = 0,
            StrictNicheCompetition = world.WorldSettings.AutoEvoConfiguration.StrictNicheCompetition,
        };
        var simulation = new AutoEvoSimulation(world, 2);
        var initialTime = world.TotalPassedTime;
        var timeStep = world.CalculateNextTimeStep(1) - initialTime;
        var initialGeneration = world.PlayerSpecies.Generation;
        var initialHistory = world.GenerationHistory.Count;
        simulation.Process();

        try
        {
            for (int generation = 0; generation < 2; ++generation)
            {
                var run = simulation.CurrentRun!;
                WaitForRun(run);
                AssertThat(run.WasSuccessful).IsTrue();
                var computeTime = run.RunDuration;
                var previousInput = AutoEvoWorldState.Capture(world);

                simulation.Process();

                AssertThat(simulation.Error).IsNull();
                AssertThat(simulation.CompletedGenerations).IsEqual(generation + 1);
                var result = simulation.Generations[generation];
                AssertThat(result.ComputeSeconds).IsEqual(computeTime.TotalSeconds);
                AssertThat(result.WallSeconds >= result.ComputeSeconds).IsTrue();
                AssertThat(JsonConvert.SerializeObject(result.Input))
                    .IsEqual(JsonConvert.SerializeObject(previousInput));
                AssertThat(world.TotalPassedTime).IsEqual(initialTime + (generation + 1) * timeStep);
                AssertThat(world.PlayerSpecies.Generation).IsEqual(initialGeneration + generation + 1);
                AssertThat(world.GenerationHistory.Count).IsEqual(initialHistory + generation + 1);
            }

            AssertThat(simulation.WasSuccessful).IsTrue();
            AssertThat(JsonConvert.SerializeObject(simulation.Generations[1].Input))
                .IsEqual(JsonConvert.SerializeObject(simulation.Generations[0].Output));
        }
        finally
        {
            Drain(simulation);
        }
    }

    /// <summary>
    ///   Verifies that cancellation before or after starting a run leaves the world unchanged,
    ///   protecting the terminal cancellation state against committing results or restarting work.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Cancellation_DoesNotCommitOrStartAnotherGeneration(bool startWork)
    {
        var world = CreateWorld();
        var original = JsonConvert.SerializeObject(AutoEvoWorldState.Capture(world));
        var simulation = new AutoEvoSimulation(world, 2);
        if (startWork)
            simulation.Process();

        Drain(simulation);
        AssertThat(simulation.Finished).IsTrue();
        AssertThat(simulation.WasSuccessful).IsFalse();
        AssertThat(simulation.Error).IsNotNull();
        AssertThat(simulation.CompletedGenerations).IsEqual(0);
        AssertThat(JsonConvert.SerializeObject(AutoEvoWorldState.Capture(world))).IsEqual(original);
        simulation.Process();
        AssertThat(simulation.CompletedGenerations).IsEqual(0);
    }

    /// <summary>
    ///   Verifies that a history failure after one successful generation preserves that result and stops processing,
    ///   protecting against retrying a partially applied generation or discarding earlier output.
    /// </summary>
    [TestCase]
    public void HistoryFailure_PreservesCompletedResultsAndStopsAfterPartialApplication()
    {
        var world = CreateWorld();

        // A duplicate second-generation key fails only after time and populations have already been applied.
        world.GenerationHistory.Add(2, world.GenerationHistory[0]);
        var simulation = new AutoEvoSimulation(world, 3);
        var initialTime = world.TotalPassedTime;
        var step = world.CalculateNextTimeStep(1) - initialTime;
        simulation.Process();

        try
        {
            WaitForRun(simulation.CurrentRun!);
            simulation.Process();
            AssertThat(simulation.CompletedGenerations).IsEqual(1);
            var firstResult = JsonConvert.SerializeObject(simulation.Generations[0]);

            WaitForRun(simulation.CurrentRun!);
            simulation.Process();
            AssertThat(simulation.Finished).IsTrue();
            AssertThat(simulation.WasSuccessful).IsFalse();
            AssertThat(simulation.Error).IsNotNull();
            AssertThat(simulation.CompletedGenerations).IsEqual(1);
            AssertThat(world.TotalPassedTime).IsEqual(initialTime + 2 * step);
            AssertThat(world.PlayerSpecies.Generation).IsEqual(3);

            simulation.Process();
            AssertThat(world.TotalPassedTime).IsEqual(initialTime + 2 * step);
            AssertThat(simulation.CompletedGenerations).IsEqual(1);
            AssertThat(JsonConvert.SerializeObject(simulation.Generations[0])).IsEqual(firstResult);
        }
        finally
        {
            Drain(simulation);
        }
    }

    /// <summary>
    ///   Verifies that a failing world effect terminates the simulation before populations or history are committed,
    ///   protecting the strict application policy used by unattended runs.
    /// </summary>
    [TestCase]
    public void WorldEffectFailure_StopsBeforeApplyingPopulationResults()
    {
        var world = CreateWorld();
        world.TimedEffects.RegisterEffect("test_failure", new CallbackWorldEffect(() =>
            throw new InvalidOperationException("controlled world effect failure")));
        var original = JsonConvert.SerializeObject(AutoEvoWorldState.Capture(world).PatchPopulations);
        var simulation = new AutoEvoSimulation(world, 2);
        simulation.Process();

        try
        {
            WaitForRun(simulation.CurrentRun!);
            simulation.Process();
            AssertThat(simulation.Finished).IsTrue();
            AssertThat(simulation.Error!.Contains("controlled world effect failure")).IsTrue();
            AssertThat(simulation.CompletedGenerations).IsEqual(0);
            AssertThat(world.GenerationHistory.Count).IsEqual(1);
            AssertThat(JsonConvert.SerializeObject(AutoEvoWorldState.Capture(world).PatchPopulations))
                .IsEqual(original);
        }
        finally
        {
            Drain(simulation);
        }
    }

    /// <summary>
    ///   Verifies that cancellation during application remains a failed outcome for both final and intermediate
    ///   generations, protecting against reporting success or starting another run after the current commit.
    /// </summary>
    [TestCase(1)]
    [TestCase(2)]
    public void CancellationDuringApplication_ReportsCancellationAndDoesNotStartAnotherRun(int requested)
    {
        var world = CreateWorld();
        var simulation = new AutoEvoSimulation(world, requested);
        world.TimedEffects.RegisterEffect("cancel_during_apply", new CallbackWorldEffect(simulation.Cancel));
        simulation.Process();

        try
        {
            var run = simulation.CurrentRun;
            WaitForRun(run!);
            simulation.Process();
            AssertThat(simulation.Finished).IsTrue();
            AssertThat(simulation.WasSuccessful).IsFalse();
            AssertThat(simulation.Error).IsEqual("Auto-Evo cancelled");
            AssertThat(simulation.CompletedGenerations).IsEqual(1);
            AssertThat(simulation.CurrentRun).IsSame(run);
            AssertThat(world.GenerationHistory.Count).IsEqual(2);
        }
        finally
        {
            Drain(simulation);
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

    private static void WaitForRun(AutoEvoRun run)
    {
        if (!SpinWait.SpinUntil(() => run.Finished, TimeSpan.FromSeconds(60)))
            throw new TimeoutException("Auto-Evo did not finish");
    }

    private static void Drain(AutoEvoSimulation simulation)
    {
        simulation.Cancel();
        if (simulation.CurrentRun != null)
            WaitForRun(simulation.CurrentRun);

        simulation.Process();
    }

    private sealed class CallbackWorldEffect : IWorldEffect
    {
        private readonly Action callback;

        public CallbackWorldEffect(Action callback)
        {
            this.callback = callback;
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
            callback();
        }
    }
}
