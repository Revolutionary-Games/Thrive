using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The scene where the player species becomes ascended before entering the actual gameplay of the ascension stage
/// </summary>
public partial class AscensionCeremony : Node
{
    [Export]
    public float SpeciesWalkSpeed = 3.0f;

    [Export]
    public float ScreenFadeDuration = 5;

    [Export]
    public NodePath? GateWalkerSpawnPointPath;

    [Export]
    public Godot.Collections.Array<NodePath> ObserverSpawnPointPaths = new();

    [Export]
    public NodePath RootOfDynamicallySpawnedPath = null!;

    [Export]
    public NodePath RampStartPointPath = null!;

    [Export]
    public NodePath RampEndPointPath = null!;

    [Export]
    public NodePath AscensionPointPath = null!;

    [Export]
    public NodePath CreditsDisplayPath = null!;

    [Export]
    public NodePath CreditsSkipInfoContainerPath = null!;

    [Export]
    public NodePath CreditsSkipPromptPath = null!;

    [Export]
    public NodePath CustomScreenBlankerPath = null!;

    [Export]
    public NodePath WorldCameraToDisablePath = null!;

    private readonly List<Node3D> observerSpawnPoints = new();

#pragma warning disable CA2213
    private Node3D gateWalkerSpawn = null!;

    private Node rootOfDynamicallySpawned = null!;

    private Node3D rampStartPoint = null!;
    private Node3D rampEndPoint = null!;
    private Node3D ascensionPoint = null!;

    private CreditsScroll creditsDisplay = null!;
    private Control creditsSkipInfoContainer = null!;
    private HoldKeyPrompt creditsSkipPrompt = null!;

    private ColorRect customScreenBlanker = null!;

    private Camera3D worldCameraToDisable = null!;

    private MulticellularCreature? gateWalker;
#pragma warning restore CA2213

    private double stateTimer;

    private State currentState;

    private bool returningToScene;

    private enum State
    {
        WalkingToRamp,
        ClimbingRamp,
        WalkingToAscension,
        Ascending,
        FadingOut,
        Credits,
        Ended,
    }

    public GameProperties? CurrentGame { get; set; }
    public SpaceStage? ReturnToScene { get; set; }

