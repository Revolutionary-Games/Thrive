﻿using Godot;

public class DebugPanel : Control
{
    [Export]
    private NodePath fpsCheckBoxPath = null!;

    [Export]
    private NodePath performanceMetricsCheckBoxPath = null!;

    [Export]
    private NodePath dialogPath = null!;

    private CustomDialog dialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;

    private FPSCounter fpsCounter = null!;
    private PerformanceMetrics performanceMetrics = null!;

    public override void _Ready()
    {
        fpsCheckBox = GetNode<CustomCheckBox>(fpsCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(performanceMetricsCheckBoxPath);
        dialog = GetNode<CustomDialog>(dialogPath);

        fpsCounter = GetNode<FPSCounter>("FPSCounter");
        performanceMetrics = GetNode<PerformanceMetrics>("PerformanceMetrics");
        performanceMetrics.Connect(nameof(PerformanceMetrics.OnHidden), this, nameof(OnPerformanceMetricsToggled));
        base._Ready();
    }

    public override void _EnterTree()
    {
        Show();
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false, Priority = 1)]
    public void OnPerformanceMetricsToggled()
    {
        performanceMetricsCheckBox.Pressed = !performanceMetricsCheckBox.Pressed;
    }

    [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false, Priority = 1)]
    public void OnFpsToggled()
    {
        fpsCheckBox.Pressed = !fpsCheckBox.Pressed;
    }

    [RunOnKeyDown("toggle_debug_panel", OnlyUnhandled = false, Priority = 2)]
    public void OnDebugPanelToggled()
    {
        if (!dialog.Visible)
        {
            dialog.Show();
        }
        else
        {
            dialog.Hide();
        }
    }

    private void OnPerformanceMetricsCheckBoxToggled(bool state)
    {
        performanceMetrics.Toggle(state);
    }

    private void OnFpsCheckBoxToggled(bool state)
    {
        fpsCounter.ToggleFps(state);
    }

    private void OnCollisionShapeCheckBoxToggled(bool state)
    {
        GetTree().DebugCollisionsHint = state;
    }

    private void OnEntityLabelCheckBoxToggled(bool state)
    {
        // TODO:
    }
}
