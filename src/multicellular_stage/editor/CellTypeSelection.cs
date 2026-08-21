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
            if (ReferenceEquals(cellType, value))
                return;

            cellType = value;
            cellTypePreview.PreviewCellType = cellType;
        }
    }

    public void ReportTypeChanged()
    {
        cellTypePreview.PreviewCellType = cellType;
    }

    private void UpdateWarningBadge()
    {
        atpBalanceWarningBadge.Visible = showInsufficientATPWarning;
    }
}
