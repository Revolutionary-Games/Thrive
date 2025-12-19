using System;
using System.Collections.Generic;
using System.Linq;
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
            GD.PrintErr("Confirmed editing for cell body plan editor when finish callback is not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        ignoredEditorWarnings.Add(EditorUserOverride.NotProducingEnoughATP);
        OnFinish.Invoke(ignoredEditorWarnings);
    }

    /// <summary>
    ///   Updates the process list. TODO: make this only show a single cell type's processes and make the type
    ///   selectable.
    ///   https://github.com/Revolutionary-Games/Thrive/issues/5863
    /// </summary>
    private void HandleProcessList(EnergyBalanceInfoFull energyBalance, IBiomeConditions biome)
    {
        // TODO: this used to have an unused "cells" parameter so figure out why it was added and if it should have
        // done something

        // Empty list to later fill
        var processStatistics = new List<ProcessSpeedInformation>();

        var processes = new List<TweakedProcess>();

        UpdateCellTypesCounts();
        var newProcesses = new List<TweakedProcess>();
        foreach (var cellType in cellTypesCount)
        {
            newProcesses.Clear();

            ProcessSystem.ComputeActiveProcessList(cellType.Key.ModifiableOrganelles, ref newProcesses);

            for (int i = 0; i < newProcesses.Count; ++i)
            {
                newProcesses[i] = new TweakedProcess(newProcesses[i].Process, newProcesses[i].Rate * cellType.Value)
                {
                    SpeedMultiplier = newProcesses[i].SpeedMultiplier,
                };
            }

            ProcessSystem.MergeProcessLists(processes, newProcesses);
        }

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        foreach (var process in processes)
        {
            // This requires the inputs to be in the biome to give a realistic prediction of how fast the processes
            // *might* run once swimming around in the stage.
            var singleProcess = ProcessSystem.CalculateProcessMaximumSpeed(process,
                environmentalTolerances.ProcessSpeedModifier, biome, CompoundAmountType.Current, true);

            // If produces more ATP than consumes, lower down production for inputs and for outputs,
            // otherwise use maximum production values (this matches the equilibrium display mode and what happens
            // in the game once exiting the editor)
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

    private void UpdateGrowthOrderButtons()
    {
        if (selectedSelectionMenuTab == SelectionMenuTab.GrowthOrder)
        {
            growthOrderGUI.UpdateItems(
                growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable().Select(o => o.Data!)));
        }

        UpdateGrowthOrderNumbers();
    }

    private void OnResetGrowthOrderPressed()
    {
        growthOrderGUI.UpdateItems(editedMicrobeCells.AsModifiable().Select(o => o.Data!));

        UpdateGrowthOrderNumbers();
    }

    private void UpdateGrowthOrderNumbers()
    {
        UpdateFloatingLabels(GrowthOrderFloatingNumbers());
    }

    private IEnumerable<(Vector2 Position, string Text)> GrowthOrderFloatingNumbers()
    {
        var orderList = growthOrderGUI.GetCurrentOrder();
        var orderListCount = orderList.Count;

        var cells = editedMicrobeCells;
        var cellCount = cells.Count;

        for (int i = 0; i < cellCount; ++i)
        {
            var cell = cells[i];

            // TODO: fallback numbers if item not found?
            var order = -1;

            for (int j = 0; j < orderListCount; ++j)
            {
                if (ReferenceEquals(orderList[j], cell.Data!))
                {
                    // +1 to be user readable numbers
                    order = j + 1;
                    break;
                }
            }

            yield return (camera!.UnprojectPosition(Hex.AxialToCartesian(cell.Position)), order.ToString());
        }
    }

    private void OnGrowthOrderCoordinatesToggled(bool show)
    {
        growthOrderGUI.ShowCoordinates = show;
    }
}
