﻿using System;
using Godot;

/// <summary>
///   Started for ascension stage (basically starts <see cref="MainGameState.SpaceStage"/> with ascended mode enabled)
/// </summary>
public partial class AscensionStageStarter : ComplexStageStarterBase
{
    protected override MainGameState SimplyLoadableGameState => throw new InvalidOperationException("Should be unused");

    public static SpaceStage SetupNewAscendedSpaceStage(GameProperties? currentGame)
    {
        currentGame ??= GameProperties.StartAscensionStageGame(new WorldGenerationSettings());

        var spaceStage = SceneManager.Instance.LoadScene(MainGameState.SpaceStage).Instantiate<SpaceStage>();
        spaceStage.CurrentGame = currentGame;

        return spaceStage;
    }

    public static void PrepareSpaceStageForFreshAscension(SpaceStage createdScene)
    {
        // We need to setup things here like done when coming from the industrial stage
        createdScene.SetupForExistingGameFromAnotherStage(true,
            SimulationParameters.Instance.GetUnitType("advancedSpaceship"), null);

        createdScene.OnBecomeAscended();
    }

    protected override Node LoadScene()
    {
        return SetupNewAscendedSpaceStage(null);
    }

    protected override void CustomizeLoadedScene(Node scene)
    {
    }

    protected override void CustomizeAttachedScene(Node scene)
    {
        base.CustomizeAttachedScene(scene);

        var spaceStage = (SpaceStage)scene;

        PrepareSpaceStageForFreshAscension(spaceStage);
    }
}
