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
    private readonly List<Label> activeToleranceWarnings = new();

    private int usedToleranceWarnings;

    private bool changingPlayerGameteTypeAutomatically;

    public void OnReproductionMethodSelected(int selectedOption)
    {
        var selectedMethod = ReproductionMethodIndexToValue(selectedOption);

        if (ReproductionMethod == selectedMethod)
            return;

        var action = new SingleEditorAction<MulticellularReproductionActionData>(DoReproductionMethodChangeAction,
            UndoReproductionMethodChangeAction,
            new MulticellularReproductionActionData(ReproductionMethod, selectedMethod));

        Editor.EnqueueAction(action);

        UpdateReproductionMethodChoice();
    }

    public void OnSporeCellTypeSelected(int selectedOption)
    {
        var cellType = Editor.EditedSpecies.ModifiableCellTypes[selectedOption];

        if (cellType == SporeCellType)
            return;

        var action = new SingleEditorAction<SporeCellTypeChangeActionData>(DoSporeCellChangeAction,
            UndoSporeCellChangeAction, new SporeCellTypeChangeActionData(SporeCellType, cellType));

        Editor.EnqueueAction(action);

        UpdateSporeCellDropdown();
    }

    public void OnGameteACellTypeSelected(int selectedOption)
    {
        var cellType = Editor.EditedSpecies.ModifiableCellTypes[selectedOption];

        if (cellType == GameteACellType)
            return;

        var action = new SingleEditorAction<GameteACellTypeChangeActionData>(DoGameteACellChangeAction,
            UndoGameteACellChangeAction, new GameteACellTypeChangeActionData(GameteACellType, cellType));

        Editor.EnqueueAction(action);

        UpdateGameteDropdowns();
    }

    public void OnGameteBCellTypeSelected(int selectedOption)
    {
        var cellType = Editor.EditedSpecies.ModifiableCellTypes[selectedOption];

        if (cellType == GameteBCellType)
            return;

        var action = new SingleEditorAction<GameteBCellTypeChangeActionData>(DoGameteBCellChangeAction,
            UndoGameteBCellChangeAction, new GameteBCellTypeChangeActionData(GameteBCellType, cellType));

        Editor.EnqueueAction(action);

        UpdateGameteDropdowns();
    }

    public void OnMassBuddingCellCountChanged(float count)
    {
        var newCellCount = (int)count;

        if (newCellCount == DesiredMassBuddingCellCount)
            return;

        var maxValue = massBuddingCellCountSlider.MaxValue;

        // Allow the desired value to be higher than max (to handle the case of cell removal)
        if (newCellCount == maxValue && DesiredMassBuddingCellCount > maxValue)
            return;

        var action = new SingleEditorAction<MassBuddingCellCountActionData>(DoMassBuddingCellCountChangeAction,
            UndoMassBuddingCellCountChangeAction,
            new MassBuddingCellCountActionData(DesiredMassBuddingCellCount, newCellCount, editedMicrobeCells.Count));

        Editor.EnqueueAction(action);

        UpdateMassBuddingCellCountSlider();
    }

    public void SendObjectsToTutorials(TutorialState tutorial, MulticellularEditorTutorialGUI gui)
    {
        _ = tutorial;

        gui.RightPanelScrollContainer = rightPanelScrollContainer;
    }

    protected override void OnTranslationsChanged()
    {
        base.OnTranslationsChanged();

        organismStatisticsPanel.OnTranslationsChanged();

        UpdateSpecializationDisplay();
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

            var specialization =
                MicrobeInternalCalculations.CalculateSpecializationBonus(cellType.Key.ModifiableOrganelles,
                    tempMemory3);

            for (int i = 0; i < newProcesses.Count; ++i)
            {
                // Apply specialization here to approximate it in this editor
                newProcesses[i] = new TweakedProcess(newProcesses[i].Process,
                    newProcesses[i].Rate * cellType.Value * specialization)
                {
                    SpeedMultiplier = newProcesses[i].SpeedMultiplier,
                };
            }

            ProcessSystem.MergeProcessLists(processes, newProcesses);
        }

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        var environmentalTolerances =
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(Editor.CalculateRawTolerances());

        foreach (var process in processes)
        {
            // This requires the inputs to be in the biome to give a realistic prediction of how fast the processes
            // *might* run once swimming around in the stage.
            // This uses just environmental factors as we put the specialization into the above loop.
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

    private void UpdateCompoundBalances(Dictionary<Compound, CompoundBalance> balances,
        HashSet<Compound> dayNightVaryingCompoundProductions)
    {
        var warningTime = Editor.CurrentGame.GameWorld.LightCycle.DayLengthRealtimeSeconds *
            Editor.CurrentGame.GameWorld.WorldSettings.DaytimeFraction;

        // Don't show warning when day/night is not enabled
        if (!Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled)
            warningTime = 10000000;

        organismStatisticsPanel.UpdateCompoundBalances(balances, dayNightVaryingCompoundProductions, warningTime);
    }

    private void UpdateCompoundLastingTimes(Dictionary<Compound, CompoundBalance> normalBalance,
        Dictionary<Compound, CompoundBalance> nightBalance, float nominalStorage,
        Dictionary<Compound, float> specificStorages, HashSet<Compound> compoundsThatWarnFillTime)
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
            specificStorages, warningTime, fillingUpTime, compoundsThatWarnFillTime);
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

        UpdateReproductionMethodChoice();
        UpdateSporeCellDropdown();
        UpdateGameteDropdowns();
        UpdateMassBuddingCellCountSlider();

        UpdateCancelButtonVisibility();
    }

    private void UpdateGrowthOrderUI()
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
        if (!ShowGrowthOrder)
            return;

        UpdateFloatingLabelConfiguration(GrowthOrderFloatingNumbers());
    }

    private IEnumerable<(Vector3 Position, string Text, Color TextColor)> GrowthOrderFloatingNumbers()
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
                    // +1 to be user-readable numbers
                    order = j + 1;
                    break;
                }
            }

            yield return (Hex.AxialToCartesian(cell.Position), order.ToString(),
                wrongGrowthOrderCells.Contains(cell.Position) ? Colors.Red : Colors.White);
        }
    }

    private void OnGrowthOrderCoordinatesToggled(bool show)
    {
        growthOrderGUI.ShowCoordinates = show;
    }

    private void CalculateAndDisplayToleranceWarnings()
    {
        // We exclude bonuses here so that the warnings display doesn't have a partial line about a debuff and then
        // inexplicably also a bonus percentage as that would be very confusing to see.
        var tolerances = CalculateRawTolerances(true);

        MicrobeEnvironmentalToleranceCalculations.ManageToleranceProblemListGUI(ref usedToleranceWarnings,
            activeToleranceWarnings, tolerances,
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances), toleranceWarningContainer,
            toleranceWarningsFont, MaxToleranceWarnings);

        if (usedToleranceWarnings > 0)
        {
            tolerancesTabButton.Visible = true;
        }
    }

    private void OnTolerancesEditorChangedData()
    {
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }

    private void UpdateSpecializationDisplay()
    {
        double totalSpecialization = 0;
        float maxSpecialization = -1;
        string mostSpecializedCellName = Localization.Translate("NONE");

        var cells = editedMicrobeCells;

        var count = cells.Count;
        for (int i = 0; i < count; ++i)
        {
            var cell = cells[i].Data!;
            var type = GetEditedCellDataIfEdited(cell.ModifiableCellType);

            var specialization =
                MicrobeInternalCalculations.CalculateSpecializationBonus(type.ModifiableOrganelles, tempMemory3);
            var adjacencySpecialization =
                CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(cell, cells);

            totalSpecialization += specialization * adjacencySpecialization;

            if (specialization > maxSpecialization)
            {
                maxSpecialization = specialization;
                mostSpecializedCellName = type.CellTypeName;
            }
        }

        organismStatisticsPanel.UpdateCellBodyPlanSpecialization((float)(totalSpecialization / count), count,
            maxSpecialization, mostSpecializedCellName);
    }

    private void UpdateReproductionMethodChoice()
    {
        reproductionMethodDropdown.Select(ReproductionMethodToIndex(ReproductionMethod));

        buddingReproductionSection.Visible = false;
        sporeReproductionSection.Visible = false;
        massBuddingReproductionSection.Visible = false;
        sexualReproductionSection.Visible = false;

        switch (ReproductionMethod)
        {
            case MulticellularReproductionMethod.Budding:
                buddingReproductionSection.Visible = true;
                break;
            case MulticellularReproductionMethod.Sporulation:
                sporeReproductionSection.Visible = true;
                break;
            case MulticellularReproductionMethod.MassBudding:
                massBuddingReproductionSection.Visible = true;
                break;
            case MulticellularReproductionMethod.SexualIsogamy:
            case MulticellularReproductionMethod.SexualAnisogamy:
                sexualReproductionSection.Visible = true;
                break;
        }
    }

    private void UpdateSporeCellDropdown()
    {
        if (!sporeCellTypeDropdown.Visible)
            return;

        sporeCellTypeDropdown.Clear();
        foreach (var cellType in Editor.EditedSpecies.ModifiableCellTypes)
        {
            sporeCellTypeDropdown.AddItem(cellType.FormattedName);
        }

        if (SporeCellType == null)
        {
            sporeCellTypeDropdown.Select(-1);
            return;
        }

        sporeCellTypeDropdown.Select(Editor.EditedSpecies.ModifiableCellTypes.IndexOf(SporeCellType));
    }

    private void UpdateGameteDropdowns()
    {
        if (gameteACellTypeDropdown.Visible)
        {
            gameteACellTypeDropdown.Clear();

            foreach (var cellType in Editor.EditedSpecies.ModifiableCellTypes)
            {
                gameteACellTypeDropdown.AddItem(cellType.FormattedName);
            }

            if (GameteACellType == null)
            {
                gameteACellTypeDropdown.Select(-1);
            }
            else
            {
                gameteACellTypeDropdown.Select(Editor.EditedSpecies.ModifiableCellTypes.IndexOf(GameteACellType));
            }
        }

        if (!gameteBCellTypeDropdown.Visible)
            return;

        gameteBCellTypeDropdown.Clear();

        foreach (var cellType in Editor.EditedSpecies.ModifiableCellTypes)
        {
            gameteBCellTypeDropdown.AddItem(cellType.FormattedName);
        }

        if (GameteBCellType == null)
        {
            gameteBCellTypeDropdown.Select(-1);
        }
        else
        {
            gameteBCellTypeDropdown.Select(Editor.EditedSpecies.ModifiableCellTypes.IndexOf(GameteBCellType));
        }
    }

    private void UpdateAnisogamyStateAndCost()
    {
        if (ReproductionMethod == MulticellularReproductionMethod.SexualAnisogamy)
        {
            sexualAnisogamyUpgradeButton.Visible = false;
            anisogamySettingsContainer.Visible = true;
            gameteSelectionALabel.Text = Localization.Translate("GAMETE_CELL_TYPE_A");
        }
        else
        {
            sexualAnisogamyUpgradeButton.Visible = true;
            sexualAnisogamyUpgradeButton.Text =
                Localization.Translate("SEXUAL_REPRODUCTION_UPGRADE_ANISOGAMY")
                    .FormatSafe(Constants.MULTICELLULAR_ANISOGAMY_UPGRADE_COST);

            anisogamySettingsContainer.Visible = false;
            gameteSelectionALabel.Text = Localization.Translate("GAMETE_CELL_TYPE");
        }

        // Update also the selected gamete type for the player
        UpdateSelectedPlayerGameteType();
    }

    private void UpdateSelectedPlayerGameteType()
    {
        changingPlayerGameteTypeAutomatically = true;
        try
        {
            // If not using sexual reproduction, select the default option
            if (ReproductionMethod is not MulticellularReproductionMethod.SexualIsogamy
                and not MulticellularReproductionMethod.SexualAnisogamy)
            {
                playerGameteSelectionA.ButtonPressed = true;
                return;
            }

            if (SelectedGameteTypeForPlayer is GameteType.A or GameteType.All)
            {
                playerGameteSelectionA.ButtonPressed = true;
                playerGameteSelectionB.ButtonPressed = false;
            }
            else
            {
                playerGameteSelectionB.ButtonPressed = true;
                playerGameteSelectionA.ButtonPressed = false;
            }
        }
        finally
        {
            changingPlayerGameteTypeAutomatically = false;
        }
    }

    private void UpdateToAnisogamy()
    {
        if (ReproductionMethod == MulticellularReproductionMethod.SexualAnisogamy)
            return;

        if (ReproductionMethod != MulticellularReproductionMethod.SexualIsogamy)
        {
            GD.PrintErr("Invalid reproduction method to upgrade to anisogamy to");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        var action = new SingleEditorAction<MulticellularReproductionActionData>(DoReproductionMethodChangeAction,
            UndoReproductionMethodChangeAction,
            new MulticellularReproductionActionData(ReproductionMethod,
                MulticellularReproductionMethod.SexualAnisogamy));

        Editor.EnqueueAction(action);

        UpdateAnisogamyStateAndCost();
    }

    private void OnPlayerSetGameteA(bool pressed)
    {
        if (!pressed)
            return;

        if (SelectedGameteTypeForPlayer == GameteType.A)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        SelectedGameteTypeForPlayer = GameteType.A;
    }

    private void OnPlayerSetGameteB(bool pressed)
    {
        if (!pressed)
            return;

        if (SelectedGameteTypeForPlayer == GameteType.B)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        SelectedGameteTypeForPlayer = GameteType.B;
    }

    private void UpdateMassBuddingCellCountSlider()
    {
        var maxBudSize = CellBodyPlanInternalCalculations.MaxBudSize(editedMicrobeCells.Count);

        var clampedBudSize = Math.Min(DesiredMassBuddingCellCount, maxBudSize);

        massBuddingCellCountSlider.MaxValue = maxBudSize;
        massBuddingCellCountSlider.SetValueNoSignal(clampedBudSize);

        massBuddingCellCountLabel.Text = clampedBudSize.ToString();
    }

    private int ReproductionMethodToIndex(MulticellularReproductionMethod reproductionMethod)
    {
        switch (reproductionMethod)
        {
            case MulticellularReproductionMethod.Budding:
                return 0;
            case MulticellularReproductionMethod.Sporulation:
                return 2;
            case MulticellularReproductionMethod.MassBudding:
                return 1;
            case MulticellularReproductionMethod.SexualIsogamy or MulticellularReproductionMethod.SexualAnisogamy:
                return 3;
            default:
                throw new Exception($"Invalid reproduction mode: {reproductionMethod}");
        }
    }

    private MulticellularReproductionMethod ReproductionMethodIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return MulticellularReproductionMethod.Budding;
            case 1:
                return MulticellularReproductionMethod.MassBudding;
            case 2:
                return MulticellularReproductionMethod.Sporulation;
            case 3:
                return MulticellularReproductionMethod.SexualIsogamy;
            default:
                throw new Exception($"Invalid reproduction mode index: {index}");
        }
    }

    // These next 4 methods related to endosymbiosis are copied from the CellEditor as there's no easy way to share
    // this code
    private void UpdateEndosymbiosisSpeciesData()
    {
        // Multicellular is never prokaryotic so we don't read the flag here whether it is bacteria or not
        endosymbiosisPopup.UpdateData(Editor.EditedBaseSpecies.Endosymbiosis,
            false, Editor.CurrentPatch.SpeciesInPatch.Keys);
    }

    private void OnEndosymbiosisSelected(int targetSpecies, string targetOrganelle, int cost)
    {
        if (Editor.EditedBaseSpecies.Endosymbiosis.StartedEndosymbiosis != null)
        {
            GD.PrintErr("Already has endosymbiosis in-progress");
            PlayInvalidActionSound();
            endosymbiosisPopup.Hide();
            return;
        }

        var organelle = SimulationParameters.Instance.GetOrganelleType(targetOrganelle);

        if (!Editor.EditedBaseSpecies.Endosymbiosis.StartEndosymbiosis(targetSpecies, organelle, cost))
        {
            GD.PrintErr("Endosymbiosis failed to be started");
            PlayInvalidActionSound();
        }
    }

    private void OnAbandonEndosymbiosisOperation(int targetSpeciesId)
    {
        if (!Editor.EditedBaseSpecies.Endosymbiosis.CancelEndosymbiosisTarget(targetSpeciesId))
        {
            GD.PrintErr("Couldn't cancel endosymbiosis operation on target species: ", targetSpeciesId);
            PlayInvalidActionSound();
        }
    }

    private void OnEndosymbiosisButtonPressed()
    {
        // Disallow if currently has an inprogress action as that would complicate logic and allow rare bugs
        if (CanCancelAction)
        {
            GD.Print("Not allowing opening endosymbiosis menu with a pending action");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        endosymbiosisPopup.Lawk = Editor.CurrentGame.GameWorld.WorldSettings.LAWK;

        UpdateEndosymbiosisSpeciesData();

        endosymbiosisPopup.OpenCentered(false);
    }

    private void ConfirmFinishEditingWithEndosymbiosis()
    {
        if (OnFinish == null)
        {
            GD.PrintErr("Confirmed editing for multicellular when finish callback is not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        ignoredEditorWarnings.Add(EditorUserOverride.EndosymbiosisPending);
        OnFinish.Invoke(ignoredEditorWarnings);
    }
}
