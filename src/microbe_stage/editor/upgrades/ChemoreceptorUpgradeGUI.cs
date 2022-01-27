using System.Collections.Generic;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath CompoundsPath;

    [Export]
    public NodePath MaximumDistancePath;

    [Export]
    public NodePath MinimumAmountPath;

    [Export]
    public NodePath ColourPath;

    private OptionButton compounds;
    private Slider maximumDistance;
    private Slider minimumAmount;
    private TweakedColourPicker colour;

    private List<Compound> shownChoices;
    private OrganelleTemplate storedOrganelle;

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
            colour.Color = Colors.White;
        }
    }

    public void ApplyChanges(MicrobeEditor editor)
    {
        // Force some compound to be selected
        if (compounds.Selected == -1)
            compounds.Selected = 0;

        // TODO: make an undoable action
        storedOrganelle.SetCustomUpgradeObject(new ChemoreceptorUpgrades
        {
            TargetCompound = shownChoices[compounds.Selected],
            SearchRange = (float)maximumDistance.Value,
            SearchAmount = (float)minimumAmount.Value,
            LineColour = colour.Color,
        });
    }
}
