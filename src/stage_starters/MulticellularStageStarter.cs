using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Direct starter for multicellular (basically <see cref="MainGameState.MicrobeStage"/> with multicellular
///   species)
/// </summary>
public partial class MulticellularStageStarter : ComplexStageStarterBase
{
    protected override MainGameState SimplyLoadableGameState => MainGameState.MicrobeStage;

    protected override void CustomizeLoadedScene(Node scene)
    {
        var stage = (MicrobeStage)scene;

        // Set up a new game with the player being a simple multicellular species already
        var game = GameProperties.StartNewMulticellularGame(new WorldGenerationSettings());

        // Unlike the editor, add 2 player cell copies already to make this a bit more realistic as a start
        var playerSpecies = (MulticellularSpecies)game.GameWorld.PlayerSpecies;

        var cellType = playerSpecies.ModifiableCellTypes.First();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        for (int q = 1; q < 1000; ++q)
        {
            var template = new CellTemplate(cellType, new Hex(q, 1), 0);

            if (!playerSpecies.ModifiableGameplayCells.CanPlace(template, workMemory1, workMemory2))
                continue;

            playerSpecies.ModifiableGameplayCells.AddFast(template, workMemory1, workMemory2);
            break;
        }

        for (int q = -1; q > -1000; --q)
        {
            var template = new CellTemplate(cellType, new Hex(q, 1), 0);

            if (!playerSpecies.ModifiableGameplayCells.CanPlace(template, workMemory1, workMemory2))
                continue;

            playerSpecies.ModifiableGameplayCells.AddFast(template, workMemory1, workMemory2);
            break;
        }

        // Refresh the editor cells
        var editorCells = playerSpecies.ModifiableEditorCells;
        editorCells.Clear();
        MulticellularLayoutHelpers.GenerateEditorLayoutFromGameplayLayout(editorCells,
            playerSpecies.ModifiableGameplayCells, workMemory1, workMemory2);

        playerSpecies.OnEdited();

        stage.CurrentGame = game;
    }
}
