using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
///   Runs consecutive generations in one world, polled on the main thread.
///   Failure is terminal: a partially changed world must not be reused as a successful input.
/// </summary>
public sealed class AutoEvoSimulation
{
    private readonly GameWorld world;
    private readonly Stopwatch generationTimer = new();
    private readonly List<AutoEvoGenerationResult> generations = new();
    private AutoEvoWorldState? generationInput;
    private volatile bool cancelRequested;

    public AutoEvoSimulation(GameWorld world, int generations)
    {
        if (generations < 1)
            throw new ArgumentOutOfRangeException(nameof(generations));

        this.world = world;
        RequestedGenerations = generations;
    }

    public int RequestedGenerations { get; }
    public IReadOnlyList<AutoEvoGenerationResult> Generations => generations;
    public int CompletedGenerations => generations.Count;

    /// <summary>
    ///   The current calculation, for progress observation. Do not modify its inputs while it is running.
    /// </summary>
    public AutoEvoRun? CurrentRun { get; private set; }

    public string? Error { get; private set; }
    public bool Finished { get; private set; }
    public bool WasSuccessful => Finished && Error == null && CompletedGenerations == RequestedGenerations;

    public void Cancel()
    {
        if (Finished)
            return;

        cancelRequested = true;
        CurrentRun?.Abort();
    }

    /// <summary>
    ///   Starts or observes work without blocking the main thread. Cancellation drains accepted work before finishing.
    /// </summary>
    public void Process()
    {
        if (Finished)
            return;

        if (cancelRequested || Error != null)
        {
            Error ??= "Auto-Evo cancelled";
            CurrentRun?.Abort();
            Finished = CurrentRun == null || CurrentRun.Finished;
            return;
        }

        try
        {
            if (CurrentRun == null)
            {
                StartGeneration();
                return;
            }

            if (!CurrentRun.Finished)
                return;

            if (!CurrentRun.WasSuccessful)
                throw new InvalidOperationException("Auto-Evo computation failed or was aborted; see the run log");

            CurrentRun.CalculateAndApplyFinalExternalEffectSizes(true);
            CurrentRun.ApplyGeneration(true, true);
            generationTimer.Stop();

            generations.Add(new AutoEvoGenerationResult
            {
                Input = generationInput!,
                Output = AutoEvoWorldState.Capture(world),
                ComputeSeconds = CurrentRun.RunDuration.TotalSeconds,
                WallSeconds = generationTimer.Elapsed.TotalSeconds,
            });

            if (cancelRequested)
            {
                Error = "Auto-Evo cancelled";
                Finished = true;
                return;
            }

            if (CompletedGenerations == RequestedGenerations)
            {
                Finished = true;
                return;
            }

            StartGeneration();
        }
        catch (Exception e)
        {
            Error = e.ToString();
            CurrentRun?.Abort();
            Finished = CurrentRun == null || CurrentRun.Finished;
        }
    }

    private void StartGeneration()
    {
        generationInput = AutoEvoWorldState.Capture(world);
        generationTimer.Restart();
        CurrentRun = new AutoEvoRun(world, AutoEvoRun.GetGlobalCache(CurrentRun, world.WorldSettings))
        {
            FullSpeed = true,
        };
        if (cancelRequested)
            CurrentRun.Abort();

        CurrentRun.Start();
    }
}
