using System;
using Godot;

/// <summary>
///   Owns prediction scheduling and cached input reuse while the cell editor is processing.
///   Callbacks run synchronously on the main thread; stopping processing does not cancel or drain a run.
/// </summary>
public sealed class CellEditorPrediction(Func<MicrobeSpecies> createSpecies, Action<MicrobeSpecies> fillSpecies,
    Func<MicrobeSpecies, EditorAutoEvoRun> createRun, Func<Species> getOriginalSpeciesAfterStart,
    Action showWaiting, Action<AutoEvoRun, Species, Species> displayCompleted)
{
    private bool autoEvoPredictionDirty = true;
    private double autoEvoPredictionStartTimer = 5;
    private PendingAutoEvoPrediction? waitingForPrediction;

    /// <summary>
    ///   Reused between runs after the previous run has finished borrowing it.
    ///   Current editor changes are applied to predict their effect on the population.
    /// </summary>
    private MicrobeSpecies? cachedAutoEvoPredictionSpecies;

    public void MarkDirty()
    {
        autoEvoPredictionDirty = true;
    }

    /// <summary>
    ///   Advances prediction scheduling independently of the editor tab's visibility.
    /// </summary>
    public void Process(double delta, bool editorReady, bool predictionDisabled)
    {
        autoEvoPredictionStartTimer += delta;

        // Don't start before the editor is ready
        if (autoEvoPredictionDirty && editorReady)
        {
            // Don't constantly trigger prediction runs
            if (autoEvoPredictionStartTimer >= Constants.AUTO_EVO_PREDICTION_UPDATE_INTERVAL)
            {
                if (CancelPreviousAutoEvoPrediction())
                {
                    // Start a new run once safe to do so
                    autoEvoPredictionStartTimer = 0;
                    StartAutoEvoPrediction(editorReady, predictionDisabled);
                }
            }
        }
        else if (editorReady)
        {
            CheckRunningAutoEvoPrediction();
        }
    }

    private void StartAutoEvoPrediction(bool editorReady, bool predictionDisabled)
    {
        // For now disabled in the multicellular editor as the microbe logic being used there doesn't make sense
        if (predictionDisabled)
        {
            // Don't be dirty to not constantly try to retry the run
            autoEvoPredictionDirty = false;
            return;
        }

        // The first prediction can be made only after population numbers from the previous run are applied,
        // so this is just here to guard against that potential programming mistake that may happen when code is
        // changed
        if (!editorReady)
        {
            GD.PrintErr("Can't start auto-evo prediction before editor is ready");
            return;
        }

        cachedAutoEvoPredictionSpecies ??= createSpecies();
        fillSpecies(cachedAutoEvoPredictionSpecies);

        var run = createRun(cachedAutoEvoPredictionSpecies);
        run.Start();
        autoEvoPredictionDirty = false;

        // Preserve the host's identity read after Start, before showing the waiting state.
        UpdateAutoEvoPrediction(run, getOriginalSpeciesAfterStart(), cachedAutoEvoPredictionSpecies);
    }

    private void CheckRunningAutoEvoPrediction()
    {
        if (waitingForPrediction?.Finished != true)
            return;

        DisplayCompletedPrediction(waitingForPrediction);
        waitingForPrediction = null;
    }

    private void UpdateAutoEvoPrediction(EditorAutoEvoRun startedRun, Species playerSpeciesOriginal,
        MicrobeSpecies playerSpeciesNew)
    {
        if (waitingForPrediction != null)
        {
            GD.PrintErr(
                $"{nameof(CancelPreviousAutoEvoPrediction)} has not been called before starting a new prediction");
        }

        showWaiting();

        var prediction = new PendingAutoEvoPrediction(startedRun, playerSpeciesOriginal, playerSpeciesNew);

        if (startedRun.Finished)
        {
            DisplayCompletedPrediction(prediction);
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
    /// <returns>True, once there is no active run</returns>
    private bool CancelPreviousAutoEvoPrediction()
    {
        if (waitingForPrediction == null)
            return true;

        if (!waitingForPrediction.AutoEvoRun.Aborted)
        {
            GD.Print("Canceling previous auto-evo prediction run as it didn't manage to finish yet");
            waitingForPrediction.AutoEvoRun.Abort();
        }

        // Abort only requests cancellation. Keep the cached input untouched until the run has finished.
        if (waitingForPrediction.Finished)
        {
            GD.Print("Previous auto-evo prediction run finished now");
            waitingForPrediction = null;
            return true;
        }

        return false;
    }

    private void DisplayCompletedPrediction(PendingAutoEvoPrediction prediction)
    {
        displayCompleted(prediction.AutoEvoRun, prediction.PlayerSpeciesOriginal, prediction.PlayerSpeciesNew);
    }

    private class PendingAutoEvoPrediction(AutoEvoRun autoEvoRun, Species playerSpeciesOriginal,
        Species playerSpeciesNew)
    {
        public readonly AutoEvoRun AutoEvoRun = autoEvoRun;
        public readonly Species PlayerSpeciesOriginal = playerSpeciesOriginal;
        public readonly Species PlayerSpeciesNew = playerSpeciesNew;

        public bool Finished => AutoEvoRun.Finished;
    }
}
