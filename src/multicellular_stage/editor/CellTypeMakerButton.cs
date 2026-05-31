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
    private SpeciesPreview speciesPreview = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnClickedEventHandler();

    public void OnMainButtonClicked()
    {
        EmitSignal(SignalName.OnClicked);
    }

    public override void _Ready()
    {
        UpdateDisplayedCellType(null);
    }


    public void UpdateDisplayedCellType(CellType? assignedCellType)
    {
        if (assignedCellType == null)
        {
            sporeNameLabel.Text = Localization.Translate("CHOOSE_CELL_TYPE");

            addCellTypeIcon.Visible = true;
            speciesPreview.Visible = false;
            return;
        }

        sporeNameLabel.Text = assignedCellType.ReadableName;

        addCellTypeIcon.Visible = false;
        speciesPreview.Visible = true;

        // TBD: Implement this correctly; with a small refactor around cell type texture generation or something
        // speciesPreview.PreviewSpecies
    }
}
