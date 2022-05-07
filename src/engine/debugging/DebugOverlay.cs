using System.Collections.Generic;
using Godot;

public class DebugOverlay : Control
{
    private readonly Dictionary<Microbe, Label> microbeLabels = new();
    private readonly Dictionary<FloatingChunk, Label> floatingChunkLabels = new();

    [Export]
    private NodePath fpsCheckBoxPath = null!;

    [Export]
    private NodePath performanceMetricsCheckBoxPath = null!;

    [Export]
    private NodePath dialogPath = null!;

    [Export]
    private NodePath fpsCounterPath = null!;

    [Export]
    private NodePath performanceMetricsPath = null!;

    [Export]
    private NodePath labelsLayerPath = null!;

    private bool showLabels;

    private CustomDialog dialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private FPSCounter fpsCounter = null!;
    private PerformanceMetrics performanceMetrics = null!;
    private Control labelsLayer = null!;

    private SceneTree rootTree = null!;

    private bool ShowLabels
    {
        get => showLabels;
        set
        {
            showLabels = value;
            labelsLayer.Visible = value;
        }
    }

    public override void _Ready()
    {
        fpsCheckBox = GetNode<CustomCheckBox>(fpsCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(performanceMetricsCheckBoxPath);
        dialog = GetNode<CustomDialog>(dialogPath);
        fpsCounter = GetNode<FPSCounter>(fpsCounterPath);
        performanceMetrics = GetNode<PerformanceMetrics>(performanceMetricsPath);
        labelsLayer = GetNode<Control>(labelsLayerPath);

        base._Ready();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Show();
        InputManager.RegisterReceiver(this);
        rootTree = GetTree();

        // TODO: Assess expenses
        rootTree.Connect("node_added", this, nameof(OnNodeAdded));
        rootTree.Connect("node_removed", this, nameof(OnNodeRemoved));
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (showLabels)
        {
            UpdateLabels();
        }
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false)]
    public void OnPerformanceMetricsToggled()
    {
        performanceMetricsCheckBox.Pressed = !performanceMetricsCheckBox.Pressed;
    }

    [RunOnKeyDown("toggle_debug_panel", OnlyUnhandled = false)]
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

    [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
    public void OnFpsToggled()
    {
        fpsCheckBox.Pressed = !fpsCheckBox.Pressed;
    }

    private void UpdateLabels()
    {
        var camera = GetViewport().GetCamera();

        if (camera == null)
            return;

        foreach (var pair in microbeLabels)
        {
            var microbe = pair.Key;
            var label = pair.Value;

            if (label.Text.Empty() || label.Text[0] == '<')
                label.Text = microbe.ToString();

            label.RectPosition = camera.UnprojectPosition(microbe.Transform.origin);
        }

        foreach (var pair in floatingChunkLabels)
        {
            var floatingChunk = pair.Key;
            var label = pair.Value;

            label.RectPosition = camera.UnprojectPosition(floatingChunk.Transform.origin);
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
        rootTree.DebugCollisionsHint = state;
    }

    private void OnEntityLabelCheckBoxToggled(bool state)
    {
        ShowLabels = state;
    }

    private void OnRigiditySliderValueChanged(float value)
    {
        dialog.Modulate = new Color(1, 1, 1, 1 - value);
    }

    private void OnNodeAdded(Node node)
    {
        switch (node)
        {
            case Microbe microbe:
            {
                var label = new Label { Text = microbe.ToString() };
                labelsLayer.AddChild(label);
                microbeLabels.Add(microbe, label);
                break;
            }

            case FloatingChunk floatingChunk:
            {
                var label = new Label { Text = floatingChunk.ToString() };
                labelsLayer.AddChild(label);
                floatingChunkLabels.Add(floatingChunk, label);
                break;
            }
        }
    }

    private void OnNodeRemoved(Node node)
    {
        switch (node)
        {
            case Microbe microbe:
            {
                if (microbeLabels.TryGetValue(microbe, out var label))
                {
                    labelsLayer.RemoveChild(label);
                    label.QueueFree();
                }

                microbeLabels.Remove(microbe);
                break;
            }

            case FloatingChunk floatingChunk:
            {
                if (floatingChunkLabels.TryGetValue(floatingChunk, out var label))
                {
                    labelsLayer.RemoveChild(label);
                    label.QueueFree();
                }

                floatingChunkLabels.Remove(floatingChunk);
                break;
            }
        }
    }
}
