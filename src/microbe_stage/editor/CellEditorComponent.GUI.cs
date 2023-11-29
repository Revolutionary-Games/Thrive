using System;
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

    public void SendHighlightsToTutorial(TutorialState tutorial)
    {
        tutorial.EditorUndoTutorial.EditorUndoButtonControl = componentBottomLeftButtons.UndoButton;
        tutorial.EditorRedoTutorial.EditorRedoButtonControl = componentBottomLeftButtons.RedoButton;

        tutorial.AutoEvoPrediction.EditorAutoEvoPredictionPanel = autoEvoPredictionPanel;
        tutorial.AtpBalanceIntroduction.ATPBalanceBarControl = atpBalancePanel;
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
        digestionEfficiencyLabel.RegisterToolTipForControl("digestionEfficiencyDetails", "editor");
        storageLabel.RegisterToolTipForControl("storageDetails", "editor");
    }

    protected override void OnTranslationsChanged()
    {
        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();

        CalculateOrganelleEffectivenessInCurrentPatch();
        UpdatePatchDependentBalanceData();

        UpdateMicrobePartSelections();
        UpdateMutationPointsBar();

        UpdateDigestionEfficiencies(CalculateDigestionEfficiencies());
        UpdateTotalDigestionSpeed(CalculateTotalDigestionSpeed());
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

        // Osmoregulation cost is based on the membrane, so update the osmoregulation tooltips
        // after updating the membrane
        UpdateOsmoregulationTooltips();
    }

    private void UpdateOsmoregulationTooltips()
    {
        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        float osmoregulationCostPerHex = Membrane.OsmoregulationFactor * Constants.ATP_COST_FOR_OSMOREGULATION
            * Editor.CurrentGame.GameWorld.WorldSettings.OsmoregulationMultiplier;

        foreach (var organelle in organelles)
        {
            // Don't bother updating the tooltips for organelles that aren't even shown
            if (organelle.Unimplemented || organelle.EditorButtonGroup == OrganelleDefinition.OrganelleGroup.Hidden)
                continue;

            float osmoregulationCost = organelle.HexCount * osmoregulationCostPerHex;

            var tooltip = GetSelectionTooltip(organelle.InternalName, "organelleSelection");

            if (tooltip != null)
                tooltip.OsmoregulationCost = osmoregulationCost;
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
        var baseMobilityModifier = rigidityTooltip.GetModifierInfo("baseMobility");

        float healthChange = convertedRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
        float baseMobilityChange = -1 * convertedRigidity * Constants.MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER;

        healthModifier.ModifierValue =
            StringUtils.FormatPositiveWithLeadingPlus(healthChange.ToString("F0", CultureInfo.CurrentCulture),
                healthChange);

        baseMobilityModifier.ModifierValue =
            StringUtils.FormatPositiveWithLeadingPlus(baseMobilityChange.ToString("P0", CultureInfo.CurrentCulture),
                baseMobilityChange);

        healthModifier.AdjustValueColor(healthChange);
        baseMobilityModifier.AdjustValueColor(baseMobilityChange);
    }

    private void UpdateSize(int size)
    {
        sizeLabel.Value = size;
    }

    private void UpdateGeneration(int generation)
    {
        generationLabel.Text = generation.ToString(CultureInfo.CurrentCulture);
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

    private void UpdateHitpoints(float hp)
    {
        hpLabel.Value = hp;
    }

    private void UpdateStorage(float nominalStorage, Dictionary<Compound, float> storage)
    {
        storageLabel.Value = (float)Math.Round(nominalStorage, 1);

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

        foreach (var entry in storage)
        {
            if (!first)
                description.Append("\n");

            first = false;

            description.Append(entry.Key.Name);
            description.Append(": ");
            description.Append(entry.Value);
        }

        tooltip.Description = description.ToString();
    }

    private void UpdateTotalDigestionSpeed(float speed)
    {
        digestionSpeedLabel.Format = TranslationServer.Translate("DIGESTION_SPEED_VALUE");
        digestionSpeedLabel.Value = (float)Math.Round(speed, 2);
    }

    private void UpdateDigestionEfficiencies(Dictionary<Enzyme, float> efficiencies)
    {
        if (efficiencies.Count == 1)
        {
            digestionEfficiencyLabel.Format = TranslationServer.Translate("PERCENTAGE_VALUE");
            digestionEfficiencyLabel.Value = (float)Math.Round(efficiencies.First().Value * 100, 2);
        }
        else
        {
            digestionEfficiencyLabel.Format = TranslationServer.Translate("MIXED_DOT_DOT_DOT");

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

    /// <summary>
    ///   Updates the organelle efficiencies in tooltips.
    /// </summary>
    private void UpdateOrganelleEfficiencies(Dictionary<string, OrganelleEfficiency> organelleEfficiency)
    {
        foreach (var organelleInternalName in organelleEfficiency.Keys)
        {
            var efficiency = organelleEfficiency[organelleInternalName];

            if (efficiency.Organelle.Unimplemented ||
                efficiency.Organelle.EditorButtonGroup == OrganelleDefinition.OrganelleGroup.Hidden)
            {
                continue;
            }

            var tooltip = GetSelectionTooltip(organelleInternalName, "organelleSelection");
            tooltip?.WriteOrganelleProcessList(efficiency.Processes);
        }
    }

    private void UpdateOrganelleUnlockTooltips()
    {
        var organelles = SimulationParameters.Instance.GetAllOrganelles();
        foreach (var organelle in organelles)
        {
            if (organelle.Unimplemented || organelle.EditorButtonGroup == OrganelleDefinition.OrganelleGroup.Hidden)
                continue;

            var tooltip = GetSelectionTooltip(organelle.InternalName, "organelleSelection");
            if (tooltip != null)
            {
                tooltip.RequiresNucleus = organelle.RequiresNucleus && !HasNucleus;
            }
        }
    }

    private void UpdateOrganelleLAWKSettings()
    {
        // Don't use placeablePartSelectionElements as the thermoplast isn't placeable yet but is LAWK-dependent
        foreach (var entry in allPartSelectionElements)
        {
            entry.Value.Visible = !Editor.CurrentGame.GameWorld.WorldSettings.LAWK || entry.Key.LAWK;
        }
    }

    private SelectionMenuToolTip? GetSelectionTooltip(string name, string group)
    {
        return (SelectionMenuToolTip?)ToolTipManager.Instance.GetToolTip(name, group);
    }

    /// <summary>
    ///   Updates the MP costs for organelle, membrane, and rigidity button lists and tooltips. Taking into account
    ///   MP cost factor.
    /// </summary>
    private void UpdateMPCost()
    {
        // Set the cost factor for each organelle button
        foreach (var entry in placeablePartSelectionElements)
        {
            var cost = (int)Math.Min(entry.Key.MPCost * CostMultiplier, 100);

            entry.Value.MPCost = cost;

            // Set the cost factor for each organelle tooltip
            var tooltip = GetSelectionTooltip(entry.Key.InternalName, "organelleSelection");
            if (tooltip != null)
                tooltip.MutationPointCost = cost;
        }

        // Set the cost factor for each membrane button
        foreach (var entry in membraneSelectionElements)
        {
            var cost = (int)Math.Min(entry.Key.EditorCost * CostMultiplier, 100);

            entry.Value.MPCost = cost;

            // Set the cost factor for each membrane tooltip
            var tooltip = GetSelectionTooltip(entry.Key.InternalName, "membraneSelection");
            if (tooltip != null)
                tooltip.MutationPointCost = cost;
        }

        // Set the cost factor for the rigidity tooltip
        var rigidityTooltip = GetSelectionTooltip("rigiditySlider", "editor");
        if (rigidityTooltip != null)
        {
            rigidityTooltip.MutationPointCost = (int)Math.Min(
                Constants.MEMBRANE_RIGIDITY_COST_PER_STEP * CostMultiplier, 100);
        }
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

        TutorialState?.SendEvent(TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged,
            new EnergyBalanceEventArgs(energyBalance), this);

        UpdateEnergyBalanceToolTips(energyBalance);
    }

    private void UpdateEnergyBalanceToolTips(EnergyBalanceInfo energyBalance)
    {
        foreach (var subBar in atpProductionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesProduction");

            if (tooltip == null)
                throw new InvalidOperationException("Could not find process production tooltip");

            subBar.RegisterToolTipForControl(tooltip, true);

            tooltip.Description = TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_PRODUCTION").FormatSafe(
                SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name,
                energyBalance.Production[subBar.Name]);
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

            tooltip.Description = TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_CONSUMPTION").FormatSafe(
                displayName, energyBalance.Consumption[subBar.Name]);
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

        totalPopulationLabel.Value = float.NaN;

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
            entry.Value.MPCost = (int)(entry.Key.MPCost * CostMultiplier);
            entry.Value.PartIcon = entry.Key.LoadedIcon;
        }

        foreach (var entry in membraneSelectionElements)
        {
            entry.Value.PartName = entry.Key.Name;
            entry.Value.MPCost = (int)(entry.Key.EditorCost * CostMultiplier);
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
        ApplySymmetryForCurrentOrganelle();

        SetSpeciesInfo(newName, Membrane, Colour, Rigidity, behaviourEditor.Behaviour);
        UpdateGeneration(species.Generation);
        UpdateHitpoints(CalculateHitpoints());
        UpdateStorage(GetNominalCapacity(), GetAdditionalCapacities());

        ApplyLightLevelOption();
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
