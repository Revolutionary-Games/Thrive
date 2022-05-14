using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using Nito.Collections;

public class PerformanceMetrics : Control
{
    [Export]
    public NodePath DialogPath = null!;

    [Export]
    public NodePath FPSLabelPath = null!;

    [Export]
    public NodePath DeltaLabelPath = null!;

    [Export]
    public NodePath MetricsTextPath = null!;

    // TODO: make this time based
    private const int SpawnHistoryLength = 300;

    private static PerformanceMetrics? instance;

    private readonly Deque<int> spawnHistory = new(SpawnHistoryLength);
    private readonly Deque<int> despawnHistory = new(SpawnHistoryLength);

    private CustomDialog dialog = null!;
    private Label fpsLabel = null!;
    private Label deltaLabel = null!;
    private Label metricsText = null!;

    private int entities;
    private int children;
    private int currentSpawned;
    private int currentDespawned;

    private PerformanceMetrics()
    {
        instance = this;
    }

    [Signal]
    public delegate void OnHidden();

    public static PerformanceMetrics Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        dialog = GetNode<CustomDialog>(DialogPath);
        fpsLabel = GetNode<Label>(FPSLabelPath);
        deltaLabel = GetNode<Label>(DeltaLabelPath);
        metricsText = GetNode<Label>(MetricsTextPath);

        if (Visible)
            dialog.Show();
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
        deltaLabel.Text = new LocalizedString("FRAME_DURATION", delta).ToString();

        var currentProcess = Process.GetCurrentProcess();

        var processorTime = currentProcess.TotalProcessorTime;
        var threads = currentProcess.Threads.Count;
        var usedMemory = Math.Round(currentProcess.WorkingSet64 / (double)Constants.MEBIBYTE, 1);

        // These don't seem to work:
        // Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects),
        // Performance.GetMonitor(Performance.Monitor.Physics3dCollisionPairs),
        // Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount),

        metricsText.Text =
            new LocalizedString("METRICS_CONTENT", Performance.GetMonitor(Performance.Monitor.TimeProcess),
                    Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess),
                    entities, children, spawnHistory.Sum(), despawnHistory.Sum(),
                    Performance.GetMonitor(Performance.Monitor.ObjectNodeCount), usedMemory,
                    Math.Round(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / Constants.MEBIBYTE, 1),
                    Performance.GetMonitor(Performance.Monitor.RenderObjectsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.Render2dDrawCallsInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderMaterialChangesInFrame),
                    Performance.GetMonitor(Performance.Monitor.RenderShaderChangesInFrame),
                    Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
                    Performance.GetMonitor(Performance.Monitor.AudioOutputLatency) * 1000, threads, processorTime)
                .ToString();

        entities = 0;
        children = 0;

        spawnHistory.AddToBack(currentSpawned);
        despawnHistory.AddToBack(currentDespawned);

        while (spawnHistory.Count > SpawnHistoryLength)
            spawnHistory.RemoveFromFront();

        while (despawnHistory.Count > SpawnHistoryLength)
            despawnHistory.RemoveFromFront();

        currentSpawned = 0;
        currentDespawned = 0;
    }

    public void ReportEntities(int totalEntities, int otherChildren)
    {
        entities = totalEntities;
        children = otherChildren;
    }

    public void ReportSpawns(int newSpawns)
    {
        currentSpawned += newSpawns;
    }

    public void ReportDespawns(int newDespawns)
    {
        currentDespawned += newDespawns;
    }

    public void Toggle(bool state)
    {
        if (Visible == state)
            return;

        if (state)
        {
            Show();
            dialog.Show();
        }
        else
        {
            Hide();
            dialog.Hide();
        }
    }

    public void DialogHidden()
    {
        if (Visible)
        {
            Visible = false;
            EmitSignal(nameof(OnHidden));
        }
    }
}
