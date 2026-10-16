using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Shared presentation of editor Auto-Evo results. The editor owns and schedules the run.
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

    private Color detailsButtonNormalColour;
    private bool? autoEvoPredictionRunSuccessful;
    private bool unavailable;
    private bool predictionResultsInvalidated;
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
            Callable.From(UpdateTranslatedResults).CallDeferred();
        }
    }

    public void ConfigureHost(Control finishButton, bool showSuggestions)
    {
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

    public void ShowUnavailable()
    {
        unavailable = true;
        predictionResultsInvalidated = true;
        autoEvoPredictionRunSuccessful = null;
        predictedPopulation = 0;
        bestPatchName = null;
        bestPatchEnergyGathered = 0;
        bestPatchPopulation = 0;
        worstPatchName = null;
        worstPatchEnergyGathered = 0;
        worstPatchPopulation = 0;
        predictionDetailsText = null;
        predictionMiches = null;
        predictionPatch = null;
        worldSettings = null;

        // The open details window also needs an enabled exit if its miche entry is about to lose interaction.
        if (micheViewButton.HasFocus())
            autoEvoPredictionExplanationPopup.FocusCloseButton();

        detailsButton.Disabled = true;
        detailsButton.SelfModulate = detailsButtonNormalColour * Colors.Gray;
        micheViewButton.Disabled = true;
        totalEnergyLabel.ResetInitialValue();
        totalEnergyLabel.Value = float.NaN;
        totalEnergyLabel.TooltipText = string.Empty;

        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();
        micheViewer.ShowUnavailable();
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

        // Recover an open viewer without reopening one the player has closed, or changing normal waiting behaviour.
        if (predictionResultsInvalidated)
            micheViewer.UpdateMichesIfOpen(patch, predictionMiches, settings);

        predictionResultsInvalidated = false;
        detailsButton.Disabled = false;
        detailsButton.SelfModulate = detailsButtonNormalColour;
        micheViewButton.Disabled = false;
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
        if (unavailable || autoEvoPredictionRunSuccessful == false)
        {
            totalEnergyLabel.Value = float.NaN;
            autoEvoPredictionFailedLabel.Text =
                Localization.Translate(unavailable ? "NO_PREDICTION_FOR_CURRENT_DESIGN" : "FAILED");
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

    private void DummyKeepTranslation()
    {
        // This keeps this translation string existing if we ever still want to use worst and best population numbers
        Localization.Translate("POPULATION_IN_PATCH_SHORT");
    }

    private void OpenAutoEvoPredictionDetails()
    {
        if (predictionResultsInvalidated)
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
        if (predictionDetailsText != null)
        {
            autoEvoPredictionExplanationLabel.ExtendedBbcode = predictionDetailsText.ToString();
        }
        else
        {
            autoEvoPredictionExplanationLabel.ExtendedBbcode = Localization.Translate(predictionResultsInvalidated ?
                "NO_PREDICTION_FOR_CURRENT_DESIGN" :
                "NO_DATA_TO_SHOW");
        }
    }

    private void OnMicheViewRequested()
    {
        if (predictionResultsInvalidated)
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
}
