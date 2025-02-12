using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Displays organism statistics calculated by an editor component
/// </summary>
public partial class OrganismStatisticsPanel : PanelContainer
{
    [Export]
    public bool ShowHealthStat;

    [Export]
    public bool ShowSizeStat;

    [Export]
    public bool ShowStorageStat;

    [Export]
    public bool ShowSpeedStat;

    [Export]
    public bool ShowRotationSpeedStat;

    [Export]
    public bool ShowDigestionSpeedStat;

    [Export]
    public bool ShowDigestionEfficiencyStat;

    [Export]
    public bool ShowOrganellesCostStat;

#pragma warning disable CA2213

    [Export]
    public LabelSettings ATPBalanceNormalText = null!;

    [Export]
    public LabelSettings ATPBalanceNotEnoughText = null!;

#pragma warning restore CA2213

    private readonly StringBuilder atpToolTipTextBuilder = new();

    private readonly ATPComparer atpComparer = new();

#pragma warning disable CA2213

    [Export]
    private CellStatsIndicator sizeLabel = null!;

    [Export]
    private CellStatsIndicator speedLabel = null!;

    [Export]
    private CellStatsIndicator rotationSpeedLabel = null!;

    [Export]
    private CellStatsIndicator hpLabel = null!;

    [Export]
    private CellStatsIndicator storageLabel = null!;

    [Export]
    private CellStatsIndicator digestionSpeedLabel = null!;

    [Export]
    private CellStatsIndicator digestionEfficiencyLabel = null!;

    [Export]
    private CellStatsIndicator ammoniaCostLabel = null!;

    [Export]
    private CellStatsIndicator phosphatesCostLabel = null!;

    [Export]
    private Control basicStatsSeparator = null!;

    [Export]
    private Control movementStatsSeparator = null!;

    [Export]
    private Control digestionStatsSeparator = null!;

    [Export]
    private Control organellesCostsSeparator = null!;

    [Export]
    private Label generationLabel = null!;

    [Export]
    private Control atpBalancePanel = null!;

    [Export]
    private Label atpBalanceLabel = null!;

    [Export]
    private Label atpProductionLabel = null!;

    [Export]
    private Label atpConsumptionLabel = null!;

    [Export]
    private SegmentedBar atpProductionBar = null!;

    [Export]
    private SegmentedBar atpConsumptionBar = null!;

    [Export]
    private CompoundBalanceDisplay compoundBalance = null!;

    [Export]
    private CompoundStorageStatistics compoundStorageLastingTimes = null!;

    [Export]
    private CustomRichTextLabel notEnoughStorageWarning = null!;

    [Export]
    private CheckBox calculateBalancesAsIfDay = null!;

    [Export]
    private CheckBox calculateBalancesWhenMoving = null!;

    [Export]
    private Button processListButton = null!;

    [Export]
    private ProcessList processList = null!;

    [Export]
    private CustomWindow processListWindow = null!;

    [Export]
    private LightConfigurationPanel lightConfigurationPanel = null!;

#pragma warning restore CA2213

    private LightLevelOption selectedLightLevelOption = LightLevelOption.Current;

    private EnergyBalanceInfoFull? energyBalanceInfo;

    [Signal]
    public delegate void OnLightLevelChangedEventHandler(int option);

    [Signal]
    public delegate void OnEnergyBalanceOptionsChangedEventHandler();

    [Signal]
    public delegate void OnResourceLimitingModeChangedEventHandler();

    public TutorialState? TutorialState { get; set; }

    public BalanceDisplayType BalanceDisplayType => compoundBalance.CurrentDisplayType;

    public CompoundAmountType CompoundAmountType => calculateBalancesAsIfDay.ButtonPressed ?
        CompoundAmountType.Biome :
        CompoundAmountType.Current;

    public ResourceLimitingMode ResourceLimitingMode { get; set; }

    public bool CalculateBalancesWhenMoving => calculateBalancesWhenMoving.ButtonPressed;

    public override void _Ready()
    {
        base._Ready();

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        UpdateStatVisibility();
    }

    public void OnTranslationsChanged()
    {
        if (energyBalanceInfo != null)
        {
            UpdateEnergyBalance(energyBalanceInfo);
        }
    }

