using Godot;

/// <summary>
///   Panel that displays warnings related to the organism's tolerance levels.
/// </summary>
public partial class ToleranceWarningsPanel : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    public Label OsmoregulationCostValue = null!;

    [Export]
    public Label BioprocessSpeedValue = null!;

    [Export]
    public Label HealthValue = null!;

    [Export]
    public HBoxContainer OsmoregulationCostContainer = null!;

    [Export]
    public HBoxContainer BioprocessSpeedContainer = null!;

    [Export]
    public HBoxContainer HealthContainer = null!;

    [Export]
    public Label TemperatureConditionLabel = null!;

    [Export]
    public Label PressureConditionLabel = null!;

    [Export]
    public Label OxygenConditionLabel = null!;

    [Export]
    public Label UVConditionLabel = null!;

    [Export]
    public HBoxContainer TemperatureConditionContainer = null!;

    [Export]
    public HBoxContainer PressureConditionContainer = null!;

    [Export]
    public HBoxContainer OxygenConditionContainer = null!;

    [Export]
    public HBoxContainer UVConditionContainer = null!;
#pragma warning enable CA2213
}
