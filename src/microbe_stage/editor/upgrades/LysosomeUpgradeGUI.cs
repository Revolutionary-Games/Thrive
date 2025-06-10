using System.Collections.Generic;
using Godot;

/// <summary>
///   Upgrade GUI for the lysosome that allows picking what enzymes it provides
/// </summary>
public partial class LysosomeUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
#pragma warning disable CA2213
    [Export]
    private OptionButton enzymes = null!;

    [Export]
    private Label description = null!;
#pragma warning restore CA2213

    private IReadOnlyList<Enzyme>? shownChoices;

    public override void _Ready()
    {
        enzymes.Clear();
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        shownChoices = SimulationParameters.Instance.GetHydrolyticEnzymes();

        foreach (var enzyme in shownChoices)
        {
            enzymes.AddItem(enzyme.Name);
        }

        // Select lipase by default
        var defaultCompoundIndex =
            shownChoices.FindIndex(c => c.InternalName == Constants.LIPASE_ENZYME);

        if (defaultCompoundIndex < 0)
            defaultCompoundIndex = 0;

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is LysosomeUpgrades configuration)
        {
            enzymes.Selected = shownChoices.FindIndex(c => c == configuration.Enzyme);
        }
        else
        {
            enzymes.Selected = defaultCompoundIndex;
        }

        UpdateDescription();
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (shownChoices == null)
        {
            GD.PrintErr("Lysosome upgrade GUI was not opened properly");
            return false;
        }

        // Force some compound to be selected
        if (enzymes.Selected == -1)
            enzymes.Selected = 0;

        organelleUpgrades.CustomUpgradeData = new LysosomeUpgrades(shownChoices[enzymes.Selected]);
        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(420, 160);
    }

    private void OnEnzymeSelected(int index)
    {
        _ = index;
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        if (shownChoices == null)
            return;

        var enzyme = shownChoices[enzymes.Selected];

        description.Text = enzyme.Description;
    }
}
