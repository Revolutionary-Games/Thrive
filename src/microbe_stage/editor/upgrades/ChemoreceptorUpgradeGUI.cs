using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChemoreceptorUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? CompoundsPath;

    [Export]
    public NodePath MaximumDistancePath = null!;

    [Export]
    public NodePath MinimumAmountPath = null!;

    [Export]
    public NodePath ColourPath = null!;

#pragma warning disable CA2213
    private OptionButton compounds = null!;
    private Slider maximumDistance = null!;
    private Slider minimumAmount = null!;
    private TweakedColourPicker colour = null!;
#pragma warning restore CA2213

    private IReadOnlyList<Compound>? shownChoices;

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

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (shownChoices == null)
        {
            GD.PrintErr("Chemoreceptor upgrade GUI was not opened properly");
            return false;
        }

        // Force some compound to be selected
        if (compounds.Selected == -1)
            compounds.Selected = 0;

        organelleUpgrades.CustomUpgradeData = new ChemoreceptorUpgrades(shownChoices[compounds.Selected],
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
        if (shownChoices?.Any(c => c.Colour == colour.Color) == true)
        {
            colour.Color = shownChoices[index].Colour;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CompoundsPath != null)
            {
                CompoundsPath.Dispose();
                MaximumDistancePath.Dispose();
                MinimumAmountPath.Dispose();
                ColourPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
