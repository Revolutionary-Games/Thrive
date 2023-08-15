using System;
using System.Collections.Generic;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? TargetTypesPath;

    [Export]
    public NodePath CompoundsPath = null!;

    [Export]
    public NodePath CompoundsLabelPath = null!;

    [Export]
    public NodePath SpeciesPath = null!;

    [Export]
    public NodePath SpeciesLabelPath = null!;

    [Export]
    public NodePath MaximumDistancePath = null!;

    [Export]
    public NodePath MinimumAmountPath = null!;

    [Export]
    public NodePath MinimumAmountLabelPath = null!;

    [Export]
    public NodePath ColourPath = null!;

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

    public override void _Ready()
    {
        targetTypes = GetNode<OptionButton>(TargetTypesPath);
        compounds = GetNode<OptionButton>(CompoundsPath);
        compoundLabel = GetNode<Label>(CompoundsLabelPath);
        species = GetNode<OptionButton>(SpeciesPath);
        speciesLabel = GetNode<Label>(SpeciesLabelPath);
        maximumDistance = GetNode<Slider>(MaximumDistancePath);
        minimumAmount = GetNode<Slider>(MinimumAmountPath);
        minimumAmountLabel = GetNode<Label>(MinimumAmountLabelPath);
        colour = GetNode<TweakedColourPicker>(ColourPath);

        compounds.Clear();
        species.Clear();

        maximumDistance.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistance.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;
        minimumAmount.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmount.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;

        TypeChanged(0);
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

        var defaultCompound = SimulationParameters.Instance.GetCompound(Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);
        var defaultConfiguration = new ChemoreceptorUpgrades(defaultCompound, null,
            Constants.CHEMORECEPTOR_RANGE_DEFAULT, Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, defaultCompound.Colour);

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            LoadConfiguration(configuration, shownCompoundChoices, shownSpeciesChoices);
        }
        else
        {
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
            targetTypes.Selected = 0;
        if (compounds.Selected == -1)
            compounds.Selected = 0;
        if (species.Selected == -1)
            species.Selected = 0;

        // Only one type of object can be detected
        Compound? choiceCompound = null;
        Species? choiceSpecies = null;

        if (targetTypes.Selected == 0)
        {
            choiceCompound = shownCompoundChoices[compounds.Selected];
        }
        else if (targetTypes.Selected == 1)
        {
            choiceSpecies = shownSpeciesChoices[species.Selected];
        }

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(
            choiceCompound, choiceSpecies,
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

        switch (index)
        {
            case 0:
                compounds.Visible = true;
                compoundLabel.Visible = true;
                minimumAmount.Visible = true;
                minimumAmountLabel.Visible = true;
                break;
            case 1:
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
            if (TargetTypesPath != null)
            {
                TargetTypesPath.Dispose();
                CompoundsPath.Dispose();
                CompoundsLabelPath.Dispose();
                SpeciesPath.Dispose();
                SpeciesLabelPath.Dispose();
                MaximumDistancePath.Dispose();
                MinimumAmountPath.Dispose();
                MinimumAmountLabelPath.Dispose();
                ColourPath.Dispose();
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
            TypeChanged(0);
            targetTypes.Selected = 0;
            compounds.Selected = shownCompoundChoices.FindIndex(c => c == configuration.TargetCompound);
        }
        else if (configuration.TargetSpecies != null)
        {
            TypeChanged(1);
            targetTypes.Selected = 1;
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
        if (targetTypes.Selected == 0 && shownCompoundChoices != null && compounds.Selected >= 0)
            colour.Color = shownCompoundChoices[compounds.Selected].Colour;
        if (targetTypes.Selected == 1 && shownSpeciesChoices != null && species.Selected >= 0)
            colour.Color = shownSpeciesChoices[species.Selected].Colour;
    }
}
