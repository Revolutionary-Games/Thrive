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
                cellEditorTab.PredictionPanel.DisplayRun(predictionRun, predictionRun.OriginalEditedSpecies,
                    predictionRun.ModifiedProperties, cellEditorTab.PreviousPlayerGatheredEnergy,
                    predictionCurrentPatch!, CurrentGame.GameWorld.WorldSettings);
            }

            predictionRun = null;
        }

        if (!predictionDirty ||
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
        var liveType = selectedCellTypeToEdit == null ?
            null :
            cellEditorTab.CreatePredictionCellType(selectedCellTypeToEdit);
        var originalLiveType = selectedCellTypeToEdit == null ?
            null :
            cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit);
        if (!bodyPlanEditorTab.TryCreatePredictionSpecies(out var current, originalLiveType, liveType))
        {
            bodyPlanEditorTab.PredictionPanel.ShowUnavailable();
            cellEditorTab.PredictionPanel.ShowUnavailable();
            return;
        }

        predictionCurrentPatch = CurrentPatch;
        predictionRun = new EditorAutoEvoRun(CurrentGame.GameWorld, CurrentGame.GameWorld.AutoEvoGlobalCache,
            EditedBaseSpecies, current!, TargetPatch);
        predictionStartTimer = 0;
        bodyPlanEditorTab.PredictionPanel.ShowWaiting();
        cellEditorTab.PredictionPanel.ShowWaiting();
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
