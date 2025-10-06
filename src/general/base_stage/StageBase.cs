using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base for all stages
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
[GodotAbstract]
public partial class StageBase : NodeWithInput, IStageBase, IGodotEarlyNodeResolve
{
#pragma warning disable CA2213
    protected Node world = null!;
    protected Node rootOfDynamicallySpawned = null!;

    [Export]
    protected PauseMenu pauseMenu = null!;

    [Export]
    protected Control hudRoot = null!;

    protected Node3D? graphicsPreloadNode;
#pragma warning restore CA2213

    [JsonProperty]
    protected Random random = new();

    /// <summary>
    ///   True when the player is extinct
    /// </summary>
    [JsonProperty]
    protected bool gameOver;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    protected bool wantsToSave;

    private bool transitionFinished;

    /// <summary>
    ///   Used to control how often updated light level data is read from the day/night cycle.
    /// </summary>
    private double elapsedSinceLightLevelUpdate = 1;

    protected StageBase()
    {
    }

    public enum LoadState
    {
        NotLoading,
        Loading,
        GraphicsPreload,
        RenderWithPreload,
        GraphicsClear,
        Finished,
    }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame?.GameWorld ?? throw new InvalidOperationException("Game not started yet");

    /// <summary>
    ///   True when the player is ascended and they should be let to do crazy stuff
    /// </summary>
    [JsonIgnore]
    public bool Ascended => CurrentGame?.Ascended == true;

    [JsonIgnore]
    public Node GameStateRoot => this;

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    [JsonIgnore]
    public virtual MainGameState GameState => throw new GodotAbstractPropertyNotOverriddenException();

    /// <summary>
    ///   True once stage fade-in is complete
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This used to have an internal set (<see cref="CreatureStageBase{TPlayer,TSimulation}.MovingToEditor"/>
    ///     had that as well) but with the needed <see cref="ICreatureStage"/> that seems no longer possible
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public bool TransitionFinished
    {
        get => transitionFinished;
        set
        {
            transitionFinished = value;
            pauseMenu.GameLoading = !transitionFinished;
        }
    }

    /// <summary>
    ///   True when the stage is showing a loading screen and waiting to start.
    ///   Normal processing should be skipped in this state.
    /// </summary>
    [JsonIgnore]
    protected LoadState StageLoadingState { get; private set; }

    public virtual void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        world = GetNode<Node>("World");
        rootOfDynamicallySpawned = world.GetNode<Node>("DynamicallySpawned");
        NodeReferencesResolved = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (AchievementsManager.HasUsedCheats)
        {
            Invoke.Instance.QueueForObject(() =>
            {
                if (CurrentGame != null)
                {
                    if (CurrentGame.CheatsUsed != true)
                    {
                        GD.Print("Copying cheats used state to current game on scene enter tree");
                        CurrentGame.ReportCheatsUsed();
                    }
                }
                else
                {
                    GD.PrintErr("Stage base expected current game data to be initialized already");
                }
            }, this);
        }

        AchievementsManager.OnPlayerHasCheatedEvent += OnCheatsUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        AchievementsManager.OnPlayerHasCheatedEvent -= OnCheatsUsed;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (StageLoadingState != LoadState.NotLoading)
        {
            if (PerformStageLoadingAction())
            {
                StartFinalFadeInIfNotStarted();
            }

            return;
        }

        // Save if wanted
        if (TransitionFinished && wantsToSave)
        {
            if (CurrentGame == null)
            {
                throw new InvalidOperationException(
                    "Stage doesn't have a game state even though it should be initialized");
            }

            if (!CurrentGame.FreeBuild)
                AutoSave();

            wantsToSave = false;
        }

        GameWorld.Process((float)delta * GetWorldTimeMultiplier());

        elapsedSinceLightLevelUpdate += delta;
        if (elapsedSinceLightLevelUpdate > Constants.LIGHT_LEVEL_UPDATE_INTERVAL)
        {
            elapsedSinceLightLevelUpdate = 0;
            OnLightLevelUpdate();
        }
    }

    public virtual void StartMusic()
    {
        GD.PrintErr($"{nameof(StartMusic)} method not overridden for stage");
    }

    public virtual void StartNewGame()
    {
        OnGameStarted();
    }

