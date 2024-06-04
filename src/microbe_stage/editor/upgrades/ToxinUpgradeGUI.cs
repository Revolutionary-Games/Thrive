using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;

/// <summary>
///   Upgrades for toxin vacuole and prokaryotic variant of it
/// </summary>
public partial class ToxinUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    private readonly Dictionary<ToxinType, AvailableUpgrade> typeToUpgradeInfo = new();

#pragma warning disable CA2213
    [Export]
    private OptionButton toxinTypeSelection = null!;

    [Export]
    private Label toxinTypeDescription = null!;
#pragma warning restore CA2213

    public ToxinUpgradeGUI()
    {
        var toxinDefinition = SimulationParameters.Instance.GetOrganelleType("oxytoxy");

        foreach (var availableUpgrade in toxinDefinition.AvailableUpgrades)
        {
            typeToUpgradeInfo[ToxinUpgradeNames.ToxinTypeFromName(availableUpgrade.Key)] = availableUpgrade.Value;
        }
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        toxinTypeSelection.Clear();

        var upgradeTranslationTemplate = Localization.Translate("UPGRADE_COST");

        foreach (var toxinType in Enum.GetValues<ToxinType>())
        {
            var info = typeToUpgradeInfo[toxinType];

            toxinTypeSelection.AddItem(upgradeTranslationTemplate.FormatSafe(Localization.Translate(info.Name),
                Math.Round(info.MPCost * costMultiplier)), (int)toxinType);
        }

        var currentlySelectedType = ToxinType.Oxytoxy;

        if (organelle.Upgrades != null)
        {
            currentlySelectedType = organelle.Upgrades.GetToxinTypeFromUpgrades();

            if (organelle.Upgrades.CustomUpgradeData is ToxinUpgrades toxinUpgrades)
            {
                if (currentlySelectedType != toxinUpgrades.BaseType)
                {
                    GD.PrintErr("Mismatch between custom toxin upgrade data and unlocked features list");
                }
            }
        }

        ApplySelection(currentlySelectedType);
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (toxinTypeSelection.ItemCount < 1)
        {
            GD.PrintErr("Toxin upgrade GUI not opened properly");
            return false;
        }

        int selectedIndex = toxinTypeSelection.Selected;

        if (selectedIndex < 0)
        {
            GD.PrintErr("No toxin type selected");
            selectedIndex = 0;
        }

        // Clear previous data
        organelleUpgrades.CustomUpgradeData = null;

        var featuresToClear = new List<string>();

        foreach (var unlockedFeature in organelleUpgrades.UnlockedFeatures)
        {
            if (ToxinUpgradeNames.TryGetToxinTypeFromName(unlockedFeature, out _))
            {
                featuresToClear.Add(unlockedFeature);
            }
        }

        foreach (var feature in featuresToClear)
        {
            organelleUpgrades.UnlockedFeatures.Remove(feature);
        }

        // Apply new data
        var selectedType = (ToxinType)toxinTypeSelection.GetItemId(selectedIndex);

        var upgradeName = ToxinUpgradeNames.ToxinNameFromType(selectedType);

        // Default upgrade name is skipped
        if (upgradeName != Constants.ORGANELLE_UPGRADE_SPECIAL_NONE)
            organelleUpgrades.UnlockedFeatures.Add(upgradeName);

        // TODO: this will only really be needed when we have potency / toxicity slider implemented
        // organelleUpgrades.CustomUpgradeData = new ToxinUpgrades(selectedType);

        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(350, 350);
    }

    private void ApplySelection(ToxinType toxinType)
    {
        toxinTypeSelection.Select(toxinTypeSelection.GetItemIndex((int)toxinType));

        toxinTypeDescription.Text = Localization.Translate(toxinType.GetAttribute<DescriptionAttribute>().Description);
    }

    private void OnToxinTypeSelected(int index)
    {
        ApplySelection((ToxinType)toxinTypeSelection.GetItemId(index));
    }
}
