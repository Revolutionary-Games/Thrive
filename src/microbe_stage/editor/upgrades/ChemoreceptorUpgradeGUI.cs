using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? CompoundsPath;

    [Export]
    public NodePath? SpeciesPath;

    [Export]
    public NodePath MaximumDistancePath = null!;

    [Export]
    public NodePath MinimumAmountPath = null!;

    [Export]
    public NodePath ColourPath = null!;

#pragma warning disable CA2213
    private OptionButton compounds = null!;
    private OptionButton species = null!;
    private Slider maximumDistance = null!;
    private Slider minimumAmount = null!;
    private TweakedColourPicker colour = null!;
#pragma warning restore CA2213

    private List<Compound>? shownCompoundChoices;
    private List<Species>? shownSpeciesChoices;

    public override void _Ready()
    {
        compounds = GetNode<OptionButton>(CompoundsPath);
        species = GetNode<OptionButton>(SpeciesPath);
        maximumDistance = GetNode<Slider>(MaximumDistancePath);
        minimumAmount = GetNode<Slider>(MinimumAmountPath);
        colour = GetNode<TweakedColourPicker>(ColourPath);

        compounds.Clear();

        maximumDistance.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistance.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;

        minimumAmount.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmount.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame)
    {
        shownCompoundChoices = SimulationParameters.Instance.GetCloudCompounds();

        foreach (var choice in shownCompoundChoices)
        {
            compounds.AddItem(choice.Name);
        }

        shownSpeciesChoices = currentGame.GameWorld.Map.FindAllSpeciesWithPopulation();

        foreach (var choice in shownSpeciesChoices)
        {
            species.AddItem(string.Join(choice.Genus, " ", choice.Epithet));
        }

        // Select glucose by default
        var defaultCompoundIndex =
            shownCompoundChoices.FindIndex(c => c.InternalName == Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);

        if (defaultCompoundIndex < 0)
            defaultCompoundIndex = 0;

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            compounds.Selected = shownCompoundChoices.FindIndex(c => c == configuration.TargetCompound);
            species.Selected = shownSpeciesChoices.FindIndex(c => c == configuration.TargetSpecies);
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

        // Force some compound/species to be selected
        if (compounds.Selected == -1)
            compounds.Selected = 0;
        if (species.Selected == -1)
            species.Selected = 0;

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(
            shownCompoundChoices[compounds.Selected],
            shownSpeciesChoices[species.Selected],
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CompoundsPath != null)
                CompoundsPath.Dispose();
            if (SpeciesPath != null)
                SpeciesPath.Dispose();
            if (CompoundsPath != null || SpeciesPath != null)
            {
                MaximumDistancePath.Dispose();
                MinimumAmountPath.Dispose();
                ColourPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
