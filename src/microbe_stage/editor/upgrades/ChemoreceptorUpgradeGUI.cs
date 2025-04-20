using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Upgrade GUI for the chemoreceptor to configure what it detects
/// </summary>
public partial class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
#pragma warning disable CA2213
    [Export]
    private OptionButton targetTypeSelector = null!;
    [Export]
    private OptionButton compoundsSelector = null!;
    [Export]
    private Label compoundLabel = null!;
    [Export]
    private OptionButton speciesSelector = null!;
    [Export]
    private Label speciesLabel = null!;
    [Export]
    private Slider maximumDistanceSlider = null!;
    [Export]
    private Slider minimumAmountSlider = null!;
    [Export]
    private Label minimumAmountLabel = null!;
    [Export]
    private TweakedColourPicker colourSelector = null!;
#pragma warning restore CA2213

    private IReadOnlyList<CompoundDefinition>? shownCompoundChoices;
    private IReadOnlyList<Species>? shownSpeciesChoices;

    private enum TargetType
    {
        // Values here must match what is set in the Godot editor
        Compound = 0,
        Species = 1,
    }

    public override void _Ready()
    {
        compoundsSelector.Clear();
        speciesSelector.Clear();

        maximumDistanceSlider.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistanceSlider.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;
        minimumAmountSlider.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmountSlider.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;

        TypeChanged((int)TargetType.Compound);
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        shownCompoundChoices = SimulationParameters.Instance.GetCloudCompounds()
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var choice in shownCompoundChoices)
        {
            compoundsSelector.AddItem(choice.Name);
        }

        shownSpeciesChoices = currentGame.GameWorld.Map.FindAllSpeciesWithPopulation()
            .OrderBy(c => c.FormattedName, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var choice in shownSpeciesChoices)
        {
            speciesSelector.AddItem(choice.FormattedName);
        }

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            LoadConfiguration(configuration, shownCompoundChoices, shownSpeciesChoices);
        }
        else
        {
            var defaultCompound =
                SimulationParameters.Instance.GetCompoundDefinition(Constants.CHEMORECEPTOR_DEFAULT_COMPOUND);
            var defaultConfiguration = new ChemoreceptorUpgrades(defaultCompound.ID, null,
                Constants.CHEMORECEPTOR_RANGE_DEFAULT, Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, defaultCompound.Colour);
            LoadConfiguration(defaultConfiguration, shownCompoundChoices, shownSpeciesChoices);
        }
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (shownCompoundChoices == null || shownSpeciesChoices == null)
        {
            GD.PrintErr("Chemoreceptor upgrade GUI was not opened properly");
            return false;
        }

        // Force some type/compound/species to be selected
        if (targetTypeSelector.Selected == -1)
            targetTypeSelector.Selected = (int)TargetType.Compound;
        if (compoundsSelector.Selected == -1)
            compoundsSelector.Selected = 0;
        if (speciesSelector.Selected == -1)
            speciesSelector.Selected = 0;

        // Only one type of object can be detected
        Compound compoundChoice = Compound.Invalid;
        Species? speciesChoice = null;

        if (targetTypeSelector.Selected == (int)TargetType.Compound)
        {
            compoundChoice = shownCompoundChoices[compoundsSelector.Selected].ID;
        }
        else if (targetTypeSelector.Selected == (int)TargetType.Species)
        {
            speciesChoice = shownSpeciesChoices[speciesSelector.Selected];
        }

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(compoundChoice, speciesChoice,
            (float)maximumDistanceSlider.Value, (float)minimumAmountSlider.Value, colourSelector.Color);
        return true;
    }

    public void SelectionChanged(int index)
    {
        ApplySelectionColour();
    }

    public void TypeChanged(int index)
    {
        // Make either species or compound menu visible
        speciesSelector.Visible = false;
        speciesLabel.Visible = false;
        compoundsSelector.Visible = false;
        compoundLabel.Visible = false;
        minimumAmountSlider.Visible = false;
        minimumAmountLabel.Visible = false;

        switch ((TargetType)index)
        {
            case TargetType.Compound:
                compoundsSelector.Visible = true;
                compoundLabel.Visible = true;
                minimumAmountSlider.Visible = true;
                minimumAmountLabel.Visible = true;
                break;
            case TargetType.Species:
                speciesSelector.Visible = true;
                speciesLabel.Visible = true;
                break;
            default:
                GD.PrintErr("Unknown type to show in chemoreceptor upgrade GUI");
                break;
        }

        ApplySelectionColour();
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(400, 320);
    }

    /// <summary>
    ///   Sets the GUI up to reflect an existing configuration
    /// </summary>
    private void LoadConfiguration(ChemoreceptorUpgrades configuration,
        IReadOnlyList<CompoundDefinition> shownCompoundChoices, IReadOnlyList<Species> shownSpeciesChoices)
    {
        if (configuration.TargetCompound != Compound.Invalid)
        {
            TypeChanged((int)TargetType.Compound);
            targetTypeSelector.Selected = (int)TargetType.Compound;
            compoundsSelector.Selected = shownCompoundChoices.FindIndex(c => c.ID == configuration.TargetCompound);
        }
        else if (configuration.TargetSpecies != null)
        {
            TypeChanged((int)TargetType.Species);
            targetTypeSelector.Selected = (int)TargetType.Species;
            speciesSelector.Selected = shownSpeciesChoices.FindIndex(c => c == configuration.TargetSpecies);
        }

        maximumDistanceSlider.Value = configuration.SearchRange;
        minimumAmountSlider.Value = configuration.SearchAmount;
        colourSelector.Color = configuration.LineColour;
    }

    /// <summary>
    ///   Applies the color of a selected target to the color picker
    /// </summary>
    private void ApplySelectionColour()
    {
        if (targetTypeSelector.Selected == (int)TargetType.Compound && shownCompoundChoices != null
            && compoundsSelector.Selected >= 0)
        {
            colourSelector.Color = shownCompoundChoices[compoundsSelector.Selected].Colour;
        }

        if (targetTypeSelector.Selected == (int)TargetType.Species && shownSpeciesChoices != null
            && speciesSelector.Selected >= 0)
        {
            colourSelector.Color = shownSpeciesChoices[speciesSelector.Selected].Colour;
        }
    }
}
