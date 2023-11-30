using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public float TimeToKeepPhysicalWorldData = 0.4f;

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

    private float entities;
    private int children;
    private float currentSpawned;
    private float currentDespawned;

    public bool PerformanceMetricsVisible
    {
        get => performanceMetrics.Visible;
        private set
        {
            if (performanceMetricsCheckBox.Pressed == value)
                return;

            performanceMetricsCheckBox.Pressed = value;
        }
    }

    public void ReportEntities(float totalEntities, int otherChildren)
    {
        entities = totalEntities;
        children = otherChildren;
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

    private void UpdateMetrics(float delta)
    {
        UpdatePhysicalWorldDataExpiration(delta);

        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
        deltaLabel.Text = new LocalizedString("FRAME_DURATION", delta).ToString();

        var currentProcess = Process.GetCurrentProcess();

        var processorTime = currentProcess.TotalProcessorTime;
        var threads = currentProcess.Threads.Count;
        var usedMemory = Math.Round(currentProcess.WorkingSet64 / (double)Constants.MEBIBYTE, 1);
        var usedVideoMemory = Math.Round(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) /
            Constants.MEBIBYTE, 1);
        var mibFormat = TranslationServer.Translate("MIB_VALUE");

        // These don't seem to work:
        // Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects),
        // Performance.GetMonitor(Performance.Monitor.Physics3dCollisionPairs),
        // Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount),

        float customPhysicsTime = customPhysics.Sum(s => s.LatestPhysicsTime);

        // TODO: show the average physics time as well
        float customPhysicsAverage = customPhysics.Sum(s => s.AveragePhysicsTime);
        _ = customPhysicsAverage;

        metricsText.Text =
            new LocalizedString("METRICS_CONTENT", Performance.GetMonitor(Performance.Monitor.TimeProcess),
                    Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) + customPhysicsTime,
                    Math.Round(entities, 1), children,
                    Math.Round(spawnHistory.Sum(), 1), Math.Round(despawnHistory.Sum(), 1),
                    Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
                    OS.GetName() == Constants.OS_WINDOWS_NAME ?
                        TranslationServer.Translate("UNKNOWN_ON_WINDOWS") :
                        mibFormat.FormatSafe(usedMemory),
                    mibFormat.FormatSafe(usedVideoMemory),
                    Performance.GetMonitor(Performance.Monitor.RenderObjectsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.Render2dDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderMaterialChangesInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderShaderChangesInFrame),
                    Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
                    Performance.GetMonitor(Performance.Monitor.AudioOutputLatency) * 1000, threads, processorTime)
                .ToString();

        entities = 0.0f;
        children = 0;

        spawnHistory.AddToBack(currentSpawned);
        despawnHistory.AddToBack(currentDespawned);

        while (spawnHistory.Count > SpawnHistoryLength)
            spawnHistory.RemoveFromFront();

        while (despawnHistory.Count > SpawnHistoryLength)
            despawnHistory.RemoveFromFront();

        currentSpawned = 0.0f;
        currentDespawned = 0.0f;
    }

    private void UpdatePhysicalWorldDataExpiration(float delta)
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
        public float TimeSinceUpdate;
        public float LatestPhysicsTime;
        public float AveragePhysicsTime;

        public PhysicalWorldStats(WeakReference<PhysicalWorld> world)
        {
            World = world;
        }
    }
}
