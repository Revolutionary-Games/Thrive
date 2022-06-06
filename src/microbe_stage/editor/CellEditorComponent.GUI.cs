﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Partial class to mostly separate the GUI interacting parts from the cell editor
/// </summary>
/// <remarks>
///   <para>
///     This is done this way as multiple scripts can't be attached to the same node). And this is done in the first
///     place because otherwise the CellEditorComponent class would be a very long file.
///   </para>
/// </remarks>
public partial class CellEditorComponent
{
    private bool cellStatsIndicatorsDirty = true;

    private Texture questionIcon = null!;

    [Signal]
    public delegate void Clicked();

    /// <summary>
    ///   Detects presses anywhere to notify the name input to unfocus
    /// </summary>
    /// <param name="event">The input event</param>
    /// <remarks>
    ///   <para>
    ///     This doesn't use <see cref="Control._GuiInput"/> as this needs to always see events, even ones that are
    ///     handled elsewhere
    ///   </para>
    /// </remarks>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true })
        {
            EmitSignal(nameof(Clicked));
        }
    }

    public void SendUndoRedoToTutorial(TutorialState tutorial)
    {
        tutorial.EditorUndoTutorial.EditorUndoButtonControl = componentBottomLeftButtons.UndoButton;
        tutorial.EditorRedoTutorial.EditorRedoButtonControl = componentBottomLeftButtons.RedoButton;

        tutorial.AutoEvoPrediction.EditorAutoEvoPredictionPanel = autoEvoPredictionPanel;
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
        ToolTipManager.Instance.ShowPopup(
            TranslationServer.Translate("ACTION_BLOCKED_WHILE_ANOTHER_IN_PROGRESS"), 1.5f);
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        rigiditySlider.RegisterToolTipForControl("rigiditySlider", "editor");
    }

    protected override void OnTranslationsChanged()
    {
        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();

        CalculateOrganelleEffectivenessInPatch(Editor.CurrentPatch);
        UpdatePatchDependentBalanceData();

        UpdateMicrobePartSelections();
        UpdateMutationPointsBar();
    }

    private void UpdateCellStatsIndicators()
    {
        sizeIndicator.Show();

        if (MicrobeHexSize > initialCellSize)
        {
            sizeIndicator.Texture = increaseIcon;
        }
        else if (MicrobeHexSize < initialCellSize)
        {
            sizeIndicator.Texture = decreaseIcon;
        }
        else
        {
            sizeIndicator.Hide();
        }

        speedIndicator.Show();

        var speed = CalculateSpeed();
        if (speed > initialCellSpeed)
        {
            speedIndicator.Texture = increaseIcon;
        }
        else if (speed < initialCellSpeed)
        {
            speedIndicator.Texture = decreaseIcon;
        }
        else
        {
            speedIndicator.Hide();
        }

        rotationSpeedIndicator.Show();

        var rotationSpeed = CalculateRotationSpeed();
        if (rotationSpeed > initialRotationSpeed)
        {
            rotationSpeedIndicator.Texture = increaseIcon;
        }
        else if (rotationSpeed < initialRotationSpeed)
        {
            rotationSpeedIndicator.Texture = decreaseIcon;
        }
        else
        {
            rotationSpeedIndicator.Hide();
        }

        hpIndicator.Show();

        if (CalculateHitpoints() > initialCellHp)
        {
            hpIndicator.Texture = increaseIcon;
        }
        else if (CalculateHitpoints() < initialCellHp)
        {
            hpIndicator.Texture = decreaseIcon;
        }
        else
        {
            hpIndicator.Hide();
        }

        storageIndicator.Show();

        if (CalculateStorage() > initialCellStorage)
        {
            storageIndicator.Texture = increaseIcon;
        }
        else if (CalculateStorage() < initialCellStorage)
        {
            storageIndicator.Texture = decreaseIcon;
        }
        else
        {
            storageIndicator.Hide();
        }
    }

    private void CheckRunningAutoEvoPrediction()
    {
        if (waitingForPrediction?.Finished != true)
            return;

        OnAutoEvoPredictionComplete(waitingForPrediction);
        waitingForPrediction = null;
    }

    private void SetMembraneTooltips(MembraneType referenceMembrane)
    {
        // Pass in a membrane that the values are taken as relative to
        foreach (var membraneType in SimulationParameters.Instance.GetAllMembranes())
        {
            var tooltip = GetSelectionTooltip(membraneType.InternalName, "membraneSelection");
            tooltip?.WriteMembraneModifierList(referenceMembrane, membraneType);
        }
    }

    /// <summary>
    ///   Updates the fluidity / rigidity slider tooltip
    /// </summary>
    private void SetRigiditySliderTooltip(int rigidity)
    {
        float convertedRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;

        var rigidityTooltip = GetSelectionTooltip("rigiditySlider", "editor");

        if (rigidityTooltip == null)
            throw new InvalidOperationException("Could not find rigidity tooltip");

        var healthModifier = rigidityTooltip.GetModifierInfo("health");
        var mobilityModifier = rigidityTooltip.GetModifierInfo("mobility");

        float healthChange = convertedRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
        float mobilityChange = -1 * convertedRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER;

        healthModifier.ModifierValue = ((healthChange >= 0) ? "+" : string.Empty)
            + healthChange.ToString("F0", CultureInfo.CurrentCulture);

        mobilityModifier.ModifierValue = ((mobilityChange >= 0) ? "+" : string.Empty)
            + mobilityChange.ToString("P0", CultureInfo.CurrentCulture);

        healthModifier.AdjustValueColor(healthChange);
        mobilityModifier.AdjustValueColor(mobilityChange);
    }

    private void UpdateRigiditySliderState(int mutationPoints)
    {
        int costPerStep = (int)(Constants.MEMBRANE_RIGIDITY_COST_PER_STEP * editorCostFactor);
        if (mutationPoints >= costPerStep && MovingPlacedHex == null)
        {
            rigiditySlider.Editable = true;
        }
        else
        {
            rigiditySlider.Editable = false;
        }
    }

    private void UpdateSize(int size)
    {
        sizeLabel.Text = size.ToString(CultureInfo.CurrentCulture);

        cellStatsIndicatorsDirty = true;
    }

    private void UpdateGeneration(int generation)
    {
        generationLabel.Text = generation.ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateSpeed(float speed)
    {
        speedLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", speed);

        cellStatsIndicatorsDirty = true;
    }

    private void UpdateRotationSpeed(float speed)
    {
        rotationSpeedLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}",
            MicrobeInternalCalculations.RotationSpeedToUserReadableNumber(speed));

        cellStatsIndicatorsDirty = true;
    }

    private void UpdateHitpoints(float hp)
    {
        hpLabel.Text = hp.ToString(CultureInfo.CurrentCulture);

        cellStatsIndicatorsDirty = true;
    }

    private void UpdateStorage(float storage)
    {
        storageLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", storage);

        cellStatsIndicatorsDirty = true;
    }

    /// <summary>
    ///   Updates the organelle efficiencies in tooltips.
    /// </summary>
    private void UpdateOrganelleEfficiencies(Dictionary<string, OrganelleEfficiency> organelleEfficiency)
    {
        foreach (var organelleInternalName in organelleEfficiency.Keys)
        {
            if (organelleInternalName == protoplasm.InternalName)
                continue;

            var tooltip = GetSelectionTooltip(organelleInternalName, "organelleSelection");
            tooltip?.WriteOrganelleProcessList(organelleEfficiency[organelleInternalName].Processes);
        }
    }

    private void UpdateOrganelleUnlockTooltips()
    {
        var organelles = SimulationParameters.Instance.GetAllOrganelles();
        foreach (var organelle in organelles)
        {
            if (organelle.InternalName == protoplasm.InternalName)
                continue;

            var tooltip = GetSelectionTooltip(organelle.InternalName, "organelleSelection");
            if (tooltip != null)
            {
                tooltip.RequiresNucleus = organelle.RequiresNucleus && !HasNucleus;
            }
        }
    }

    private SelectionMenuToolTip? GetSelectionTooltip(string name, string group)
    {
        return (SelectionMenuToolTip?)ToolTipManager.Instance.GetToolTip(name, group);
    }

    /// <summary>
    ///   Updates the MP costs in organelle, membrane, and rigidity tooltips by setting the cost factor for each one
    /// </summary>
    private void UpdateTooltipMPCostFactors()
    {
        var organelleNames = SimulationParameters.Instance.GetAllOrganelles().Select(o => o.InternalName);
        foreach (string name in organelleNames)
        {
            if (name == protoplasm.InternalName)
                continue;

            var tooltip = GetSelectionTooltip(name, "organelleSelection");
            if (tooltip != null)
                tooltip.EditorCostFactor = editorCostFactor;
        }

        var membraneNames = SimulationParameters.Instance.GetAllMembranes().Select(m => m.InternalName);
        foreach (var name in membraneNames)
        {
            var tooltip = GetSelectionTooltip(name, "membraneSelection");
            if (tooltip != null)
                tooltip.EditorCostFactor = editorCostFactor;
        }

        var rigidityTooltip = GetSelectionTooltip("rigiditySlider", "editor");
        if (rigidityTooltip != null)
            rigidityTooltip.EditorCostFactor = editorCostFactor;
    }

    private void UpdateCompoundBalances(Dictionary<Compound, CompoundBalance> balances)
    {
        compoundBalance.UpdateBalances(balances);
    }

    private void UpdateEnergyBalance(EnergyBalanceInfo energyBalance)
    {
        energyBalanceInfo = energyBalance;

        if (energyBalance.FinalBalance > 0)
        {
            atpBalanceLabel.Text = TranslationServer.Translate("ATP_PRODUCTION");
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        }
        else
        {
            atpBalanceLabel.Text = TranslationServer.Translate("ATP_PRODUCTION") + " - " +
                TranslationServer.Translate("ATP_PRODUCTION_TOO_LOW");
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f));
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
        foreach (var subBar in atpProductionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesProduction");

            if (tooltip == null)
                throw new InvalidOperationException("Could not find process production tooltip");

            subBar.RegisterToolTipForControl(tooltip);

            tooltip.Description = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_PRODUCTION"),
                SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name,
                energyBalance.Production[subBar.Name]);
        }

        foreach (var subBar in atpConsumptionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesConsumption");

            if (tooltip == null)
                throw new InvalidOperationException("Could not find process consumption tooltip");

            subBar.RegisterToolTipForControl(tooltip);

            string displayName;

            switch (subBar.Name)
            {
                case "osmoregulation":
                {
                    displayName = TranslationServer.Translate("OSMOREGULATION");
                    break;
                }

                case "baseMovement":
                {
                    displayName = TranslationServer.Translate("BASE_MOVEMENT");
                    break;
                }

                default:
                {
                    displayName = SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name;
                    break;
                }
            }

            tooltip.Description = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_CONSUMPTION"), displayName,
                energyBalance.Consumption[subBar.Name]);
        }
    }

    private void UpdateAutoEvoPrediction(EditorAutoEvoRun startedRun, Species playerSpeciesOriginal,
        MicrobeSpecies playerSpeciesNew)
    {
        if (waitingForPrediction != null)
        {
            GD.PrintErr(
                $"{nameof(CancelPreviousAutoEvoPrediction)} has not been called before starting a new prediction");
        }

        totalPopulationIndicator.Show();
        totalPopulationIndicator.Texture = questionIcon;

        var prediction = new PendingAutoEvoPrediction(startedRun, playerSpeciesOriginal, playerSpeciesNew);

        if (startedRun.Finished)
        {
            OnAutoEvoPredictionComplete(prediction);
            waitingForPrediction = null;
        }
        else
        {
            waitingForPrediction = prediction;
        }
    }

    /// <summary>
    ///   Cancels the previous auto-evo prediction run if there is one
    /// </summary>
    private void CancelPreviousAutoEvoPrediction()
    {
        if (waitingForPrediction == null)
            return;

        GD.Print("Canceling previous auto-evo prediction run as it didn't manage to finish yet");
        waitingForPrediction.AutoEvoRun.Abort();
        waitingForPrediction = null;
    }

    /// <summary>
    ///   Updates the values of all part selections from their associated part types.
    /// </summary>
    private void UpdateMicrobePartSelections()
    {
        foreach (var entry in placeablePartSelectionElements)
        {
            entry.Value.PartName = entry.Key.Name;
            entry.Value.MPCost = (int)(entry.Key.MPCost * editorCostFactor);
            entry.Value.PartIcon = entry.Key.LoadedIcon;
        }

        foreach (var entry in membraneSelectionElements)
        {
            entry.Value.PartName = entry.Key.Name;
            entry.Value.MPCost = (int)(entry.Key.EditorCost * editorCostFactor);
            entry.Value.PartIcon = entry.Key.LoadedIcon;
        }
    }

    private List<KeyValuePair<string, float>> SortBarData(Dictionary<string, float> bar)
    {
        var comparer = new ATPComparer();

        return bar.OrderBy(i => i.Key, comparer).ToList();
    }

    private void ConfirmFinishEditingWithNegativeATPPressed()
    {
        if (OnFinish == null)
        {
            GD.PrintErr("Confirmed editing for cell editor when finish callback is not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        // If we add more things that can be overridden this needs to be updated
        OnFinish.Invoke(new List<EditorUserOverride> { EditorUserOverride.NotProducingEnoughATP });
    }

    private void UpdateGUIAfterLoadingSpecies(Species species, ICellProperties properties)
    {
        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe");

        // Update GUI buttons now that we have correct organelles
        UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        // Reset to cytoplasm if nothing is selected
        OnOrganelleToPlaceSelected(ActiveActionName ?? "cytoplasm");

        SetSpeciesInfo(newName, Membrane, Colour, Rigidity, behaviourEditor.Behaviour);
        UpdateGeneration(species.Generation);
        UpdateHitpoints(CalculateHitpoints());
        UpdateStorage(CalculateStorage());
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
        public int Compare(string stringA, string stringB)
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
