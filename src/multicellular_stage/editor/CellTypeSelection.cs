using System;
using Godot;

/// <summary>
///   Handles showing the cell type preview on a selection button
/// </summary>
public partial class CellTypeSelection : MicrobePartSelection
{
    private CellType? cellType;

#pragma warning disable CA2213
    [Export]
    private Control atpBalanceWarningBadge = null!;

    [Export]
    private CellTypePreview cellTypePreview = null!;
#pragma warning restore CA2213

    private IImageTask? imageTask;

    private bool showInsufficientATPWarning;

    public bool ShowInsufficientATPWarning
    {
        get => showInsufficientATPWarning;
        set
        {
            showInsufficientATPWarning = value;
            UpdateWarningBadge();
        }
    }

    public CellType CellType
    {
        get => cellType ?? throw new InvalidOperationException("No cell type set");
        set
        {
            if (cellType == value)
                return;

            cellType = value;
            cellTypePreview.PreviewCellType = cellType;
        }
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    private void UpdateWarningBadge()
    {
        atpBalanceWarningBadge.Visible = showInsufficientATPWarning;
    }

    public void ReportTypeChanged()
    {
        cellTypePreview.PreviewCellType = cellType;
    }
}
