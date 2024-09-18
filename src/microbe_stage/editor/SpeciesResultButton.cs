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
        // Ensure minimum size is set
        CustomMinimumSize = _GetMinimumSize();
    }

    public override Vector2 _GetMinimumSize()
    {
        return buttonContentContainer.CustomMinimumSize;
    }

    public void DisplaySpecies(Species species)
    {
        preview.PreviewSpecies = species;

        // TODO: underline when showing the species?
        nameLabel.Text = species.FormattedName;

        // TODO: showing the indicators
    }

    public void DisplayPopulation(long newPopulation, long oldPopulation)
    {
        partialExtinctionIndicator.Visible = newPopulation < 1;

        resultPatchPopulation.Text = newPopulation.ToString();

        var difference = newPopulation - oldPopulation;
        resultPatchPopulationDifference.Text = difference.ToString();

        // TODO: change text colour
    }

    public void HideGlobalPopulation()
    {
        globalPopulationContainer.Visible = false;
    }

    private void OnContentSizeChanged()
    {
        var newMinSize = _GetMinimumSize();

        if (newMinSize != CustomMinimumSize)
            CustomMinimumSize = newMinSize;
    }
}
