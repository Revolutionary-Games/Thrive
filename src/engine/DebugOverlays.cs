using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Override functions, debug panel
/// </summary>
public partial class DebugOverlays : Control
{
    [Export]
    public NodePath? DebugCoordinatesPath;

    [Export]
    public NodePath FPSCheckBoxPath = null!;

    [Export]
    public NodePath PerformanceMetricsCheckBoxPath = null!;

    [Export]
    public NodePath InspectorCheckboxPath = null!;

    [Export]
    public NodePath DebugPanelDialogPath = null!;

    [Export]
    public NodePath FPSCounterPath = null!;

    [Export]
    public NodePath PerformanceMetricsPath = null!;

    [Export]
    public NodePath EntityLabelsPath = null!;

    [Export]
    public NodePath InspectorDialogPath = null!;

    private static DebugOverlays? instance;

#pragma warning disable CA2213
    private Label debugCoordinates = null!;
    private CustomWindow inspectorDialog = null!;
    private CustomWindow debugPanelDialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private CustomCheckBox inspectorCheckbox = null!;
    private Control fpsCounter = null!;
    private CustomWindow performanceMetrics = null!;
    private Control labelsLayer = null!;
#pragma warning restore CA2213

    private Rect2? reportedViewportSize;

    private DebugOverlays()
    {
        instance = this;
    }

    public static DebugOverlays Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        base._Ready();

        debugCoordinates = GetNode<Label>(DebugCoordinatesPath);
        inspectorDialog = GetNode<CustomWindow>(InspectorDialogPath);
        inspectorCheckbox = GetNode<CustomCheckBox>(InspectorCheckboxPath);
        fpsCheckBox = GetNode<CustomCheckBox>(FPSCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(PerformanceMetricsCheckBoxPath);
        debugPanelDialog = GetNode<CustomWindow>(DebugPanelDialogPath);
        fpsCounter = GetNode<Control>(FPSCounterPath);
        performanceMetrics = GetNode<CustomWindow>(PerformanceMetricsPath);
        labelsLayer = GetNode<Control>(EntityLabelsPath);
        smallerFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Tiny.tres");
        fpsLabel = GetNode<Label>(FPSLabelPath);
        deltaLabel = GetNode<Label>(DeltaLabelPath);
        metricsText = GetNode<Label>(MetricsTextPath);
        fpsDisplayLabel = GetNode<Label>(FPSDisplayLabelPath);
    }

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

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (inspectorDialog.Visible)
            UpdateInspector();

        // Entity label
        if (showEntityLabels)
            UpdateEntityLabels();

        // Performance metrics
        if (performanceMetrics.Visible)
            UpdateMetrics(delta);

        // FPS counter
        if (fpsCounter.Visible)
            UpdateFPS();

        // Parts of the game that aren't the GUI may want to know the actual logical size of our window (for example to
        // check mouse coordinates), so this seems like a sensible place to do that as there's no longer a general
        // overlay manager class
        var size = GetViewportRect();

        if (reportedViewportSize != size)
        {
            GUICommon.Instance.ReportViewportRect(size);
            reportedViewportSize = size;
        }
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false)]
    public void OnPerformanceMetricsToggled()
    {
        performanceMetricsCheckBox.ButtonPressed = !performanceMetricsCheckBox.ButtonPressed;
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
        fpsCheckBox.ButtonPressed = !fpsCheckBox.ButtonPressed;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (DebugCoordinatesPath != null)
            {
                DebugCoordinatesPath.Dispose();
                FPSCheckBoxPath.Dispose();
                FPSLabelPath.Dispose();
                DeltaLabelPath.Dispose();
                MetricsTextPath.Dispose();
                InspectorDialogPath.Dispose();

                PerformanceMetricsCheckBoxPath.Dispose();
                InspectorCheckboxPath.Dispose();
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
        // Standard Godot physics debug
        GetTree().DebugCollisionsHint = state;

        // Custom physics debug
        if (state)
        {
            DebugDrawer.Instance.EnablePhysicsDebug();

            if (!DebugDrawer.Instance.PhysicsDebugDrawAvailable)
            {
                ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("DEBUG_DRAW_NOT_AVAILABLE"), 4);
            }
        }
        else
        {
            DebugDrawer.Instance.DisablePhysicsDebugLevel();
        }
    }

    private void OnEntityLabelCheckBoxToggled(bool state)
    {
        if (showEntityLabels == state)
            return;

        ShowEntityLabels = state;

        if (!state)
        {
            ClearEntityLabels();
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
