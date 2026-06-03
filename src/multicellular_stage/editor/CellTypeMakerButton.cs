using System;
using Godot;

/// <summary>
///   Allows creating new cell types for things like spores.
/// </summary>
public partial class CellTypeMakerButton : Control
{
#pragma warning disable CA2213
    [Export]
    private Label sporeNameLabel = null!;

    [Export]
    private TextureRect addCellTypeIcon = null!;

    [Export]
    private CellTypePreview cellTypePreview = null!;

    [Export]
    private Control resetButton = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnClickedEventHandler();

    [Signal]
    public delegate void OnResetClickedEventHandler();

    public override void _Ready()
    {
        UpdateDisplayedCellType(null);
    }

    public void OnMainButtonClicked()
    {
        EmitSignal(SignalName.OnClicked);
    }

    public void OnResetButtonClicked()
    {
        EmitSignal(SignalName.OnResetClicked);
    }

    public void UpdateDisplayedCellType(CellType? assignedCellType)
    {
        if (assignedCellType == null)
        {
            sporeNameLabel.Text = Localization.Translate("CLICK_TO_SET_CELL_TYPE");

            addCellTypeIcon.Visible = true;
            cellTypePreview.Visible = false;

            resetButton.Visible = false;
            return;
        }

        sporeNameLabel.Text = assignedCellType.ReadableName;

        addCellTypeIcon.Visible = false;
        cellTypePreview.Visible = true;

        resetButton.Visible = true;

        cellTypePreview.PreviewCellType = assignedCellType;
    }
}