    public virtual float GetWorldTimeMultiplier()
    {
        return 1;
    }

    public virtual void OnBlankScreenBeforeFadeIn()
    {
        // Collect any accumulated garbage before running the main game stage, which is much more framerate-sensitive
        // than the scene switching process
        GC.Collect();

        // Let gameplay code start running while fading in as that makes things smoother
        StageLoadingState = LoadState.NotLoading;

        // Unpause gameplay nodes and code
        world.ProcessMode = ProcessModeEnum.Inherit;
    }

    [RunOnKeyDown("g_toggle_gui")]
    public void ToggleGUI()
    {
        hudRoot.Visible = !hudRoot.Visible;
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        if (!TransitionFinished || wantsToSave)
        {
            GD.Print("Skipping quick save as stage transition is not finished or saving is queued");
            return;
        }

        GD.Print("quick saving stage");
        PerformQuickSave();
    }

    public void GameOver()
    {
        // Player is extinct and has lost the game
        gameOver = true;

        OnGameOver();
    }

    public virtual void OnFinishTransitioning()
    {
        TransitionFinished = true;
    }

    public virtual void OnFinishLoading(Save save)
    {
    }

    /// <summary>
    ///   Tries to open God Tools for the specified game object
    /// </summary>
    /// <param name="godotEntity">
    ///   The object that was passed through Godot signals. This uses the Godot type here to ensure
    /// </param>
    public virtual void OpenGodToolsForEntity(GodotObject? godotEntity)
    {
        if (godotEntity == null || !IsInstanceValid(godotEntity))
            return;

        if (godotEntity is IEntity entity)
        {
            OnOpenGodTools(entity);
        }
        else
        {
            GD.PrintErr("Unknown godot passed object to open god tools for: ", godotEntity);
        }
    }

    /// <summary>
    ///   Prepares the stage for playing. Also begins a new game if one hasn't been started yet for easier debugging.
    /// </summary>
    protected virtual void SetupStage()
    {
        if (!IsLoadedFromSave)
        {
            if (CurrentGame == null)
            {
                StartNewGame();
            }
            else
            {
                OnGameStarted();
            }
        }

        // Unlock everything if the stage is unsupported
        if (!UnlockProgress.SupportsGameState(GameState))
        {
            CurrentGame!.GameWorld.UnlockProgress.UnlockAll = true;
        }

        GD.Print(CurrentGame!.GameWorld.WorldSettings);

        pauseMenu.GameProperties = CurrentGame ?? throw new InvalidOperationException("current game is not set");

        pauseMenu.SetNewSaveNameFromSpeciesName();

        StartMusic();

        OnStartLoading();
        StartGUIStageTransition(!IsLoadedFromSave, false);
    }

    protected virtual void OnStartLoading()
    {
        // Ignore duplicate requests to start loading which happen when exiting the editor
        if (StageLoadingState != LoadState.NotLoading)
            return;

        StageLoadingState = LoadState.Loading;
        world.ProcessMode = ProcessModeEnum.Disabled;

        // Preloading of graphics assets and showing a loading screen
        ResourceManager.Instance.OnStageLoadStart(GameState);
    }

    protected virtual bool PerformStageLoadingAction()
    {
        switch (StageLoadingState)
        {
            case <= LoadState.Loading:
            {
                var resourceManager = ResourceManager.Instance;
                if (resourceManager.ProgressStageLoad())
                {
                    StageLoadingState = LoadState.GraphicsPreload;
                    UpdateStageLoadingMessage(StageLoadingState, 0, 0);
                }
                else
                {
                    UpdateStageLoadingMessage(StageLoadingState, resourceManager.StageLoadCurrentProgress,
                        resourceManager.StageLoadTotalItems);
                }

                return false;
            }

            case LoadState.GraphicsPreload:
                InstantiateGraphicsPreload();
                StageLoadingState = LoadState.RenderWithPreload;
                UpdateStageLoadingMessage(StageLoadingState, 0, 0);
                return false;

            case LoadState.RenderWithPreload:
                StageLoadingState = LoadState.GraphicsClear;
                UpdateStageLoadingMessage(StageLoadingState, 0, 0);
                return false;

            case LoadState.GraphicsClear:
                CleanupGraphicsPreload();
                UpdateStageLoadingMessage(StageLoadingState, 0, 0);
                return true;

            case LoadState.Finished:
                // Nothing more to do
                UpdateStageLoadingMessage(StageLoadingState, 0, 0);
                return true;

            default:
                GD.PrintErr("Unknown stage loading state: ", StageLoadingState);
                return true;
        }
    }

