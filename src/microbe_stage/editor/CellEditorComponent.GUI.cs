﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using UnlockConstraints;

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
    private readonly Dictionary<int, GrowthOrderLabel> createdGrowthOrderLabels = new();

    private StringBuilder atpToolTipTextBuilder = new();

    private bool inProgressSuggestionCheckRunning;

    [Signal]
    public delegate void ClickedEventHandler();

    /// <summary>
    ///   Detects presses anywhere to notify the name input to defocus
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
            EmitSignal(SignalName.Clicked);
        }
    }

    public void SendObjectsToTutorials(TutorialState tutorial, MicrobeEditorTutorialGUI gui)
    {
        tutorial.EditorUndoTutorial.EditorUndoButtonControl = componentBottomLeftButtons.UndoButton;
        tutorial.EditorRedoTutorial.EditorRedoButtonControl = componentBottomLeftButtons.RedoButton;

        tutorial.AutoEvoPrediction.EditorAutoEvoPredictionPanel = autoEvoPredictionPanel;

        tutorial.AtpBalanceIntroduction.ATPBalanceBarControl = atpBalancePanel;

        gui.RightPanelScrollContainer = rightPanelScrollContainer;
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
        ToolTipManager.Instance.ShowPopup(Localization.Translate("ACTION_BLOCKED_WHILE_ANOTHER_IN_PROGRESS"),
            1.5f);
    }

    public void UnlockAllOrganelles()
    {
        foreach (var entry in allPartSelectionElements)
            entry.Value.Show();

        UpdateOrganelleLAWKSettings();

        RemoveUndiscoveredOrganelleButtons();
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

        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);

        UpdateOsmoregulationTooltips();
        UpdateMPCost();
    }

    private void CheckRunningAutoEvoPrediction()
    {
        if (waitingForPrediction?.Finished != true)
            return;

        OnAutoEvoPredictionComplete(waitingForPrediction);
        waitingForPrediction = null;
    }

    private void CheckRunningSuggestion(double delta)
    {
        suggestionStartTimer += delta;

        if (suggestionStartTimer > 1)
        {
            suggestionStartTimer = 0;

            if (suggestionDirty || inProgressSuggestion == null)
            {
                if (inProgressSuggestion == null)
                {
                    var suggestionSpecies = new MicrobeSpecies(Editor.EditedBaseSpecies,
                        Editor.EditedCellProperties ??
                        throw new InvalidOperationException(
                            "can't start auto-evo suggestion without current cell properties"),
                        hexTemporaryMemory, hexTemporaryMemory2);

                    // For this use-case it is probably not critical to clone the player species flag (as only
                    // comparative numbers are used, but if anyone checks this code and writes something based on this,
                    // this is done fully correctly)
                    if (Editor.EditedBaseSpecies.PlayerSpecies)
                    {
                        suggestionSpecies.BecomePlayerSpecies();
                    }

                    inProgressSuggestion ??=
                        new OrganelleSuggestionCalculation(suggestionSpecies, CopyEditedPropertiesToSpecies,
                            Editor.CurrentGame, Editor.EditedBaseSpecies);
                }

                inProgressSuggestion.StartNew(DetectAvailableOrganelles(), Editor.CurrentPatch);
                suggestionDirty = false;
            }
        }
        else if (inProgressSuggestion != null)
        {
            if (inProgressSuggestion.IsCompleted)
            {
                if (inProgressSuggestion.ReadAndResetResultFlag())
                {
                    organelleSuggestionLoadingIndicator.Visible = false;
                    organelleSuggestionLabel.Visible = true;

                    var result = inProgressSuggestion.GetResult();

                    if (result == null)
                    {
                        organelleSuggestionLabel.Text = Localization.Translate("NO_SUGGESTION");
                    }
                    else
                    {
                        organelleSuggestionLabel.Text = result.UntranslatedName;
                    }
                }
            }
            else
            {
                // This uses background checking as some pretty expensive organelle positioning logic can be triggered
                if (!inProgressSuggestionCheckRunning)
                {
                    inProgressSuggestionCheckRunning = true;

                    // This allocates a task each frame, but as this would be hard to workaround and is in the editor,
                    // this is just left like this
                    TaskExecutor.Instance.AddTask(new Task(CheckSuggestionProgress));
                }
            }
        }
    }

    private void CheckSuggestionProgress()
    {
        try
        {
            // This should only be queued when this is not null
            inProgressSuggestion!.CheckProgress();
        }
        finally
        {
            inProgressSuggestionCheckRunning = false;
        }
    }

    /// <summary>
    ///   Checks the GUI button statuses to find the organelles available to the player
    /// </summary>
    private List<OrganelleDefinition> DetectAvailableOrganelles()
    {
        var result = new List<OrganelleDefinition>();

        foreach (var entry in placeablePartSelectionElements)
        {
            if (entry.Value.Undiscovered || entry.Value.Locked)
                continue;

            result.Add(entry.Key);
        }

        return result;
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

        // Don't show negative zero
        if (baseMobilityChange == 0 && float.IsNegative(baseMobilityChange))
            baseMobilityChange = 0;

        if (healthModifier != null)
        {
            healthModifier.ModifierValue =
                StringUtils.FormatPositiveWithLeadingPlus(healthChange.ToString("F0", CultureInfo.CurrentCulture),
                    healthChange);

            healthModifier.AdjustValueColor(healthChange);
        }
        else
        {
            GD.PrintErr("Missing health modifier in rigidity tooltip");
        }

        if (baseMobilityModifier != null)
        {
            baseMobilityModifier.ModifierValue =
                StringUtils.FormatPositiveWithLeadingPlus(baseMobilityChange.ToString("P0", CultureInfo.CurrentCulture),
                    baseMobilityChange);

            baseMobilityModifier.AdjustValueColor(baseMobilityChange);
        }
        else
        {
            GD.PrintErr("Missing base mobility modifier in rigidity tooltip");
        }
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

    private void UpdateTotalDigestionSpeed(float speed)
    {
        digestionSpeedLabel.Format = Localization.Translate("DIGESTION_SPEED_VALUE");
        digestionSpeedLabel.Value = (float)Math.Round(speed, 2);
    }

    private void UpdateDigestionEfficiencies(Dictionary<Enzyme, float> efficiencies)
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

    private void UpdateOrganelleUnlockTooltips(bool autoUnlockOrganelles)
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

        CreateUndiscoveredOrganellesButtons(true, autoUnlockOrganelles);
    }

    private void UpdateOrganelleLAWKSettings()
    {
        foreach (var entry in allPartSelectionElements)
        {
            if (Editor.CurrentGame.GameWorld.WorldSettings.LAWK && !entry.Key.LAWK)
                entry.Value.Hide();
        }
    }

    private void CreateUndiscoveredOrganellesButtons(bool refresh = false, bool autoUnlock = true)
    {
        // Note that if autoUnlock is true and this is called after the editor is initialized there's a potential
        // logic conflict with UndoEndosymbiontPlaceAction in case we ever decide to allow organelle actions to also
        // occur after entering the editor (other than endosymbiosis unlocks)

        // Find groups with undiscovered organelles
        var groupsWithUndiscoveredOrganelles =
            new Dictionary<OrganelleDefinition.OrganelleGroup, (LocalizedStringBuilder UnlockText, int Count)>();

        var worldAndPlayerArgs = GetUnlockPlayerDataSource();

        foreach (var entry in allPartSelectionElements)
        {
            var organelle = entry.Key;
            var control = entry.Value;

            // Skip already unlocked organelles
            if (Editor.CurrentGame.GameWorld.UnlockProgress.IsUnlocked(organelle, worldAndPlayerArgs,
                    Editor.CurrentGame, autoUnlock))
            {
                control.Undiscovered = false;

                control.Show();
                continue;
            }

            // Skip hidden organelles unless they are hidden because of missing requirements
            if (!control.Visible && !control.Undiscovered)
                continue;

            control.Hide();
            control.Undiscovered = true;

            // Skip adding unlock conditions for organelles prevented by LAWK setting
            if (Editor.CurrentGame.GameWorld.WorldSettings.LAWK && !organelle.LAWK)
            {
                continue;
            }

            var buttonGroup = organelle.EditorButtonGroup;

            // This needs to be done as some organelles like the Toxin Vacuole have newlines in the translations
            var formattedName = organelle.Name.Replace("\n", " ");
            var unlockTextString = new LocalizedString("UNLOCK_WITH_ANY_OF_FOLLOWING", formattedName);

            // Create unlock text
            if (groupsWithUndiscoveredOrganelles.TryGetValue(buttonGroup, out var group))
            {
                // Add a new organelle to the group
                group.Count += 1;
                group.UnlockText.Append("\n\n");
                group.UnlockText.Append(unlockTextString);
                group.UnlockText.Append(" ");
                organelle.GenerateUnlockRequirementsText(group.UnlockText, worldAndPlayerArgs);
                groupsWithUndiscoveredOrganelles[buttonGroup] = group;
            }
            else
            {
                // Add the first organelle to the group
                var unlockText = new LocalizedStringBuilder();

                unlockText.Append(new LocalizedString("ORGANELLES_WILL_BE_UNLOCKED_NEXT_GENERATION"));
                unlockText.Append("\n\n");

                unlockText.Append(unlockTextString);
                unlockText.Append(" ");
                organelle.GenerateUnlockRequirementsText(unlockText, worldAndPlayerArgs);
                groupsWithUndiscoveredOrganelles.Add(buttonGroup, (unlockText, 1));
            }
        }

        // Remove any buttons that might've been created before
        if (refresh)
            RemoveUndiscoveredOrganelleButtons();

        // Generate undiscovered organelle buttons
        foreach (var groupWithUndiscovered in groupsWithUndiscoveredOrganelles)
        {
            var group = partsSelectionContainer.GetNode<CollapsibleList>(groupWithUndiscovered.Key.ToString());
            var (unlockText, count) = groupWithUndiscovered.Value;

            var button = undiscoveredOrganellesScene.Instantiate<UndiscoveredOrganellesButton>();
            button.Count = count;
            group.AddItem(button);

            // Register tooltip
            var tooltip = undiscoveredOrganellesTooltipScene.Instantiate<UndiscoveredOrganellesTooltip>();
            tooltip.UnlockText = unlockText;
            ToolTipManager.Instance.AddToolTip(tooltip, "lockedOrganelles");
            button.RegisterToolTipForControl(tooltip, true);
        }

        // Apply LAWK settings so that no-unexpected organelles are shown
        UpdateOrganelleLAWKSettings();
    }

    private void RemoveUndiscoveredOrganelleButtons()
    {
        foreach (var child in partsSelectionContainer.GetChildren())
        {
            if (child is CollapsibleList list)
                list.RemoveAllOfType<UndiscoveredOrganellesButton>();
        }

        ToolTipManager.Instance.ClearToolTips("lockedOrganelles", false);
    }

    private void OnUnlockedOrganellesChanged()
    {
        UpdateMicrobePartSelections();
        CreateUndiscoveredOrganellesButtons(true, false);
        UpdateOrganelleButtons(activeActionName);
    }

    private WorldAndPlayerDataSource GetUnlockPlayerDataSource()
    {
        return new WorldAndPlayerDataSource(Editor.CurrentGame.GameWorld, Editor.CurrentPatch,
            energyBalanceInfo, Editor.EditedCellProperties);
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

    private void UpdateLightSelectionPanelVisibility()
    {
        topPanel.Visible = Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled &&
            Editor.CurrentPatch.HasDayAndNight;

        // When not in a patch with light, hide the useless always day selector
        if (!topPanel.Visible)
        {
            calculateBalancesAsIfDay.ButtonPressed = false;
            calculateBalancesAsIfDay.Visible = false;
        }
        else
        {
            calculateBalancesAsIfDay.Visible = true;
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

        if (Visible)
        {
            TutorialState?.SendEvent(TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged,
                new EnergyBalanceEventArgs(energyBalance), this);
        }

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

    private void UpdateAutoEvoPrediction(EditorAutoEvoRun startedRun, Species playerSpeciesOriginal,
        MicrobeSpecies playerSpeciesNew)
    {
        if (waitingForPrediction != null)
        {
            GD.PrintErr(
                $"{nameof(CancelPreviousAutoEvoPrediction)} has not been called before starting a new prediction");
        }

        totalEnergyLabel.Value = float.NaN;

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

        endosymbiosisPopup.UpdateData(Editor.EditedBaseSpecies.Endosymbiosis,
            Editor.EditedCellProperties?.IsBacteria ??
            throw new Exception("Cell properties needs to be known already"));

        endosymbiosisPopup.OpenCentered(false);
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

        // For now leave the GUI open to show the player the progress information as feedback to what they've done
    }

    private void OnAbandonEndosymbiosisOperation(int targetSpeciesId)
    {
        if (!Editor.EditedBaseSpecies.Endosymbiosis.CancelEndosymbiosisTarget(targetSpeciesId))
        {
            GD.PrintErr("Couldn't cancel endosymbiosis operation on target species: ", targetSpeciesId);
            PlayInvalidActionSound();
        }
    }

    private void OnEndosymbiosisFinished(int targetSpecies)
    {
        // Must disallow if a movement action is in progress as that'd otherwise conflict
        if (CanCancelAction)
        {
            GD.PrintErr("Cannot complete endosymbiosis with another action in progress");
            PlayInvalidActionSound();
            return;
        }

        endosymbiosisPopup.Hide();

        GD.Print("Starting free organelle placement action after completing endosymbiosis");
        var targetData = Editor.EditedBaseSpecies.Endosymbiosis.StartedEndosymbiosis;

        if (targetData == null)
        {
            GD.PrintErr("Couldn't find in-progress endosymbiosis even though there should be one");
            PlayInvalidActionSound();
            return;
        }

        if (targetData.Species.ID != targetSpecies)
            GD.PrintErr("Completed endosymbiosis place for wrong species");

        // Create the pending placement action
        PendingEndosymbiontPlace = new EndosymbiontPlaceActionData(targetData);

        // There's now a pending action
        OnActionStatusChanged();
    }

    private void OnCompoundBalanceTypeChanged(BalanceDisplayType newType)
    {
        // Called by 2 different things so ignore the parameter and read the new values directly from the relevant
        // objects
        _ = newType;

        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);
    }

    private void OnBalanceShowOptionsChanged(bool pressed)
    {
        _ = pressed;

        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);
    }

    private List<KeyValuePair<string, float>> SortBarData(Dictionary<string, float> bar)
    {
        var comparer = new ATPComparer();

        return bar.OrderBy(i => i.Key, comparer).ToList();
    }

    private void SelectATPBalanceMode(int index)
    {
        balanceMode = (ResourceLimitingMode)index;

        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);
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

    private void ConfirmFinishEditingWithEndosymbiosis()
    {
        if (OnFinish == null)
        {
            GD.PrintErr("Confirmed editing for cell editor when finish callback is not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        ignoredEditorWarnings.Add(EditorUserOverride.EndosymbiosisPending);
        OnFinish.Invoke(ignoredEditorWarnings);
    }

    private void UpdateGUIAfterLoadingSpecies(Species species)
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

        UpdateCancelButtonVisibility();
    }

    private void UpdateGrowthOrderNumbers()
    {
        if (!ShowGrowthOrder)
        {
            growthOrderNumberContainer.Visible = false;
            return;
        }

        if (camera == null)
        {
            GD.PrintErr("Camera must be set for growth order numbers");
            return;
        }

        growthOrderNumberContainer.Visible = true;

        // Setup tracking for what gets used
        foreach (var orderLabel in createdGrowthOrderLabels.Values)
        {
            orderLabel.Marked = false;
        }

        var orderList = growthOrderGUI.GetCurrentOrder();
        var orderListCount = orderList.Count;

        var organelles = editedMicrobeOrganelles.Organelles;
        var organellesCount = organelles.Count;

        for (int i = 0; i < organellesCount; ++i)
        {
            var editedMicrobeOrganelle = organelles[i];

            // TODO: fallback numbers if item not found?
            var order = -1;

            for (int j = 0; j < orderListCount; ++j)
            {
                if (ReferenceEquals(orderList[j], editedMicrobeOrganelle))
                {
                    // +1 to be user readable numbers
                    order = j + 1;
                    break;
                }
            }

            if (!createdGrowthOrderLabels.TryGetValue(order, out var graphicalLabel))
            {
                graphicalLabel = GrowthOrderLabel.Create(order);
                growthOrderNumberContainer.AddChild(graphicalLabel);
                createdGrowthOrderLabels.Add(order, graphicalLabel);
            }

            graphicalLabel.Position = camera.UnprojectPosition(Hex.AxialToCartesian(editedMicrobeOrganelle.Position));
            graphicalLabel.Visible = true;
            graphicalLabel.Marked = true;
        }

        // Hide unused labels
        foreach (var orderLabel in createdGrowthOrderLabels.Values)
        {
            if (!orderLabel.Marked)
                orderLabel.Visible = false;
        }
    }

    private void UpdateGrowthOrderButtons()
    {
        // To save on performance, only update this when it is actually visible to the player
        if (selectedSelectionMenuTab == SelectionMenuTab.GrowthOrder)
        {
            growthOrderGUI.UpdateItems(growthOrderGUI.ApplyOrderingToItems(editedMicrobeOrganelles.Organelles));
        }

        UpdateGrowthOrderNumbers();
    }

    private void OnResetGrowthOrderPressed()
    {
        growthOrderGUI.UpdateItems(editedMicrobeOrganelles.Organelles);
        UpdateGrowthOrderNumbers();
    }

    /// <summary>
    ///   A simple label showing the growth order of something
    /// </summary>
    private partial class GrowthOrderLabel : Label
    {
        public bool Marked { get; set; }

        public static GrowthOrderLabel Create(int number)
        {
            return new GrowthOrderLabel
            {
                Text = number.ToString(),
                Marked = true,
            };
        }
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
