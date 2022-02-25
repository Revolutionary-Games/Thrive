using System;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base common class with shared editor functionality
/// </summary>
/// <typeparam name="TGUI">Class of the editor GUI to use</typeparam>
/// <typeparam name="TAction">Editor action type the action history uses in this editor</typeparam>
/// <typeparam name="TStage">Class of the stage this editor returns to</typeparam>
/// <remarks>
///   <para>
///     When inheriting from this class, the inheriting class should have attributes:
///     <code>
///       [JsonObject(IsReference = true)]
///       [SceneLoadedClass("res://PATH/TO/SCENE.tscn")]
///       [DeserializedCallbackTarget]
///     </code>
///   </para>
/// </remarks>
public abstract class EditorBase<TGUI, TAction, TStage> : NodeWithInput, IEditor, ILoadableGameState, IGodotEarlyNodeResolve
    where TGUI : class, IEditorGUI
    where TAction : MicrobeEditorAction
    where TStage : Node, IReturnableGameState
{
    [Export]
    public NodePath PauseMenuPath = null!;

    protected Node world = null!;
    protected Spatial rootOfDynamicallySpawned = null!;
    protected PauseMenu pauseMenu = null!;

    /// <summary>
    ///   Where all user actions will  be registered
    /// </summary>
    [JsonProperty]
    protected ActionHistory<TAction> history = null!;

    /// <summary>
    ///   True once auto-evo (and possibly other stuff) we need to wait for is ready
    /// </summary>
    [JsonProperty]
    protected bool ready;

    [JsonProperty]
    protected LocalizedStringBuilder? autoEvoSummary;

    [JsonProperty]
    protected LocalizedStringBuilder? autoEvoExternal;

    [JsonProperty]
    protected string? activeActionName;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    protected bool wantsToSave;

    [JsonIgnore]
    public bool TransitionFinished { get; protected set; }

    [JsonProperty]
    public int MutationPoints { get; protected set; }

    [JsonProperty]
    public bool FreeBuilding { get; protected set; }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    /// <summary>
    ///   If set the editor returns to this stage. The CurrentGame
    ///   should be shared with this stage. If not set returns to a newly created instance of the stage
    /// </summary>
    [JsonProperty]
    public TStage? ReturnToStage { get; set; }

    /// <summary>
    ///   True when the editor view is active and the user can perform an action (for example place an organelle)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Hover hexes and models are only shown if this is true. This is saved to make this work better when the
    ///     player was in the cell editor tab and saved.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public bool ShowHover { get; set; }

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public TGUI GUI { get; protected set; } = null!;

    [JsonIgnore]
    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    public bool NodeReferencesResolved { get; private set; }

    [JsonIgnore]
    public bool Ready
    {
        get => ready;
        set
        {
            ready = value;
            pauseMenu.GameLoading = !value;
        }
    }

    [JsonIgnore]
    public abstract bool CanCancelAction { get; }

    protected abstract Species EditedBaseSpecies { get; }

    protected abstract string MusicCategory { get; }

    protected abstract MainGameState ReturnToState { get; }

    protected abstract string EditorLoadingMessage { get; }

    /// <summary>
    ///   True when there is an inprogress action that prevents other actions from being done
    /// </summary>
    protected abstract bool HasInProgressAction { get; }

    public override void _Ready()
    {
        base._Ready();
        ResolveNodeReferences();

        TransitionFinished = false;

        OnEnterEditor();
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        world = GetNode("EditorWorld");
        rootOfDynamicallySpawned = world.GetNode<Spatial>("DynamicallySpawned");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);

        ResolveDerivedTypeNodeReferences();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // As we will no longer return to the stage we need to free it, if we have it
        // This might be disposed if this was loaded from a save and we loaded another save
        try
        {
            if (IsLoadedFromSave)
            {
                // When loaded from save, the stage needs to be attached as a scene for the callbacks that reattach
                // children to run, otherwise some objects won't be correctly deleted
                if (ReturnToStage != null)
                    SceneManager.Instance.AttachAndDetachScene(ReturnToStage);
            }

            if (ReturnToStage?.GetParent() != null)
                GD.PrintErr("ReturnToStage has a parent when editor is wanting to free it");

            ReturnToStage?.QueueFree();
        }
        catch (ObjectDisposedException)
        {
            GD.Print("Editor's return to stage is already disposed");
        }
    }

    public void OnFinishLoading(Save save)
    {
        // // Handle the stage to return to specially, as it also needs to run the code
        // // for fixing the stuff in order to return there
        // // TODO: this could be probably moved now to just happen when it enters the scene first time

        ReturnToStage?.OnFinishLoading(save);

        // Probably shouldn't be needed as the stage object is not orphaned automatically
        // // We need to not let the objects be deleted before we apply them
        // TemporaryLoadedNodeDeleter.Instance.AddDeletionHold(Constants.DELETION_HOLD_MICROBE_EDITOR);
    }

    public void OnFinishTransitioning()
    {
        TransitionFinished = true;
    }

    public override void _Process(float delta)
    {
        if (!Ready)
        {
            if (!CurrentGame!.GameWorld.IsAutoEvoFinished())
            {
                LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState,
                    TranslationServer.Translate("WAITING_FOR_AUTO_EVO") + " " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }

            OnEditorReady();
        }

        // Auto save after editor entry is complete
        if (TransitionFinished && wantsToSave)
        {
            if (!CurrentGame!.FreeBuild)
                PerformAutoSave();

            wantsToSave = false;
        }

        UpdateEditor(delta);
    }

    /// <summary>
    ///   Applies the changes done and exits the editor
    /// </summary>
    public virtual void OnFinishEditing()
    {
        GD.Print(GetType().Name, ": applying changes to edited Species");

        if (EditedBaseSpecies == null)
            throw new InvalidOperationException("Editor not initialized, missing edited species");

        MakeSureEditorReturnIsGood();
    }

    /// <summary>
    ///   Sets the visibility of placed cell parts, editor forward arrow, etc.
    /// </summary>
    public virtual void SetEditorObjectVisibility(bool shown)
    {
        rootOfDynamicallySpawned.Visible = shown;
    }

    [RunOnKeyDown("e_primary")]
    public virtual void PerformPrimaryAction()
    {
        // Derived types should handle this case
        if (HasInProgressAction)
            return;

        if (string.IsNullOrEmpty(activeActionName))
            return;

        PerformActiveAction();
    }

    [RunOnKeyDown("e_redo")]
    public void Redo()
    {
        if (HasInProgressAction)
            return;

        if (history.Redo())
        {
            OnRedoPerformed();
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("e_undo")]
    public void Undo()
    {
        if (HasInProgressAction)
            return;

        if (history.Undo())
        {
            OnUndoPerformed();
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        // Can only save once the editor is ready
        if (Ready)
        {
            GD.Print("quick saving ", GetType().Name);
            PerformQuickSave();
        }
    }

    [RunOnKeyDown("g_toggle_gui")]
    public void ToggleGUI()
    {
        GUI.Visible = !GUI.Visible;
    }

    public abstract float CalculateCurrentActionCost();
    public abstract bool CancelCurrentAction();

    /// <summary>
    ///   Changes the number of mutation points left. Should only be called by editor actions
    /// </summary>
    internal void ChangeMutationPoints(int change)
    {
        if (FreeBuilding || CheatManager.InfiniteMP)
            return;

        MutationPoints = (MutationPoints + change).Clamp(0, Constants.BASE_MUTATION_POINTS);

        OnMutationPointsChanged();
    }

    protected abstract void InitConcreteGUI();

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    protected virtual void OnEnterEditor()
    {
        // Clear old stuff in the world
        rootOfDynamicallySpawned.FreeChildren();

        if (!IsLoadedFromSave)
        {
            history = new ActionHistory<TAction>();

            // Start a new game if no game has been started
            if (CurrentGame == null)
            {
                if (ReturnToStage != null)
                    throw new Exception("stage to return to should have set our current game");

                GD.Print("Starting a new game for ", GetType().Name);
                CurrentGame = StartNewGameForEditor();
            }
        }

        InitEditor();

        StartMusic();
    }

    protected virtual void InitEditor()
    {
        InitConcreteGUI();

        if (!IsLoadedFromSave)
        {
            // Auto save is wanted once possible
            wantsToSave = true;

            InitEditorFresh();
        }
        else
        {
            InitEditorSaved();
        }

        if (CurrentGame == null)
            throw new Exception($"Editor setup which was just ran didn't setup {nameof(CurrentGame)}");

        pauseMenu.SetNewSaveNameFromSpeciesName();
    }

    protected virtual void InitEditorFresh()
    {
        MutationPoints = Constants.BASE_MUTATION_POINTS;

        // For now we only show a loading screen if auto-evo is not ready yet
        if (!CurrentGame!.GameWorld.IsAutoEvoFinished())
        {
            Ready = false;
            LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState,
                CurrentGame.GameWorld.GetAutoEvoRun().Status);

            CurrentGame.GameWorld.FinishAutoEvoRunAtFullSpeed();
        }
        else
        {
            OnEditorReady();
        }

        if (CurrentGame.FreeBuild)
        {
            GD.Print("Editor going to freebuild mode because player has activated freebuild");
            FreeBuilding = true;
        }
        else
        {
            // Make sure freebuilding doesn't get stuck on
            FreeBuilding = false;
        }
    }

    protected virtual void InitEditorSaved()
    {
        if (Ready != true || CurrentGame == null)
            throw new InvalidOperationException("loaded editor isn't in the ready state, or missing current game");

        // Make absolutely sure the current game doesn't have an auto-evo run
        CurrentGame.GameWorld.ResetAutoEvoRun();

        FadeIn();
    }

    /// <summary>
    ///   Called once auto-evo results are ready
    /// </summary>
    protected virtual void OnEditorReady()
    {
        Ready = true;
        LoadingScreen.Instance.Hide();

        // Get summary before applying results in order to get comparisons to the previous populations
        var run = CurrentGame!.GameWorld.GetAutoEvoRun();

        if (run.Results != null)
        {
            autoEvoSummary = run.Results.MakeSummary(CurrentGame.GameWorld.Map, true, run.ExternalEffects);
            autoEvoExternal = run.MakeSummaryOfExternalEffects();

            run.Results.LogResultsToTimeline(CurrentGame.GameWorld, run.ExternalEffects);
        }
        else
        {
            autoEvoSummary = null;
            autoEvoExternal = null;
        }

        ApplyAutoEvoResults();

        FadeIn();
    }

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    protected void EnqueueAction(TAction action)
    {
        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (MutationPoints < action.Cost)
        {
            // Flash the MP bar and play sound
            OnInsufficientMP();
            return;
        }

        if (HasInProgressAction)
        {
            if (!DoesActionEndInProgressAction(action))
            {
                // Play sound
                OnActionBlockedWhileMoving();
                return;
            }
        }

        history.AddAction(action);

        UpdateUndoRedoButtons();
    }

    protected virtual void OnUndoPerformed()
    {
    }

    protected virtual void OnRedoPerformed()
    {
    }

    protected abstract void ResolveDerivedTypeNodeReferences();
    protected abstract void UpdateEditor(float delta);

    protected abstract void PerformAutoSave();
    protected abstract void PerformQuickSave();

    protected abstract GameProperties StartNewGameForEditor();

    protected abstract void PerformActiveAction();
    protected abstract bool DoesActionEndInProgressAction(TAction action);

    protected abstract void OnInsufficientMP();
    protected abstract void OnActionBlockedWhileMoving();

    protected abstract void OnMutationPointsChanged();
    protected abstract void UpdateUndoRedoButtons();

    private void MakeSureEditorReturnIsGood()
    {
        if (CurrentGame == null)
            throw new Exception("Editor must have active game when returning to the stage");

        if (ReturnToStage == null)
        {
            GD.Print("Creating new stage of type", typeof(TStage).Name, " as there isn't one yet");

            var scene = SceneManager.Instance.LoadScene(typeof(TStage).GetCustomAttribute<SceneLoadedClassAttribute>());

            ReturnToStage = (TStage)scene.Instance();
            ReturnToStage.CurrentGame = CurrentGame;
        }
    }

    private void ApplyAutoEvoResults()
    {
        var run = CurrentGame!.GameWorld.GetAutoEvoRun();
        GD.Print("Applying auto-evo results. Auto-evo run took: ", run.RunDuration);
        run.ApplyExternalEffects();

        CurrentGame.GameWorld.Map.UpdateGlobalTimePeriod(CurrentGame.GameWorld.TotalPassedTime);

        // Update populations before recording conditions - should not affect per-patch population
        CurrentGame.GameWorld.Map.UpdateGlobalPopulations();

        // Needs to be before the remove extinct species call, so that extinct species could still be stored
        // for reference in patch history (e.g. displaying it as zero on the species population chart)
        foreach (var entry in CurrentGame.GameWorld.Map.Patches)
        {
            entry.Value.RecordSnapshot(true);
        }

        var extinct = CurrentGame.GameWorld.Map.RemoveExtinctSpecies(FreeBuilding);

        foreach (var species in extinct)
        {
            CurrentGame.GameWorld.RemoveSpecies(species);

            GD.Print("Species ", species.FormattedName, " has gone extinct from the world.");
        }

        // Clear the run to make the cell stage start a new run when we go back there
        CurrentGame.GameWorld.ResetAutoEvoRun();
    }

    /// <summary>
    ///   Starts a fade in transition
    /// </summary>
    private void FadeIn()
    {
        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeIn, 0.5f);
        TransitionManager.Instance.StartTransitions(this, nameof(OnFinishTransitioning));
    }

    private void StartMusic()
    {
        Jukebox.Instance.PlayCategory(MusicCategory);
    }
}
