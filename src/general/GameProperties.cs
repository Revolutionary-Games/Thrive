using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   This contains the single game settings.
///   This is recreated when starting a new game
/// </summary>
[JsonObject(IsReference = true)]
public class GameProperties
{
    [JsonProperty]
    private readonly Dictionary<string, bool> setBoolStatuses = new();

    [JsonProperty]
    private bool freeBuild;

    [JsonConstructor]
    private GameProperties(WorldGenerationSettings? settings = null)
    {
        settings ??= new WorldGenerationSettings();
        GameWorld = new GameWorld(settings);
        TutorialState = new TutorialState();
    }

    /// <summary>
    ///   The world this game is played in. Has all the species and map data
    /// </summary>
    [JsonProperty]
    public GameWorld GameWorld { get; private set; }

    /// <summary>
    ///   When true the player is in freebuild mode and various things
    ///   should be disabled / different.
    /// </summary>
    [JsonIgnore]
    public bool FreeBuild => freeBuild;

    /// <summary>
    ///   The tutorial state for this game
    /// </summary>
    [JsonProperty]
    public TutorialState TutorialState { get; private set; }

    // TODO: start using this to prevent saving
    /// <summary>
    ///   Set to true when the player has entered the stage prototypes and some extra restrictions apply
    /// </summary>
    public bool InPrototypes { get; private set; }

    /// <summary>
    ///   Starts a new game in the microbe stage
    /// </summary>
    public static GameProperties StartNewMicrobeGame(WorldGenerationSettings settings, bool freebuild = false)
    {
        var game = new GameProperties(settings);

        if (freebuild)
        {
            game.EnterFreeBuild();
            game.GameWorld.GenerateRandomSpeciesForFreeBuild();
            game.TutorialState.Enabled = false;
        }

        return game;
    }

    /// <summary>
    ///   Starts a new game in the early multicellular stage
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: add some other species as well to the world to make it not as empty as starting a new microbe game
    ///     this way
    ///   </para>
    /// </remarks>
    public static GameProperties StartNewEarlyMulticellularGame()
    {
        var game = new GameProperties();

        // Modify the player species to actually make sense to be in the multicellular stage
        var playerSpecies = MakePlayerOrganellesMakeSenseForMulticellular(game);

        game.GameWorld.ChangeSpeciesToMulticellular(playerSpecies);

        game.EnterPrototypes();

        return game;
    }

    /// <summary>
    ///   Starts a new game in the late multicellular stage
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: add some other species as well to the world to make it not as empty
    ///   </para>
    /// </remarks>
    public static GameProperties StartNewLateMulticellularGame()
    {
        var game = new GameProperties();

        var playerSpecies = MakePlayerOrganellesMakeSenseForMulticellular(game);

        var earlySpecies = game.GameWorld.ChangeSpeciesToMulticellular(playerSpecies);
        MakeCellPlacementMakeSenseForLateMulticellular(earlySpecies);
        game.GameWorld.ChangeSpeciesToLateMulticellular(earlySpecies);

        game.EnterPrototypes();

        return game;
    }

    /// <summary>
    ///   Returns whether a key has a true bool set to it
    /// </summary>
    public bool IsBoolSet(string key)
    {
        setBoolStatuses.TryGetValue(key, out bool boolean);
        return boolean;
    }

    /// <summary>
    ///   Binds a string to a bool
    /// </summary>
    public void SetBool(string key, bool value)
    {
        setBoolStatuses[key] = value;
    }

    /// <summary>
    ///   Enters free build mode. There is purposefully no way to undo
    ///   this other than starting a new game.
    /// </summary>
    public void EnterFreeBuild()
    {
        GD.Print("Entering freebuild mode");
        freeBuild = true;
    }

    public void EnterPrototypes()
    {
        GD.Print("Game is in now in prototypes. EXPECT MAJOR BUGS!");
        InPrototypes = true;
    }

    private static MicrobeSpecies MakePlayerOrganellesMakeSenseForMulticellular(GameProperties game)
    {
        var simulationParameters = SimulationParameters.Instance;
        var playerSpecies = (MicrobeSpecies)game.GameWorld.PlayerSpecies;

        playerSpecies.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"),
            new Hex(0, -3), 0));
        playerSpecies.IsBacteria = false;

        var mitochondrion = simulationParameters.GetOrganelleType("mitochondrion");

        playerSpecies.Organelles.Add(new OrganelleTemplate(mitochondrion,
            new Hex(-1, 1), 0));

        playerSpecies.Organelles.Add(new OrganelleTemplate(mitochondrion,
            new Hex(1, 0), 0));

        playerSpecies.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("bindingAgent"),
            new Hex(0, 1), 0));

        playerSpecies.OnEdited();
        return playerSpecies;
    }

    private static void MakeCellPlacementMakeSenseForLateMulticellular(EarlyMulticellularSpecies species)
    {
        // We want at least COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC cells in a kind of long pattern
        species.Cells.Clear();

        var type = species.CellTypes.First();

        int columns = 3;

        var inEachColumn = Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC / columns;

        var startHex = new Hex(0, 0);
        var columnCellOffset = new Hex(0, -1);

        foreach (var columnDirection in new int[] { 0, 1, -1 })
        {
            var columnStart = startHex + new Hex(columnDirection, 0);

            bool placed = false;

            // Find where we can place the first cell in this column
            while (!placed)
            {
                bool breakInnerLoop = false;

                while (!placed)
                {
                    var template = new CellTemplate(type, columnStart, 0);
                    if (species.Cells.CanPlace(template))
                    {
                        species.Cells.Add(template);
                        placed = true;
                        break;
                    }

                    columnStart += new Hex(columnDirection, 0);

                    if (breakInnerLoop)
                        break;

                    breakInnerLoop = true;
                }

                if (placed)
                    break;

                columnStart -= new Hex(0, -1);
            }

            int columnCellsLeft = inEachColumn - 1;

            for (int distance = 0; distance < 10000; ++distance)
            {
                var template = new CellTemplate(type, columnStart + columnCellOffset * distance, 0);
                if (species.Cells.CanPlace(template))
                {
                    species.Cells.Add(template);
                    --columnCellsLeft;

                    if (columnCellsLeft < 1)
                        break;
                }
            }
        }

        // Make sure we hit the required cell count
        while (species.Cells.Count < Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC)
        {
            var direction = new Vector2(0, -1);

            for (int distance = 1; distance < 1000; ++distance)
            {
                var finalPos = direction * distance;
                var template = new CellTemplate(type,
                    new Hex(Mathf.RoundToInt(finalPos.x), Mathf.RoundToInt(finalPos.y)), 0);

                if (species.Cells.CanPlace(template))
                {
                    species.Cells.Add(template);
                    break;
                }
            }
        }

        species.RepositionToOrigin();
    }
}
