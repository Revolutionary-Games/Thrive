/// <summary>
///   Owns the multicellular editor's prediction lifecycle. Only the main thread captures inputs and updates panels.
/// </summary>
public partial class MulticellularEditor
{
    private EditorAutoEvoRun? predictionRun;
    private Patch? predictionCurrentPatch;
    private bool predictionDirty = true;
    private bool predictionStopped;
    private bool predictionExitPending;
    private bool predictionExitApplied;
    private double predictionStartTimer = 5;

    public override void _Process(double delta)
    {
        base._Process(delta);
        predictionStartTimer += delta;

        if (predictionExitPending)
        {
            if (predictionRun?.Finished == false)
                return;

            CompletePredictionExit();
            return;
        }

        if (predictionStopped || !EditorReady)
            return;

        if (predictionRun?.Finished == true)
        {
            if (!predictionDirty)
            {
                bodyPlanEditorTab.PredictionPanel.DisplayRun(predictionRun, predictionRun.OriginalEditedSpecies,
                    predictionRun.ModifiedProperties, cellEditorTab.PreviousPlayerGatheredEnergy,
                    predictionCurrentPatch!, CurrentGame.GameWorld.WorldSettings);
            }

            predictionRun = null;
        }

        // Cell type edits remain buffered on the report and patch map tabs as well as the type editor tab.
        // Wait until returning to the body plan tab synchronises them before capturing prediction inputs.
        if (!predictionDirty || selectedEditorTab != EditorTab.CellEditor ||
            bodyPlanEditorTab.CanCancelMove || cellEditorTab.CanCancelMove ||
            predictionStartTimer < Constants.AUTO_EVO_PREDICTION_UPDATE_INTERVAL)
        {
            return;
        }

        if (predictionRun != null)
        {
            predictionRun.Abort();
            return;
        }

        predictionDirty = false;
        if (!bodyPlanEditorTab.TryCreatePredictionSpecies(out var current))
        {
            bodyPlanEditorTab.PredictionPanel.ShowUnavailable();
            return;
        }

        predictionCurrentPatch = CurrentPatch;
        predictionRun = new EditorAutoEvoRun(CurrentGame.GameWorld, CurrentGame.GameWorld.AutoEvoGlobalCache,
            EditedBaseSpecies, current!, TargetPatch);
        predictionStartTimer = 0;
        bodyPlanEditorTab.PredictionPanel.ShowWaiting();
        predictionRun.Start();
    }

    /// <summary>
    ///   Marks a committed simulation input change. Prediction species creation waits until a later process frame.
    /// </summary>
    public void MarkPredictionDirty()
    {
        predictionDirty = true;
    }

    private void CompletePredictionExit()
    {
        if (predictionExitApplied)
            return;

        predictionExitApplied = true;
        predictionExitPending = false;
        predictionRun = null;

        // Finish the live type buffer only after prediction stops reading the world.
        if (selectedEditorTab == EditorTab.CellTypeEditor)
            FinishEditingSelectedCell();

        // Clear the edited cell type to avoid the cell editor applying the changes unnecessarily.
        selectedCellTypeToEdit = null;
        base.OnEditorExitTransitionFinished();
    }
}
