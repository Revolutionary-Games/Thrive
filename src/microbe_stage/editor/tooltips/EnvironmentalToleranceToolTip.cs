using System.Globalization;
using Godot;

/// <summary>
///   Tooltip on the tolerances section of the editor. This shows information of the current state and advice.
/// </summary>
public partial class EnvironmentalToleranceToolTip : Control, ICustomToolTip
{
    /// <summary>
    ///   The type this acts as in the tooltip
    /// </summary>
    [Export]
    public StatType ToleranceType = StatType.Temperature;

#pragma warning disable CA2213
    [Export]
    [ExportCategory("Internal")]
    private Label nameLabel = null!;

    [Export]
    private Label mpLabel = null!;

    [Export]
    private CustomRichTextLabel descriptionLabel = null!;

    [Export]
    private ModifierInfoLabel osmoregulationLabel = null!;

    [Export]
    private ModifierInfoLabel healthLabel = null!;

    [Export]
    private ModifierInfoLabel processSpeedLabel = null!;

    [Export]
    private Control badlyAdaptedWarning = null!;
#pragma warning restore CA2213

    private string? displayName;
    private float mpCost;

    public enum StatType
    {
        Temperature,
        Pressure,
        Oxygen,
        UV,
    }

    [Export]
    [ExportCategory("Configuration")]
    public string DisplayName
    {
        get => displayName ?? "EnvironmentalToleranceToolTip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    [Export]
    public float MPCost
    {
        get => mpCost;
        set
        {
            mpCost = value;
            UpdateMPCost();
        }
    }

    [Export]
    public string? Description { get; set; }

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        base._Ready();

        descriptionLabel.ExtendedBbcode = Description;
        badlyAdaptedWarning.Visible = false;

        UpdateName();
        UpdateMPCost();
    }

    private void UpdateName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            nameLabel.Text = displayName;
        }
    }

    private void UpdateMPCost()
    {
        mpLabel.Text = mpCost.ToString("0.##", CultureInfo.CurrentCulture);
    }
}
