using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Override functions, debug panel
/// </summary>
[GodotAutoload]
public partial class DebugOverlays : Control
{
    private static DebugOverlays? instance;

#pragma warning disable CA2213
    [Export]
    private Label debugCoordinates = null!;

    [Export]
    private CustomWindow inspectorDialog = null!;

    [Export]
    private CustomWindow debugPanelDialog = null!;

    [Export]
    private CheckBox fpsCheckBox = null!;

    [Export]
    private CheckBox performanceMetricsCheckBox = null!;

    [Export]
    private CheckBox inspectorCheckbox = null!;

    [Export]
    private Control fpsCounter = null!;

    [Export]
    private CustomWindow performanceMetrics = null!;

    [Export]
    private Control labelsLayer = null!;
#pragma warning restore CA2213

    private Rect2? reportedViewportSize;

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

        if (instance == this)
            instance = null;
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

    private void OnPerformanceMetricsCheckBoxToggled(bool state)
    {
        PerformanceMetricsVisible = state;
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
                ToolTipManager.Instance.ShowPopup(Localization.Translate("DEBUG_DRAW_NOT_AVAILABLE"), 4);
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
