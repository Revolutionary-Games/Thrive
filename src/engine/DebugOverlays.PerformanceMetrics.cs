using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using Nito.Collections;

/// <summary>
///   Partial class: Performance metrics
/// </summary>
public partial class DebugOverlays
{
    [Export]
    public NodePath FPSLabelPath = null!;

    [Export]
    public NodePath DeltaLabelPath = null!;

    [Export]
    public NodePath MetricsTextPath = null!;

    /// <summary>
    ///   How long to keep physical world's stats since the last time they were reported, used to clear old world data
    ///   out of the display.
    /// </summary>
    [Export]
    public double TimeToKeepPhysicalWorldData = 0.4f;

    // TODO: make this time based
    private const int SpawnHistoryLength = 300;

    private readonly Deque<float> spawnHistory = new(SpawnHistoryLength);
    private readonly Deque<float> despawnHistory = new(SpawnHistoryLength);

    private readonly List<PhysicalWorldStats> customPhysics = new();

    private readonly List<PhysicalWorldStats> customPhysicsToRemove = new();

#pragma warning disable CA2213
    private Label fpsLabel = null!;
    private Label deltaLabel = null!;
    private Label metricsText = null!;
#pragma warning restore CA2213

    private float entityWeight;
    private int entityCount;
    private float currentSpawned;
    private float currentDespawned;

    /// <summary>
    ///   Needs to have a cached value of this visible to allow access from other threads
    /// </summary>
    private bool showPerformance;

    public bool PerformanceMetricsVisible
    {
        get => showPerformance;
        private set
        {
            if (showPerformance == value)
                return;

            showPerformance = value;

            if (showPerformance)
            {
                performanceMetrics.Show();
            }
            else
            {
                performanceMetrics.Hide();
            }

            if (performanceMetricsCheckBox.ButtonPressed == showPerformance)
                return;

            performanceMetricsCheckBox.ButtonPressed = showPerformance;
        }
    }

    public void ReportEntities(float totalWeight, int rawCount)
    {
        entityWeight = totalWeight;
        entityCount = rawCount;
    }

    public void ReportEntities(int count)
    {
        entityWeight = count;
        entityCount = count;
    }

    public void ReportSpawns(float newSpawns)
    {
        currentSpawned += newSpawns;
    }

    public void ReportDespawns(float newDespawns)
    {
        currentDespawned += newDespawns;
    }

    public void ReportPhysicalWorldStats(PhysicalWorld physicalWorld)
    {
        foreach (var entry in customPhysics)
        {
            if (entry.World.TryGetTarget(out var target) && physicalWorld == target)
            {
                entry.TimeSinceUpdate = 0;
                entry.LatestPhysicsTime = physicalWorld.LatestPhysicsDuration;
                entry.AveragePhysicsTime = physicalWorld.AveragePhysicsDuration;
                return;
            }
        }

        customPhysics.Add(new PhysicalWorldStats(new WeakReference<PhysicalWorld>(physicalWorld))
        {
            LatestPhysicsTime = physicalWorld.LatestPhysicsDuration,
            AveragePhysicsTime = physicalWorld.AveragePhysicsDuration,
        });
    }

    private void UpdateMetrics(double delta)
    {
        UpdatePhysicalWorldDataExpiration(delta);

        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
        deltaLabel.Text = new LocalizedString("FRAME_DURATION", Math.Round(delta, 8)).ToString();

        var currentProcess = Process.GetCurrentProcess();

        var processorTime = currentProcess.TotalProcessorTime;

        int threads;
        try
        {
            threads = currentProcess.Threads.Count;
        }
        catch (IOException)
        {
            // Seems like on Linux a read of this property can sometimes fail like this
            threads = -1;
        }

        var usedMemory = Math.Round(currentProcess.WorkingSet64 / (double)Constants.MEBIBYTE, 1);
        var usedVideoMemory = Math.Round(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) /
            Constants.MEBIBYTE, 1);
        var mibFormat = Localization.Translate("MIB_VALUE");

        var customPhysicsTime = customPhysics.Sum(s => s.LatestPhysicsTime);

        // TODO: show the average physics time as well
        var customPhysicsAverage = customPhysics.Sum(s => s.AveragePhysicsTime);
        _ = customPhysicsAverage;

        metricsText.Text =
            new LocalizedString("METRICS_CONTENT", Performance.GetMonitor(Performance.Monitor.TimeProcess),
                    Math.Round(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) + customPhysicsTime, 10),
                    entityCount, Math.Round(entityWeight, 1),
                    Math.Round(spawnHistory.Sum(), 1), Math.Round(despawnHistory.Sum(), 1),
                    Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
                    mibFormat.FormatSafe(usedMemory),
                    mibFormat.FormatSafe(usedVideoMemory),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
                    Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
                    Math.Round(Performance.GetMonitor(Performance.Monitor.AudioOutputLatency) * 1000, 3), threads,
                    processorTime)
                .ToString();

        entityWeight = 0.0f;
        entityCount = 0;

        spawnHistory.AddToBack(currentSpawned);
        despawnHistory.AddToBack(currentDespawned);

        while (spawnHistory.Count > SpawnHistoryLength)
            spawnHistory.RemoveFromFront();

        while (despawnHistory.Count > SpawnHistoryLength)
            despawnHistory.RemoveFromFront();

        currentSpawned = 0.0f;
        currentDespawned = 0.0f;
    }

    private void UpdatePhysicalWorldDataExpiration(double delta)
    {
        foreach (var entry in customPhysics)
        {
            entry.TimeSinceUpdate += delta;

            if (entry.TimeSinceUpdate > TimeToKeepPhysicalWorldData)
                customPhysicsToRemove.Add(entry);
        }

        foreach (var toRemove in customPhysicsToRemove)
        {
            customPhysics.Remove(toRemove);
        }

        customPhysicsToRemove.Clear();
    }

    private class PhysicalWorldStats
    {
        public readonly WeakReference<PhysicalWorld> World;
        public double TimeSinceUpdate;
        public float LatestPhysicsTime;
        public float AveragePhysicsTime;

        public PhysicalWorldStats(WeakReference<PhysicalWorld> world)
        {
            World = world;
        }
    }
}
