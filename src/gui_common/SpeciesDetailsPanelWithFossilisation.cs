using System;
using Godot;

/// <summary>
///   This is an extended version of <see cref="SpeciesDetailsPanel"/>,
///   adding fossilisation functionalities, including a button and a popup.
/// </summary>
public partial class SpeciesDetailsPanelWithFossilisation : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private SpeciesDetailsPanel? speciesDetailsPanel;

    [Export]
    private Button fossilisationButton = null!;

    [Export]
    private FossilisationDialog fossilisationDialog = null!;
#pragma warning restore CA2213

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            if (previewSpecies == value)
                return;

            previewSpecies = value;

            if (speciesDetailsPanel != null)
                speciesDetailsPanel.PreviewSpecies = value;

            UpdateFossilisationButtonState();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesDetailsPanel!.PreviewSpecies = previewSpecies;

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
        fossilisationButton.Disabled = previewSpecies is not MicrobeSpecies;
    }
}
