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
public abstract class CreatureStageBase<TPlayer> : StageBase, ICreatureStage
    where TPlayer : class
{
#pragma warning disable CA2213
    protected DirectionalLight worldLight = null!;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    protected DayNightCycle lightCycle = null!;

    /// <summary>
    ///   Used to differentiate between spawning the player the first time and respawning
    /// </summary>
    [JsonProperty]
    protected bool spawnedPlayer;

    [JsonProperty]
    protected float playerRespawnTimer;

    /// <summary>
    ///   True when the player is extinct in the current patch. The player can still move to another patch.
    /// </summary>
    [JsonProperty]
    protected bool playerExtinctInCurrentPatch;

    /// <summary>
    ///   The current player or null.
    ///   TODO: check: Due to references on save load this needs to be after the systems
    /// </summary>
    [JsonProperty]
    public TPlayer? Player { get; protected set; }

    [JsonIgnore]
    public bool HasPlayer => Player != null;

    /// <summary>
    ///   True when transitioning to the editor. Note this should only be unset *after* switching scenes to the editor
    ///   otherwise some tree exit operations won't run correctly.
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

    [JsonIgnore]
    protected abstract ICreatureStageHUD BaseHUD { get; }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        worldLight = world.GetNode<DirectionalLight>("WorldLight");
        lightCycle = new DayNightCycle();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Cancel auto-evo if it is running to not leave background runs from other games running if the player
        // just loaded a save
        if (!MovingToEditor)
        {
            GameWorld.ResetAutoEvoRun();
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        lightCycle.Process(delta);

        if (gameOver)
        {
            // Player is extinct and has lost the game Show the game lost popup if not already visible
            BaseHUD.ShowExtinctionBox();
            return;
        }

        // Handle player respawning
        if (!HasPlayer && !MovingToEditor)
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

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
        {
            float totalEntityWeight = 0;
            int totalEntityCount = 0;

            foreach (var entity in rootOfDynamicallySpawned.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP))
            {
                totalEntityWeight += entity.EntityWeight;
                ++totalEntityCount;
            }

            var childCount = rootOfDynamicallySpawned.GetChildCount();
            debugOverlay.ReportEntities(totalEntityWeight, childCount - totalEntityCount);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            // TODO: the following doesn't seem to do anything so confirm that and remove
            if (CurrentGame?.GameWorld.Map.CurrentPatch == null)
                throw new InvalidOperationException("Stage not initialized properly");
        }
    }

    public override void StartNewGame()
    {
        SpawnPlayer();

        base.StartNewGame();
    }

    /// <summary>
    ///   Called when returning from the editor
    /// </summary>
    public virtual void OnReturnFromEditor()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Returning to stage from editor without a game setup");

        // Update the generation history with the newly edited player species, but only if the history has been
        // generated (this is not the case in older saves)
        if (GameWorld.GenerationHistory.Count > 0)
        {
            var lastGeneration = GameWorld.GenerationHistory.Keys.Max();
            GameWorld.GenerationHistory[lastGeneration].UpdateSpeciesData(GameWorld.PlayerSpecies);
        }

        // Now the editor increases the generation so we don't do that here anymore

        // Make sure player is spawned
        SpawnPlayer();

        BaseHUD.OnEnterStageTransition(false, true);
        BaseHUD.HideReproductionDialog();

        // Pass some extra time to hud messages to make short lived messages from the previous life (like editor ready
        // disappear)
        BaseHUD.HUDMessages.PassExtraTime(Constants.HUD_MESSAGES_EXTRA_ELAPSE_TIME_FROM_EDITOR);

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

    protected override void SetupStage()
    {
        if (IsLoadedFromSave)
        {
            lightCycle.CalculateDependentLightData(GameWorld.WorldSettings);
        }

        base.SetupStage();
    }

    protected override void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        BaseHUD.OnEnterStageTransition(longDuration, returnFromEditor);
    }

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

    protected override bool IsGameOver()
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

    protected override void OnGameOver()
    {
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
