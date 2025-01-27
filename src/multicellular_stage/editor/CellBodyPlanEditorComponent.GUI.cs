using System;
using System.Collections.Generic;
using Godot;
using Systems;

/// <summary>
///   The partial class containing GUI updating actions
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    protected override void OnTranslationsChanged()
    {
        organismStatisticsPanel.OnTranslationsChanged();
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
        // Empty list to later fill
        var processStatistics = new List<ProcessSpeedInformation>();

        var processes = new List<TweakedProcess>();

        foreach (var cellType in GetCellTypes())
        {
            var newProcesses = new List<TweakedProcess>();

            ProcessSystem.ComputeActiveProcessList(cellType.Type.Organelles, ref newProcesses);

            for (int i = 0; i < newProcesses.Count; ++i)
            {
                newProcesses[i] = new TweakedProcess(newProcesses[i].Process, newProcesses[i].Rate * cellType.Count)
                {
                    SpeedMultiplier = newProcesses[i].SpeedMultiplier,
                };
            }

            ProcessSystem.MergeProcessLists(processes, newProcesses);
        }

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
        // TODO: Check if it's possible to move those calculations elsewhere to avoid duplication with
        // CellEditorComponent.UpdateCompoundLastingTimes
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

    private void UpdateGUIAfterLoadingSpecies(Species species)
    {
        GD.Print("Starting multicellular editor with: ", editedMicrobeCells.Count,
            " cells in the microbe");

        SetSpeciesInfo(newName,
            behaviourEditor.Behaviour ?? throw new Exception("Editor doesn't have Behaviour setup"));

        organismStatisticsPanel.UpdateGeneration(species.Generation);
        organismStatisticsPanel.UpdateStorage(GetAdditionalCapacities(out var nominalCapacity), nominalCapacity);

        organismStatisticsPanel.ApplyLightLevelSelection();

        UpdateCancelButtonVisibility();
    }
}
