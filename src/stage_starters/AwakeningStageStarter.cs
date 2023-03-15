using Godot;

public class AwakeningStageStarter : AwareStageStarter
{
    protected override MainGameState SimplyLoadableGameState => MainGameState.MulticellularStage;

    protected override void CustomizeLoadedScene(Node scene)
    {
        var stage = (MulticellularStage)scene;

        var game = GameProperties.StartNewAwakeningStageGame(new WorldGenerationSettings());

        stage.CurrentGame = game;
    }

    protected override void CustomizeAttachedScene(Node scene)
    {
        base.CustomizeAttachedScene(scene);

        var stage = (MulticellularStage)scene;

        // TODO: remove this once the going to land part is implemented properly
        stage.TeleportToLand();
        stage.MoveToAwakeningStage();
    }
}
