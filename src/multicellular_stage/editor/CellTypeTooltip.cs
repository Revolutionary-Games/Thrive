using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A tooltip displaying info about a cell type
/// </summary>
public partial class CellTypeTooltip : ControlWithInput, ICustomToolTip
{
#pragma warning disable CA2213
    [Export]
    private Label nameLabel = null!;

    [Export]
    private Label mpLabel = null!;

    [Export]
    private CompoundBalanceDisplay compoundBalanceDisplay = null!;

    [Export]
    private CellTypeStatLabel healthLabel = null!;

    [Export]
    private CellTypeStatLabel storageLabel = null!;

    [Export]
    private CellTypeStatLabel speedLabel = null!;

    [Export]
    private CellTypeStatLabel rotationSpeedLabel = null!;

    [Export]
    private CellTypeStatLabel sizeLabel = null!;

    [Export]
    private CellTypeStatLabel digestionSpeedLabel = null!;
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

    /// <summary>
    ///   The displayable name/title for this tooltip.
    /// </summary>
    public string DisplayName
    {
        get => displayName ?? "CellTypeTooltip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    /// <summary>
    ///   The main message the tooltip contains.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///   Used to delay how long it takes for this tooltip to appear. Set this to zero for no delay.
    /// </summary>
    public float DisplayDelay { get; set; }

    /// <summary>
    ///   Where a tooltip should be positioned on display.
    /// </summary>
    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    /// <summary>
    ///   How a tooltip should transition on becoming visible and on being hidden.
    /// </summary>
    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; } = true;

    /// <summary>
    ///   Control node of this tooltip
    /// </summary>
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
        digestionSpeedLabel.Value = MathF.Round(value, 2);
    }
}
