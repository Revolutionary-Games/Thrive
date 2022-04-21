using System;
using System.Diagnostics;
using Godot;

public class PerformanceMetrics : ControlWithInput
{
    [Export]
    public NodePath DialogPath = null!;

    [Export]
    public NodePath FPSLabelPath = null!;

    [Export]
    public NodePath MetricsTextPath = null!;

    private CustomDialog dialog = null!;
    private Label fpsLabel = null!;
    private Label metricsText = null!;

    public override void _Ready()
    {
        dialog = GetNode<CustomDialog>(DialogPath);
        fpsLabel = GetNode<Label>(FPSLabelPath);
        metricsText = GetNode<Label>(MetricsTextPath);

        if (Visible)
            dialog.Show();
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();

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
    }

    [RunOnKeyToggle("toggle_metrics", OnlyUnhandled = false)]
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

    private void DialogHidden()
    {
        Visible = false;
    }
}
