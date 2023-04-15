using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? TargetTypesPath;

    [Export]
    public NodePath? CompoundsPath;

    [Export]
    public NodePath? CompoundsLabelPath;

    [Export]
    public NodePath? SpeciesPath;

    [Export]
    public NodePath? SpeciesLabelPath;

    [Export]
    public NodePath MaximumDistancePath = null!;

    [Export]
    public NodePath MinimumAmountPath = null!;

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
        colour = GetNode<TweakedColourPicker>(ColourPath);

        compounds.Clear();
        species.Clear();
        TypeChanged(0);

        maximumDistance.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistance.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;

        minimumAmount.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmount.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame)
    {
        // TODO Translate this
        targetTypes.AddItem("Compound");
        targetTypes.AddItem("Species");

        shownCompoundChoices = SimulationParameters.Instance.GetCloudCompounds();

        foreach (var choice in shownCompoundChoices)
        {
            compounds.AddItem(choice.Name);
        }

        shownSpeciesChoices = currentGame.GameWorld.Map.FindAllSpeciesWithPopulation();

        foreach (var choice in shownSpeciesChoices)
        {
            species.AddItem(choice.Genus + " " + choice.Epithet);
        }

        // Select glucose by default
        var defaultCompoundIndex =
            shownCompoundChoices.FindIndex(c => c.InternalName == Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);

        if (defaultCompoundIndex < 0)
            defaultCompoundIndex = 0;

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            if (configuration.TargetCompound != null)
            {
                TypeChanged(0);
                compounds.Selected = shownCompoundChoices.FindIndex(c => c == configuration.TargetCompound);
            }
            else if (configuration.TargetSpecies != null)
            {
                TypeChanged(1);
                species.Selected = shownSpeciesChoices.FindIndex(c => c == configuration.TargetSpecies);
            }

            maximumDistance.Value = configuration.SearchRange;
            minimumAmount.Value = configuration.SearchAmount;
            colour.Color = configuration.LineColour;
        }
        else
        {
            compounds.Selected = defaultCompoundIndex;
            maximumDistance.Value = Constants.CHEMORECEPTOR_RANGE_DEFAULT;
            minimumAmount.Value = Constants.CHEMORECEPTOR_AMOUNT_DEFAULT;
            colour.Color = shownCompoundChoices[defaultCompoundIndex].Colour;
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

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(400, 320);
    }

    public void CompoundChanged(int index)
    {
        // If the currently selected colour is in the shownChoices list change the colour to the colour of the newly
        // selected compound to make setting up chemoreceptors easier
        if (shownCompoundChoices?.Any(c => c.Colour == colour.Color) == true)
        {
            colour.Color = shownCompoundChoices[index].Colour;
        }
    }

    public void SpeciesChanged(int index)
    {
        // If the currently selected colour is in the shownChoices list change the colour to the colour of the newly
        // selected compound to make setting up chemoreceptors easier
        if (shownSpeciesChoices?.Any(c => c.Colour == colour.Color) == true)
        {
            colour.Color = shownSpeciesChoices[index].Colour;
        }
    }

    public void TypeChanged(int index)
    {
        // Make only species or compound menu visible
        species.Visible = false;
        speciesLabel.Visible = false;
        compounds.Visible = false;
        compoundLabel.Visible = false;

        switch (index)
        {
            case 0:
                compounds.Visible = true;
                compoundLabel.Visible = true;
                break;
            case 1:
                species.Visible = true;
                speciesLabel.Visible = true;
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TargetTypesPath != null)
                TargetTypesPath.Dispose();
            if (CompoundsPath != null)
                CompoundsPath.Dispose();
            if (SpeciesPath != null)
                SpeciesPath.Dispose();
            if (CompoundsPath != null || SpeciesPath != null || TargetTypesPath != null)
            {
                MaximumDistancePath.Dispose();
                MinimumAmountPath.Dispose();
                ColourPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
