using System;
using Godot;

/// <summary>
///   Handles showing the cell type preview on a selection button
/// </summary>
public partial class CellTypeSelection : MicrobePartSelection
{
    private CellType? cellType;

#pragma warning disable CA2213
    private Texture2D placeholderIcon = null!;

    private Texture2D? cellImage;

    [Export]
    private ProgressBar atpProductionBar = null!;

    [Export]
    private ProgressBar atpConsumptionBar = null!;

    [Export]
    private Control atpBalanceWarningBadge = null!;
#pragma warning restore CA2213

    private IImageTask? imageTask;

    private bool enableATPBalanceDisplay = true;

    private float energyProduction;
    private float energyConsumption;
    private float maxEnergyValue;

    public bool EnableATPBalanceBars
    {
        get => enableATPBalanceDisplay;
        set
        {
            if (enableATPBalanceDisplay == value)
                return;

            enableATPBalanceDisplay = value;

            UpdateATPBalanceBarVisibility();
        }
    }

    public CellType CellType
    {
        get => cellType ?? throw new InvalidOperationException("No cell type set");
        set
        {
            if (cellType == value)
                return;

            ReportTypeChanged();
            cellType = value;
        }
    }

    public float EnergyProduction
    {
        get => energyProduction;
        set
        {
            energyProduction = value;

            UpdateProductionBar();
            UpdateWarningBadge();
        }
    }

    public float EnergyConsumption
    {
        get => energyConsumption;
        set
        {
            energyConsumption = value;

            UpdateConsumptionBar();
            UpdateWarningBadge();
        }
    }

    /// <summary>
    ///   The maximum production/consumption across all other cell type selection buttons.
    /// </summary>
    public float MaxEnergyValue
    {
        get => maxEnergyValue;
        set
        {
            maxEnergyValue = value;

            UpdateATPBalanceDisplay();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        placeholderIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/IconGenerating.png");

        PartIcon = placeholderIcon;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (cellImage != null)
            return;

        if (imageTask != null)
        {
            if (imageTask.Finished)
            {
                cellImage = imageTask.FinalImage;
                PartIcon = cellImage;
            }

            return;
        }

        // We need to generate a cell image
        imageTask = PhotoStudio.Instance.GenerateImage(CellType);

        // Show placeholder while generating
        // TODO: only show placeholder after 10-ish frames if we don't have the real image ready
        PartIcon = placeholderIcon;
    }

    public void ReportTypeChanged()
    {
        // TODO: should this change to using a hash code for the cell type to determine when we actually need to
        // recreate the image (caching layer should probably maybe go into PhotoStudio)
        cellImage = null;
        imageTask = null;
    }

    public void UpdateATPBalanceBarVisibility()
    {
        atpConsumptionBar.Visible = enableATPBalanceDisplay;
        atpProductionBar.Visible = enableATPBalanceDisplay;

        if (!enableATPBalanceDisplay)
            atpBalanceWarningBadge.Visible = false;
    }

    public void SetEnergyBalanceValues(float newProduction, float newConsumption)
    {
        energyProduction = newProduction;
        energyConsumption = newConsumption;

        UpdateATPBalanceDisplay();
    }

    public void UpdateATPBalanceDisplay()
    {
        UpdateProductionBar();
        UpdateConsumptionBar();

        UpdateWarningBadge();
    }

    private void UpdateWarningBadge()
    {
        atpBalanceWarningBadge.Visible = enableATPBalanceDisplay && energyConsumption > energyProduction;
    }

    private void UpdateProductionBar()
    {
        atpProductionBar.Value = 100.0f * energyProduction / maxEnergyValue;

        atpProductionBar.TooltipText = Localization.Translate("CELL_TYPE_BUTTON_ATP_PRODUCTION")
            .FormatSafe(MathF.Round(energyProduction, 2));
    }

    private void UpdateConsumptionBar()
    {
        atpConsumptionBar.Value = 100.0f * energyConsumption / maxEnergyValue;

        atpConsumptionBar.TooltipText = Localization.Translate("CELL_TYPE_BUTTON_ATP_CONSUMPTION")
            .FormatSafe(MathF.Round(energyConsumption, 2));
    }
}
