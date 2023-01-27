﻿using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Override functions, debug panel
/// </summary>
public partial class DebugOverlays : Control
{
    [Export]
    public NodePath? FPSCheckBoxPath;

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

    private static DebugOverlays? instance;

#pragma warning disable CA2213
    private CustomDialog debugPanelDialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private Control fpsCounter = null!;
    private CustomDialog performanceMetrics = null!;
    private Control labelsLayer = null!;
#pragma warning restore CA2213

    private DebugOverlays()
    {
        instance = this;
    }

    public static DebugOverlays Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _EnterTree()
    {
        base._EnterTree();

        Show();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    public override void _Ready()
    {
        base._Ready();

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
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // Entity label
        if (showEntityLabels)
            UpdateEntityLabels();

        // Performance metrics
        if (performanceMetrics.Visible)
            UpdateMetrics(delta);

        // FPS counter
        if (fpsCounter.Visible)
            UpdateFPS();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (FPSCheckBoxPath != null)
            {
                FPSCheckBoxPath.Dispose();
                FPSLabelPath.Dispose();
                DeltaLabelPath.Dispose();
                MetricsTextPath.Dispose();

                PerformanceMetricsCheckBoxPath.Dispose();
                DebugPanelDialogPath.Dispose();
                FPSCounterPath.Dispose();
                PerformanceMetricsPath.Dispose();
                EntityLabelsPath.Dispose();
                FPSDisplayLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
        if (showEntityLabels == state)
            return;

        ShowEntityLabels = state;

        if (state)
        {
            InitiateEntityLabels();
        }
        else
        {
            CleanEntityLabels();
        }
    }

    private void OnTransparencySliderValueChanged(float value)
    {
        performanceMetrics.Modulate = debugPanelDialog.Modulate = new Color(1, 1, 1, 1 - value);
    }

    private void OnDumpSceneTreeButtonPressed()
    {
        DumpSceneTreeToFile(GetTree().Root);
    }
}
