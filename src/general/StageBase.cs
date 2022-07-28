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
    protected Control hudRoot = null!;

    [JsonProperty]
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
    ///   True when the player is extinct in the current patch. The player can still move to another patch.
    /// </summary>
    [JsonProperty]
    protected bool playerExtinctInCurrentPatch;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    protected bool wantsToSave;

    private bool transitionFinished;

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

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
        {
            var entities = rootOfDynamicallySpawned.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP).Count();
            var childCount = rootOfDynamicallySpawned.GetChildCount();
            debugOverlay.ReportEntities(entities, childCount - entities);
        }
    }

    public abstract void StartMusic();

    public virtual void StartNewGame()
    {
        SpawnPlayer();

        OnGameStarted();
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

        BaseHUD.OnEnterStageTransition(false, true);
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
    ///   Called when the player died out in a patch and selected a new one
    /// </summary>
    public void MoveToPatch(Patch patch)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Moving to a new patch but stage doesn't have a game state");

        CurrentGame.GameWorld.Map.CurrentPatch = patch;
        UpdatePatchSettings();
        PatchExtinctionResolved();
        SpawnPlayer();
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

        GD.Print(CurrentGame!.GameWorld.WorldSettings);

        pauseMenu.GameProperties = CurrentGame ?? throw new InvalidOperationException("current game is not set");

        pauseMenu.SetNewSaveNameFromSpeciesName();

        StartMusic();

        BaseHUD.OnEnterStageTransition(!IsLoadedFromSave, false);
    }

    /// <summary>
    ///   Common logic for the case where we directly open this scene or start a new game normally from the menu
    /// </summary>
    protected abstract void OnGameStarted();

    protected abstract void UpdatePatchSettings(bool promptPatchNameChange = true);

    /// <summary>
    ///   Increases the population by the constant for the player reproducing
    /// </summary>
    protected void GiveReproductionPopulationBonus()
    {
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulationInCurrentPatch(
            playerSpecies, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT,
            TranslationServer.Translate("PLAYER_REPRODUCED"),
            false, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT);
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    protected void HandlePlayerRespawn()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Can't respawn without current game");

        BaseHUD.HintText = string.Empty;

        var playerSpecies = CurrentGame.GameWorld.PlayerSpecies;

        if (!CurrentGame.FreeBuild)
        {
            if (GameWorld.Map.CurrentPatch == null)
                throw new InvalidOperationException("No current patch set");

            if (IsGameOver())
            {
                GameOver();
            }
            else if (GameWorld.Map.CurrentPatch.GetSpeciesGameplayPopulation(playerSpecies) <= 0)
            {
                // Has run out of population in current patch but not globally
                PlayerExtinctInPatch();
            }

            if (gameOver || playerExtinctInCurrentPatch)
                return;
        }

        // Player is not extinct, so can respawn
        SpawnPlayer();
    }

    protected bool IsGameOver()
    {
        return GameWorld.Map.GetSpeciesGlobalGameplayPopulation(CurrentGame!.GameWorld.PlayerSpecies) <= 0 &&
            !CurrentGame.FreeBuild;
    }

    /// <summary>
    ///   Base class handling of the player dying
    /// </summary>
    protected void HandlePlayerDeath()
    {
        GD.Print("The player has died");

        // Decrease the population by the constant for the player dying
        GameWorld.AlterSpeciesPopulationInCurrentPatch(
            GameWorld.PlayerSpecies,
            Constants.PLAYER_DEATH_POPULATION_LOSS_CONSTANT,
            TranslationServer.Translate("PLAYER_DIED"),
            true, Constants.PLAYER_DEATH_POPULATION_LOSS_COEFFICIENT
            / GameWorld.WorldSettings.PlayerDeathPopulationPenalty);

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

    protected virtual void GameOver()
    {
        // Player is extinct and has lost the game

        gameOver = true;

        // Just to make sure _Process doesn't run
        playerExtinctInCurrentPatch = true;

        // Show the game lost popup if not already visible
        BaseHUD.ShowExtinctionBox();
    }

    protected virtual void PlayerExtinctInPatch()
    {
        playerExtinctInCurrentPatch = true;

        BaseHUD.ShowPatchExtinctionBox();
    }

    private void PatchExtinctionResolved()
    {
        playerExtinctInCurrentPatch = false;

        // Decrease the population by the constant for the player dying out in a patch
        // If the player does not have sufficient population in the new patch then the population drops to 0 and
        // they have to select a new patch if they die again.
        GameWorld.AlterSpeciesPopulationInCurrentPatch(
            GameWorld.PlayerSpecies, Constants.PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_CONSTANT,
            TranslationServer.Translate("EXTINCT_IN_PATCH"),
            true, Constants.PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_COEFFICIENT
            / GameWorld.WorldSettings.PlayerDeathPopulationPenalty);

        // Do not grant the player population even if the global population is 0,
        // they will go extinct the next time they die

        BaseHUD.HidePatchExtinctionBox();
    }
}
