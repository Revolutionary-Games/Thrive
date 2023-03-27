using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class for managing the late multicellular stage
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularStage.tscn")]
[DeserializedCallbackTarget]
[UseThriveSerializer]
public class MulticellularStage : StageBase<MulticellularCreature>
{
    [Export]
    public NodePath? InteractableSystemPath;

    [Export]
    public NodePath InteractionPopupPath = null!;

    [Export]
    public NodePath ProgressBarSystemPath = null!;

    [Export]
    public NodePath SelectBuildingPopupPath = null!;

    [Export]
    public NodePath WorldEnvironmentNodePath = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem dummySpawner = null!;

#pragma warning disable CA2213
    private InteractableSystem interactableSystem = null!;
    private InteractablePopup interactionPopup = null!;

    private ProgressBarSystem progressBarSystem = null!;

    private SelectBuildingPopup selectBuildingPopup = null!;

    private WorldEnvironment worldEnvironmentNode = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to detect when the player automatically advances stages in the editor (awakening is explicit with a
    ///   button as it should be only used after moving to land)
    /// </summary>
    [JsonProperty]
    private MulticellularSpeciesType previousPlayerStage;

    [JsonProperty]
    private float moveToSocietyTimer;

    [JsonProperty]
    private Transform societyCameraAnimationStart = Transform.Identity;

    [JsonProperty]
    private Transform societyCameraAnimationEnd = Transform.Identity;

    [JsonProperty]
    private bool movingToSocietyStage;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularCamera PlayerCamera { get; private set; } = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularHUD HUD { get; private set; } = null!;

    // TODO: create a multicellular equivalent class
    [JsonIgnore]
    public PlayerInspectInfo HoverInfo { get; private set; } = null!;

    protected override IStageHUD BaseHUD => HUD;

    private LocalizedString CurrentPatchName =>
        GameWorld.Map.CurrentPatch?.Name ?? throw new InvalidOperationException("no current patch");

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        HUD.Init(this);

        // HoverInfo.Init(Camera, Clouds);

        interactableSystem.Init(PlayerCamera.CameraNode, rootOfDynamicallySpawned);
        interactionPopup.OnInteractionSelectedHandler += ForwardInteractionSelectionToPlayer;

        progressBarSystem.Init(PlayerCamera.CameraNode, rootOfDynamicallySpawned);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<MulticellularHUD>("MulticellularHUD");
        HoverInfo = GetNode<PlayerInspectInfo>("PlayerLookingAtInfo");

        interactableSystem = GetNode<InteractableSystem>(InteractableSystemPath);
        interactionPopup = GetNode<InteractablePopup>(InteractionPopupPath);
        progressBarSystem = GetNode<ProgressBarSystem>(ProgressBarSystemPath);
        selectBuildingPopup = GetNode<SelectBuildingPopup>(SelectBuildingPopupPath);
        worldEnvironmentNode = GetNode<WorldEnvironment>(WorldEnvironmentNodePath);

        // TODO: implement late multicellular specific look at info, for now it's disabled by removing it
        HoverInfo.Free();

        PlayerCamera = world.GetNode<MulticellularCamera>("PlayerCamera");

        // These need to be created here as well for child property save load to work
        // TODO: systems

        // We don't actually spawn anything currently, and anyway will want a different spawn system for late
        // multicellular
        dummySpawner = new SpawnSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (gameOver)
            return;

        if (playerExtinctInCurrentPatch)
            return;

        if (Player != null)
        {
            var playerPosition = Player.GlobalTranslation;

            if (Player.Species.MulticellularType == MulticellularSpeciesType.Awakened)
            {
                // TODO: player interaction reach modifier from the species
                interactableSystem.UpdatePlayerPosition(playerPosition, 0);
                interactableSystem.SetActive(true);
            }
            else
            {
                interactableSystem.SetActive(false);
            }

            progressBarSystem.UpdatePlayerPosition(playerPosition);
        }

        if (movingToSocietyStage)
        {
            moveToSocietyTimer += delta;
            float interpolationProgress = moveToSocietyTimer / Constants.SOCIETY_STAGE_ENTER_ANIMATION_DURATION;

            if (interpolationProgress >= 1)
            {
                interpolationProgress = 1;

                // Fade to black and queue the transition
                HUD.EnsureGameIsUnpausedForEditor();

                GD.Print("Starting fade out to society stage");
                TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.5f, SwitchToSocietyScene, false);
                MovingToEditor = true;
                movingToSocietyStage = false;
            }

            // TODO: switch to some other animation type like quintic once that is usable without a tween node
            PlayerCamera.GlobalTransform =
                societyCameraAnimationStart.InterpolateWith(societyCameraAnimationEnd, interpolationProgress);
        }

