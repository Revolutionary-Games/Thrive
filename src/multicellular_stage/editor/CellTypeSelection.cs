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
#pragma warning restore CA2213

    private IImageTask? imageTask;

    private float energyProduction;
    private float energyConsumption;

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
        }
    }

    public float EnergyConsumption
    {
        get => energyConsumption;
        set
        {
            energyConsumption = value;

            UpdateConsumptionBar();
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

    public void UpdateProductionBar()
    {
        atpProductionBar.Value = EnergyProduction;
    }

    public void UpdateConsumptionBar()
    {
        atpConsumptionBar.Value = energyConsumption;
    }
}
