using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? TargetTypeSelectorPath;

    [Export]
    public NodePath CompoundsSelectorPath = null!;

    [Export]
    public NodePath CompoundsLabelPath = null!;

    [Export]
    public NodePath SpeciesSelectorPath = null!;

    [Export]
    public NodePath SpeciesLabelPath = null!;

    [Export]
    public NodePath MaximumDistanceSliderPath = null!;

    [Export]
    public NodePath MinimumAmountSliderPath = null!;

    [Export]
    public NodePath MinimumAmountLabelPath = null!;

    [Export]
    public NodePath ColourSelectorPath = null!;

#pragma warning disable CA2213
    private OptionButton targetTypeSelector = null!;
    private OptionButton compoundsSelector = null!;
    private Label compoundLabel = null!;
    private OptionButton speciesSelector = null!;
    private Label speciesLabel = null!;
    private Slider maximumDistanceSlider = null!;
    private Slider minimumAmountSlider = null!;
    private Label minimumAmountLabel = null!;
    private TweakedColourPicker colourSelector = null!;
#pragma warning restore CA2213

    private IReadOnlyList<Compound>? shownCompoundChoices;
    private IReadOnlyList<Species>? shownSpeciesChoices;

    private enum TargetType
    {
        Compound,
        Species,
    }

    public override void _Ready()
    {
        targetTypeSelector = GetNode<OptionButton>(TargetTypeSelectorPath);
        compoundsSelector = GetNode<OptionButton>(CompoundsSelectorPath);
        compoundLabel = GetNode<Label>(CompoundsLabelPath);
        speciesSelector = GetNode<OptionButton>(SpeciesSelectorPath);
        speciesLabel = GetNode<Label>(SpeciesLabelPath);
        maximumDistanceSlider = GetNode<Slider>(MaximumDistanceSliderPath);
        minimumAmountSlider = GetNode<Slider>(MinimumAmountSliderPath);
        minimumAmountLabel = GetNode<Label>(MinimumAmountLabelPath);
        colourSelector = GetNode<TweakedColourPicker>(ColourSelectorPath);

        compoundsSelector.Clear();
        speciesSelector.Clear();

        maximumDistanceSlider.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistanceSlider.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;
        minimumAmountSlider.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmountSlider.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;

        TypeChanged((int)TargetType.Compound);
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame)
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
                SimulationParameters.Instance.GetCompound(Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);
            var defaultConfiguration = new ChemoreceptorUpgrades(defaultCompound, null,
                Constants.CHEMORECEPTOR_RANGE_DEFAULT, Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, defaultCompound.Colour);

            // Assign the default ChemoreceptorUpgrades to the OrganelleUpgrades
            // so we can check if it's been changed at the end
            organelle.Upgrades ??= new OrganelleUpgrades();
            organelle.Upgrades.CustomUpgradeData = defaultConfiguration;

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
        Compound? compoundChoice = null;
        Species? speciesChoice = null;

        if (targetTypeSelector.Selected == (int)TargetType.Compound)
        {
            compoundChoice = shownCompoundChoices[compoundsSelector.Selected];
        }
        else if (targetTypeSelector.Selected == (int)TargetType.Species)
        {
            speciesChoice = shownSpeciesChoices[speciesSelector.Selected];
        }

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(
            compoundChoice, speciesChoice,
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
        }

        ApplySelectionColour();
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(400, 320);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TargetTypeSelectorPath != null)
            {
                TargetTypeSelectorPath.Dispose();
                CompoundsSelectorPath.Dispose();
                CompoundsLabelPath.Dispose();
                SpeciesSelectorPath.Dispose();
                SpeciesLabelPath.Dispose();
                MaximumDistanceSliderPath.Dispose();
                MinimumAmountSliderPath.Dispose();
                MinimumAmountLabelPath.Dispose();
                ColourSelectorPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Sets the GUI up to reflect an existing configuration
    /// </summary>
    private void LoadConfiguration(ChemoreceptorUpgrades configuration,
        IReadOnlyList<Compound> shownCompoundChoices, IReadOnlyList<Species> shownSpeciesChoices)
    {
        if (configuration.TargetCompound != null)
        {
            TypeChanged((int)TargetType.Compound);
            targetTypeSelector.Selected = (int)TargetType.Compound;
            compoundsSelector.Selected = shownCompoundChoices.FindIndex(c => c == configuration.TargetCompound);
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
