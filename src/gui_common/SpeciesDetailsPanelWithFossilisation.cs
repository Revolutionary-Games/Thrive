using System;
using Godot;

public class SpeciesDetailsPanelWithFossilisation : VBoxContainer
{
    [Export]
    public NodePath SpeciesDetailsPanelPath = null!;

    [Export]
    public NodePath FossilisationButtonPath = null!;

    [Export]
    public NodePath FossilisationDialogPath = null!;

    private SpeciesDetailsPanel speciesDetailsPanel = null!;
    private Button? fossilisationButton;
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
            speciesDetailsPanel.PreviewSpecies = value;

            UpdateFossilisationButtonState();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesDetailsPanel = GetNode<SpeciesDetailsPanel>(SpeciesDetailsPanelPath);
        fossilisationButton = GetNode<Button>(FossilisationButtonPath);
        fossilisationDialog = GetNode<FossilisationDialog>(FossilisationDialogPath);

        UpdateFossilisationButtonState();
    }

    private void OnFossilisePressed()
    {
        if (previewSpecies is not MicrobeSpecies)
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");

        fossilisationDialog.SelectedSpecies = previewSpecies;
        fossilisationDialog.PopupCenteredShrink();
    }

    private void UpdateFossilisationButtonState()
    {
        if (fossilisationButton != null)
            fossilisationButton.Disabled = previewSpecies is not MicrobeSpecies;
    }
}
