using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base common class with shared editor functionality. Note that most editor functionality is done by
///   <see cref="EditorComponentBase{TEditor}"/> derived types.
/// </summary>
/// <typeparam name="TAction">Editor action type the action history uses in this editor</typeparam>
/// <typeparam name="TStage">Class of the stage this editor returns to</typeparam>
/// <remarks>
///   <para>
///     The overall structure of the editor system is such that there is a class derived from this that is attached
///     to the editor scene root Node. That then contains editor components as children which implement most of the
///     editing functionality, the editor derived class mostly acts as glue logic and sets things up.
///   </para>
///   <para>
///     When inheriting from this class, the inheriting class should have attributes:
///     <code>
///       [JsonObject(IsReference = true)]
///       [SceneLoadedClass("res://PATH/TO/SCENE.tscn")]
///     </code>
///   </para>
///   <para>
///     This base class also contains some editor GUI logic (that was in its own class). It turns out that the GUI
///     class with the editor components refactor wouldn't end up with much of anything in it, as such for convenience
///     those few operations are merged into this class.
///   </para>
/// </remarks>
public abstract class EditorBase<TAction, TStage> : NodeWithInput, IEditor, ILoadableGameState,
    IGodotEarlyNodeResolve
    where TAction : EditorAction
    where TStage : Node, IReturnableGameState
{
    [Export]
    public NodePath? PauseMenuPath;

    [Export]
    public NodePath EditorGUIBaseNodePath = null!;

    [Export]
    public NodePath? EditorTabSelectorPath;

#pragma warning disable CA2213
    protected Node world = null!;
    protected PauseMenu pauseMenu = null!;
    protected MicrobeEditorTabButtons? editorTabSelector;
#pragma warning restore CA2213

    /// <summary>
    ///   Where all user actions will  be registered
    /// </summary>
    [JsonProperty]
    protected EditorActionHistory<TAction> history = null!;

    protected bool ready;

    [JsonProperty]
    protected LocalizedStringBuilder? autoEvoSummary;

    [JsonProperty]
    protected LocalizedStringBuilder? autoEvoExternal;

    [JsonProperty]
    protected EditorTab selectedEditorTab = EditorTab.Report;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    protected bool wantsToSave;

    /// <summary>
    ///   This is protected only so that this is loaded from a save. No derived class should modify this
    /// </summary>
    [JsonProperty]
    protected GameProperties? currentGame;

#pragma warning disable CA2213
    private Control editorGUIBaseNode = null!;
#pragma warning restore CA2213

    private int? mutationPointsCache;

    /// <summary>
    ///   The light level the editor is previewing things at
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is saved but there's a slight bug that the selected light level gets reset anyway when loading a save
    ///     made in the editor
    ///   </para>
    /// </remarks>
    [JsonProperty]
    private float lightLevel = 1.0f;

    /// <summary>
    ///   Base Node where all dynamically created world Nodes in the editor should go. Optionally grouped under
    ///   a one more level of parent nodes so that different editor components can have their things visible at
    ///   different times
    /// </summary>
    public Spatial RootOfDynamicallySpawned { get; private set; } = null!;

    [JsonIgnore]
    public bool TransitionFinished { get; protected set; }

    [JsonIgnore]
    public int MutationPoints
    {
        get => mutationPointsCache ?? CalculateMutationPointsLeft();
        set
        {
            _ = value;
            DirtyMutationPointsCache();
        }
    }

    [JsonProperty]
    public bool FreeBuilding { get; protected set; }

    [JsonIgnore]
    public GameProperties CurrentGame
    {
        get => currentGame ?? throw new InvalidOperationException("Editor not initialized with current game yet");
        set => currentGame = value;
    }

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

    [JsonIgnore]
    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    public bool NodeReferencesResolved { get; private set; }

    [JsonIgnore]
    public float LightLevel
    {
        get => lightLevel;
        set
        {
            lightLevel = value;

            ApplyComponentLightLevels();
        }
    }

    [JsonProperty]
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

    public abstract Species EditedBaseSpecies { get; }

    protected abstract string MusicCategory { get; }

    protected abstract MainGameState ReturnToState { get; }

    protected abstract string EditorLoadingMessage { get; }

    /// <summary>
    ///   True when there is an inprogress action that prevents other actions from being done (and tab changes)
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
        RootOfDynamicallySpawned = world.GetNode<Spatial>("DynamicallySpawned");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
        editorGUIBaseNode = GetNode<Control>(EditorGUIBaseNodePath);

        if (EditorTabSelectorPath != null)
            editorTabSelector = GetNode<MicrobeEditorTabButtons>(EditorTabSelectorPath);

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

    public override void _Process(float delta)
    {
        if (!Ready)
        {
            if (!CurrentGame.GameWorld.IsAutoEvoFinished())
            {
                LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState,
                    TranslationServer.Translate("WAITING_FOR_AUTO_EVO") + " " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }

            Ready = true;
            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.5f, OnEditorReady, false, false);
        }

        // Auto save after editor entry is complete
        if (TransitionFinished && wantsToSave)
        {
            if (!CurrentGame.FreeBuild)
                PerformAutoSave();

            wantsToSave = false;
        }

        UpdateEditor(delta);
    }

    public void OnFinishLoading(Save save)
    {
        // Handle the stage to return to specially, as it also needs to run the code
        // for fixing the stuff in order to return there
        // TODO: this could be probably moved now to just happen when it enters the scene first time
        ReturnToStage?.OnFinishLoading(save);
    }

    /// <summary>
    ///   Tries to start editor apply results and exit
    /// </summary>
    /// <returns>True if started. False if something is not good and editor can't be exited currently.</returns>
    public bool OnFinishEditing(List<EditorUserOverride>? overrides = null)
    {
        // Prevent exiting when the transition hasn't finished
        if (!TransitionFinished)
            return false;

        // Can't finish an organism edit if an action is in progress
        if (CanCancelAction)
        {
            OnActionBlockedWhileMoving();
            return false;
        }

        overrides ??= new List<EditorUserOverride>();

        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (!editorComponent.CanFinishEditing(overrides))
            {
                GD.Print(editorComponent.GetType().Name, " editor component is not allowing editing to finish yet");
                return false;
            }
        }

        if (EditedBaseSpecies == null)
            throw new InvalidOperationException("Editor not initialized, missing edited species");

        TransitionManager.Instance.AddSequence(
            ScreenFade.FadeType.FadeOut, 0.3f, OnEditorExitTransitionFinished, false);

        return true;
    }

    public void NotifyUndoRedoStateChanged()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.UpdateUndoRedoButtons(history.CanUndo(), history.CanRedo());
        }
    }

    /// <summary>
    ///   Sets the visibility of placed cell parts, editor forward arrow, etc. for all editor components
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Individual editor parts may have a SetEditorWorldTabSpecificObjectVisibility
    ///     method to be able to set the visibility per editor component
    ///   </para>
    /// </remarks>
    public virtual void SetEditorObjectVisibility(bool shown)
    {
        RootOfDynamicallySpawned.Visible = shown;
    }

    public void DirtyMutationPointsCache()
    {
        mutationPointsCache = null;
    }

    public bool HexPlacedThisSession<THex>(THex hex)
        where THex : class, IActionHex
    {
        return history.HexPlacedThisSession(hex);
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

        NotifyUndoRedoStateChanged();
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

        NotifyUndoRedoStateChanged();
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
        editorGUIBaseNode.Visible = !editorGUIBaseNode.Visible;
    }

    public abstract bool CancelCurrentAction();

    public abstract int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions);

    public virtual bool EnqueueAction(TAction action)
    {
        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (!CheckEnoughMPForAction(WhatWouldActionsCost(action.Data)))
            return false;

        if (HasInProgressAction)
        {
            GD.Print("Editor action blocked due to an in-progress action");
            OnActionBlockedWhileMoving();
            return false;
        }

        history.AddAction(action);

        NotifyUndoRedoStateChanged();

        DirtyMutationPointsCache();

        return true;
    }

    public bool EnqueueAction(ReversibleAction action)
    {
        return EnqueueAction((TAction)action);
    }

    public bool CheckEnoughMPForAction(int cost)
    {
        // Freebuilding check is here because in freebuild we are allowed to make edits that consume more than the max
        // MP in a single go, and those wouldn't work without this freebuilding check here
        if (MutationPoints < cost && !FreeBuilding)
        {
            // Flash the MP bar and play sound
            OnInsufficientMP();
            return false;
        }

        return true;
    }

    public virtual void OnInsufficientMP(bool playSound = true)
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (editorComponent.Visible)
            {
                editorComponent.OnInsufficientMP(playSound);
                break;
            }
        }
    }

    public virtual void OnActionBlockedWhileMoving()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (editorComponent.Visible)
            {
                editorComponent.OnActionBlockedWhileAnotherIsInProgress();
                break;
            }
        }
    }

    public virtual void OnInvalidAction()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (editorComponent.Visible)
            {
                editorComponent.OnInvalidAction();
                break;
            }
        }
    }

    public virtual void OnValidAction()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (editorComponent.Visible)
            {
                editorComponent.OnValidAction();
                break;
            }
        }
    }

    public bool RequestFinishEditingWithOverride(List<EditorUserOverride> userOverrides)
    {
        if (userOverrides.Count < 1)
            throw new ArgumentException("empty list of overrides", nameof(userOverrides));

        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (!editorComponent.CanFinishEditing(userOverrides))
            {
                GD.Print(editorComponent.GetType().Name,
                    " editor component is still not allowing editing to finish yet");
                return false;
            }
        }

        return OnFinishEditing(userOverrides);
    }

    protected abstract void ResolveDerivedTypeNodeReferences();

    protected abstract void InitEditorGUI(bool fresh);

    protected virtual void InitEditor(bool fresh)
    {
        pauseMenu.GameProperties = CurrentGame;

        if (fresh)
        {
            // Auto save is wanted once possible
            wantsToSave = true;
        }

        InitEditorGUI(fresh);
        NotifyUndoRedoStateChanged();

        // TODO: dynamic MP changes
        // NotifySymmetryButtonState();

        // Set the right active tab if it isn't the default or we loaded a save
        ApplyEditorTab();

        if (fresh)
        {
            SetupEditedSpecies();

            // For now we only show a loading screen if auto-evo is not ready yet
            if (!CurrentGame.GameWorld.IsAutoEvoFinished())
            {
                Ready = false;
                LoadingScreen.Instance.Show(EditorLoadingMessage, ReturnToState,
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);

                CurrentGame.GameWorld.FinishAutoEvoRunAtFullSpeed();

                TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.5f, null, false, false);
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
        else
        {
            if (Ready != true || CurrentGame == null)
                throw new InvalidOperationException("loaded editor isn't in the ready state, or missing current game");

            // Make absolutely sure the current game doesn't have an auto-evo run
            CurrentGame.GameWorld.ResetAutoEvoRun();

            // Make sure non-default tab button is highlighted right if we loaded a save where the tab was changed
            editorTabSelector?.SetCurrentTab(selectedEditorTab);

            // Just assume that a transition is finished (even though one may still be running after save load is
            // complete). This should be fine as it will just be skipped if the player immediately exits the editor
            // to the stage
            TransitionFinished = true;
        }

        if (CurrentGame == null)
            throw new Exception($"Editor setup which was just ran didn't setup {nameof(CurrentGame)}");

        if (EditedBaseSpecies == null)
            throw new Exception($"Editor setup which was just ran didn't setup {nameof(EditedBaseSpecies)}");

        pauseMenu.SetNewSaveNameFromSpeciesName();

        ApplyComponentLightLevels();
    }

    protected bool ForwardEditorComponentFinishRequest(List<EditorUserOverride>? userOverrides)
    {
        if (userOverrides == null)
        {
            return OnFinishEditing();
        }

        return RequestFinishEditingWithOverride(userOverrides);
    }

    protected abstract IEnumerable<IEditorComponent> GetAllEditorComponents();

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    protected virtual void OnEnterEditor()
    {
        // Clear old stuff in the world
        RootOfDynamicallySpawned.FreeChildren();

        if (!IsLoadedFromSave)
        {
            history = new EditorActionHistory<TAction>();

            // Start a new game if no game has been started
            if (currentGame == null)
            {
                if (ReturnToStage != null)
                    throw new Exception("stage to return to should have set our current game");

                GD.Print("Starting a new game for ", GetType().Name);
                CurrentGame = StartNewGameForEditor();
            }
        }
        else
        {
            UpdateHistoryCallbackTargets(history);
        }

        InitEditor(!IsLoadedFromSave);
        SendFreebuildStatusToComponents();

        StartMusic();
    }

    protected abstract void UpdateHistoryCallbackTargets(ActionHistory<TAction> actionHistory);

    /// <summary>
    ///   Called once auto-evo results are ready
    /// </summary>
    protected virtual void OnEditorReady()
    {
        Ready = true;
        LoadingScreen.Instance.Hide();

        GD.Print("Elapsing time on editor entry");
        ElapseEditorEntryTime();

        // Get summary before applying results in order to get comparisons to the previous populations
        var run = CurrentGame.GameWorld.GetAutoEvoRun();

        if (run.Results != null)
        {
            // External effects need to be finalized now before we use them for printing summaries or anything like
            // that
            run.CalculateFinalExternalEffectSizes();

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

    protected void SetEditorTab(EditorTab tab)
    {
        if (HasInProgressAction)
        {
            // Make sure button status is reset so that it doesn't look like the wrong tab button is now active
            editorTabSelector?.SetCurrentTab(selectedEditorTab);

            ToolTipManager.Instance.ShowPopup(
                TranslationServer.Translate("ACTION_BLOCKED_WHILE_ANOTHER_IN_PROGRESS"), 1.5f);
            return;
        }

        selectedEditorTab = tab;

        ApplyEditorTab();
        editorTabSelector?.SetCurrentTab(selectedEditorTab);
    }

    protected abstract void ApplyEditorTab();

    protected void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SceneManager.Instance.QuitThrive();
    }

    protected IEditorComponent? GetActiveEditorComponent()
    {
        // Assume first visible editor component is the active one
        foreach (var editorComponent in GetAllEditorComponents())
        {
            if (editorComponent.Visible)
                return editorComponent;
        }

        return null;
    }

    protected virtual void OnUndoPerformed()
    {
        DirtyMutationPointsCache();
    }

    protected virtual void OnRedoPerformed()
    {
        DirtyMutationPointsCache();
    }

    protected virtual void UpdateEditor(float delta)
    {
        if (mutationPointsCache == null)
        {
            // This calls OnMutationPointsChanged anyway so we call this directly here to save a duplicate callback
            // dispatch to editor components
            CalculateMutationPointsLeft();
        }
    }

    protected abstract void ElapseEditorEntryTime();

    protected abstract void PerformAutoSave();
    protected abstract void PerformQuickSave();

    protected abstract void SaveGame(string name);

    protected abstract GameProperties StartNewGameForEditor();

    protected virtual void SendFreebuildStatusToComponents()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.NotifyFreebuild(FreeBuilding);
        }
    }

    protected virtual void SetupEditedSpecies()
    {
        // Derived types must initialize the species data before calling us
        var species = EditedBaseSpecies;
        if (species == null)
        {
            throw new InvalidOperationException(
                $"Derived editor types must setup edited species before calling {nameof(SetupEditedSpecies)}");
        }

        species.Generation += 1;

        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.OnEditorSpeciesSetup(species);
        }
    }

    protected void OnMutationPointsChanged()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.OnMutationPointsChanged(MutationPoints);
        }
    }

    /// <summary>
    ///   Applies the changes done and exits the editor back to <see cref="ReturnToStage"/>
    /// </summary>
    protected virtual void OnEditorExitTransitionFinished()
    {
        MakeSureEditorReturnIsGood();

        GD.Print(GetType().Name, ": applying changes to edited Species");

        foreach (var editorComponent in GetAllEditorComponents())
        {
            GD.Print(editorComponent.GetType().Name, ": applying changes of component");
            editorComponent.OnFinishEditing();
        }

        var stage = ReturnToStage!;

        // This needs to be reset here to not free this when we exit the tree
        ReturnToStage = null;

        SceneManager.Instance.SwitchToScene(stage);

        stage.OnReturnFromEditor();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PauseMenuPath != null)
            {
                PauseMenuPath.Dispose();
                EditorGUIBaseNodePath.Dispose();
            }

            EditorTabSelectorPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void MakeSureEditorReturnIsGood()
    {
        if (currentGame == null)
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
        var run = CurrentGame.GameWorld.GetAutoEvoRun();
        GD.Print("Applying auto-evo results. Auto-evo run took: ", run.RunDuration);
        run.ApplyAllResultsAndEffects(FreeBuilding);

        // Add the current generation to history before resetting Auto-Evo
        CurrentGame.GameWorld.AddCurrentGenerationToHistory();

        // Clear the run to make the cell stage start a new run when we go back there
        CurrentGame.GameWorld.ResetAutoEvoRun();
    }

    /// <summary>
    ///   Calculates the remaining MP from the action history
    /// </summary>
    /// <returns>The remaining MP</returns>
    private int CalculateMutationPointsLeft()
    {
        if (FreeBuilding || CheatManager.InfiniteMP)
            return Constants.BASE_MUTATION_POINTS;

        mutationPointsCache = history.CalculateMutationPointsLeft();

        if (mutationPointsCache.Value is < 0 or > Constants.BASE_MUTATION_POINTS)
        {
            GD.PrintErr("Invalid MP amount: ", mutationPointsCache,
                " This should only happen if the user disabled the Infinite MP cheat while having mutated too much.");
        }

        OnMutationPointsChanged();

        return mutationPointsCache.Value;
    }

    private void ApplyComponentLightLevels()
    {
        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.OnLightLevelChanged(lightLevel);
        }
    }

    /// <summary>
    ///   Starts a fade in transition
    /// </summary>
    private void FadeIn()
    {
        TransitionManager.Instance.AddSequence(
            ScreenFade.FadeType.FadeIn, 0.5f, () => TransitionFinished = true, false);
    }

    private void StartMusic()
    {
        Jukebox.Instance.PlayCategory(MusicCategory);
    }
}
