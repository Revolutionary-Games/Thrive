﻿using System;
using System.Collections.Generic;
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
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem dummySpawner = null!;

    /// <summary>
    ///   Used to detect when the player automatically advances stages in the editor (awakening is explicit with a
    ///   button as it should be only used after moving to land)
    /// </summary>
    [JsonProperty]
    private MulticellularSpeciesType previousPlayerStage;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularCamera PlayerCamera { get; private set; } = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularHUD HUD { get; private set; } = null!;

    // TODO: create a multicellular equivalent class
    [JsonIgnore]
    public PlayerHoverInfo HoverInfo { get; private set; } = null!;

    protected override IStageHUD BaseHUD => HUD;

    private LocalizedString CurrentPatchName =>
        GameWorld.Map.CurrentPatch?.Name ?? throw new InvalidOperationException("no current patch");

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        HUD.Init(this);

        // HoverInfo.Init(Camera, Clouds);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<MulticellularHUD>("MulticellularHUD");
        HoverInfo = GetNode<PlayerHoverInfo>("PlayerLookingAtInfo");

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
                    "You are now aware. This prototype has nothing extra yet, please move to the awakening stage",
                    DisplayDuration.Long);
            }
            else if (previousPlayerStage == MulticellularSpeciesType.Awakened)
            {
                // TODO: something
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
                Size = new Vector2(200, 200),
                Material = new SpatialMaterial
                {
                    // Not good style but this is like this so I could just quickly copy-paste from the Godot editor
                    AlbedoColor = new Color("#321d09"),
                },
            },
        });

        rootOfDynamicallySpawned.AddChild(ground);

        // A not super familiar (different than underwater) rock strewn around for reference
        var rockShape = GD.Load<Shape>("res://assets/models/Iron4.shape");
        var rockScene = GD.Load<PackedScene>("res://assets/models/Iron4.tscn");
        var rockMaterial = new PhysicsMaterial
        {
            Friction = 1,
            Bounce = 0.3f,
            Absorbent = false,
            Rough = false,
        };

        foreach (var position in new[]
                 {
                     new Vector3(10, 0, 5),
                     new Vector3(15, 0, 5),
                     new Vector3(10, 0, 8),
                     new Vector3(-3, 0, 5),
                     new Vector3(-8, 0, 6),
                     new Vector3(18, 0, 11),
                 })
        {
            var rock = new RigidBody
            {
                PhysicsMaterialOverride = rockMaterial,
            };

            rock.AddChild(new CollisionShape
            {
                Shape = rockShape,
            });

            rock.AddChild(rockScene.Instance());

            rootOfDynamicallySpawned.AddChild(rock);

            rock.Translation = new Vector3(position.x, 0.1f, position.z);
        }

        // Modify player state for being on land
        Player.MovementMode = MovementMode.Walking;

        if (Player.Translation.y <= 0)
        {
            Player.Translation = new Vector3(Player.Translation.x, 0.1f, Player.Translation.z);
        }

        // Fade back in after the "teleport"
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.3f, null, false);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // if (!IsLoadedFromSave)
        //     spawner.Init();

        // TODO: implement
        if (!IsLoadedFromSave)
        {
            // If this is a new game (first time entering the stage), start the camera in top down view
            // as a learning tool
            if (!CurrentGame!.IsBoolSet("played_multicellular"))
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
}
