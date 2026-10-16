/// <summary>
///   Owns the multicellular editor's prediction lifecycle. Only the main thread captures inputs and updates panels.
/// </summary>
public partial class MulticellularEditor
{
    private MulticellularSpecies? predictionOriginal;
    private EditorAutoEvoRun? predictionRun;
    private Patch? predictionCurrentPatch;
    private bool predictionHistoryChecked;
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

        if (!predictionHistoryChecked)
        {
            predictionHistoryChecked = true;

            // Source species identities stay fixed until editor exit, so validate them before the first prediction.
            if (!HasPredictionPlayerIdConflict(CurrentGame.GameWorld.Map, CurrentGame.GameWorld.PlayerSpecies) &&
                CurrentGame.GameWorld.GenerationHistory.TryGetValue(EditedSpecies.Generation - 1, out var generation) &&
                generation.AllSpeciesData.TryGetValue(EditedSpecies.ID, out var record) &&
                record.Species is MulticellularSpecies historical)
            {
                predictionOriginal = historical.Clone(true, true);
                predictionOriginal.BecomePlayerSpecies();
            }
            else
            {
                bodyPlanEditorTab.PredictionPanel.ShowUnavailable();
            }
        }

        if (predictionOriginal == null)
            return;

        if (predictionRun?.Finished == true)
        {
            if (!predictionDirty)
            {
                bodyPlanEditorTab.PredictionPanel.DisplayRun(predictionRun, predictionOriginal,
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

        var map = new PatchMap();
        foreach (var patch in CurrentGame.GameWorld.Map.Patches.Values)
            map.AddPatch(patch.CreatePredictionCopy(CurrentGame.GameWorld.PlayerSpecies, predictionOriginal));

        predictionCurrentPatch = map.GetPatch(CurrentPatch.ID);
        map.CurrentPatch = predictionCurrentPatch;
        var target = TargetPatch == null ? null : map.GetPatch(TargetPatch.ID);
        predictionRun = new EditorAutoEvoRun(CurrentGame.GameWorld, CurrentGame.GameWorld.AutoEvoGlobalCache,
            predictionOriginal, current!, target, map);
        predictionStartTimer = 0;
        bodyPlanEditorTab.PredictionPanel.ShowWaiting();
        predictionRun.Start();
    }

    /// <summary>
    ///   Marks a committed simulation input change. Snapshot creation waits until a later process frame.
    /// </summary>
    public void MarkPredictionDirty()
    {
        predictionDirty = true;
    }

    /// <summary>
    ///   Checks for source map species that downstream prediction logic could confuse with the player by ID.
    /// </summary>
    internal static bool HasPredictionPlayerIdConflict(PatchMap map, Species playerSpecies)
    {
        foreach (var patch in map.Patches.Values)
        {
            foreach (var species in patch.SpeciesInPatch.Keys)
            {
                if (species.ID == playerSpecies.ID && !ReferenceEquals(species, playerSpecies))
                    return true;
            }
        }

        return false;
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
