using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Debug panel
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

        EntityLabelReady();
        PerformanceMetricsReady();
        FPSCounterReady();

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
        PerformanceMetricsProcess(delta);
        FPSCounterProcess();
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
