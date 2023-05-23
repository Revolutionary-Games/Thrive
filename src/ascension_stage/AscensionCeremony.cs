using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The scene where the player species becomes ascended before entering the actual gameplay of the ascension stage
/// </summary>
public class AscensionCeremony : Node
{
    [Export]
    public float SpeciesWalkSpeed = 3.0f;

    [Export]
    public NodePath? GateWalkerSpawnPointPath;

    [Export]
    public List<NodePath> ObserverSpawnPointPaths = new();

    [Export]
    public NodePath RootOfDynamicallySpawnedPath = null!;

    [Export]
    public NodePath RampStartPointPath = null!;

    [Export]
    public NodePath RampEndPointPath = null!;

    [Export]
    public NodePath AscensionPointPath = null!;

    private readonly List<Spatial> observerSpawnPoints = new();

#pragma warning disable CA2213
    private Spatial gateWalkerSpawn = null!;

    private Node rootOfDynamicallySpawned = null!;

    private Spatial rampStartPoint = null!;
    private Spatial rampEndPoint = null!;
    private Spatial ascensionPoint = null!;

    private MulticellularCreature? gateWalker;
#pragma warning restore CA2213

    private float elapsed;

    private State currentState;

    private bool returningToScene;

    private enum State
    {
        WalkingToRamp,
        ClimbingRamp,
        WalkingToAscension,
        FadingOut,
    }

    public GameProperties? CurrentGame { get; set; }
    public SpaceStage? ReturnToScene { get; set; }

    public override void _Ready()
    {
        gateWalkerSpawn = GetNode<Spatial>(GateWalkerSpawnPointPath);
        foreach (var spawnPointPath in ObserverSpawnPointPaths)
        {
            observerSpawnPoints.Add(GetNode<Spatial>(spawnPointPath));
        }

        rootOfDynamicallySpawned = GetNode<Node>(RootOfDynamicallySpawnedPath);

        rampStartPoint = GetNode<Spatial>(RampStartPointPath);
        rampEndPoint = GetNode<Spatial>(RampEndPointPath);
        ascensionPoint = GetNode<Spatial>(AscensionPointPath);

        // Setup a new game if not already started
        if (CurrentGame == null)
        {
            GD.Print("Starting a new ascension game");
            CurrentGame = GameProperties.StartAscensionStageGame(new WorldGenerationSettings());
        }

        SetupSceneActors();

        // Start the fade in
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 1, null, false);

        // TODO: should this force unpause to really ensure the player can't get stuck here?

        // TODO: ascension room music?
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Delete the scene if we aren't returning to it
        if (!returningToScene)
            ReturnToScene?.QueueFree();
    }

    public override void _Process(float delta)
    {
        elapsed += delta;

        // To prevent getting stuck in the scene in case it doesn't work
        if (elapsed > Constants.ASCENSION_CEREMONY_MAX_DURATION)
        {
            OnCeremonyEnded();
        }

        switch (currentState)
        {
            case State.WalkingToRamp:
                if (WalkTowards(rampStartPoint.GlobalTranslation, delta))
                {
                    currentState = State.ClimbingRamp;
                }

                break;
            case State.ClimbingRamp:
                if (WalkTowards(rampEndPoint.GlobalTranslation, delta))
                {
                    currentState = State.WalkingToAscension;
                }

                break;
            case State.WalkingToAscension:
                if (WalkTowards(ascensionPoint.GlobalTranslation, delta))
                {
                    // TODO: play a sound effect?
                    OnCeremonyEnded();
                }

                break;
            case State.FadingOut:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (GateWalkerSpawnPointPath != null)
            {
                GateWalkerSpawnPointPath.Dispose();

                RootOfDynamicallySpawnedPath.Dispose();
                RampStartPointPath.Dispose();
                RampEndPointPath.Dispose();
                AscensionPointPath.Dispose();
            }

            foreach (var spawnPointPath in ObserverSpawnPointPaths)
                spawnPointPath.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Creates the player creatures that are attending the ceremony
    /// </summary>
    private void SetupSceneActors()
    {
        var actorScene = SpawnHelpers.LoadMulticellularScene();
        var dummySpawner = new DummySpawnSystem();

        var playerSpecies = CurrentGame!.GameWorld.PlayerSpecies;

        gateWalker = SpawnHelpers.SpawnCreature(playerSpecies, gateWalkerSpawn.GlobalTranslation,
            rootOfDynamicallySpawned, actorScene, false, dummySpawner, CurrentGame);

        // We control the walker through code
        gateWalker.Mode = RigidBody.ModeEnum.Kinematic;

        gateWalker.LookAt(rampStartPoint.GlobalTranslation, Vector3.Up);

        // TODO: could pick a rotating set of species if the player empire is composed of multiple species
        foreach (var spawnPoint in observerSpawnPoints)
        {
            SpawnObserver(spawnPoint.GlobalTranslation, dummySpawner, actorScene, playerSpecies);
        }
    }

    private void SpawnObserver(Vector3 location, DummySpawnSystem dummySpawner, PackedScene observerScene,
        Species observerSpecies)
    {
        var observer = SpawnHelpers.SpawnCreature(observerSpecies, location,
            rootOfDynamicallySpawned, observerScene, false, dummySpawner, CurrentGame!);

        var lookAt = ascensionPoint.GlobalTranslation;

        // Need to look at the gate without pitching up or down
        lookAt.y = 0;

        // Make the observer not move and look at the gate
        observer.Mode = RigidBody.ModeEnum.Kinematic;
        observer.LookAt(lookAt, Vector3.Up);
    }

    private bool WalkTowards(Vector3 point, float delta)
    {
        if (gateWalker == null)
        {
            GD.PrintErr("Gate walker has disappeared");
            return true;
        }

        var current = gateWalker.GlobalTranslation;

        var direction = point - current;

        if (direction.Length() < SpeciesWalkSpeed * delta)
        {
            gateWalker.GlobalTranslation = point;
            return true;
        }

        gateWalker.GlobalTranslation += direction.Normalized() * SpeciesWalkSpeed * delta;
        return false;
    }

    private void OnCeremonyEnded()
    {
        if (currentState == State.FadingOut)
            return;

        currentState = State.FadingOut;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 1, SwitchToSpaceScene, false);
    }

    private void SwitchToSpaceScene()
    {
        GD.Print("Switching to space scene from ascension gate");

        returningToScene = true;

        if (ReturnToScene == null)
        {
            GD.Print("Returning to a new space scene as we didn't have one already");

            var spaceStage = AscensionStageStarter.SetupNewAscendedSpaceStage(CurrentGame);
            SceneManager.Instance.SwitchToScene(spaceStage);

            AscensionStageStarter.PrepareSpaceStageForFreshAscension(spaceStage);
        }
        else
        {
            SceneManager.Instance.SwitchToScene(ReturnToScene);

            // TODO: zooming out from the ascension gate object?

            ReturnToScene.OnReturnedFromAscension();
        }
    }
}
