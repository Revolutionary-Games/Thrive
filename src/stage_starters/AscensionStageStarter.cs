using System;
using Godot;

public class AscensionStageStarter : ComplexStageStarterBase
{
    protected override MainGameState SimplyLoadableGameState => throw new InvalidOperationException("Should be unused");

    public static SpaceStage SetupNewAscendedSpaceStage(GameProperties? currentGame)
    {
        currentGame ??= GameProperties.StartAscensionStageGame(new WorldGenerationSettings());

        var spaceStage = SceneManager.Instance.LoadScene(MainGameState.SpaceStage).Instance<SpaceStage>();
        spaceStage.CurrentGame = currentGame;

        return spaceStage;
    }

    public static void PrepareSpaceStageForFreshAscension(SpaceStage createdScene)
    {
        // We need to setup things here like done when coming from the industrial stage
        createdScene.SetupForExistingGameFromAnotherStage(true,
            SimulationParameters.Instance.GetUnitType("simpleSpaceRocket"), null);

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
