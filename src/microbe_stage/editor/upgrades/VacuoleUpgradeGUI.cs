using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class VacuoleUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? CompoundsPath;

    [Export]
    public NodePath IsSpecializedCheckboxPath = null!;

    [Export]
    public NodePath CompoundDescriptionPath = null!;

    [Export]
    public NodePath CompoundSelectionPath = null!;

#pragma warning disable CA2213
    private OptionButton compounds = null!;
    private Label description = null!;
    private CheckBox isSpecializedCheckbox = null!;
    private VBoxContainer compoundSelection = null!;
#pragma warning restore CA2213

    private Compound mucilage = null!;

    private List<Compound>? shownChoices;

    public override void _Ready()
    {
        compounds = GetNode<OptionButton>(CompoundsPath);
        description = GetNode<Label>(CompoundDescriptionPath);
        isSpecializedCheckbox = GetNode<CheckBox>(IsSpecializedCheckboxPath);
        compoundSelection = GetNode<VBoxContainer>(CompoundSelectionPath);

        compounds.Clear();

        mucilage = SimulationParameters.Instance.GetCompound("mucilage");
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame)
    {
        shownChoices = SimulationParameters.Instance.GetAllCompounds().Values
            .Where(c => !c.IsEnvironmental && (!c.IsAgent || c.InternalName == mucilage.InternalName)).ToList();

        foreach (var compound in shownChoices)
            compounds.AddItem(compound.Name);

        // Select glucose by default
        var defaultCompoundIndex =
            shownChoices.FindIndex(c => c.InternalName == Constants.VACUOLE_DEFAULT_COMPOUND_NAME);

        if (defaultCompoundIndex < 0)
            defaultCompoundIndex = 0;

        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is StorageComponentUpgrades configuration)
        {
            Compound? specialization = shownChoices.Find(c => c == configuration.SpecializedFor);
            isSpecializedCheckbox.ButtonPressed = specialization != null;

            compounds.Selected = specialization != null ?
                shownChoices.IndexOf(specialization) :
                defaultCompoundIndex;
        }
        else
        {
            compounds.Selected = defaultCompoundIndex;
            isSpecializedCheckbox.ButtonPressed = false;
        }

        UpdateGUI();
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (shownChoices == null)
        {
            GD.PrintErr("Vacuole upgrade GUI was not opened properly");
            return false;
        }

        // Force some compound to be selected
        if (compounds.Selected == -1)
            compounds.Selected = 0;

        organelleUpgrades.CustomUpgradeData = new StorageComponentUpgrades(
            isSpecializedCheckbox.ButtonPressed ? shownChoices[compounds.Selected] : null);

        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(420, 135);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CompoundsPath != null)
            {
                CompoundsPath.Dispose();
                CompoundDescriptionPath.Dispose();
                IsSpecializedCheckboxPath.Dispose();
                CompoundSelectionPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnIsSpecializedToggled(bool isSpecialized)
    {
        _ = isSpecialized;
        UpdateGUI();
    }

    private void UpdateGUI()
    {
        // Update visibility of the compound selection
        compoundSelection.Visible = isSpecializedCheckbox.ButtonPressed;

        if (shownChoices == null)
            return;

        float capacity = SimulationParameters.Instance.GetOrganelleType("vacuole").Components.Storage!.Capacity;
        if (!isSpecializedCheckbox.ButtonPressed)
        {
            var text = new LocalizedString("VACUOLE_NOT_SPECIALIZED_DESCRIPTION", capacity);
            description.Text = text.ToString();
        }
        else
        {
            var text = new LocalizedString("VACUOLE_SPECIALIZED_DESCRIPTION", capacity * 2);
            description.Text = text.ToString();
        }
    }
}
