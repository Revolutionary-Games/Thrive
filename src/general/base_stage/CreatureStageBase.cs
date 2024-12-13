﻿using System;
using System.Linq;
using Components;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base stage for the stages where the player controls a single creature
/// </summary>
/// <typeparam name="TPlayer">The type of the player object</typeparam>
/// <typeparam name="TSimulation">The type of simulation this stage uses</typeparam>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
[GodotAbstract]
public partial class CreatureStageBase<TPlayer, TSimulation> : StageBase, ICreatureStage
    where TSimulation : class, IWorldSimulation, new()
{
#pragma warning disable CA2213
    protected DirectionalLight3D worldLight = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to differentiate between spawning the player the first time and respawning
    /// </summary>
    [JsonProperty]
    protected bool spawnedPlayer;

    [JsonProperty]
    protected double playerRespawnTimer;

    /// <summary>
    ///   True when the player is extinct in the current patch. The player can still move to another patch.
    /// </summary>
    [JsonProperty]
    protected bool playerExtinctInCurrentPatch;

    public CreatureStageBase()
    {
    }

    protected CreatureStageBase(TSimulation worldSimulation)
    {
        WorldSimulation = worldSimulation;
    }

    // TODO: eventually convert this just to a Entity without having any generic type configurability here
    /// <summary>
    ///   The current player or null.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is JSON ignored as the entity reference can't be loaded currently, see why here:
    ///     <see cref="SaveContext.OldToNewEntityMapping"/>
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public TPlayer? Player { get; protected set; }

    [JsonIgnore]
    public virtual bool HasPlayer => throw new GodotAbstractPropertyNotOverriddenException();

    [JsonIgnore]
    public virtual bool HasAlivePlayer => throw new GodotAbstractPropertyNotOverriddenException();

    [JsonProperty]
    public TSimulation WorldSimulation { get; private set; } = null!;

    /// <summary>
    ///   True when transitioning to the editor. Note this should only be unset *after* switching scenes to the editor
    ///   otherwise some tree exit operations won't run correctly.
    /// </summary>
    [JsonIgnore]
    public bool MovingToEditor { get; set; }

    [JsonIgnore]
    protected virtual ICreatureStageHUD BaseHUD => throw new GodotAbstractPropertyNotOverriddenException();

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        worldLight = world.GetNode<DirectionalLight3D>("WorldLight");
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

    public override void _Process(double delta)
    {
        base._Process(delta);

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

            foreach (var entity in WorldSimulation.EntitySystem)
            {
                ++totalEntityCount;

                if (!entity.Has<Spawned>())
                    continue;

                totalEntityWeight += entity.Get<Spawned>().EntityWeight;
            }

            debugOverlay.ReportEntities(totalEntityWeight, totalEntityCount);
        }

        if (CheatManager.ManuallySetTime)
        {
            GameWorld.LightCycle.FractionOfDayElapsed = CheatManager.DayNightFraction;
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

        // Now the editor increases the generation, so we don't do that here anymore

        // Make sure player is spawned
        SpawnPlayer();

        BaseHUD.OnEnterStageTransition(false, true);
        BaseHUD.HideReproductionDialog();

        // Pass some extra time to hud messages to make short-lived messages from the previous life (like editor ready
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

        // Collect any accumulated garbage before running the main game stage which is much more framerate-sensitive
        // than the scene switching process
        GC.Collect();
    }

    public virtual void MoveToEditor()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void OnSuicide()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

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

    public void ContinueGameAsSpecies(Species species)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Trying to continue game but game is not set");

        GD.Print("Continuing the game as: ", species.FormattedIdentifier);

        CurrentGame.GameWorld.ReplacePlayerSpeciesWith(species);

        // Find a patch to resume playing in
        var patch = CurrentGame.GameWorld.Map.FindBestPatchForSpecies(species);

        if (patch == null)
        {
            GD.PrintErr("Species selected to continue as has no population in any patch");
            return;
        }

        // Stop being in game over state
        BaseHUD.HidePatchExtinctionBox();
        gameOver = false;
        playerExtinctInCurrentPatch = false;

        // Allow derived classes to customize things
        OnGameContinuedAsSpecies(species, patch);

        // Switch to the new patch to continue playing
        if (CurrentGame.GameWorld.Map.CurrentPatch != patch)
        {
            CurrentGame.GameWorld.Map.CurrentPatch = patch;
            UpdatePatchSettings();
        }

        // And spawn the player to continue
        SpawnPlayer();

        // Auto-evo needs to be re-done as player species is different
        CurrentGame.GameWorld.ResetAutoEvoRun();

        // Stop the extinction music
        StartMusic();
    }

    protected override void SetupStage()
    {
        EnsureWorldSimulationIsCreated();

        base.SetupStage();
    }

    protected void EnsureWorldSimulationIsCreated()
    {
        // When loading a save the world simulation is loaded from the save, otherwise it needs to be created
        if (WorldSimulation == null!)
        {
            WorldSimulation = new TSimulation();
            WorldSimulation.ResolveNodeReferences();
        }
    }

    protected override void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        BaseHUD.OnEnterStageTransition(longDuration, returnFromEditor);
    }

    protected virtual void UpdatePatchSettings(bool promptPatchNameChange = true)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Increases the population by the constant for the player reproducing
    /// </summary>
    protected void GiveReproductionPopulationBonus()
    {
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulationInCurrentPatch(playerSpecies,
            Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT,
            Localization.Translate("PLAYER_REPRODUCED"),
            false, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT);
    }

    /// <summary>
    ///   Handles respawning the player and checking for Extinction
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
        GameWorld.AlterSpeciesPopulationInCurrentPatch(GameWorld.PlayerSpecies,
            Constants.PLAYER_DEATH_POPULATION_LOSS_CONSTANT,
            Localization.Translate("PLAYER_DIED"),
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
    protected virtual void SpawnPlayer()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

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

    /// <summary>
    ///   Called when the player has picked a new species to play as. Called before the new player is spawned.
    /// </summary>
    /// <param name="newPlayerSpecies">New player species</param>
    /// <param name="inPatch">Patch in which the game is continuing (the patch hasn't been switched to yet)</param>
    protected virtual void OnGameContinuedAsSpecies(Species newPlayerSpecies, Patch inPatch)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Guard against uninitialized stage object created temporarily in Godot from crashing
            if (WorldSimulation != null!)
                WorldSimulation.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PatchExtinctionResolved()
    {
        playerExtinctInCurrentPatch = false;

        // Decrease the population by the constant for the player dying out in a patch
        // If the player does not have sufficient population in the new patch then the population drops to 0 and
        // they have to select a new patch if they die again.
        GameWorld.AlterSpeciesPopulationInCurrentPatch(GameWorld.PlayerSpecies,
            Constants.PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_CONSTANT,
            Localization.Translate("EXTINCT_IN_PATCH"),
            true, Constants.PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_COEFFICIENT
            / GameWorld.WorldSettings.PlayerDeathPopulationPenalty);

        // Do not grant the player population even if the global population is 0,
        // they will go extinct the next time they die

        BaseHUD.HidePatchExtinctionBox();
    }
}
