﻿using System;
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

    // TODO: make this time based
    private const int SpawnHistoryLength = 300;

    private readonly Deque<float> spawnHistory = new(SpawnHistoryLength);
    private readonly Deque<float> despawnHistory = new(SpawnHistoryLength);

    private Label fpsLabel = null!;
    private Label deltaLabel = null!;
    private Label metricsText = null!;

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

    private void UpdateMetrics(float delta)
    {
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

        metricsText.Text =
            new LocalizedString("METRICS_CONTENT", Performance.GetMonitor(Performance.Monitor.TimeProcess),
                    Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess),
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
}
