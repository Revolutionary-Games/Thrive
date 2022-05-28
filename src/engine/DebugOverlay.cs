using System;
using System.Diagnostics;
using System.Linq;
using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Override functions, debug panel
/// </summary>
public partial class DebugOverlay : Control
{
    [Export]
    public NodePath FPSCheckBoxPath = null!;

    [Export]
    public NodePath PerformanceMetricsCheckBoxPath = null!;

    [Export]
    public NodePath DebugPanelDialogPath = null!;

    [Export]
    public NodePath FPSCounterPath = null!;

    [Export]
    public NodePath PerformanceMetricsPath = null!;

    [Export]
    public NodePath EntityLabelsPath = null!;

    private static DebugOverlay? instance;

    private CustomDialog debugPanelDialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private Control fpsCounter = null!;
    private CustomDialog performanceMetrics = null!;
    private Control labelsLayer = null!;

    private DebugOverlay()
    {
        instance = this;
    }

    public static DebugOverlay Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        fpsCheckBox = GetNode<CustomCheckBox>(FPSCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(PerformanceMetricsCheckBoxPath);
        debugPanelDialog = GetNode<CustomDialog>(DebugPanelDialogPath);
        fpsCounter = GetNode<Control>(FPSCounterPath);
        performanceMetrics = GetNode<CustomDialog>(PerformanceMetricsPath);
        labelsLayer = GetNode<Control>(EntityLabelsPath);
        smallerFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Tiny.tres");
        fpsLabel = GetNode<Label>(FPSLabelPath);
        deltaLabel = GetNode<Label>(DeltaLabelPath);
        metricsText = GetNode<Label>(MetricsTextPath);
        fpsDisplayLabel = GetNode<Label>(FPSDisplayLabelPath);

        base._Ready();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Show();
        InputManager.RegisterReceiver(this);

        // Entity label
        var rootTree = GetTree();
        rootTree.Connect("node_added", this, nameof(OnNodeAdded));
        rootTree.Connect("node_removed", this, nameof(OnNodeRemoved));
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);

        // Entity label
        var rootTree = GetTree();
        rootTree.Disconnect("node_added", this, nameof(OnNodeAdded));
        rootTree.Disconnect("node_removed", this, nameof(OnNodeRemoved));

        base._ExitTree();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (activeCamera is not { Current: true })
            activeCamera = GetViewport().GetCamera();

        // Entity label
        if (showEntityLabels)
            UpdateEntityLabels();

        // Performance metrics
        if (performanceMetrics.Visible)
        {
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
                        Math.Round(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / Constants.MEBIBYTE,
                            1),
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

        // FPS counter
        if (fpsCounter.Visible)
            fpsDisplayLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false)]
    public void OnPerformanceMetricsToggled()
    {
        performanceMetricsCheckBox.Pressed = !performanceMetricsCheckBox.Pressed;
    }

    [RunOnKeyDown("toggle_debug_panel", OnlyUnhandled = false)]
    public void OnDebugPanelToggled()
    {
        if (!debugPanelDialog.Visible)
        {
            debugPanelDialog.Show();
        }
        else
        {
            debugPanelDialog.Hide();
        }
    }

    [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
    public void OnFpsToggled()
    {
        fpsCheckBox.Pressed = !fpsCheckBox.Pressed;
    }

    private void OnPerformanceMetricsCheckBoxToggled(bool state)
    {
        if (performanceMetrics.Visible == state)
            return;

        if (state)
        {
            performanceMetrics.Show();
        }
        else
        {
            performanceMetrics.Hide();
        }
    }

    private void OnFpsCheckBoxToggled(bool state)
    {
        fpsCounter.Visible = state;
    }

    private void OnCollisionShapeCheckBoxToggled(bool state)
    {
        GetTree().DebugCollisionsHint = state;
    }

    private void OnEntityLabelCheckBoxToggled(bool state)
    {
        ShowEntityLabels = state;
    }

    private void OnTransparencySliderValueChanged(float value)
    {
        performanceMetrics.Modulate = debugPanelDialog.Modulate = new Color(1, 1, 1, 1 - value);
    }
}
