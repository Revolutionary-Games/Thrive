using System.Collections.Generic;
using System.Globalization;
using Godot;

public class DebugOverlay : Control
{
    private readonly Dictionary<Microbe, Label> microbeLabels = new();
    private readonly Dictionary<FloatingChunk, Label> floatingChunkLabels = new();
    private readonly Dictionary<AgentProjectile, Label> agentProjectileLabels = new();

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

    private bool showEntityLabels;

    private CustomDialog dialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private FPSCounter fpsCounter = null!;
    private PerformanceMetrics performanceMetrics = null!;
    private Control labelsLayer = null!;
    private Font smallerFont = null!;

    private Camera? activeCamera;

    private bool ShowEntityLabels
    {
        get => showEntityLabels;
        set
        {
            showEntityLabels = value;
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
        smallerFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Tiny.tres");

        base._Ready();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Show();
        InputManager.RegisterReceiver(this);

        var rootTree = GetTree();
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

        if (activeCamera is not { Current: true })
            activeCamera = GetViewport().GetCamera();

        if (showEntityLabels)
        {
            UpdateEntityLabels();
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

    private void UpdateEntityLabels()
    {
        if (activeCamera == null)
            return;

        foreach (var pair in microbeLabels)
        {
            var microbe = pair.Key;
            var label = pair.Value;

            if (label.Text.Empty())
                label.Text = microbe.ToString();

            label.RectPosition = activeCamera.UnprojectPosition(microbe.Transform.origin);
        }

        foreach (var pair in floatingChunkLabels)
        {
            var floatingChunk = pair.Key;
            var label = pair.Value;

            if (label.Text.Empty())
                label.Text = floatingChunk.ToString();

            label.RectPosition = activeCamera.UnprojectPosition(floatingChunk.Transform.origin);
        }

        foreach (var pair in agentProjectileLabels)
        {
            var agentProjectile = pair.Key;
            var label = pair.Value;

            label.Text = !agentProjectile.Visible ?
                string.Empty :
                $"[AP:{agentProjectile.GetInstanceId()}:Amount={agentProjectile.Amount}:" +
                $"TTL={agentProjectile.TimeToLiveRemaining.ToString("F2", CultureInfo.CurrentCulture)}]";

            label.RectPosition = activeCamera.UnprojectPosition(agentProjectile.Transform.origin);
        }
    }

    private void UpdateLabelOnMicrobeDeath(Microbe microbe)
    {
        if (microbeLabels.TryGetValue(microbe, out var label))
            label.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
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
        ShowEntityLabels = state;
    }

    private void OnTransparencySliderValueChanged(float value)
    {
        performanceMetrics.Modulate = dialog.Modulate = new Color(1, 1, 1, 1 - value);
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
                microbe.OnDeath += UpdateLabelOnMicrobeDeath;
                break;
            }

            case FloatingChunk floatingChunk:
            {
                var label = new Label { Text = floatingChunk.ToString() };
                label.AddFontOverride("font", smallerFont);
                labelsLayer.AddChild(label);
                floatingChunkLabels.Add(floatingChunk, label);
                break;
            }

            case AgentProjectile agentProjectile:
            {
                var label = new Label { Text = agentProjectile.ToString() };
                label.AddFontOverride("font", smallerFont);
                labelsLayer.AddChild(label);
                agentProjectileLabels.Add(agentProjectile, label);
                break;
            }
        }
    }

    private void OnNodeRemoved(Node node)
    {
        switch (node)
        {
            case Camera camera:
            {
                // When a camera is removed from the scene tree, it can't be active and will be disposed soon.
                // This makes sure the active camera is not disposed so we don't check it in _Process().
                if (activeCamera == camera)
                    activeCamera = null;

                break;
            }

            case Microbe microbe:
            {
                if (microbeLabels.TryGetValue(microbe, out var label))
                {
                    labelsLayer.RemoveChild(label);
                    label.QueueFree();
                    microbe.OnDeath -= UpdateLabelOnMicrobeDeath;
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

            case AgentProjectile agentProjectile:
            {
                if (agentProjectileLabels.TryGetValue(agentProjectile, out var label))
                {
                    labelsLayer.RemoveChild(label);
                    label.QueueFree();
                }

                agentProjectileLabels.Remove(agentProjectile);
                break;
            }
        }
    }
}
