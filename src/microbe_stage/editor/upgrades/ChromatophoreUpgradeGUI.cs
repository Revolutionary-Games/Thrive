using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Systems;

/// <summary>
///   Upgrades for toxin vacuole and prokaryotic variant of it
/// </summary>
public partial class ChromatophoreUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    private readonly Dictionary<CalvinType, AvailableUpgrade> typeToUpgradeInfo = new();

#pragma warning disable CA2213
    [Export]
    private OptionButton calvinTypeSelection = null!;

    [Export]
    private Label typeDescription = null!;

    [Export]
    private CellStatsIndicator damageIndicator = null!;

    [Export]
    private CellStatsIndicator damagePerOxygenIndicator = null!;

    [Export]
    private CellStatsIndicator baseMovementIndicator = null!;

    [Export]
    private CellStatsIndicator atpIndicator = null!;

    [Export]
    private Slider toxicitySlider = null!;

#pragma warning restore CA2213

    private CalvinType latestCalvinType;

    public ChromatophoreUpgradeGUI()
    {
        var toxinDefinition = SimulationParameters.Instance.GetOrganelleType("chromatophore");

        foreach (var availableUpgrade in toxinDefinition.AvailableUpgrades)
        {
            var typeInput = CalvinUpgradeNames.CalvinTypeFromName(availableUpgrade.Key, "chromatophore");
            typeToUpgradeInfo[typeInput] = availableUpgrade.Value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        var percentage = Localization.Translate("PERCENTAGE_VALUE");
        damagePerOxygenIndicator.Format = percentage;
        baseMovementIndicator.Format = percentage;
        atpIndicator.Format = percentage;
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        calvinTypeSelection.Clear();

        var upgradeTranslationTemplate = Localization.Translate("UPGRADE_COST");

        foreach (var calvinType in Enum.GetValues<CalvinType>())
        {
            var info = typeToUpgradeInfo[calvinType];

            calvinTypeSelection.AddItem(upgradeTranslationTemplate.FormatSafe(Localization.Translate(info.Name),
                Math.Round(info.MPCost * costMultiplier)), (int)calvinType);
        }

        var currentlySelectedType = CalvinType.NoCalvin;

        toxicitySlider.Value = Constants.DEFAULT_TOXICITY;

        if (organelle.Upgrades != null)
        {
            currentlySelectedType = organelle.Upgrades.GetCalvinTypeFromUpgrades("chromatophore");

            if (organelle.Upgrades.CustomUpgradeData is CalvinUpgrades toxinUpgrades)
            {
                if (currentlySelectedType != toxinUpgrades.BaseType)
                {
                    // GD.PrintErr("Mismatch between custom toxin upgrade data and unlocked features list");
                }

                toxicitySlider.Value = toxinUpgrades.Toxicity;
            }
        }

        ApplySelection(currentlySelectedType);
        GD.Print(currentlySelectedType);
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        if (calvinTypeSelection.ItemCount < 1)
        {
            GD.PrintErr("Toxin upgrade GUI not opened properly");
            return false;
        }

        int selectedIndex = calvinTypeSelection.Selected;

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
            if (CalvinUpgradeNames.TryGetCalvinTypeFromName(unlockedFeature, "chromatophore", out _))
            {
                featuresToClear.Add(unlockedFeature);
            }
        }

        foreach (var feature in featuresToClear)
        {
            organelleUpgrades.ModifiableUnlockedFeatures.Remove(feature);
        }

        // Apply new data
        var selectedType = (CalvinType)calvinTypeSelection.GetItemId(selectedIndex);

        var upgradeName = CalvinUpgradeNames.CalvinNameFromType(selectedType, "chromatophore");

        // Default upgrade name is skipped
        if (upgradeName != Constants.ORGANELLE_UPGRADE_SPECIAL_NONE)
            organelleUpgrades.ModifiableUnlockedFeatures.Add(upgradeName);

        organelleUpgrades.CustomUpgradeData = new CalvinUpgrades(selectedType, (float)toxicitySlider.Value);

        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(350, 380);
    }

    private void ApplySelection(CalvinType calvinType)
    {
        calvinTypeSelection.Select(calvinTypeSelection.GetItemIndex((int)calvinType));

        typeDescription.Text = Localization.Translate(calvinType.GetAttribute<DescriptionAttribute>().Description);

        UpdateStatIndicators(calvinType);
    }

    private void UpdateStatIndicators(CalvinType calvinType)
    {
        damageIndicator.Visible = true;
        damagePerOxygenIndicator.Visible = false;
        baseMovementIndicator.Visible = false;
        atpIndicator.Visible = false;

        latestCalvinType = calvinType;

        UpdateCalvinStats(calvinType);
    }

    private void UpdateCalvinStats(CalvinType calvinType)
    {
        var damageMultiplier =
            1;

        switch (calvinType)
        {
            case CalvinType.Glucose:
            {
                damageIndicator.Value = 1;
                damagePerOxygenIndicator.Visible = true;
                damagePerOxygenIndicator.Value = -100 * Constants.OXYTOXY_DAMAGE_DEBUFF_PER_ORGANELLE;
                baseMovementIndicator.Value = 0;
                atpIndicator.Value = 0;
                break;
            }

            case CalvinType.NoCalvin:
            {
                damageIndicator.Value = 1;
                damagePerOxygenIndicator.Value = 0;
                baseMovementIndicator.Value = 0;
                atpIndicator.Value = 0;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(calvinType), calvinType, null);
        }
    }

    private void OnCalvinTypeSelected(int index)
    {
        ApplySelection((CalvinType)calvinTypeSelection.GetItemId(index));
    }

    private void OnToxicityChanged(float value)
    {
        _ = value;

        // Update stats as the toxicity affects these
        UpdateCalvinStats(latestCalvinType);
    }
}
