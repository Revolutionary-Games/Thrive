using System;
using System.Collections.Generic;
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
    private OptionButton targetTypes = null!;
    private OptionButton compounds = null!;
    private Label compoundLabel = null!;
    private OptionButton species = null!;
    private Label speciesLabel = null!;
    private Slider maximumDistance = null!;
    private Slider minimumAmount = null!;
    private Label minimumAmountLabel = null!;
    private TweakedColourPicker colour = null!;
#pragma warning restore CA2213

    private List<Compound>? shownCompoundChoices;
    private List<Species>? shownSpeciesChoices;

    private enum TargetType
    {
        Compound,
        Species,
    }

    public override void _Ready()
    {
        targetTypes = GetNode<OptionButton>(TargetTypeSelectorPath);
        compounds = GetNode<OptionButton>(CompoundsSelectorPath);
        compoundLabel = GetNode<Label>(CompoundsLabelPath);
        species = GetNode<OptionButton>(SpeciesSelectorPath);
        speciesLabel = GetNode<Label>(SpeciesLabelPath);
        maximumDistance = GetNode<Slider>(MaximumDistanceSliderPath);
        minimumAmount = GetNode<Slider>(MinimumAmountSliderPath);
        minimumAmountLabel = GetNode<Label>(MinimumAmountLabelPath);
        colour = GetNode<TweakedColourPicker>(ColourSelectorPath);

        compounds.Clear();
        species.Clear();

        maximumDistance.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistance.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;
        minimumAmount.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmount.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;

        TypeChanged((int)TargetType.Compound);
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame)
    {
        shownCompoundChoices = SimulationParameters.Instance.GetCloudCompounds();
        shownCompoundChoices.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        foreach (var choice in shownCompoundChoices)
        {
            compounds.AddItem(choice.Name);
        }

        shownSpeciesChoices = currentGame.GameWorld.Map.FindAllSpeciesWithPopulation();
        shownSpeciesChoices.Sort((x, y) => string.Compare(x.FormattedName, y.FormattedName,
            StringComparison.OrdinalIgnoreCase));

        foreach (var choice in shownSpeciesChoices)
        {
            species.AddItem(choice.FormattedName);
        }

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            LoadConfiguration(configuration, shownCompoundChoices, shownSpeciesChoices);
        }
        else
        {
            var defaultCompound = SimulationParameters.Instance.GetCompound(Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);
            var defaultConfiguration = new ChemoreceptorUpgrades(defaultCompound, null,
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
        if (targetTypes.Selected == -1)
            targetTypes.Selected = (int)TargetType.Compound;
        if (compounds.Selected == -1)
            compounds.Selected = 0;
        if (species.Selected == -1)
            species.Selected = 0;

        // Only one type of object can be detected
        Compound? compoundChoice = null;
        Species? speciesChoice = null;

        if (targetTypes.Selected == (int)TargetType.Compound)
        {
            compoundChoice = shownCompoundChoices[compounds.Selected];
        }
        else if (targetTypes.Selected == (int)TargetType.Species)
        {
            speciesChoice = shownSpeciesChoices[species.Selected];
        }

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(
            compoundChoice, speciesChoice,
            (float)maximumDistance.Value, (float)minimumAmount.Value, colour.Color);
        return true;
    }

    public void SelectionChanged(int index)
    {
        ApplySelectionColour();
    }

    public void TypeChanged(int index)
    {
        // Make either species or compound menu visible
        species.Visible = false;
        speciesLabel.Visible = false;
        compounds.Visible = false;
        compoundLabel.Visible = false;
        minimumAmount.Visible = false;
        minimumAmountLabel.Visible = false;

        switch ((TargetType)index)
        {
            case TargetType.Compound:
                compounds.Visible = true;
                compoundLabel.Visible = true;
                minimumAmount.Visible = true;
                minimumAmountLabel.Visible = true;
                break;
            case TargetType.Species:
                species.Visible = true;
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
        List<Compound> shownCompoundChoices, List<Species> shownSpeciesChoices)
    {
        if (configuration.TargetCompound != null)
        {
            TypeChanged((int)TargetType.Compound);
            targetTypes.Selected = (int)TargetType.Compound;
            compounds.Selected = shownCompoundChoices.FindIndex(c => c == configuration.TargetCompound);
        }
        else if (configuration.TargetSpecies != null)
        {
            TypeChanged((int)TargetType.Species);
            targetTypes.Selected = (int)TargetType.Species;
            species.Selected = shownSpeciesChoices.FindIndex(c => c == configuration.TargetSpecies);
        }

        maximumDistance.Value = configuration.SearchRange;
        minimumAmount.Value = configuration.SearchAmount;
        colour.Color = configuration.LineColour;
    }

    /// <summary>
    ///   Applies the color of a selected target to the color picker
    /// </summary>
    private void ApplySelectionColour()
    {
        if (targetTypes.Selected == (int)TargetType.Compound && shownCompoundChoices != null
            && compounds.Selected >= 0)
            colour.Color = shownCompoundChoices[compounds.Selected].Colour;
        if (targetTypes.Selected == (int)TargetType.Species && shownSpeciesChoices != null
            && species.Selected >= 0)
            colour.Color = shownSpeciesChoices[species.Selected].Colour;
    }
}
