using System.Collections.Generic;
using Godot;

public class DebugOverlay : Control
{
    private readonly Dictionary<RigidBody, Label> entityLabels = new();

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

        foreach (var pair in entityLabels)
        {
            var body = pair.Key;
            var label = pair.Value;

            // Update names
            if (label.Text.Empty())
            {
                switch (body)
                {
                    case Microbe microbe:
                    {
                        if (microbe.Species != null!)
                        {
                            label.Text = $"[{microbe.Name}:{microbe.Species.Genus[0]}." +
                                $"{(microbe.Species.Epithet.Length >= 4 ? microbe.Species.Epithet.Substring(0, 4) : microbe.Species.Epithet)}]";
                        }

                        break;
                    }

                    case FloatingChunk chunk:
                    {
                        label.Text = $"[{chunk.Name}:{chunk.ChunkName}]";

                        break;
                    }

                    default:
                    {
                        label.Text = $"[{body.Name}]";
                        break;
                    }
                }
            }

            label.RectPosition = activeCamera.UnprojectPosition(body.Transform.origin);
        }
    }

    private void UpdateLabelOnMicrobeDeath(Microbe microbe)
    {
        if (entityLabels.TryGetValue(microbe, out var label))
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
        if (node is not RigidBody body)
            return;

        var label = new Label();
        labelsLayer.AddChild(label);
        entityLabels.Add(body, label);

        switch (body)
        {
            case Microbe microbe:
            {
                microbe.OnDeath += UpdateLabelOnMicrobeDeath;
                break;
            }

            case FloatingChunk:
            case AgentProjectile:
            {
                label.AddFontOverride("font", smallerFont);
                break;
            }
        }
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is Camera camera)
        {
            // When a camera is removed from the scene tree, it can't be active and will be disposed soon.
            // This makes sure the active camera is not disposed so we don't check it in _Process().
            if (activeCamera == camera)
                activeCamera = null;
            return;
        }

        if (node is not RigidBody body)
            return;

        if (entityLabels.TryGetValue(body, out var label))
        {
            labelsLayer.RemoveChild(label);
            label.QueueFree();
            entityLabels.Remove(body);

            if (body is Microbe microbe)
            {
                microbe.OnDeath -= UpdateLabelOnMicrobeDeath;
            }
        }
    }
}
