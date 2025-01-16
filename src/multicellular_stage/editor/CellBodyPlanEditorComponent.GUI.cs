using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using Systems;

/// <summary>
///   The partial class containing GUI updating actions
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    private StringBuilder atpToolTipTextBuilder = new();

    protected override void OnTranslationsChanged()
    {
        CalculateEnergyAndCompoundBalance(editedMicrobeCells);
    }

    private void UpdateStorage(Dictionary<Compound, float> storage, float nominalStorage)
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

    private void UpdateSpeed(float speed)
    {
        speedLabel.Value = (float)Math.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(speed), 1);
    }

    private void UpdateRotationSpeed(float speed)
    {
        rotationSpeedLabel.Value = (float)Math.Round(
            MicrobeInternalCalculations.RotationSpeedToUserReadableNumber(speed), 1);
    }

    private void UpdateEnergyBalance(EnergyBalanceInfo energyBalance)
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

    private void UpdateEnergyBalanceToolTips(EnergyBalanceInfo energyBalance)
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

    private void UpdateCompoundBalances(Dictionary<Compound, CompoundBalance> balances)
    {
        var warningTime = Editor.CurrentGame.GameWorld.LightCycle.DayLengthRealtimeSeconds *
            Editor.CurrentGame.GameWorld.WorldSettings.DaytimeFraction;

        // Don't show warning when day/night is not enabled
        if (!Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled)
            warningTime = 10000000;

        compoundBalance.UpdateBalances(balances, warningTime);
    }

    private void UpdateCompoundLastingTimes(Dictionary<Compound, CompoundBalance> normalBalance,
        Dictionary<Compound, CompoundBalance> nightBalance, float nominalStorage,
        Dictionary<Compound, float> specificStorages)
    {
        float lightFraction = Editor.CurrentGame.GameWorld.WorldSettings.DaytimeFraction;

        var warningTime = Editor.CurrentGame.GameWorld.LightCycle.DayLengthRealtimeSeconds * (1 - lightFraction);

        var fillingUpTime = Editor.CurrentGame.GameWorld.LightCycle.DayLengthRealtimeSeconds * lightFraction;

        // Don't show warning when day/night is not enabled
        if (!Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled)
        {
            warningTime = 10000000;
            fillingUpTime = warningTime;
        }

        compoundStorageLastingTimes.UpdateStorage(normalBalance, nightBalance, nominalStorage, specificStorages,
            warningTime, fillingUpTime, notEnoughStorageWarning);
    }

    private void HandleProcessList(IReadOnlyList<HexWithData<CellTemplate>> cells, EnergyBalanceInfo energyBalance,
        IBiomeConditions biome)
    {
        var processes = new List<TweakedProcess>();

        // Empty list to later fill
        var processStatistics = new List<ProcessSpeedInformation>();

        ProcessSystem.ComputeActiveProcessList(cells[0].Data!.Organelles, ref processes);

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        foreach (var process in processes)
        {
            // This requires the inputs to be in the biome to give a realistic prediction of how fast the processes
            // *might* run once swimming around in the stage.
            var singleProcess = ProcessSystem.CalculateProcessMaximumSpeed(process, biome, CompoundAmountType.Current,
                true);

            // If produces more ATP than consumes, lower down production for inputs and for outputs,
            // otherwise use maximum production values (this matches the equilibrium display mode and what happens
            // in game once exiting the editor)
            if (consumptionProductionRatio < 1.0f)
            {
                singleProcess.ScaleSpeed(consumptionProductionRatio, processSpeedWorkMemory);
            }

            processStatistics.Add(singleProcess);
        }

        processList.ProcessesToShow = processStatistics;
    }

    private void OnCompoundBalanceTypeChanged(BalanceDisplayType newType)
    {
        // Called by 2 different things so ignore the parameter and read the new values directly from the relevant
        // objects
        _ = newType;

        CalculateEnergyAndCompoundBalance(editedMicrobeCells);
    }

    private void OnBalanceShowOptionsChanged(bool pressed)
    {
        _ = pressed;

        CalculateEnergyAndCompoundBalance(editedMicrobeCells);
    }

    private List<KeyValuePair<string, float>> SortBarData(Dictionary<string, float> bar)
    {
        var comparer = new ATPComparer();

        return bar.OrderBy(i => i.Key, comparer).ToList();
    }

    private void SelectATPBalanceMode(int index)
    {
        balanceMode = (ResourceLimitingMode)index;

        CalculateEnergyAndCompoundBalance(editedMicrobeCells);
    }

    private void ToggleProcessList()
    {
        processListWindow.Visible = !processListWindow.Visible;
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
