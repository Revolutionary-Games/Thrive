using System;
using Godot;

public class SpeciesDetailsPanel : PanelContainer
{
    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    [Export]
    public NodePath FossilisationButtonPath = null!;

    [Export]
    public NodePath FossilisationDialogPath = null!;

    private CustomRichTextLabel speciesDetailsLabel = null!;
    private SpeciesPreview? speciesPreview;
    private CellHexesPreview hexesPreview = null!;
    private Button fossilisationButton = null!;
    private FossilisationDialog fossilisationDialog = null!;

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            if (previewSpecies == value)
                return;

            previewSpecies = value;

            if (previewSpecies != null)
                UpdateSpeciesPreview();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);
        fossilisationButton = GetNode<Button>(FossilisationButtonPath);
        fossilisationDialog = GetNode<FossilisationDialog>(FossilisationDialogPath);

        if (previewSpecies != null)
            UpdateSpeciesPreview();
    }

    /// <summary>
    ///   Updates displayed species information based on the set preview species.
    /// </summary>
    private void UpdateSpeciesPreview()
    {
        if (speciesPreview == null)
            return;

        speciesPreview.PreviewSpecies = PreviewSpecies;

        if (PreviewSpecies is MicrobeSpecies microbeSpecies)
        {
            hexesPreview.PreviewSpecies = microbeSpecies;
            fossilisationButton.Disabled = false;
        }
        else
        {
            fossilisationButton.Disabled = true;
            GD.PrintErr("Unknown species type to preview: ", PreviewSpecies);
        }

        speciesDetailsLabel.ExtendedBbcode = PreviewSpecies?.GetDetailString();
    }

    private void OnFossilisePressed()
    {
        if (speciesPreview!.PreviewSpecies is not MicrobeSpecies)
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");

        fossilisationDialog.SelectedSpecies = speciesPreview.PreviewSpecies;
        fossilisationDialog.PopupCenteredShrink();
    }
}
