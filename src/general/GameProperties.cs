using System;
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

    private GameProperties(WorldGenerationSettings? settings = null, Species? startingSpecies = null)
    {
        settings ??= new WorldGenerationSettings();
        GameWorld = new GameWorld(settings, startingSpecies);
        TutorialState = new TutorialState();
    }

    [JsonConstructor]
    private GameProperties(GameWorld gameWorld, TutorialState tutorialState)
    {
        GameWorld = gameWorld;
        TutorialState = tutorialState;
    }

    /// <summary>
    ///   The world this game is played in. Has all the species and map data
    /// </summary>
    [JsonProperty]
    public GameWorld GameWorld { get; }

    /// <summary>
    ///   When true the player is in freebuild mode and various things
    ///   should be disabled / different.
    /// </summary>
    [JsonIgnore]
    public bool FreeBuild => freeBuild;

    /// <summary>
    ///   True when the player is currently ascended and should be allowed to do anything
    /// </summary>
    [JsonProperty]
    public bool Ascended { get; private set; }

    /// <summary>
    ///   Counts how many times the player has ascended with the current game in total
    /// </summary>
    [JsonProperty]
    public int AscensionCounter { get; private set; }

    /// <summary>
    ///   The tutorial state for this game
    /// </summary>
    [JsonProperty]
    public TutorialState TutorialState { get; }

    // TODO: start using this to prevent saving
    /// <summary>
    ///   Set to true when the player has entered the stage prototypes and some extra restrictions apply
    /// </summary>
    public bool InPrototypes { get; private set; }

    // Not saved for now as this is only in prototypes
    [JsonIgnore]
    public TechWeb TechWeb { get; private set; } = new();

    /// <summary>
    ///   Starts a new game in the microbe stage
    /// </summary>
    public static GameProperties StartNewMicrobeGame(WorldGenerationSettings settings, bool freebuild = false,
        Species? startingSpecies = null)
    {
        var game = new GameProperties(settings, startingSpecies);

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
    public static GameProperties StartNewEarlyMulticellularGame(WorldGenerationSettings settings)
    {
        var game = new GameProperties(settings);

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
    public static GameProperties StartNewLateMulticellularGame(WorldGenerationSettings settings)
    {
        var game = new GameProperties(settings);

        var playerSpecies = MakePlayerOrganellesMakeSenseForMulticellular(game);

        var earlySpecies = game.GameWorld.ChangeSpeciesToMulticellular(playerSpecies);
        MakeCellPlacementMakeSenseForLateMulticellular(earlySpecies);
        game.GameWorld.ChangeSpeciesToLateMulticellular(earlySpecies);

        game.EnterPrototypes();

        return game;
    }

    public static GameProperties StartNewAwareStageGame(WorldGenerationSettings settings)
    {
        var game = StartNewLateMulticellularGame(settings);

        // Modify the player species to have enough brain power to reach the target stage
        var playerSpecies = (LateMulticellularSpecies)game.GameWorld.PlayerSpecies;

        // Create the brain tissue type
        var brainType = (CellType)playerSpecies.CellTypes.First().Clone();
        brainType.TypeName = TranslationServer.Translate("BRAIN_CELL_NAME_DEFAULT");
        brainType.Colour = new Color(0.807f, 0.498f, 0.498f);

        var axon = SimulationParameters.Instance.GetOrganelleType("axon");

        for (int r = 0; r > -1000; --r)
        {
            var template = new OrganelleTemplate(axon, new Hex(0, r), 0);

            if (!brainType.Organelles.CanPlaceAndIsTouching(template, true, false))
                continue;

            brainType.Organelles.Add(template);
            brainType.RepositionToOrigin();
            break;
        }

        if (!brainType.IsBrainTissueType())
            throw new Exception("Converting to brain tissue type failed");

        playerSpecies.CellTypes.Add(brainType);

        // Place enough of that for becoming aware
        while (LateMulticellularSpecies.CalculateMulticellularTypeFromLayout(playerSpecies.BodyLayout,
                   playerSpecies.Scale) == MulticellularSpeciesType.LateMulticellular)
        {
            AddBrainTissue(playerSpecies);
        }

        playerSpecies.OnEdited();

        if (playerSpecies.MulticellularType != MulticellularSpeciesType.Aware)
            throw new Exception("Adding enough brain power to reach aware stage failed");

        return game;
    }

    public static GameProperties StartNewAwakeningStageGame(WorldGenerationSettings settings)
    {
        var game = StartNewAwareStageGame(settings);

        // Further modify the player species to qualify for awakening stage
        var playerSpecies = (LateMulticellularSpecies)game.GameWorld.PlayerSpecies;

        while (LateMulticellularSpecies.CalculateMulticellularTypeFromLayout(playerSpecies.BodyLayout,
                   playerSpecies.Scale) != MulticellularSpeciesType.Awakened)
        {
            AddBrainTissue(playerSpecies);
        }

        playerSpecies.OnEdited();

        if (playerSpecies.MulticellularType != MulticellularSpeciesType.Awakened)
            throw new Exception("Adding enough brain power to reach awakening stage failed");

        return game;
    }

    public static GameProperties StartSocietyStageGame(WorldGenerationSettings settings)
    {
        var game = StartNewAwakeningStageGame(settings);

        // Initial tech unlocks the player needs
        var simulationParameters = SimulationParameters.Instance;
        game.TechWeb.UnlockTechnology(simulationParameters.GetTechnology("simpleStoneTools"));
        game.TechWeb.UnlockTechnology(simulationParameters.GetTechnology("societyCenter"));

        return game;
    }

    public static GameProperties StartIndustrialStageGame(WorldGenerationSettings settings)
    {
        var game = StartSocietyStageGame(settings);

        // Initial tech unlocks the player needs
        var simulationParameters = SimulationParameters.Instance;
        game.TechWeb.UnlockTechnology(simulationParameters.GetTechnology("steamPower"));

        return game;
    }

    public static GameProperties StartSpaceStageGame(WorldGenerationSettings settings)
    {
        var game = StartIndustrialStageGame(settings);

        // Initial tech unlocks the player needs
        var simulationParameters = SimulationParameters.Instance;
        game.TechWeb.UnlockTechnology(simulationParameters.GetTechnology("rocketry"));

        return game;
    }

    public static GameProperties StartAscensionStageGame(WorldGenerationSettings settings)
    {
        var game = StartSpaceStageGame(settings);

        // Initial tech unlocks the player needs
        var simulationParameters = SimulationParameters.Instance;
        game.TechWeb.UnlockTechnology(simulationParameters.GetTechnology("ascension"));

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

    public void OnBecomeAscended()
    {
        if (Ascended)
        {
            GD.PrintErr("Already ascended");
            return;
        }

        GD.Print("Current game is now ascended");
        Ascended = true;
        ++AscensionCounter;

        // TODO: stop game time tracking to have a stable final time for this save
    }

    public void BecomeDescendedVersionOf(GameProperties descendedGame)
    {
        AscensionCounter = descendedGame.AscensionCounter;

        // TODO: copy total game time

        // TODO: copy anything else?

        // Modify the game and world to make sure the descension perks are applied
        ApplyDescensionPerks();
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

        foreach (var columnDirection in new[] { 0, 1, -1 })
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

    private static void AddBrainTissue(LateMulticellularSpecies species, float brainTissueSize = 1)
    {
        var axonType = species.CellTypes.First(c => c.IsBrainTissueType());

        // TODO: a more intelligent algorithm
        // For now just find free positions above the origin and link it to the closest metaball
        var offsetsToCheck = new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0),
        };

        var metaball = new MulticellularMetaball(axonType)
        {
            Size = brainTissueSize,
        };

        // Start at a slightly positive value to put the brain above
        for (float y = 0.6f; y < 100; y += 0.34f)
        {
            for (float radius = 0; radius < 5; radius += 0.5f)
            {
                foreach (var offset in offsetsToCheck)
                {
                    var position = offset * radius + new Vector3(0, y, 0);

                    metaball.Position = position;

                    var (overlap, parent) = species.BodyLayout.CheckOverlapAndFindClosest(metaball);

                    if (overlap)
                        continue;

                    // Found a suitable place, adjust the position to be touching the parent
                    metaball.Parent = parent;
                    metaball.AdjustPositionToTouchParent();

                    // Skip if now the metaball would end up being inside something else
                    // TODO: a better approach would be to slide the metaball around its parent until it is no longer
                    // touching
                    if (species.BodyLayout.CheckOverlapAndFindClosest(metaball).Overlap)
                    {
                        metaball.Parent = null;
                        continue;
                    }

                    species.BodyLayout.Add(metaball);
                    return;
                }
            }
        }

        throw new Exception("Could not find a place to put more brain tissue");
    }

    private void ApplyDescensionPerks()
    {
        // TODO: implement the other perks
        float osmoregulationMultiplier = Mathf.Pow(0.8f, AscensionCounter);

        // Need to ensure the world has a custom difficulty we can modify here
        var modifiedDifficulty = GameWorld.WorldSettings.Difficulty.Clone();

        modifiedDifficulty.OsmoregulationMultiplier *= osmoregulationMultiplier;

        GameWorld.WorldSettings.Difficulty = modifiedDifficulty;
    }
}
