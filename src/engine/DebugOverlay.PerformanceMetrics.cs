using Godot;
using Nito.Collections;

/// <summary>
///   Partial class: Performance metrics
/// </summary>
public partial class DebugOverlay
{
    [Export]
    public NodePath FPSLabelPath = null!;

    [Export]
    public NodePath DeltaLabelPath = null!;

    [Export]
    public NodePath MetricsTextPath = null!;

    // TODO: make this time based
    private const int SpawnHistoryLength = 300;

    private readonly Deque<int> spawnHistory = new(SpawnHistoryLength);
    private readonly Deque<int> despawnHistory = new(SpawnHistoryLength);

    private Label fpsLabel = null!;
    private Label deltaLabel = null!;
    private Label metricsText = null!;

    private int entities;
    private int children;
    private int currentSpawned;
    private int currentDespawned;

    public bool PerformanceMetricsVisible
    {
        get => performanceMetrics.Visible;
        private set
        {
            if (performanceMetricsCheckBox.Pressed == value)
                return;

            performanceMetricsCheckBox.Pressed = value;
        }
    }

    public void ReportEntities(int totalEntities, int otherChildren)
    {
        entities = totalEntities;
        children = otherChildren;
    }

    public void ReportSpawns(int newSpawns)
    {
        currentSpawned += newSpawns;
    }

    public void ReportDespawns(int newDespawns)
    {
        currentDespawned += newDespawns;
    }
}
