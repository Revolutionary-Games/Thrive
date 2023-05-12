using System;
using Godot;

/// <summary>
///   This is an extended version of <see cref="SpeciesDetailsPanel"/>,
///   adding fossilisation functionalities, including a button and a popup.
/// </summary>
public class SpeciesDetailsPanelWithFossilisation : VBoxContainer
{
    [Export]
    public NodePath? SpeciesDetailsPanelPath;

    [Export]
    public NodePath FossilisationButtonPath = null!;

    [Export]
    public NodePath FossilisationDialogPath = null!;

#pragma warning disable CA2213
    private SpeciesDetailsPanel? speciesDetailsPanel;
    private Button fossilisationButton = null!;
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
            {
                speciesDetailsPanel.PreviewSpecies = value;
                UpdateFossilisationButtonState();
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesDetailsPanel = GetNode<SpeciesDetailsPanel>(SpeciesDetailsPanelPath);
        fossilisationButton = GetNode<Button>(FossilisationButtonPath);
        fossilisationDialog = GetNode<FossilisationDialog>(FossilisationDialogPath);

        speciesDetailsPanel.PreviewSpecies = previewSpecies;

        UpdateFossilisationButtonState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SpeciesDetailsPanelPath != null)
            {
                FossilisationButtonPath.Dispose();
                FossilisationDialogPath.Dispose();
                SpeciesDetailsPanelPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
