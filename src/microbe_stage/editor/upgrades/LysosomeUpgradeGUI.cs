using System.Collections.Generic;
using Godot;

public class LysosomeUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath EnzymesPath = null!;

    [Export]
    public NodePath EnzymeDescriptionPath = null!;

    private IOrganelleUpgradeDialog dialog = null!;
    private OptionButton enzymes = null!;
    private Label description = null!;

    private List<Enzyme>? shownChoices;
    private OrganelleTemplate? storedOrganelle;

    public override void _Ready()
    {
        enzymes = GetNode<OptionButton>(EnzymesPath);
        description = GetNode<Label>(EnzymeDescriptionPath);

        enzymes.Clear();
    }

    public void OnStartFor(OrganelleTemplate organelle, IOrganelleUpgradeDialog dialog)
    {
        this.dialog = dialog;
        storedOrganelle = organelle;
        shownChoices = SimulationParameters.Instance.GetDigestiveEnzymes();

        foreach (var enzyme in shownChoices)
        {
            enzymes.AddItem(enzyme.Name);
        }

        // Select lipase by default
        var defaultCompoundIndex =
            shownChoices.FindIndex(c => c.InternalName == Constants.LYSOSOME_DEFAULT_ENZYME_NAME);

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

    public void ApplyChanges(ICellEditorData editor)
    {
        if (storedOrganelle == null || shownChoices == null)
        {
            GD.PrintErr("Lysosome upgrade GUI was not opened properly");
            return;
        }

        // Force some compound to be selected
        if (enzymes.Selected == -1)
            enzymes.Selected = 0;

        // TODO: make an undoable action
        storedOrganelle.SetCustomUpgradeObject(new LysosomeUpgrades(shownChoices[enzymes.Selected]));
    }

    private void OnEnzymeSelected(int index)
    {
        _ = index;
        UpdateDescription();
        dialog.Redraw();
    }

    private void UpdateDescription()
    {
        if (shownChoices == null)
            return;

        var enzyme = shownChoices[enzymes.Selected];

        switch (enzyme.InternalName)
        {
            case "lipase":
                description.Text = TranslationServer.Translate("LIPASE_DESCRIPTION");
                break;
            case "cellulase":
                description.Text = TranslationServer.Translate("CELLULASE_DESCRIPTION");
                break;
            case "chitinase":
                description.Text = TranslationServer.Translate("CHITINASE_DESCRIPTION");
                break;
        }
    }
}
