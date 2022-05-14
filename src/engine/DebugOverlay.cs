using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Debug panel
/// </summary>
public partial class DebugOverlay : Control
{
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

    private CustomDialog dialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private FPSCounter fpsCounter = null!;
    private PerformanceMetrics performanceMetrics = null!;
    private Control labelsLayer = null!;

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

        EntityLabelEnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);

        EntityLabelExitTree();

        base._ExitTree();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        EntityLabelProcess();
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
}