    protected virtual Node3D CreateGraphicsPreloadNode()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void InstantiateGraphicsPreload()
    {
        if (graphicsPreloadNode != null)
        {
            GD.PrintErr("Graphics pre-load node already exists");
            graphicsPreloadNode.QueueFree();
        }

        graphicsPreloadNode = CreateGraphicsPreloadNode();

        if (graphicsPreloadNode.GetParent() == null)
            throw new Exception("Graphics preload node has no parent");

        var resources = SimulationParameters.Instance.GetStageResources(GameState);

        CreateGraphicsPreloads(graphicsPreloadNode, resources);
    }

    protected virtual void CreateGraphicsPreloads(Node3D parent, StageResourcesList resources)
    {
        foreach (var resource in resources.RequiredScenes)
        {
            var scene = resource.LoadedScene;

            if (scene == null)
            {
                GD.PrintErr("Scene not loaded for preload resource: ", resource.Path);
                continue;
            }

            parent.AddChild(scene.Instantiate());
        }

        foreach (var resource in resources.RequiredVisualResources)
        {
            var scene = resource.LoadedNormalQuality;

            if (scene == null)
            {
                GD.PrintErr("Visual not loaded for preload resource: ", resource.VisualIdentifier);
                continue;
            }

            parent.AddChild(scene.Instantiate());
        }
    }

    protected virtual void CleanupGraphicsPreload()
    {
        if (graphicsPreloadNode == null)
        {
            GD.PrintErr("No graphics preload to delete");
            return;
        }

        graphicsPreloadNode.QueueFree();
        graphicsPreloadNode = null;
    }

    protected void StartFinalFadeInIfNotStarted()
    {
        // Avoid triggering the fade a bunch of times
        if (StageLoadingState == LoadState.Finished)
            return;

        StageLoadingState = LoadState.Finished;

        GD.Print("Stage load finished, will enter properly now");
        OnTriggerHUDFinalLoadFadeIn();
    }

    protected virtual void UpdateStageLoadingMessage(LoadState loadState, int currentProgress, int totalItems)
    {
        LoadingScreen.Instance.LoadingMessage = Localization.Translate("LOADING_STAGE");

        SetStageLoadingDescription(loadState, currentProgress, totalItems);
    }

    protected void SetStageLoadingDescription(LoadState loadState, int currentProgress, int totalItems)
    {
        if (loadState < LoadState.GraphicsPreload)
        {
            LoadingScreen.Instance.LoadingDescription =
                Localization.Translate("LOADING_STAGE_ASSETS").FormatSafe(currentProgress, totalItems);
        }
        else
        {
            LoadingScreen.Instance.LoadingDescription = Localization.Translate("LOADING_GRAPHICS_SHADERS");
        }
    }

    /// <summary>
    ///   Common logic for the case where we directly open this scene or start a new game normally from the menu
    /// </summary>
    protected virtual void OnGameStarted()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnTriggerHUDFinalLoadFadeIn()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual bool IsGameOver()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnGameOver()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void AutoSave()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void PerformQuickSave()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnLightLevelUpdate()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    // TODO: implement for all stages
    protected virtual void OnOpenGodTools(IEntity entity)
    {
        GD.PrintErr("Non-implemented God tools opening for entity in stage type: ", GetType().Name);
    }

    protected virtual void OnCheatsUsed()
    {
        if (CurrentGame == null)
        {
            Invoke.Instance.QueueForObject(ApplyCheatsUsedFlag, this);
            return;
        }

        ApplyCheatsUsedFlag();
    }

    private void ApplyCheatsUsedFlag()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Current game has not been set even though it should be initialized");

        if (CurrentGame.CheatsUsed)
            return;

        GD.Print("Detected player used cheats for the first time in this game");
        CurrentGame.ReportCheatsUsed();
    }
}
