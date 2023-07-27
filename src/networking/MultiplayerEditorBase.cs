using System.Collections.Generic;
using Godot;

public class MultiplayerEditorBase<TAction, TStage> : EditorBase<TAction, TStage>
    where TAction : EditorAction
    where TStage : Node, IReturnableGameState
{
    protected bool receivedSpeciesFromServer;

    private bool fadingOutFromLoadingScreen;

    public override bool CanCancelAction => false;

    public override Species EditedBaseSpecies => null!;

    protected MultiplayerGameWorld MultiplayerWorld => (MultiplayerGameWorld)CurrentGame.GameWorld;

    protected override string MusicCategory => string.Empty;

    protected override MainGameState ReturnToState => MainGameState.Invalid;

    protected override string EditorLoadingMessage => string.Empty;

    protected override bool HasInProgressAction => false;

    public override void _Process(float delta)
    {
        if (!Ready && !fadingOutFromLoadingScreen)
        {
            if (!receivedSpeciesFromServer)
            {
                LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState, "Fetching species...");
                return;
            }

            fadingOutFromLoadingScreen = true;
            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.5f, OnEditorReady, false, false);
        }

        UpdateEditor(delta);
    }

    public override bool CancelCurrentAction()
    {
        return false;
    }

    public override int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions)
    {
        return 0;
    }

    protected override void OnEnterEditor()
    {
        // Clear old stuff in the world
        RootOfDynamicallySpawned.FreeChildren();

        history = new EditorActionHistory<TAction>();

        InitEditor(true);

        StartMusic();
    }

    protected override void InitEditor(bool fresh)
    {
        pauseMenu.GameProperties = CurrentGame;

        ApplyEditorTab();

        if (!receivedSpeciesFromServer)
        {
            Ready = false;
            LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState, "Fetching species...");
            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.5f, null, false, false);
        }
    }

    protected override void InitEditorGUI(bool fresh)
    {
    }

    protected override void OnEditorReady()
    {
        fadingOutFromLoadingScreen = false;
        Ready = true;
        LoadingScreen.Instance.Hide();

        InitEditorGUI(true);
        NotifyUndoRedoStateChanged();

        SetupEditedSpecies();

        FadeIn();
    }

    protected override void ApplyEditorTab()
    {
    }

    protected override void UpdateEditor(float delta)
    {
        if (!Ready)
            return;

        base.UpdateEditor(delta);
    }

    protected override void ElapseEditorEntryTime()
    {
    }

    protected override IEnumerable<IEditorComponent> GetAllEditorComponents()
    {
        return null!;
    }

    protected override void PerformAutoSave()
    {
    }

    protected override void PerformQuickSave()
    {
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
    }

    protected override void SaveGame(string name)
    {
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMicrobeArenaGame(SimulationParameters.Instance.GetBiome("tidepool"));
    }

    protected override void UpdateHistoryCallbackTargets(ActionHistory<TAction> actionHistory)
    {
        // TODO: figure out why the callbacks are correctly pointing to the cell editor instance even without this
        // actionHistory.ReTargetCallbacksInHistory(cellEditorTab);
    }
}
