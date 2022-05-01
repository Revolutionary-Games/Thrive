using Godot;

public class DebugPanel : Control
{
    [Export]
    private NodePath fpsCheckBoxPath = null!;

    [Export]
    private NodePath performanceMetricsCheckBoxPath = null!;

    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;

    private FPSCounter fpsCounter = null!;
    private PerformanceMetrics performanceMetrics = null!;

    public override void _Ready()
    {
        fpsCheckBox = GetNode<CustomCheckBox>(fpsCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(performanceMetricsCheckBoxPath);

        fpsCounter = GetNode<FPSCounter>("FPSCounter");
        performanceMetrics = GetNode<PerformanceMetrics>("PerformanceMetrics");
        base._Ready();
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false)]
    public void OnPerformanceMetricsKeyToggled()
    {
        performanceMetricsCheckBox.Pressed = !performanceMetricsCheckBox.Pressed;
    }

    [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
    public void OnFpsKeyToggled()
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
}
