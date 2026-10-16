using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Shared presentation of editor Auto-Evo prediction results. The editor owns and schedules the run.
/// </summary>
public partial class EditorAutoEvoPredictionPanel : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    private CellStatsIndicator totalEnergyLabel = null!;

    [Export]
    private Label autoEvoPredictionFailedLabel = null!;

    [Export]
    private Label bestPatchLabel = null!;

    [Export]
    private Label worstPatchLabel = null!;

    [Export]
    private CustomWindow autoEvoPredictionExplanationPopup = null!;

    [Export]
    private CustomRichTextLabel autoEvoPredictionExplanationLabel = null!;

    [Export]
    private PopupMicheViewer micheViewer = null!;

    [Export]
    private TextureButton detailsButton = null!;

    [Export]
    private Button micheViewButton = null!;

    [Export]
    private HBoxContainer suggestionsContainer = null!;
#pragma warning restore CA2213

    private Func<PredictionInputState>? validatePredictionInput;
    private Color detailsButtonNormalColour;
    private bool? autoEvoPredictionRunSuccessful;
    private bool unavailable;
    private long predictedPopulation;
    private string? bestPatchName;
    private float bestPatchEnergyGathered;

    // This and worstPatchPopulation used to be displayed but are now kept for potential future use
    private long bestPatchPopulation;
    private string? worstPatchName;
    private float worstPatchEnergyGathered;
    private long worstPatchPopulation;
    private LocalizedStringBuilder? predictionDetailsText;
    private Miche? predictionMiches;
    private Patch? predictionPatch;
    private WorldGenerationSettings? worldSettings;

    [Signal]
    public delegate void DetailsOpenedEventHandler();

    [Signal]
    public delegate void InvalidActionEventHandler();

    public override void _Ready()
    {
        detailsButtonNormalColour = detailsButton.SelfModulate;
        UpdateAutoEvoPredictionTranslations();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && IsNodeReady())
        {
            // Child indicators reset their formatted values during translation callbacks.
            Invoke.Instance.QueueForObject(UpdateTranslatedResults, this);
        }
    }

    public void ConfigureHost(Control finishButton, bool showSuggestions,
        Func<PredictionInputState>? validateInput = null)
    {
        validatePredictionInput = validateInput;
        detailsButton.FocusNeighborBottom = detailsButton.GetPathTo(finishButton);
        detailsButton.FocusNext = detailsButton.GetPathTo(finishButton);
        suggestionsContainer.Visible = showSuggestions;
    }

    public void ShowWaiting()
    {
        unavailable = false;
        totalEnergyLabel.Value = float.NaN;
        UpdateAutoEvoPredictionTranslations();
    }

    /// <summary>
    ///   Marks the current design as invalid while keeping the last successful popup contents.
    /// </summary>
    public void ShowUnavailable()
    {
        unavailable = true;
        autoEvoPredictionRunSuccessful = null;
        predictedPopulation = 0;
        bestPatchName = null;
        bestPatchEnergyGathered = 0;
        bestPatchPopulation = 0;
        worstPatchName = null;
        worstPatchEnergyGathered = 0;
        worstPatchPopulation = 0;

        totalEnergyLabel.ResetInitialValue();
        totalEnergyLabel.Value = float.NaN;
        totalEnergyLabel.TooltipText = string.Empty;

        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();
    }

    public void DisplayRun(AutoEvoRun run, Species playerSpeciesOriginal, Species playerSpeciesNew,
        float? previousEnergy, Patch patch, WorldGenerationSettings settings)
    {
        unavailable = false;
        if (!run.WasSuccessful)
        {
            GD.PrintErr("Failed to run auto-evo prediction for showing in the editor");
            autoEvoPredictionRunSuccessful = false;
            UpdateAutoEvoPredictionTranslations();
            return;
        }

        var results = run.Results ??
            throw new Exception("Auto evo prediction has no results even though it succeeded");

        // Total population
        predictedPopulation = results.GetGlobalPopulation(playerSpeciesNew);

        autoEvoPredictionRunSuccessful = true;

        // Gather energy details
        float totalEnergy = 0;
        Patch? bestPatch = null;
        float bestPatchEnergy = 0;
        Patch? worstPatch = null;
        float worstPatchEnergy = 0;

        foreach (var entry in results.GetPatchEnergyResults(playerSpeciesNew))
        {
            // Best
            if (bestPatch == null || bestPatchEnergy < entry.Value.TotalEnergyGathered)
            {
                bestPatchEnergy = entry.Value.TotalEnergyGathered;
                bestPatch = entry.Key;
            }

            if (worstPatch == null || worstPatchEnergy > entry.Value.TotalEnergyGathered)
            {
                worstPatchEnergy = entry.Value.TotalEnergyGathered;
                worstPatch = entry.Key;
            }

            totalEnergy += entry.Value.TotalEnergyGathered;
        }

        // Set the initial value to compare against the original species
        totalEnergyLabel.ResetInitialValue();

        if (previousEnergy != null)
        {
            totalEnergyLabel.Value = previousEnergy.Value;
            totalEnergyLabel.TooltipText =
                new LocalizedString("GATHERED_ENERGY_TOOLTIP", previousEnergy).ToString();
        }
        else
        {
            GD.PrintErr("Previously gathered energy is unknown, can't compare them (this will happen with " +
                "older saves)");
        }

        var formatted = StringUtils.ThreeDigitFormat(totalEnergy);

        totalEnergyLabel.SetMultipartValue($"{formatted} ({predictedPopulation} {Constants.MICROBE_POPULATION_SUFFIX})",
            totalEnergy);

        // Set the best and worst patch displays
        worstPatchName = worstPatch?.Name.ToString();
        worstPatchEnergyGathered = worstPatchEnergy;

        if (worstPatch != null)
        {
            // For some reason in rare cases the population numbers cannot be found, using FirstOrDefault should ensure
            // here that missing population numbers get assigned 0
            worstPatchPopulation = results.GetPopulationInPatches(playerSpeciesNew)
                .FirstOrDefault(p => p.Key == worstPatch).Value;
        }

        bestPatchName = bestPatch?.Name.ToString();
        bestPatchEnergyGathered = bestPatchEnergy;

        if (bestPatch != null)
        {
            bestPatchPopulation = results.GetPopulationInPatches(playerSpeciesNew)
                .FirstOrDefault(p => p.Key == bestPatch).Value;
        }

        CreateAutoEvoPredictionDetailsText(results.GetPatchEnergyResults(playerSpeciesNew),
            playerSpeciesOriginal.FormattedNameBbCode);

        UpdateAutoEvoPredictionTranslations();

        if (Visible || autoEvoPredictionExplanationPopup.Visible)
        {
            UpdateAutoEvoPredictionDetailsText();
        }

        predictionPatch = patch;
        worldSettings = settings;
        predictionMiches = results.GetModifiableMicheForPatch(patch);
    }

    private void UpdateTranslatedResults()
    {
        if (!IsInsideTree())
            return;

        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();
        if (autoEvoPredictionRunSuccessful == true && !float.IsNaN(totalEnergyLabel.Value))
        {
            var formatted = StringUtils.ThreeDigitFormat(totalEnergyLabel.Value);
            totalEnergyLabel.SetMultipartValue(
                $"{formatted} ({predictedPopulation} {Constants.MICROBE_POPULATION_SUFFIX})", totalEnergyLabel.Value);
        }
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        detailsButton.SelfModulate = unavailable ?
            detailsButtonNormalColour * Colors.Gray :
            detailsButtonNormalColour;

        if (unavailable || autoEvoPredictionRunSuccessful == false)
        {
            totalEnergyLabel.Value = float.NaN;
            autoEvoPredictionFailedLabel.Text = unavailable ?
                Localization.Translate("AUTO_EVO_PREDICTION_INVALID_SPECIES") :
                Localization.Translate("FAILED");
            autoEvoPredictionFailedLabel.Show();
        }
        else
        {
            autoEvoPredictionFailedLabel.Hide();
        }

        var energyFormat = Localization.Translate("ENERGY_IN_PATCH_SHORT");

        if (!string.IsNullOrEmpty(bestPatchName))
        {
            var formatted = StringUtils.ThreeDigitFormat(bestPatchEnergyGathered);

            bestPatchLabel.Text =
                energyFormat.FormatSafe(Localization.Translate(bestPatchName), formatted);
        }
        else
        {
            bestPatchLabel.Text = Localization.Translate("N_A");
        }

        if (!string.IsNullOrEmpty(worstPatchName))
        {
            var formatted = StringUtils.ThreeDigitFormat(worstPatchEnergyGathered);

            worstPatchLabel.Text =
                energyFormat.FormatSafe(Localization.Translate(worstPatchName), formatted);
        }
        else
        {
            worstPatchLabel.Text = Localization.Translate("N_A");
        }
    }

    /// <summary>
    ///   Checks the live design as edits may not have reached the throttled prediction update yet.
    /// </summary>
    private bool CanOpenPredictionPopup()
    {
        var inputState = validatePredictionInput?.Invoke() ??
            (unavailable ? PredictionInputState.Invalid : PredictionInputState.Valid);

        if (inputState == PredictionInputState.NotReady)
            return false;

        if (inputState == PredictionInputState.Invalid)
        {
            ShowUnavailable();
            EmitSignal(SignalName.InvalidAction);
            return false;
        }

        // A corrected design can show the previous results while its next prediction is pending.
        if (unavailable)
            ShowWaiting();

        return true;
    }

    private void OpenAutoEvoPredictionDetails()
    {
        if (!CanOpenPredictionPopup())
            return;

        GUICommon.Instance.PlayButtonPressSound();

        UpdateAutoEvoPredictionDetailsText();

        autoEvoPredictionExplanationPopup.PopupCenteredShrink();

        EmitSignal(SignalName.DetailsOpened);
    }

    private void CloseAutoEvoPrediction()
    {
        GUICommon.Instance.PlayButtonPressSound();
        autoEvoPredictionExplanationPopup.Hide();
    }

    private void CreateAutoEvoPredictionDetailsText(
        Dictionary<Patch, RunResults.SpeciesPatchEnergyResults> energyResults, string playerSpeciesName)
    {
        predictionDetailsText = new LocalizedStringBuilder(300);

        double Round(float value)
        {
            if (value > 0.0005f)
                return Math.Round(value, 3);

            // Small values can get tiny (and still be different from getting 0 energy due to fitness), so
            // this is here for that reason
            return Math.Round(value, 8);
        }

        // This loop shows all the patches the player species is in. Could perhaps just show the current one
        foreach (var energyResult in energyResults)
        {
            predictionDetailsText.Append(new LocalizedString("ENERGY_IN_PATCH_FOR",
                energyResult.Key.Name, playerSpeciesName));
            predictionDetailsText.Append('\n');

            predictionDetailsText.Append(new LocalizedString("ENERGY_SUMMARY_LINE",
                Round(energyResult.Value.TotalEnergyGathered), Round(energyResult.Value.IndividualCost),
                $"{energyResult.Value.UnadjustedPopulation} {Constants.MICROBE_POPULATION_SUFFIX}"));

            predictionDetailsText.Append('\n');
            predictionDetailsText.Append('\n');

            predictionDetailsText.Append(new LocalizedString("ENERGY_SOURCES"));
            predictionDetailsText.Append('\n');

            foreach (var nicheInfo in energyResult.Value.PerNicheEnergy)
            {
                var data = nicheInfo.Value;
                predictionDetailsText.Append(new LocalizedString("FOOD_SOURCE_ENERGY_INFO", nicheInfo.Key,
                    Round(data.CurrentSpeciesEnergy), Round(data.CurrentSpeciesFitness),
                    Round(data.TotalAvailableEnergy),
                    Round(data.TotalFitness)));
                predictionDetailsText.Append('\n');
            }

            predictionDetailsText.Append('\n');
        }
    }

    private void UpdateAutoEvoPredictionDetailsText()
    {
        autoEvoPredictionExplanationLabel.ExtendedBbcode = predictionDetailsText != null ?
            predictionDetailsText.ToString() :
            Localization.Translate("NO_DATA_TO_SHOW");
    }

    private void OnMicheViewRequested()
    {
        if (!CanOpenPredictionPopup())
            return;

        GUICommon.Instance.PlayButtonPressSound();

        if (predictionMiches == null)
        {
            GD.PrintErr("Missing miches data, can't show the popup");
            return;
        }

        micheViewer.ShowMiches(predictionPatch!, predictionMiches, worldSettings!);

        autoEvoPredictionExplanationPopup.Hide();
    }

    private void DummyKeepTranslation()
    {
        // This keeps this translation string existing if we ever still want to use worst and best population numbers
        Localization.Translate("POPULATION_IN_PATCH_SHORT");
    }
}
