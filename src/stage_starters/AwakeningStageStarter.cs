using Godot;

/// <summary>
///   Direct starter for awakening (basically <see cref="MainGameState.MacroscopicStage"/> with awakened species)
/// </summary>
public partial class AwakeningStageStarter : AwareStageStarter
{
    protected override MainGameState SimplyLoadableGameState => MainGameState.MacroscopicStage;

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