    public void UpdateStatVisibility()
    {
        hpLabel.Visible = ShowHealthStat;
        sizeLabel.Visible = ShowSizeStat;
        storageLabel.Visible = ShowStorageStat;
        basicStatsSeparator.Visible = ShowHealthStat || ShowSizeStat || ShowStorageStat;

        speedLabel.Visible = ShowSpeedStat;
        rotationSpeedLabel.Visible = ShowRotationSpeedStat;
        movementStatsSeparator.Visible = ShowSpeedStat || ShowRotationSpeedStat;

        digestionSpeedLabel.Visible = ShowDigestionSpeedStat;
        digestionEfficiencyLabel.Visible = ShowDigestionEfficiencyStat;
        digestionStatsSeparator.Visible = ShowDigestionSpeedStat || ShowDigestionEfficiencyStat;

        ammoniaCostLabel.Visible = ShowOrganellesCostStat;
        phosphatesCostLabel.Visible = ShowOrganellesCostStat;
        digestionStatsSeparator.Visible = ShowOrganellesCostStat;
    }

    public void SendObjectsToTutorials(TutorialState tutorial, MicrobeEditorTutorialGUI gui)
    {
        tutorial.AtpBalanceIntroduction.ATPBalanceBarControl = atpBalancePanel;
    }

    public void UpdateEnergyBalance(EnergyBalanceInfoFull energyBalance)
    {
        energyBalanceInfo = energyBalance;

        if (energyBalance.FinalBalance > 0)
        {
            atpBalanceLabel.Text = Localization.Translate("ATP_PRODUCTION");
            atpBalanceLabel.LabelSettings = ATPBalanceNormalText;
        }
        else
        {
            atpBalanceLabel.Text = Localization.Translate("ATP_PRODUCTION") + " - " +
                Localization.Translate("ATP_PRODUCTION_TOO_LOW");
            atpBalanceLabel.LabelSettings = ATPBalanceNotEnoughText;
        }

        atpProductionLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", energyBalance.TotalProduction);
        atpConsumptionLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", energyBalance.TotalConsumption);

        float maxValue = Math.Max(energyBalance.TotalConsumption, energyBalance.TotalProduction);
        atpProductionBar.MaxValue = maxValue;
        atpConsumptionBar.MaxValue = maxValue;

        atpProductionBar.UpdateAndMoveBars(SortBarData(energyBalance.Production));
        atpConsumptionBar.UpdateAndMoveBars(SortBarData(energyBalance.Consumption));

        UpdateEnergyBalanceToolTips(energyBalance);
    }

    public void UpdateEnergyBalanceToolTips(EnergyBalanceInfoFull energyBalance)
    {
        var simulationParameters = SimulationParameters.Instance;

        foreach (var subBar in atpProductionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesProduction");

            if (tooltip == null)
                throw new InvalidOperationException("Could not find process production tooltip");

            subBar.RegisterToolTipForControl(tooltip, true);

            // Show required compounds for this process
            Dictionary<Compound, float>? requiredCompounds = null;

            if (energyBalance.ProductionRequiresCompounds != null)
            {
                energyBalance.ProductionRequiresCompounds.TryGetValue(subBar.Name, out requiredCompounds);
            }
            else
            {
                GD.PrintErr("Tracking for used compounds for energy not set up");
            }

            bool includedRequirement = false;

            if (requiredCompounds is { Count: > 0 })
            {
                atpToolTipTextBuilder.Clear();

                var translationFormat = Localization.Translate("ENERGY_BALANCE_REQUIRED_COMPOUND_LINE");

                foreach (var requiredCompound in requiredCompounds)
                {
                    var compound = simulationParameters.GetCompoundDefinition(requiredCompound.Key);

                    // Don't show environmental compounds as the player doesn't need to worry about having those to be
                    // able to generate ATP
                    if (compound.IsEnvironmental)
                        continue;

                    if (atpToolTipTextBuilder.Length > 0)
                        atpToolTipTextBuilder.Append('\n');

                    atpToolTipTextBuilder.Append(translationFormat.FormatSafe(compound.Name,
                        Math.Round(requiredCompound.Value, 2)));
                }

                // As we don't check for environmental compounds before starting the loop, we might not find any valid
                // data in the end in which case this needs to be skipped
                if (atpToolTipTextBuilder.Length > 0)
                {
                    tooltip.Description = Localization.Translate("ENERGY_BALANCE_TOOLTIP_PRODUCTION_WITH_REQUIREMENT")
                        .FormatSafe(SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name,
                            Math.Round(energyBalance.Production[subBar.Name], 3), atpToolTipTextBuilder.ToString());
                    includedRequirement = true;
                }
            }

            if (!includedRequirement)
            {
                // Normal display if didn't show with a requirement
                tooltip.Description = Localization.Translate("ENERGY_BALANCE_TOOLTIP_PRODUCTION").FormatSafe(
                    SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name,
                    Math.Round(energyBalance.Production[subBar.Name], 3));
            }
        }

        foreach (var subBar in atpConsumptionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesConsumption");

            if (tooltip == null)
                throw new InvalidOperationException("Could not find process consumption tooltip");

            subBar.RegisterToolTipForControl(tooltip, true);

            string displayName;

            switch (subBar.Name)
            {
                case "osmoregulation":
                {
                    displayName = Localization.Translate("OSMOREGULATION");
                    break;
                }

                case "baseMovement":
                {
                    displayName = Localization.Translate("BASE_MOVEMENT");
                    break;
                }

                default:
                {
                    displayName = SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name;
                    break;
                }
            }

            tooltip.Description = Localization.Translate("ENERGY_BALANCE_TOOLTIP_CONSUMPTION")
                .FormatSafe(displayName, Math.Round(energyBalance.Consumption[subBar.Name]));
        }
    }

