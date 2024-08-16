using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Systems;

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

    private ToxinType latestToxinType;

    public ToxinUpgradeGUI()
    {
        var toxinDefinition = SimulationParameters.Instance.GetOrganelleType("oxytoxy");

        foreach (var availableUpgrade in toxinDefinition.AvailableUpgrades)
        {
            typeToUpgradeInfo[ToxinUpgradeNames.ToxinTypeFromName(availableUpgrade.Key)] = availableUpgrade.Value;
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
        toxinTypeSelection.Clear();

        var upgradeTranslationTemplate = Localization.Translate("UPGRADE_COST");

        foreach (var toxinType in Enum.GetValues<ToxinType>())
        {
            var info = typeToUpgradeInfo[toxinType];

            toxinTypeSelection.AddItem(upgradeTranslationTemplate.FormatSafe(Localization.Translate(info.Name),
                Math.Round(info.MPCost * costMultiplier)), (int)toxinType);
        }

        var currentlySelectedType = ToxinType.Oxytoxy;

        toxicitySlider.Value = 0;

        if (organelle.Upgrades != null)
        {
            currentlySelectedType = organelle.Upgrades.GetToxinTypeFromUpgrades();

            if (organelle.Upgrades.CustomUpgradeData is ToxinUpgrades toxinUpgrades)
            {
                if (currentlySelectedType != toxinUpgrades.BaseType)
                {
                    GD.PrintErr("Mismatch between custom toxin upgrade data and unlocked features list");
                }

                toxicitySlider.Value = toxinUpgrades.Toxicity;
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

        organelleUpgrades.CustomUpgradeData = new ToxinUpgrades(selectedType, (float)toxicitySlider.Value);

        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(350, 370);
    }

    private void ApplySelection(ToxinType toxinType)
    {
        toxinTypeSelection.Select(toxinTypeSelection.GetItemIndex((int)toxinType));

        toxinTypeDescription.Text = Localization.Translate(toxinType.GetAttribute<DescriptionAttribute>().Description);

        UpdateStatIndicators(toxinType);
    }

    private void UpdateStatIndicators(ToxinType toxinType)
    {
        damageIndicator.Visible = true;
        damagePerOxygenIndicator.Visible = false;
        baseMovementIndicator.Visible = false;
        atpIndicator.Visible = false;

        latestToxinType = toxinType;

        UpdateToxinStats(toxinType);
    }

    private void UpdateToxinStats(ToxinType toxinType)
    {
        var damageMultiplier =
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity((float)toxicitySlider.Value, toxinType);

        switch (toxinType)
        {
            case ToxinType.Oxytoxy:
            {
                damageIndicator.Value = (float)Math.Round(Constants.OXYTOXY_DAMAGE * damageMultiplier, 1);
                damagePerOxygenIndicator.Visible = true;
                damagePerOxygenIndicator.Value = -100 * Constants.OXYTOXY_DAMAGE_DEBUFF_PER_ORGANELLE;
                baseMovementIndicator.Value = 0;
                atpIndicator.Value = 0;
                break;
            }

            case ToxinType.Cytotoxin:
            {
                damageIndicator.Value = (float)Math.Round(Constants.CYTOTOXIN_DAMAGE * damageMultiplier, 1);
                damagePerOxygenIndicator.Value = 0;
                baseMovementIndicator.Value = 0;
                atpIndicator.Value = 0;
                break;
            }

            case ToxinType.Macrolide:
            {
                damageIndicator.Value = 0;
                damagePerOxygenIndicator.Value = 0;
                baseMovementIndicator.Visible = true;
                baseMovementIndicator.Value =
                    (float)Math.Round(100 * Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF * damageMultiplier);
                atpIndicator.Value = 0;
                break;
            }

            case ToxinType.ChannelInhibitor:
            {
                damageIndicator.Value = 0;
                damagePerOxygenIndicator.Value = 0;
                baseMovementIndicator.Value = 0;
                atpIndicator.Visible = true;
                atpIndicator.Value = (float)Math.Round(100 * Constants.CHANNEL_INHIBITOR_ATP_DEBUFF * damageMultiplier);
                break;
            }

            case ToxinType.OxygenMetabolismInhibitor:
            {
                damageIndicator.Value = (float)Math.Round(Constants.OXYGEN_INHIBITOR_DAMAGE * damageMultiplier, 1);
                damagePerOxygenIndicator.Visible = true;
                damagePerOxygenIndicator.Value = 100 * Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_PER_ORGANELLE;
                baseMovementIndicator.Value = 0;
                atpIndicator.Value = 0;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(toxinType), toxinType, null);
        }
    }

    private void OnToxinTypeSelected(int index)
    {
        ApplySelection((ToxinType)toxinTypeSelection.GetItemId(index));
    }

    private void OnToxicityChanged(float value)
    {
        _ = value;

        // Update stats as the toxicity affects these
        UpdateToxinStats(latestToxinType);
    }
}
