using System.Linq;
using Godot;

public class EarlyMulticellularStageStarter : ComplexStageStarterBase
{
    protected override MainGameState SimplyLoadableGameState => MainGameState.MicrobeStage;

    protected override void CustomizeLoadedScene(Node scene)
    {
        var stage = (MicrobeStage)scene;

        // Setup a new game with the player being a simple multicellular species already
        var game = GameProperties.StartNewEarlyMulticellularGame(new WorldGenerationSettings());

        // Unlike the editor add 2 player cell copies already to make this a bit more realistic as a start
        var playerSpecies = (EarlyMulticellularSpecies)game.GameWorld.PlayerSpecies;

        var cellType = playerSpecies.CellTypes.First();

        for (int q = 1; q < 1000; ++q)
        {
            var template = new CellTemplate(cellType, new Hex(q, 1), 0);

            if (!playerSpecies.Cells.CanPlace(template))
                continue;

            playerSpecies.Cells.Add(template);
            break;
        }

        for (int q = -1; q > -1000; --q)
        {
            var template = new CellTemplate(cellType, new Hex(q, 1), 0);

            if (!playerSpecies.Cells.CanPlace(template))
                continue;

            playerSpecies.Cells.Add(template);
            break;
        }

        playerSpecies.OnEdited();

        stage.CurrentGame = game;
    }
}