    public override void _Ready()
    {
        gateWalkerSpawn = GetNode<Node3D>(GateWalkerSpawnPointPath);
        foreach (var spawnPointPath in ObserverSpawnPointPaths)
        {
            observerSpawnPoints.Add(GetNode<Node3D>(spawnPointPath));
        }

        rootOfDynamicallySpawned = GetNode<Node>(RootOfDynamicallySpawnedPath);

        rampStartPoint = GetNode<Node3D>(RampStartPointPath);
        rampEndPoint = GetNode<Node3D>(RampEndPointPath);
        ascensionPoint = GetNode<Node3D>(AscensionPointPath);

        creditsDisplay = GetNode<CreditsScroll>(CreditsDisplayPath);
        creditsSkipInfoContainer = GetNode<Control>(CreditsSkipInfoContainerPath);
        creditsSkipPrompt = GetNode<HoldKeyPrompt>(CreditsSkipPromptPath);

        customScreenBlanker = GetNode<ColorRect>(CustomScreenBlankerPath);

        worldCameraToDisable = GetNode<Camera3D>(WorldCameraToDisablePath);

        // Setup a new game if not already started
        if (CurrentGame == null)
        {
            GD.Print("Starting a new ascension game");
            CurrentGame = GameProperties.StartAscensionStageGame(new WorldGenerationSettings());
        }

        creditsDisplay.Visible = false;
        creditsSkipInfoContainer.Visible = false;

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

    public override void _Process(double delta)
    {
        if (currentState is State.FadingOut or State.Credits)
        {
            // Show the info on skipping if it is pressed
            creditsSkipInfoContainer.Visible = creditsSkipPrompt.HoldProgress > 0;

            // TODO: show the skip button for a second if the player is pressing random buttons
        }
        else
        {
            creditsSkipInfoContainer.Visible = false;
        }

        switch (currentState)
        {
            case State.WalkingToRamp:
            {
                if (WalkTowards(rampStartPoint.GlobalPosition, delta))
                    currentState = State.ClimbingRamp;

                break;
            }

            case State.ClimbingRamp:
            {
                if (WalkTowards(rampEndPoint.GlobalPosition, delta))
                    currentState = State.WalkingToAscension;

                break;
            }

            case State.WalkingToAscension:
            {
                if (WalkTowards(ascensionPoint.GlobalPosition, delta))
                {
                    // Stop the music a bit before switching to the credits theme
                    Jukebox.Instance.Stop(true);

                    // TODO: play a sound effect for the ascension
                    currentState = State.Ascending;
                    stateTimer = 0;
                }

                break;
            }

            case State.Ascending:
            {
                // TODO: some kind of actual ascending animation
                if (gateWalker != null)
                {
                    gateWalker.GlobalPosition += new Vector3(0, 300 * (float)delta, 0);
                }

                stateTimer += delta;

                if (stateTimer > 3)
                {
                    // Start already playing the credits to overlap with the normal screen a bit
                    creditsDisplay.Visible = true;
                    creditsDisplay.Restart();

                    // Enable the skip button
                    creditsSkipPrompt.ShowPress = true;

                    currentState = State.FadingOut;
                    stateTimer = 0;
                }

                break;
            }

            case State.FadingOut:
            {
                // To make the credits stay on top, we use a custom screen blanking animation
                stateTimer += delta;

                customScreenBlanker.Visible = true;

                var alpha = Math.Min(1, (float)(stateTimer / ScreenFadeDuration));
                customScreenBlanker.Color = new Color(0, 0, 0, alpha);

                if (stateTimer > ScreenFadeDuration)
                {
                    OnPlayCreditsMusic();

                    // Fade complete, stop rendering the 3D scene
                    worldCameraToDisable.Current = false;

                    Invoke.Instance.Queue(() => rootOfDynamicallySpawned.QueueFreeChildren());
                }

                break;
            }

            case State.Credits:
                break;
            case State.Ended:
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
                CreditsDisplayPath.Dispose();
                CreditsSkipInfoContainerPath.Dispose();
                CreditsSkipPromptPath.Dispose();
                CustomScreenBlankerPath.Dispose();
                WorldCameraToDisablePath.Dispose();
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

        gateWalker = SpawnHelpers.SpawnCreature(playerSpecies, gateWalkerSpawn.GlobalPosition,
            rootOfDynamicallySpawned, actorScene, false, dummySpawner, CurrentGame);

        // We control the walker through code
        gateWalker.Mode = RigidBody3D.ModeEnum.Kinematic;

        gateWalker.LookAt(rampStartPoint.GlobalPosition, Vector3.Up);

        // TODO: could pick a rotating set of species if the player empire is composed of multiple species
        foreach (var spawnPoint in observerSpawnPoints)
        {
            SpawnObserver(spawnPoint.GlobalPosition, dummySpawner, actorScene, playerSpecies);
        }
    }

    private void SpawnObserver(Vector3 location, DummySpawnSystem dummySpawner, PackedScene observerScene,
        Species observerSpecies)
    {
        var observer = SpawnHelpers.SpawnCreature(observerSpecies, location,
            rootOfDynamicallySpawned, observerScene, false, dummySpawner, CurrentGame!);

        var lookAt = ascensionPoint.GlobalPosition;

        // Need to look at the gate without pitching up or down
        lookAt.Y = 0;

        // Make the observer not move and look at the gate
        observer.Mode = RigidBody3D.ModeEnum.Kinematic;
        observer.LookAt(lookAt, Vector3.Up);
    }

    private bool WalkTowards(Vector3 point, double delta)
    {
        if (gateWalker == null)
        {
            GD.PrintErr("Gate walker has disappeared");
            return true;
        }

        var current = gateWalker.GlobalPosition;

        var direction = point - current;

        if (direction.Length() < SpeciesWalkSpeed * delta)
        {
            gateWalker.GlobalPosition = point;
            return true;
        }

        gateWalker.GlobalPosition += direction.Normalized() * SpeciesWalkSpeed * (float)delta;
        return false;
    }

    private void OnCeremonyEnded()
    {
        if (currentState == State.Ended)
            return;

        currentState = State.Ended;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 1.0f, SwitchToSpaceScene, false);
    }

    private void OnPlayCreditsMusic()
    {
        currentState = State.Credits;
        Jukebox.Instance.PlayCategory("Credits");
    }

    private void OnSkipCredits()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GD.Print("Skipping credits by user request");

        OnCeremonyEnded();
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
