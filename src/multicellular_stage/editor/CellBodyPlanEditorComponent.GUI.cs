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
    private readonly StringBuilder atpToolTipTextBuilder = new();

    protected override void OnTranslationsChanged()
    {
        CalculateEnergyAndCompoundBalance(editedMicrobeCells);
    }

    private void ConfirmFinishEditingWithNegativeATPPressed()
    {
        if (OnFinish == null)
        {
            GD.PrintErr("Confirmed editing for cell editor when finish callback is not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        ignoredEditorWarnings.Add(EditorUserOverride.NotProducingEnoughATP);
        OnFinish.Invoke(ignoredEditorWarnings);
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

        organismStatisticsPanel.UpdateProcessList(processStatistics);
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

    private void UpdateCompoundBalances(Dictionary<Compound, CompoundBalance> balances)
    {
        var warningTime = Editor.CurrentGame.GameWorld.LightCycle.DayLengthRealtimeSeconds *
            Editor.CurrentGame.GameWorld.WorldSettings.DaytimeFraction;

        // Don't show warning when day/night is not enabled
        if (!Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled)
            warningTime = 10000000;

        organismStatisticsPanel.UpdateCompoundBalances(balances, warningTime);
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

        organismStatisticsPanel.UpdateCompoundLastingTimes(normalBalance, nightBalance, nominalStorage,
            specificStorages, warningTime, fillingUpTime);
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
