using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base stage for the stages where the player controls a single creature
/// </summary>
/// <typeparam name="TPlayer">The type of the player object</typeparam>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public abstract class StageBase<TPlayer> : NodeWithInput, IStage, IGodotEarlyNodeResolve
    where TPlayer : class
{
    [Export]
    public NodePath PauseMenuPath = null!;

    [Export]
    public NodePath HUDRootPath = null!;

    protected Node world = null!;
    protected Node rootOfDynamicallySpawned = null!;
    protected DirectionalLight worldLight = null!;
    protected PauseMenu pauseMenu = null!;
    private bool transitionFinished;
    protected Control hudRoot = null!;

    // TODO: make this be saved (and preserve old save compatibility by creating this in on save loaded callback
    // if null)
    protected Random random = new();

    /// <summary>
    ///   Used to differentiate between spawning the player the first time and respawning
    /// </summary>
    [JsonProperty]
    protected bool spawnedPlayer;

    /// <summary>
    ///   True when the player is extinct
    /// </summary>
    [JsonProperty]
    protected bool gameOver;

    [JsonProperty]
    protected float playerRespawnTimer;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    protected bool wantsToSave;

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame?.GameWorld ?? throw new InvalidOperationException("Game not started yet");

    /// <summary>
    ///   The current player or null.
    ///   TODO: check: Due to references on save load this needs to be after the systems
    /// </summary>
    [JsonProperty]
    public TPlayer? Player { get; protected set; }

    [JsonIgnore]
    public bool HasPlayer => Player != null;

    [JsonIgnore]
    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    /// <summary>
    ///   True once stage fade-in is complete
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This used to have an internal set (<see cref="MovingToEditor"/> had that as well) but with the needed
    ///     <see cref="IStage"/> that seems no longer possible
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
    ///   True when transitioning to the editor
    /// </summary>
    [JsonIgnore]
    public bool MovingToEditor { get; set; }

    /// <summary>
    ///   List access to the dynamic entities in the stage. This is used for saving and loading
    /// </summary>
    public List<Node> DynamicEntities
    {
        get
        {
            var results = new HashSet<Node>();

            foreach (var node in rootOfDynamicallySpawned.GetChildren())
            {
                bool disposed = false;

                var casted = (Spatial)node;

                // Objects that cause disposed exceptions. Seems still pretty important to protect saving against
                // very rare issues
                try
                {
                    // Skip objects that will be deleted. This might help with Microbe saving as it might be that
                    // the contained organelles are already disposed whereas the Microbe is just only queued for
                    // deletion
                    if (casted.IsQueuedForDeletion())
                    {
                        disposed = true;
                    }
                    else
                    {
                        if (casted.Transform.origin == Vector3.Zero)
                        {
                        }
                    }
                }
                catch (ObjectDisposedException e)
                {
                    disposed = true;

                    // TODO: remove the disposed checks entirely once we confirm this never happens anymore
                    GD.PrintErr("Detected a disposed object to be saved: ", e);
                }

                if (!disposed)
                    results.Add(casted);
            }

            return results.ToList();
        }
        set
        {
            rootOfDynamicallySpawned.FreeChildren();

            foreach (var entity in value)
            {
                rootOfDynamicallySpawned.AddChild(entity);
            }
        }
    }

    protected abstract IStageHUD BaseHUD { get; }

    public virtual void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        world = GetNode<Node>("World");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        worldLight = world.GetNode<DirectionalLight>("WorldLight");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
        hudRoot = GetNode<Control>(HUDRootPath);

        NodeReferencesResolved = true;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (gameOver)
        {
            // Player is extinct and has lost the game Show the game lost popup if not already visible
            BaseHUD.ShowExtinctionBox();
            return;
        }

        if (!HasPlayer)
        {
            if (!spawnedPlayer)
            {
                GD.PrintErr("Stage was entered without spawning the player");
                SpawnPlayer();
            }
            else
            {
                // Respawn the player once the timer is up
                playerRespawnTimer -= delta;

                if (playerRespawnTimer <= 0)
                {
                    HandlePlayerRespawn();
                }
            }
        }

        // Start auto-evo if stage entry finished, don't need to auto save,
        // settings have auto-evo be started during gameplay and auto-evo is not already started
        if (TransitionFinished && !wantsToSave && Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            GameWorld.IsAutoEvoFinished(true);
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

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
        {
            var entities = rootOfDynamicallySpawned.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP).Count();
            var childCount = rootOfDynamicallySpawned.GetChildCount();
            metrics.ReportEntities(entities, childCount - entities);
        }
    }

    public abstract void StartMusic();

    public virtual void StartNewGame()
    {
        SpawnPlayer();
    }

    public abstract void OnFinishLoading(Save save);

    /// <summary>
    ///   Called when returning from the editor
    /// </summary>
    public virtual void OnReturnFromEditor()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Returning to stage from editor without a game setup");

        // Now the editor increases the generation so we don't do that here anymore

        // Make sure player is spawned
        SpawnPlayer();

        BaseHUD.OnEnterStageTransition(false);
        BaseHUD.HideReproductionDialog();

        StartMusic();

        // Reset locale to assure the stage's language.
        // Because the stage scene tree being unattached during editor, if language was
        // changed while in the editor, it doesn't update this stage's translation cache.
        TranslationServer.SetLocale(TranslationServer.GetLocale());

        // Auto save is wanted once possible (unless we are in prototypes)
        if (!CurrentGame.InPrototypes)
            wantsToSave = true;

        pauseMenu.SetNewSaveNameFromSpeciesName();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            if (CurrentGame?.GameWorld.Map.CurrentPatch == null)
                throw new InvalidOperationException("Stage not initialized properly");

            BaseHUD.UpdatePatchInfo(CurrentGame.GameWorld.Map.CurrentPatch.Name.ToString());
        }
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

    public virtual void OnFinishTransitioning()
    {
        TransitionFinished = true;
    }

    public abstract void MoveToEditor();

    public abstract void OnSuicide();

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
        }

        pauseMenu.GameProperties = CurrentGame ?? throw new InvalidOperationException("current game is not set");

        pauseMenu.SetNewSaveNameFromSpeciesName();

        StartMusic();

        if (IsLoadedFromSave)
        {
            BaseHUD.OnEnterStageTransition(false);
        }
        else
        {
            BaseHUD.OnEnterStageTransition(true);
        }
    }

    /// <summary>
    ///   Increases the population by the constant for the player reproducing
    /// </summary>
    protected void GiveReproductionPopulationBonus()
    {
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT,
            TranslationServer.Translate("PLAYER_REPRODUCED"),
            false, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT);
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    protected void HandlePlayerRespawn()
    {
        BaseHUD.HintText = string.Empty;

        // Respawn if not extinct (or freebuild)
        if (IsGameOver())
        {
            gameOver = true;
        }
        else
        {
            // Player is not extinct, so can respawn
            SpawnPlayer();
        }
    }

    protected bool IsGameOver()
    {
        return GameWorld.PlayerSpecies.Population <= 0 && !CurrentGame!.FreeBuild;
    }

    /// <summary>
    ///   Base class handling of the player dying
    /// </summary>
    protected void HandlePlayerDeath()
    {
        GD.Print("The player has died");

        // Decrease the population by the constant for the player dying
        GameWorld.AlterSpeciesPopulation(
            GameWorld.PlayerSpecies, Constants.PLAYER_DEATH_POPULATION_LOSS_CONSTANT,
            TranslationServer.Translate("PLAYER_DIED"),
            true, Constants.PLAYER_DEATH_POPULATION_LOSS_COEFFICIENT);

        if (IsGameOver())
        {
            Jukebox.Instance.PlayCategory("Extinction");
        }

        BaseHUD.HideReproductionDialog();
    }

    protected virtual void OnCanEditStatusChanged(bool canEdit)
    {
        if (canEdit)
        {
            // This is to prevent the editor button being able to be clicked multiple times in freebuild mode
            if (!MovingToEditor)
                BaseHUD.ShowReproductionDialog();
        }
        else
        {
            BaseHUD.HideReproductionDialog();
        }
    }

    /// <summary>
    ///   Spawns the player if there isn't currently a player node existing
    /// </summary>
    protected abstract void SpawnPlayer();

    protected abstract void AutoSave();
    protected abstract void PerformQuickSave();
}
