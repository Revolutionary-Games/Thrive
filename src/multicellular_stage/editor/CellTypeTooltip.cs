using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   A tooltip displaying info about a cell type
/// </summary>
public partial class CellTypeTooltip : Control, ICustomToolTip
{
#pragma warning disable CA2213
    [Export]
    private Label nameLabel = null!;

    [Export]
    private Label mpLabel = null!;

    [Export]
    private CompoundBalanceDisplay compoundBalanceDisplay = null!;

    [Export]
    private CellStatsIndicator healthLabel = null!;

    [Export]
    private CellStatsIndicator storageLabel = null!;

    [Export]
    private CellStatsIndicator speedLabel = null!;

    [Export]
    private CellStatsIndicator rotationSpeedLabel = null!;

    [Export]
    private CellStatsIndicator sizeLabel = null!;

    [Export]
    private CellStatsIndicator digestionSpeedLabel = null!;

    [Export]
    private ProgressBar atpProductionBar = null!;

    [Export]
    private Label atpProductionLabel = null!;

    [Export]
    private ProgressBar atpConsumptionBar = null!;

    [Export]
    private Label atpConsumptionLabel = null!;
#pragma warning restore CA2213

    private string? displayName;
    private double mpCost;

    public double MutationPointCost
    {
        get => mpCost;
        set
        {
            mpCost = value;
            UpdateMPCost();
        }
    }

    public string DisplayName
    {
        get => displayName ?? "CellTypeTooltip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    public string? Description { get; set; }

    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; } = true;

    public Control ToolTipNode => this;

    public void UpdateName()
    {
        nameLabel.Text = DisplayName;
    }

    public void UpdateMPCost()
    {
        mpLabel.Text = StringUtils.FormatMutationPointCost(mpCost);
    }

    public void DisplayCellTypeBalances(Dictionary<Compound, CompoundBalance> balance)
    {
        compoundBalanceDisplay.UpdateBalances(balance, float.MaxValue);
    }

    public void UpdateHealthIndicator(float value)
    {
        healthLabel.Value = MathF.Round(value, 1);
    }

    public void UpdateStorageIndicator(float value)
    {
        storageLabel.Value = MathF.Round(value, 2);
    }

    public void UpdateSpeedIndicator(float value)
    {
        speedLabel.Value = MathF.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(value), 1);
    }

    public void UpdateRotationSpeedIndicator(float value)
    {
        rotationSpeedLabel.Value = MathF.Round(MicrobeInternalCalculations.RotationSpeedToUserReadableNumber(value),
            1);
    }

    public void UpdateSizeIndicator(int value)
    {
        sizeLabel.Value = value;
    }

    public void UpdateDigestionSpeedIndicator(float value)
    {
        digestionSpeedLabel.Format = Localization.Translate("DIGESTION_SPEED_VALUE");
        digestionSpeedLabel.Value = MathF.Round(value, 2);
    }

    public void UpdateATPBalance(float production, float consumption)
    {
        float max = MathF.Max(production, consumption);

        atpProductionBar.Value = production / max;
        atpProductionLabel.Text = MathF.Round(production, 1).ToString(CultureInfo.CurrentCulture);

        atpConsumptionBar.Value = consumption / max;
        atpConsumptionLabel.Text = MathF.Round(consumption, 1).ToString(CultureInfo.CurrentCulture);
    }
}
