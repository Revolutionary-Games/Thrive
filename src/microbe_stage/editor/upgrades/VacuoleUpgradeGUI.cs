using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Upgrade GUI for the vacuole that allows specializing it
/// </summary>
public partial class VacuoleUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    /// <summary>
    ///   We need to explicitly block some compounds from upgrades as there's no way to otherwise skip these based on
    ///   just the properties of the compounds in the JSON
    /// </summary>
    public static readonly Compound[] BlockedSpecializedCompounds = [Compound.Radiation, Compound.Temperature];

#pragma warning disable CA2213
    [Export]
    private OptionButton compounds = null!;
    [Export]
    private Label description = null!;
    [Export]
    private CheckBox isSpecializedCheckbox = null!;
    [Export]
    private VBoxContainer compoundSelection = null!;
#pragma warning restore CA2213

    private List<CompoundDefinition>? shownChoices;

    public override void _Ready()
    {
        compounds.Clear();
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        // Exclude a bunch of stuff that is handled by other things or for other reasons shouldn't be selectable and
        // an explicitly disabled list of things as there's no property to block those with
        shownChoices = SimulationParameters.Instance.GetAllCompounds().Values
            .Where(c => !c.IsEnvironmental && (!c.IsAgent || c.ID == Compound.Mucilage) &&
                !BlockedSpecializedCompounds.Contains(c.ID))
            .ToList();

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
            CompoundDefinition? specialization = shownChoices.Find(c => c.ID == configuration.SpecializedFor);
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

        organelleUpgrades.CustomUpgradeData = new StorageComponentUpgrades(isSpecializedCheckbox.ButtonPressed ?
            shownChoices[compounds.Selected].ID :
            Compound.Invalid);

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
            {
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