    public void UpdateCompoundBalances(Dictionary<Compound, CompoundBalance> balances, float warningTime)
    {
        compoundBalance.UpdateBalances(balances, warningTime);
    }

    public void UpdateCompoundLastingTimes(Dictionary<Compound, CompoundBalance> normalBalance,
        Dictionary<Compound, CompoundBalance> nightBalance, float nominalStorage,
        Dictionary<Compound, float> specificStorages, float warningTime, float fillingUpTime)
    {
        compoundStorageLastingTimes.UpdateStorage(normalBalance, nightBalance, nominalStorage, specificStorages,
            warningTime, fillingUpTime, notEnoughStorageWarning);
    }

    public void RegisterTooltips()
    {
        digestionEfficiencyLabel.RegisterToolTipForControl("digestionEfficiencyDetails", "editor");
        storageLabel.RegisterToolTipForControl("storageDetails", "editor");
    }

    public void UpdateSize(int size)
    {
        sizeLabel.Value = size;
    }

    public void UpdateGeneration(int generation)
    {
        generationLabel.Text = generation.ToString(CultureInfo.CurrentCulture);
    }

    public void UpdateSpeed(float speed)
    {
        speedLabel.Value = (float)Math.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(speed), 1);
    }

    public void UpdateRotationSpeed(float speed)
    {
        rotationSpeedLabel.Value = (float)Math.Round(
            MicrobeInternalCalculations.RotationSpeedToUserReadableNumber(speed), 1);
    }

    public void UpdateHitpoints(float hp)
    {
        hpLabel.Value = hp;
    }

    public void UpdateStorage(Dictionary<Compound, float> storage, float nominalStorage)
    {
        // Storage values can be as low as 0.25 so 2 decimals are needed
        storageLabel.Value = MathF.Round(nominalStorage, 2);

        if (storage.Count == 0)
        {
            storageLabel.UnRegisterFirstToolTipForControl();
            return;
        }

        var tooltip = ToolTipManager.Instance.GetToolTip("storageDetails", "editor");
        if (tooltip == null)
        {
            GD.PrintErr("Can't update storage tooltip");
            return;
        }

        if (!storageLabel.IsToolTipRegistered(tooltip))
            storageLabel.RegisterToolTipForControl(tooltip, true);

        var description = new LocalizedStringBuilder(100);

        bool first = true;

        var simulationParameters = SimulationParameters.Instance;

        foreach (var entry in storage)
        {
            if (!first)
                description.Append("\n");

            first = false;

            description.Append(simulationParameters.GetCompoundDefinition(entry.Key).Name);
            description.Append(": ");
            description.Append(entry.Value);
        }

        tooltip.Description = description.ToString();
    }

    public void UpdateTotalDigestionSpeed(float speed)
    {
        digestionSpeedLabel.Format = Localization.Translate("DIGESTION_SPEED_VALUE");
        digestionSpeedLabel.Value = (float)Math.Round(speed, 2);
    }

    public void UpdateDigestionEfficiencies(Dictionary<Enzyme, float> efficiencies)
    {
        if (efficiencies.Count == 1)
        {
            digestionEfficiencyLabel.Format = Localization.Translate("PERCENTAGE_VALUE");
            digestionEfficiencyLabel.Value = (float)Math.Round(efficiencies.First().Value * 100, 2);
        }
        else
        {
            digestionEfficiencyLabel.Format = Localization.Translate("MIXED_DOT_DOT_DOT");

            // Set this to a value hero to fix the up/down arrow
            // Using sum makes the arrow almost always go up, using average makes the arrow almost always point down...
            // digestionEfficiencyLabel.Value = efficiencies.Select(e => e.Value).Average() * 100;
            digestionEfficiencyLabel.Value = efficiencies.Select(e => e.Value).Sum() * 100;
        }

        var description = new LocalizedStringBuilder(100);

        bool first = true;

        foreach (var enzyme in efficiencies)
        {
            if (!first)
                description.Append("\n");

            first = false;

            description.Append(enzyme.Key.Name);
            description.Append(": ");
            description.Append(new LocalizedString("PERCENTAGE_VALUE", (float)Math.Round(enzyme.Value * 100, 2)));
        }

        var tooltip = ToolTipManager.Instance.GetToolTip("digestionEfficiencyDetails", "editor");
        if (tooltip != null)
        {
            tooltip.Description = description.ToString();
        }
        else
        {
            GD.PrintErr("Can't update digestion efficiency tooltip");
        }
    }

    public void UpdateOrganellesCost(int ammoniaCost, int phosphatesCost)
    {
        ammoniaCostLabel.Value = ammoniaCost;
        phosphatesCostLabel.Value = phosphatesCost;
    }

    public void UpdateProcessList(List<ProcessSpeedInformation> processInfo)
    {
        processList.ProcessesToShow = processInfo;
    }

    public void UpdateLightSelectionPanelVisibility(bool hasDayAndNight)
    {
        lightConfigurationPanel.Visible = hasDayAndNight;

        // When not in a patch with light, hide the useless always day selector
        if (!hasDayAndNight)
        {
            calculateBalancesAsIfDay.ButtonPressed = false;
            calculateBalancesAsIfDay.Visible = false;
        }
        else
        {
            calculateBalancesAsIfDay.Visible = true;
        }
    }

    public void ApplyLightLevelSelection()
    {
        calculateBalancesAsIfDay.Disabled = false;

        lightConfigurationPanel.ApplyLightLevelSelection(selectedLightLevelOption);

        // Show selected light level
        switch (selectedLightLevelOption)
        {
            case LightLevelOption.Day:
            {
                calculateBalancesAsIfDay.ButtonPressed = true;
                calculateBalancesAsIfDay.Disabled = true;
                break;
            }

            case LightLevelOption.Night:
            {
                calculateBalancesAsIfDay.ButtonPressed = false;
                calculateBalancesAsIfDay.Disabled = true;
                break;
            }

            case LightLevelOption.Average:
            {
                calculateBalancesAsIfDay.ButtonPressed = false;
                break;
            }

            case LightLevelOption.Current:
            {
                break;
            }

            default:
                throw new Exception("Invalid light level option");
        }

        EmitSignal(SignalName.OnLightLevelChanged, (int)selectedLightLevelOption);
    }

    private void OnCompoundBalanceTypeChanged(BalanceDisplayType newType)
    {
        _ = newType;

        EmitSignal(SignalName.OnEnergyBalanceOptionsChanged);
    }

    private void OnBalanceShowOptionsChanged(bool pressed)
    {
        _ = pressed;

        EmitSignal(SignalName.OnEnergyBalanceOptionsChanged);
    }

    private void SelectATPBalanceMode(int index)
    {
        ResourceLimitingMode = (ResourceLimitingMode)index;

        EmitSignal(SignalName.OnResourceLimitingModeChanged);
    }

    private void OnProcessListButtonClicked()
    {
        processListWindow.Visible = !processListWindow.Visible;
    }

    private void OnLightLevelButtonPressed(int option)
    {
        GUICommon.Instance.PlayButtonPressSound();

        var selection = (LightLevelOption)option;

        selectedLightLevelOption = selection;

        ApplyLightLevelSelection();
    }

    private List<KeyValuePair<string, float>> SortBarData(Dictionary<string, float> bar)
    {
        return bar.OrderBy(i => i.Key, atpComparer).ToList();
    }

    private class ATPComparer : IComparer<string>
    {
        /// <summary>
        ///   Compares ATP production / consumption items
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Only works if there aren't duplicate entries of osmoregulation or baseMovement.
        ///   </para>
        /// </remarks>
        public int Compare(string? stringA, string? stringB)
        {
            if (stringA == "osmoregulation")
            {
                return -1;
            }

            if (stringB == "osmoregulation")
            {
                return 1;
            }

            if (stringA == "baseMovement")
            {
                return -1;
            }

            if (stringB == "baseMovement")
            {
                return 1;
            }

            return string.Compare(stringA, stringB, StringComparison.InvariantCulture);
        }
    }
}
