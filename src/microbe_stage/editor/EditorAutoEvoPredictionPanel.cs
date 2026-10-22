using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Displays editor Auto-Evo prediction results while the host owns the prediction run and its lifetime.
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
#pragma warning restore CA2213

    private Func<(Patch Patch, WorldGenerationSettings Settings)> getMicheContext = null!;

    private string? bestPatchName;

    // This and worstPatchPopulation used to be displayed but are now kept for potential future use
    private long bestPatchPopulation;

    private float bestPatchEnergyGathered;

    private string? worstPatchName;

    private long worstPatchPopulation;

    private float worstPatchEnergyGathered;

    private bool? autoEvoPredictionRunSuccessful;
    private LocalizedStringBuilder? predictionDetailsText;
    private Miche? predictionMiches;

    [Signal]
    public delegate void DetailsOpenedEventHandler();

    /// <summary>
    ///   Sets host navigation and obtains the current Miche context when the viewer is requested.
    /// </summary>
    public void ConfigureHost(Control finishButton,
        Func<(Patch Patch, WorldGenerationSettings Settings)> micheContext)
    {
        getMicheContext = micheContext;

        detailsButton.FocusNeighborBottom = detailsButton.GetPathTo(finishButton);
        detailsButton.FocusNext = detailsButton.GetPathTo(finishButton);
    }

    public void ShowWaiting()
    {
        totalEnergyLabel.Value = float.NaN;
    }

    /// <summary>
    ///   Displays a completed run without retaining or managing the run itself.
    /// </summary>
    public void DisplayRun(AutoEvoRun run, Species playerSpeciesOriginal, Species playerSpeciesNew,
        float? previousEnergy, Patch patch)
    {
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
        var newPopulation = results.GetGlobalPopulation(playerSpeciesNew);

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

        totalEnergyLabel.SetMultipartValue($"{formatted} ({newPopulation} {Constants.MICROBE_POPULATION_SUFFIX})",
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

        if (Visible)
        {
            UpdateAutoEvoPredictionDetailsText();
        }

        predictionMiches = results.GetModifiableMicheForPatch(patch);
    }

    /// <summary>
    ///   Called by the host at its original translation callback position, before child indicators refresh.
    /// </summary>
    public void OnTranslationsChanged()
    {
        UpdateAutoEvoPredictionTranslations();
        UpdateAutoEvoPredictionDetailsText();
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        if (autoEvoPredictionRunSuccessful == false)
        {
            totalEnergyLabel.Value = float.NaN;
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
        GUICommon.Instance.PlayButtonPressSound();

        if (predictionMiches == null)
        {
            GD.PrintErr("Missing miches data, can't show the popup");
            return;
        }

        var context = getMicheContext();
        micheViewer.ShowMiches(context.Patch, predictionMiches, context.Settings);

        autoEvoPredictionExplanationPopup.Hide();
    }
}