        // TODO: notify metrics
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("LateMulticellularStage");
    }

    public override void OnFinishLoading(Save save)
    {
        OnFinishLoading();
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewLateMulticellularGame(new WorldGenerationSettings());

        UpdatePatchSettings();

        base.StartNewGame();
    }

    // TODO: different pause key as space will be for jumping
    // [RunOnKeyDown("g_pause")]
    public void PauseKeyPressed()
    {
        // Check nothing else has keyboard focus and pause the game
        if (HUD.GetFocusOwner() == null)
        {
            HUD.PauseButtonPressed(!HUD.Paused);
        }
    }

    public override void MoveToEditor()
    {
        if (Player?.Dead != false)
        {
            GD.PrintErr("Player object disappeared or died while transitioning to the editor");
            return;
        }

        if (CurrentGame == null)
            throw new InvalidOperationException("Stage has no current game");

        var scene = SceneManager.Instance.LoadScene(MainGameState.LateMulticellularEditor);

        Node sceneInstance = scene.Instance();
        var editor = (LateMulticellularEditor)sceneInstance;

        editor.CurrentGame = CurrentGame;
        editor.ReturnToStage = this;

        GiveReproductionPopulationBonus();

        // We don't free this here as the editor will return to this scene
        if (SceneManager.Instance.SwitchToScene(sceneInstance, true) != this)
        {
            throw new Exception("failed to keep the current scene root");
        }

        MovingToEditor = false;
    }

    public override void OnReturnFromEditor()
    {
        UpdatePatchSettings();

        base.OnReturnFromEditor();

        // TODO:
        // // Spawn free food if difficulty settings call for it
        // if (GameWorld.WorldSettings.FreeGlucoseCloud)
        // {
        // }

        // Spawn another cell from the player species
        // This is done first to ensure that the player colony is still intact for spawn separation calculation
        var parent = Player!.SpawnOffspring();
        parent.BecomeFullyGrown();

        // Update the player's creature
        Player.ApplySpecies(Player.Species);

        // Reset all growth progress of the player
        Player.ResetGrowth();

        if (!CurrentGame!.TutorialState.Enabled)
        {
            // tutorialGUI.EventReceiver?.OnTutorialDisabled();
        }

        // Update state transition triggers
        if (Player.Species.MulticellularType != previousPlayerStage)
        {
            previousPlayerStage = Player.Species.MulticellularType;

            if (previousPlayerStage == MulticellularSpeciesType.Aware)
            {
                // Intentionally not translatable as a placeholder prototype text
                HUD.HUDMessages.ShowMessage(
                    "You are now aware. This prototype has nothing extra yet, please move to the Awakening Stage.",
                    DisplayDuration.ExtraLong);
            }
        }
    }

    public override void OnSuicide()
    {
        Player?.Damage(9999.0f, "suicide");
    }

    public void RotateCamera(float yawMovement, float pitchMovement)
    {
        PlayerCamera.XRotation += pitchMovement;
        PlayerCamera.YRotation += yawMovement;
    }

    /// <summary>
    ///   Temporary land part of the prototype, called from the HUD
    /// </summary>
    public void TeleportToLand()
    {
        if (Player == null)
        {
            GD.PrintErr("Player has disappeared");
            return;
        }

        // Despawn everything except the player
        foreach (Node child in rootOfDynamicallySpawned.GetChildren())
        {
            if (child != Player)
                child.QueueFree();
        }

        // And setup the land "environment"

        // Clear the underwater background
        // TODO: above water panorama backgrounds
        worldEnvironmentNode.Environment = null;

        // Ground plane
        var ground = new StaticBody
        {
            PhysicsMaterialOverride = new PhysicsMaterial
            {
                Friction = 1,
                Bounce = 0.1f,
                Absorbent = true,
                Rough = true,
            },
        };

        ground.AddChild(new CollisionShape
        {
            Shape = new PlaneShape
            {
                Plane = new Plane(new Vector3(0, 1, 0), 0),
            },
        });

        ground.AddChild(new MeshInstance
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(400, 400),
                Material = new SpatialMaterial
                {
                    AlbedoTexture = GD.Load<Texture>("res://assets/textures/environment/Terrain_01_Albedo.png"),
                    NormalEnabled = true,
                    NormalTexture = GD.Load<Texture>("res://assets/textures/environment/Terrain_01_Normals.png"),
                    Uv1Scale = new Vector3(42, 42, 42),
                },
            },
        });

        rootOfDynamicallySpawned.AddChild(ground);

        // A not super familiar (different than underwater) rock strewn around for reference
        var rockResource = SimulationParameters.Instance.GetWorldResource("rock");
        var resourceScene = SpawnHelpers.LoadResourceEntityScene();

        foreach (var position in new[]
                 {
                     new Vector3(10, 0, 5),
                     new Vector3(15, 0, 5),
                     new Vector3(10, 0, 8),
                     new Vector3(-3, 0, 5),
                     new Vector3(-8, 0, 6),
                     new Vector3(18, 0, 11),
                     new Vector3(38, 0, 11),
                     new Vector3(-15, 0, 10),
                     new Vector3(-15, 0, -15),
                     new Vector3(-25, 0, -15),
                     new Vector3(-35, 0, -15),
                     new Vector3(25, 0, -5),
                     new Vector3(29, 0, -5),
                     new Vector3(32, 0, -5),
                     new Vector3(35, 0, 5),
                 })
        {
            // But create it as a resource entity so that it can be interacted with
            SpawnHelpers.SpawnResourceEntity(rockResource, new Transform(Basis.Identity, position),
                rootOfDynamicallySpawned, resourceScene, true);
        }

        // Placeholder trees
        var treeScene = GD.Load<PackedScene>("res://assets/models/PlaceholderTree.tscn");

        foreach (var position in new[]
                 {
                     new Vector3(15, 0, 9),
                     new Vector3(25, 0, 35),
                     new Vector3(50, 0, 10),
                     new Vector3(-30, 0, 5),
                     new Vector3(18, 0, -20),
                     new Vector3(-48, 0, 27),
                 })
        {
            // TODO: proper interactable plants, this is a temporary manually created placeholder tree
            var tree = treeScene.Instance<PlaceholderTree>();

            rootOfDynamicallySpawned.AddChild(tree);
            tree.GlobalTransform =
                new Transform(new Basis(new Quat(new Vector3(0, 1, 0), Mathf.Pi * random.NextFloat())), position);

            tree.AddToGroup(Constants.INTERACTABLE_GROUP);
        }

        // Modify player state for being on land
        Player.MovementMode = MovementMode.Walking;

        if (Player.Translation.y <= 0)
        {
            Player.Translation = new Vector3(Player.Translation.x, 0.1f, Player.Translation.z);
        }

        // Modify the player species to be on land
        Player.Species.ReproductionLocation = ReproductionLocation.Land;

        // Fade back in after the "teleport"
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.3f, null, false);
    }

    public void MoveToAwakeningStage()
    {
        if (Player == null)
            return;

        GD.Print("Moving player to awakening stage prototype");

        Player.Species.MovePlayerToAwakenedStatus();

        // TODO: implement an "inspect" action for inspecting world objects that can unlock primitive
        // technologies
        // For now just add some default unlocks for the prototype

        CurrentGame!.TechWeb.UnlockTechnology(SimulationParameters.Instance.GetTechnology("simpleStoneTools"));

        // TODO: proper society center unlock conditions
        CurrentGame.TechWeb.UnlockTechnology(SimulationParameters.Instance.GetTechnology("societyCenter"));

        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "You are now in the Awakening Stage prototype. You can now interact with more world objects. " +
            "Interact with tool parts to advance.", DisplayDuration.ExtraLong);
    }

    public void AttemptPlayerWorldInteraction()
    {
        // TODO: we might in the future have somethings that an aware creature can interact with
        if (Player == null || Player.Species.MulticellularType != MulticellularSpeciesType.Awakened)
            return;

        var target = interactableSystem.GetInteractionTarget();

        if (target == null)
        {
            // Did not find anything for the player to interact with
            HUD.HUDMessages.ShowMessage(TranslationServer.Translate("NOTHING_TO_INTERACT_WITH"), DisplayDuration.Short);
            return;
        }

        // Show interaction context menu for the player to do something with the target
        interactionPopup.ShowForInteractable(target, Player.CalculatePossibleActions(target));

        // TODO: somehow refresh the inventory screen if it is open and the player decided to do a pick up action
    }

    public void PerformBuildOrOpenMenu()
    {
        if (Player == null || Player.Species.MulticellularType != MulticellularSpeciesType.Awakened)
            return;

        if (Player.IsPlacingStructure)
        {
            Player.AttemptStructurePlace();
            return;
        }

        selectBuildingPopup.OpenWithStructures(CurrentGame!.TechWeb.GetAvailableStructures(), Player, Player);

        // TODO: when a structure is being placed, should we have some kind of indicator on screen what to press to
        // cancel?
    }

    public bool CancelBuildingPlaceIfInProgress()
    {
        if (Player?.IsPlacingStructure != true)
        {
            return false;
        }

        Player.CancelStructurePlacing();
        return true;
    }

    public bool TogglePlayerInventory()
    {
        if (Player == null)
            return false;

        if (HUD.IsInventoryOpen)
        {
            HUD.CloseInventory();
            return true;
        }

        try
        {
            // Refresh the items on the ground near the player to show in the inventory screen
            var groundObjects = interactableSystem.GetAllNearbyObjects();

            // Filter to only carriable objects to not let the player to pick up trees and stuff
            HUD.OpenInventory(Player, groundObjects.Where(o => o.CanBeCarried));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Problem trying to show inventory: {e}");
            return false;
        }

        return true;
    }

    public void OnSocietyFounded(PlacedStructure societyCenter)
    {
        if (Player == null || movingToSocietyStage || MovingToEditor)
            return;

        GD.Print("Move to society stage triggered");
        HUD.HUDMessages.ShowMessage(TranslationServer.Translate("MOVING_TO_SOCIETY_STAGE"), DisplayDuration.Long);

        // Unset the player to disallow doing this multiple times in a row and to disable the player
        var moveCreatureToSocietyCenter = Player;
        Player = null;

        // TODO: actual collisions and moving to the door instead of this
        // TODO: even this doesn't work because the structure doesn't have the collision as a root node
        // moveCreatureToSocietyCenter.AddCollisionExceptionWith(societyCenter);

        var creatureToCenterVector =
            moveCreatureToSocietyCenter.GlobalTranslation - moveCreatureToSocietyCenter.GlobalTranslation;
        creatureToCenterVector.y = 0;
        creatureToCenterVector = creatureToCenterVector.Normalized();

        moveCreatureToSocietyCenter.MovementDirection =
            moveCreatureToSocietyCenter.Transform.basis.XformInv(creatureToCenterVector);

        // TODO: despawn moveCreatureToSocietyCenter once it reaches inside the society center

        // Start the transition to the next stage and a camera animation
        var cameraFocusPoint = societyCenter.GlobalTranslation;
        cameraFocusPoint += societyCenter.RotatedExtraInteractionOffset() ?? Vector3.Zero;

        moveToSocietyTimer = 0;
        societyCameraAnimationStart = PlayerCamera.GlobalTransform;
        societyCameraAnimationEnd = StrategicCameraHelpers.CalculateCameraPosition(cameraFocusPoint, 1);
        movingToSocietyStage = true;

        // Prevent inputs to not allow messing with the animation
        PlayerCamera.AllowPlayerInput = false;
        PlayerCamera.FollowedNode = null;
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // if (!IsLoadedFromSave)
        //     spawner.Init();

        CurrentGame!.TechWeb.OnTechnologyUnlockedHandler += ShowTechnologyUnlockMessage;

        // TODO: implement
        if (!IsLoadedFromSave)
        {
            // If this is a new game (first time entering the stage), start the camera in top down view
            // as a learning tool
            if (!CurrentGame.IsBoolSet("played_multicellular"))
            {
                CurrentGame.SetBool("played_multicellular", true);

                // TODO: change the view
            }

            // TODO: remove
            // Spawn a chunk to give the player some navigation reference
            var mesh = new ChunkConfiguration.ChunkScene
            {
                ScenePath = "res://assets/models/Iron5.tscn",
                ConvexShapePath = "res://assets/models/Iron5.shape",
            };
            mesh.LoadScene();
            SpawnHelpers.SpawnChunk(new ChunkConfiguration
                {
                    Name = "test",
                    Size = 10000,
                    Radius = 10,
                    Mass = 100,
                    ChunkScale = 1,
                    Meshes = new List<ChunkConfiguration.ChunkScene> { mesh },
                }, new Vector3(3, 0, -15), rootOfDynamicallySpawned, SpawnHelpers.LoadChunkScene(),
                random);
        }

        // patchManager.CurrentGame = CurrentGame;

        if (IsLoadedFromSave)
        {
            UpdatePatchSettings();
        }
    }

    protected override void OnGameStarted()
    {
        // patchManager.CurrentGame = CurrentGame;

        lightCycle.ApplyWorldSettings(GameWorld.WorldSettings);
        UpdatePatchSettings();

        SpawnPlayer();
    }

    protected override void UpdatePatchSettings(bool promptPatchNameChange = true)
    {
        // TODO: would be nice to skip this if we are loading a save made in the editor as this gets called twice when
        // going back to the stage
        // if (patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch!))
        // {
        if (promptPatchNameChange)
            HUD.ShowPatchName(CurrentPatchName.ToString());

        // }

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch!.Biome);

        // TODO: load background graphics
    }

    protected override void SpawnPlayer()
    {
        if (HasPlayer)
            return;

        Player = SpawnHelpers.SpawnCreature(GameWorld.PlayerSpecies, new Vector3(0, 0, 0),
            rootOfDynamicallySpawned, SpawnHelpers.LoadMulticellularScene(), false, dummySpawner, CurrentGame!);
        Player.AddToGroup(Constants.PLAYER_GROUP);

        Player.OnDeath = OnPlayerDied;

        Player.OnReproductionStatus = OnPlayerReproductionStatusChanged;

        Player.RequestCraftingInterfaceFor = OnOpenCraftingInterfaceFor;

        PlayerCamera.FollowedNode = Player;

        // spawner.DespawnAll();

        if (spawnedPlayer)
        {
            // Random location on respawn
            // Player.Translation = new Vector3(
            //     random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
            //     random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));

            // spawner.ClearSpawnCoordinates();
        }

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;
    }

    protected override void AutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InteractableSystemPath != null)
            {
                InteractableSystemPath.Dispose();
                InteractionPopupPath.Dispose();
                ProgressBarSystemPath.Dispose();
                SelectBuildingPopupPath.Dispose();
                WorldEnvironmentNodePath.Dispose();
            }

            interactionPopup.OnInteractionSelectedHandler -= ForwardInteractionSelectionToPlayer;

            if (CurrentGame != null)
                CurrentGame.TechWeb.OnTechnologyUnlockedHandler -= ShowTechnologyUnlockMessage;
        }

        base.Dispose(disposing);
    }

    private void SaveGame(string name)
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    private void OnFinishLoading()
    {
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerDied(MulticellularCreature player)
    {
        HandlePlayerDeath();

        PlayerCamera.FollowedNode = null;
        Player = null;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(MulticellularCreature player, bool ready)
    {
        OnCanEditStatusChanged(ready);
    }

    private void ForwardInteractionSelectionToPlayer(IInteractableEntity entity, InteractionType interactionType)
    {
        if (Player == null)
            return;

        if (!Player.AttemptInteraction(entity, interactionType))
            GD.Print("Player couldn't perform the selected action");
    }

    [DeserializedCallbackAllowed]
    private void OnOpenCraftingInterfaceFor(MulticellularCreature player, IInteractableEntity target)
    {
        if (!TogglePlayerInventory())
        {
            GD.Print("Couldn't open player inventory to then select a craftable resource");
            return;
        }

        HUD.SelectItemForCrafting(target);
    }

    private void ShowTechnologyUnlockMessage(Technology technology)
    {
        HUD.HUDMessages.ShowMessage(
            TranslationServer.Translate("TECHNOLOGY_UNLOCKED_NOTICE").FormatSafe(technology.Name),
            DisplayDuration.Long);
    }

    private void SwitchToSocietyScene()
    {
        SceneManager.Instance.SwitchToScene(MainGameState.SocietyStage);
    }
}
