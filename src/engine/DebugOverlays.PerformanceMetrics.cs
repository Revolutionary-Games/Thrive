using System;
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

    // TODO: make this time based
    private const int SpawnHistoryLength = 300;

    private readonly Deque<float> spawnHistory = new(SpawnHistoryLength);
    private readonly Deque<float> despawnHistory = new(SpawnHistoryLength);

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

    private void UpdateMetrics(double delta)
    {
        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
        deltaLabel.Text = new LocalizedString("FRAME_DURATION", delta).ToString();

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

        // These don't seem to work:
        // Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects),
        // Performance.GetMonitor(Performance.Monitor.Physics3dCollisionPairs),
        // Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount),

        // TODO: check if memory use can finally be gotten on Windows

        metricsText.Text =
            new LocalizedString("METRICS_CONTENT", Performance.GetMonitor(Performance.Monitor.TimeProcess),
                    Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess),
                    entityCount, Math.Round(entityWeight, 1),
                    Math.Round(spawnHistory.Sum(), 1), Math.Round(despawnHistory.Sum(), 1),
                    Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
                    OS.GetName() == Constants.OS_WINDOWS_NAME ?
                        Localization.Translate("UNKNOWN_ON_WINDOWS") :
                        mibFormat.FormatSafe(usedMemory),
                    mibFormat.FormatSafe(usedVideoMemory),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
                    Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
                    Performance.GetMonitor(Performance.Monitor.AudioOutputLatency) * 1000, threads, processorTime)
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
}
