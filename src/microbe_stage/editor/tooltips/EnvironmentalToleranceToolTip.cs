using System;
using System.Globalization;
using Godot;

/// <summary>
///   Tooltip on the tolerances section of the editor. This shows information of the current state and advice.
/// </summary>
public partial class EnvironmentalToleranceToolTip : Control, ICustomToolTip
{
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

    [Export]
    private LabelSettings goodStatFont = null!;

    [Export]
    private LabelSettings badStatFont = null!;

    private LabelSettings defaultStatFont = null!;
#pragma warning restore CA2213

    private string? displayName;
    private float mpCost;

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

        defaultStatFont = healthLabel.ModifierValueFont;

        UpdateName();
        UpdateMPCost();
    }

    public void UpdateStats(ResolvedMicrobeTolerances tolerances)
    {
        // Convert stats to percentages and show
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        osmoregulationLabel.ModifierValue =
            percentageFormat.FormatSafe(StringUtils.FormatPositiveWithLeadingPlus(MathF.Round(
                (tolerances.OsmoregulationModifier - 1) * 100, 2)));
        healthLabel.ModifierValue =
            percentageFormat.FormatSafe(
                StringUtils.FormatPositiveWithLeadingPlus(MathF.Round((tolerances.HealthModifier - 1) * 100, 1)));
        processSpeedLabel.ModifierValue =
            percentageFormat.FormatSafe(
                StringUtils.FormatPositiveWithLeadingPlus(MathF.Round((tolerances.ProcessSpeedModifier - 1) * 100, 2)));

        bool adapted = true;

        // And apply colours to good and bad stats
        if (tolerances.OsmoregulationModifier > 1)
        {
            osmoregulationLabel.ModifierValueFont = badStatFont;
            adapted = false;
        }
        else if (tolerances.OsmoregulationModifier < 1)
        {
            osmoregulationLabel.ModifierValueFont = goodStatFont;
        }
        else
        {
            osmoregulationLabel.ModifierValueFont = defaultStatFont;
        }

        if (tolerances.HealthModifier < 1)
        {
            healthLabel.ModifierValueFont = badStatFont;
            adapted = false;
        }
        else if (tolerances.HealthModifier > 1)
        {
            healthLabel.ModifierValueFont = goodStatFont;
        }
        else
        {
            healthLabel.ModifierValueFont = defaultStatFont;
        }

        if (tolerances.ProcessSpeedModifier < 1)
        {
            processSpeedLabel.ModifierValueFont = badStatFont;
            adapted = false;
        }
        else if (tolerances.ProcessSpeedModifier > 1)
        {
            processSpeedLabel.ModifierValueFont = goodStatFont;
        }
        else
        {
            processSpeedLabel.ModifierValueFont = defaultStatFont;
        }

        badlyAdaptedWarning.Visible = !adapted;
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
