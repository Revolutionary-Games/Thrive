using System;
using Godot;

/// <summary>
///   Contains a short results for a species from auto-evo. Can be pressed to request more info for the species.
/// </summary>
public partial class SpeciesResultButton : Button
{
#pragma warning disable CA2213
    [Export]
    private Label nameLabel = null!;

    [Export]
    private SpeciesPreview preview = null!;

    [Export]
    private Control newIndicator = null!;

    [Export]
    private Control partialExtinctionIndicator = null!;

    [Export]
    private Control mutatedIndicator = null!;

    [Export]
    private Label resultPatchPopulation = null!;

    [Export]
    private Label resultPatchPopulationDifference = null!;

    [Export]
    private Label globalPopulationLabel = null!;

    [Export]
    private Container globalPopulationContainer = null!;

    [Export]
    private Control buttonContentContainer = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
    }

    public override Vector2 _GetMinimumSize()
    {
        return buttonContentContainer._GetMinimumSize();
    }

    public void DisplaySpecies(Species species)
    {
        preview.PreviewSpecies = species;

        // TODO: underline when showing the species?
        nameLabel.Text = species.FormattedName;

        throw new NotImplementedException();
    }
}
