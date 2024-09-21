using AutoEvo;
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
    private Control mainContentContainer = null!;

    [Export]
    private LabelSettings positivePopulationChangeSettings = null!;

    [Export]
    private LabelSettings negativePopulationChangeSettings = null!;
#pragma warning restore CA2213

    private Species? shownForSpecies;

    [Signal]
    public delegate void SpeciesSelectedEventHandler(uint id);

    public override void _Ready()
    {
        // Ensure minimum size is set
        CustomMinimumSize = _GetMinimumSize();
    }

    public override Vector2 _GetMinimumSize()
    {
        return mainContentContainer.GetMinimumSize();
    }

    public void DisplaySpecies(RunResults.SpeciesResult speciesResult, bool showNewGloballyIndicator)
    {
        shownForSpecies = speciesResult.Species;
        preview.PreviewSpecies = shownForSpecies;

        // TODO: underline when showing the player species?
        nameLabel.Text = shownForSpecies.FormattedName;

        // Show the right indicators
        mutatedIndicator.Visible = speciesResult.MutatedProperties != null;

        if (showNewGloballyIndicator)
        {
            newIndicator.Visible = speciesResult.NewlyCreated != null;
        }
    }

    public void DisplayPopulation(long newPopulation, long oldPopulation, bool showNewInPatchIndicator)
    {
        partialExtinctionIndicator.Visible = newPopulation < 1;

        resultPatchPopulation.Text = newPopulation.ToString();

        var difference = newPopulation - oldPopulation;

        if (difference < 0)
        {
            resultPatchPopulationDifference.Text = difference.ToString();
            resultPatchPopulationDifference.LabelSettings = negativePopulationChangeSettings;
        }
        else
        {
            resultPatchPopulationDifference.Text = $"+{difference}";
            resultPatchPopulationDifference.LabelSettings = positivePopulationChangeSettings;
        }

        if (showNewInPatchIndicator)
        {
            // New when gets population in this patch that previously didn't have population
            newIndicator.Visible = oldPopulation < 1;
        }
    }

    public void HideGlobalPopulation()
    {
        globalPopulationContainer.Visible = false;
    }

    private void OnPressed()
    {
        if (shownForSpecies == null)
            return;

        // Hide tooltip to not cause it to stay on screen. This kind of seems like an engine bug with the mouse exit
        // not triggering
        OnMouseExit();

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.SpeciesSelected, shownForSpecies.ID);
    }

    private void OnContentSizeChanged()
    {
        var newMinSize = _GetMinimumSize();

        if (newMinSize != CustomMinimumSize)
            CustomMinimumSize = newMinSize;
    }

    private void OnMouseEnter()
    {
        var tooltip = ToolTipManager.Instance.GetToolTip<SpeciesPreviewTooltip>("speciesPreview");
        if (tooltip != null && shownForSpecies != null)
        {
            tooltip.PreviewSpecies = shownForSpecies;
            ToolTipManager.Instance.MainToolTip = tooltip;
            ToolTipManager.Instance.Display = true;
        }
    }

    private void OnMouseExit()
    {
        var tooltip = ToolTipManager.Instance.GetToolTip<SpeciesPreviewTooltip>("speciesPreview");
        if (tooltip != null && ToolTipManager.Instance.MainToolTip == tooltip)
        {
            ToolTipManager.Instance.Display = false;
        }
    }
}
