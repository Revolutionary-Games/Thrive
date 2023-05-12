using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base for all stages
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public abstract class StageBase : NodeWithInput, IStageBase, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath? PauseMenuPath;

    [Export]
    public NodePath HUDRootPath = null!;

#pragma warning disable CA2213
    protected Node world = null!;
    protected Node rootOfDynamicallySpawned = null!;
    protected PauseMenu pauseMenu = null!;
    protected Control hudRoot = null!;
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
    private float elapsedSinceLightLevelUpdate = 1;

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame?.GameWorld ?? throw new InvalidOperationException("Game not started yet");

    [JsonIgnore]
    public Node GameStateRoot => this;

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    /// <summary>
    ///   True once stage fade-in is complete
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This used to have an internal set (<see cref="CreatureStageBase{TPlayer}.MovingToEditor"/> had that as
    ///     well) but with the needed <see cref="ICreatureStage"/> that seems no longer possible
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

    public virtual void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        world = GetNode<Node>("World");
        rootOfDynamicallySpawned = world.GetNode<Node>("DynamicallySpawned");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
        hudRoot = GetNode<Control>(HUDRootPath);

        NodeReferencesResolved = true;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

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

        GameWorld.Process(delta);

        elapsedSinceLightLevelUpdate += delta;
        if (elapsedSinceLightLevelUpdate > Constants.LIGHT_LEVEL_UPDATE_INTERVAL)
        {
            elapsedSinceLightLevelUpdate = 0;
            OnLightLevelUpdate();
        }
    }

    public abstract void StartMusic();

    public virtual void StartNewGame()
    {
        OnGameStarted();
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

    public abstract void OnFinishLoading(Save save);

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

        GD.Print(CurrentGame!.GameWorld.WorldSettings);

        pauseMenu.GameProperties = CurrentGame ?? throw new InvalidOperationException("current game is not set");

        pauseMenu.SetNewSaveNameFromSpeciesName();

        StartMusic();

        StartGUIStageTransition(!IsLoadedFromSave, false);
    }

    /// <summary>
    ///   Common logic for the case where we directly open this scene or start a new game normally from the menu
    /// </summary>
    protected abstract void OnGameStarted();

    protected abstract void StartGUIStageTransition(bool longDuration, bool returnFromEditor);

    protected abstract bool IsGameOver();

    protected abstract void OnGameOver();

    protected abstract void AutoSave();
    protected abstract void PerformQuickSave();

    protected abstract void OnLightLevelUpdate();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PauseMenuPath != null)
            {
                PauseMenuPath.Dispose();
                HUDRootPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
