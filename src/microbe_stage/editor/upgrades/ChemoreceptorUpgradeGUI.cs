using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath CompoundsPath = null!;

    [Export]
    public NodePath MaximumDistancePath = null!;

    [Export]
    public NodePath MinimumAmountPath = null!;

    [Export]
    public NodePath ColourPath = null!;

    private OptionButton compounds = null!;
    private Slider maximumDistance = null!;
    private Slider minimumAmount = null!;
    private TweakedColourPicker colour = null!;

    private List<Compound>? shownChoices;
    private OrganelleTemplate? storedOrganelle;

    public override void _Ready()
    {
        compounds = GetNode<OptionButton>(CompoundsPath);
        maximumDistance = GetNode<Slider>(MaximumDistancePath);
        minimumAmount = GetNode<Slider>(MinimumAmountPath);
        colour = GetNode<TweakedColourPicker>(ColourPath);

        compounds.Clear();

        maximumDistance.MinValue = Constants.CHEMORECEPTOR_RANGE_MIN;
        maximumDistance.MaxValue = Constants.CHEMORECEPTOR_RANGE_MAX;

        minimumAmount.MinValue = Constants.CHEMORECEPTOR_AMOUNT_MIN;
        minimumAmount.MaxValue = Constants.CHEMORECEPTOR_AMOUNT_MAX;
    }

    public void OnStartFor(OrganelleTemplate organelle)
    {
        storedOrganelle = organelle;
        shownChoices = SimulationParameters.Instance.GetCloudCompounds();

        foreach (var choice in shownChoices)
        {
            compounds.AddItem(choice.Name);
        }

        // Select glucose by default
        var defaultCompoundIndex =
            shownChoices.FindIndex(c => c.InternalName == Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);

        if (defaultCompoundIndex < 0)
            defaultCompoundIndex = 0;

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades configuration)
        {
            compounds.Selected = shownChoices.FindIndex(c => c == configuration.TargetCompound);
            maximumDistance.Value = configuration.SearchRange;
            minimumAmount.Value = configuration.SearchAmount;
            colour.Color = configuration.LineColour;
        }
        else
        {
            compounds.Selected = defaultCompoundIndex;
            maximumDistance.Value = Constants.CHEMORECEPTOR_RANGE_DEFAULT;
            minimumAmount.Value = Constants.CHEMORECEPTOR_AMOUNT_DEFAULT;
            colour.Color = shownChoices[defaultCompoundIndex].Colour;
        }
    }

    public void ApplyChanges(ICellEditorData editor)
    {
        if (storedOrganelle == null || shownChoices == null)
        {
            GD.PrintErr("Chemoreceptor upgrade GUI was not opened properly");
            return;
        }

        // Force some compound to be selected
        if (compounds.Selected == -1)
            compounds.Selected = 0;

        // TODO: make an undoable action
        storedOrganelle.SetCustomUpgradeObject(new ChemoreceptorUpgrades(shownChoices[compounds.Selected],
            (float)maximumDistance.Value, (float)minimumAmount.Value, colour.Color));
    }

    public void CompoundChanged(int index)
    {
        // If the currently selected colour is in the shownChoices list change the colour to the colour of the newly
        // selected compound to make setting up chemoreceptors easier
        if (shownChoices?.Any(c => c.Colour == colour.Color) == true)
        {
            colour.Color = shownChoices[index].Colour;
        }
    }
}
