using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SharedBase.Archive;
using Xoshiro.PRNG64;

/// <summary>
///   This contains the single game settings.
///   This is recreated when starting a new game
/// </summary>
public class GameProperties : IArchivable
{
    public const int SERIALIZATION_VERSION = 1;

    private readonly Dictionary<string, bool> setBoolStatuses;

    private GameProperties(WorldGenerationSettings? settings = null, Species? startingSpecies = null)
    {
        settings ??= new WorldGenerationSettings();
        GameWorld = new GameWorld(settings, startingSpecies);
        TutorialState = new TutorialState();
        setBoolStatuses = new Dictionary<string, bool>();
    }

    // Archive constructor
    private GameProperties(GameWorld gameWorld, TutorialState tutorialState, Dictionary<string, bool> boolStatuses)
    {
        GameWorld = gameWorld;
        TutorialState = tutorialState;
        setBoolStatuses = boolStatuses;
    }

    /// <summary>
    ///   The world this game is played in. Has all the species and map data
    /// </summary>
    public GameWorld GameWorld { get; }

    /// <summary>
    ///   When true, the player is in freebuild mode, and various things
    ///   should be disabled / different.
    /// </summary>
    public bool FreeBuild { get; private set; }

    /// <summary>
    ///   True if the player has cheated in this game
    /// </summary>
    public bool CheatsUsed { get; private set; }

    /// <summary>
    ///   True when the player is currently ascended and should be allowed to do anything
    /// </summary>
    public bool Ascended { get; private set; }

    /// <summary>
    ///   Counts how many times the player has ascended with the current game in total
    /// </summary>
    public int AscensionCounter { get; private set; }

    /// <summary>
    ///   The tutorial state for this game
    /// </summary>
    public TutorialState TutorialState { get; }

    /// <summary>
    ///   Set to true when the player has entered the stage prototypes and some extra restrictions apply
    /// </summary>
    public bool InPrototypes { get; private set; }

    /// <summary>
    ///   ID of this playthrough
    /// </summary>
    public Guid PlaythroughID { get; private set; } = Guid.NewGuid();

    public TechWeb TechWeb { get; private set; } = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.GameProperties;
    public bool CanBeReferencedInArchive => true;

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
    ///   Starts a new game in the multicellular stage
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: add some other species as well to the world to make it not as empty as starting a new microbe game
    ///     this way
    ///     TODO: add a setting to trigger if in freebuild the colony stars off as a single cell or as the full
    ///     colony
    ///   </para>s
    /// </remarks>
    public static GameProperties StartNewMulticellularGame(WorldGenerationSettings settings, bool freebuild = false)
    {
        settings.Origin = WorldGenerationSettings.LifeOrigin.Pond;

        var game = new GameProperties(settings);

        OxygenateWorld(game.GameWorld.Map);

        // Modify the player species to actually make sense to be in the multicellular stage
        var playerSpecies = MakePlayerOrganellesMakeSenseForMulticellular(game);

        var finalSpecies = game.GameWorld.ChangeSpeciesToMulticellular(playerSpecies, !freebuild);

        // Make the player species match tolerances as they may have changed due to the species change
        GameWorld.SetSpeciesInitialTolerances(finalSpecies, game.GameWorld.Map, null);

        // TODO: generate multicellular species for freebuild
        if (freebuild)
        {
            game.EnterFreeBuild();
            game.GameWorld.GenerateRandomSpeciesForFreeBuild();
            game.TutorialState.Enabled = false;
        }

        return game;
    }

    /// <summary>
    ///   Starts a new game in the macroscopic stage
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: add some other species as well to the world to make it not as empty
    ///   </para>
    /// </remarks>
    public static GameProperties StartNewMacroscopicGame(WorldGenerationSettings settings)
    {
        var game = new GameProperties(settings);

        var playerSpecies = MakePlayerOrganellesMakeSenseForMulticellular(game);

        var earlySpecies = game.GameWorld.ChangeSpeciesToMulticellular(playerSpecies, false);
        MakeCellPlacementMakeSenseForMacroscopic(earlySpecies);
        var finalSpecies = game.GameWorld.ChangeSpeciesToMacroscopic(earlySpecies);

        GameWorld.SetSpeciesInitialTolerances(finalSpecies, game.GameWorld.Map, null);

        game.EnterPrototypes();

        return game;
    }

    public static GameProperties StartNewAwareStageGame(WorldGenerationSettings settings)
    {
        var game = StartNewMacroscopicGame(settings);

        // Modify the player species to have enough brain power to reach the target stage
        var playerSpecies = (MacroscopicSpecies)game.GameWorld.PlayerSpecies;

        // Create the brain tissue type
        var brainType = (CellType)playerSpecies.ModifiableCellTypes.First().Clone();
        brainType.CellTypeName = Localization.Translate("BRAIN_CELL_NAME_DEFAULT");
        brainType.Colour = new Color(0.807f, 0.498f, 0.498f);

        var axon = SimulationParameters.Instance.GetOrganelleType("axon");

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        for (int r = 0; r > -1000; --r)
        {
            var template = new OrganelleTemplate(axon, new Hex(0, r), 0);

            // Add no longer allows replacing cytoplasm by default
            if (!brainType.ModifiableOrganelles.CanPlaceAndIsTouching(template, false, workMemory1, workMemory2, false))
                continue;

            brainType.ModifiableOrganelles.AddFast(template, workMemory1, workMemory2);
            brainType.RepositionToOrigin();
            break;
        }

        if (!brainType.IsBrainTissueType())
            throw new Exception("Converting to brain tissue type failed");

        playerSpecies.ModifiableCellTypes.Add(brainType);

        // Place enough of that for becoming aware
        while (MacroscopicSpecies.CalculateMacroscopicTypeFromLayout(playerSpecies.ModifiableBodyLayout,
                   playerSpecies.Scale) == MacroscopicSpeciesType.Macroscopic)
        {
            AddBrainTissue(playerSpecies);
        }

        playerSpecies.OnEdited();

        if (playerSpecies.MacroscopicType != MacroscopicSpeciesType.Aware)
            throw new Exception("Adding enough brain power to reach aware stage failed");

        // TODO: macroscopic tolerances
        // GameWorld.SetSpeciesInitialTolerances(playerSpecies, game.GameWorld.Map, null);

        return game;
    }

    public static GameProperties StartNewAwakeningStageGame(WorldGenerationSettings settings)
    {
        var game = StartNewAwareStageGame(settings);

        // Further modify the player species to qualify for the awakening stage
        var playerSpecies = (MacroscopicSpecies)game.GameWorld.PlayerSpecies;

        while (MacroscopicSpecies.CalculateMacroscopicTypeFromLayout(playerSpecies.ModifiableBodyLayout,
                   playerSpecies.Scale) != MacroscopicSpeciesType.Awakened)
        {
            AddBrainTissue(playerSpecies);
        }

        playerSpecies.OnEdited();

        if (playerSpecies.MacroscopicType != MacroscopicSpeciesType.Awakened)
            throw new Exception("Adding enough brain power to reach awakening stage failed");

        // TODO: macroscopic tolerances
        // GameWorld.SetSpeciesInitialTolerances(playerSpecies, game.GameWorld.Map, null);

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

    public static GameProperties ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new GameProperties(reader.ReadObject<GameWorld>(),
            reader.ReadObject<TutorialState>(),
            reader.ReadObject<Dictionary<string, bool>>());

        reader.ReportObjectConstructorDone(instance, referenceId);

        instance.FreeBuild = reader.ReadBool();
        instance.CheatsUsed = reader.ReadBool();
        instance.Ascended = reader.ReadBool();
        instance.AscensionCounter = reader.ReadInt32();
        instance.InPrototypes = reader.ReadBool();
        instance.PlaythroughID = Guid.Parse(reader.ReadString() ?? throw new NullArchiveObjectException());

        // Not saved currently
        // instance.TechWeb = reader.ReadObject<TechWeb>();

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(GameWorld);
        writer.WriteObject(TutorialState);
        writer.WriteObject(setBoolStatuses);

        writer.Write(FreeBuild);
        writer.Write(CheatsUsed);
        writer.Write(Ascended);
        writer.Write(AscensionCounter);
        writer.Write(InPrototypes);
        writer.Write(PlaythroughID.ToString());

        // Not saved for now as this is only used in the prototypes

        // writer.WriteObject(TechWeb);
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
        FreeBuild = true;
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

    public void ReportCheatsUsed()
    {
        CheatsUsed = true;
    }

    public void BecomeDescendedVersionOf(GameProperties descendedGame)
    {
        AscensionCounter = descendedGame.AscensionCounter;

        // Disable tutorials, as we can assume that playing a second playthrough doesn't need tutorials
        TutorialState.Enabled = false;

        // TODO: copy total game time

        // TODO: copy anything else?

        // Modify the game and world to make sure the descension perks are applied
        ApplyDescensionPerks();
    }

    private static void OxygenateWorld(PatchMap map)
    {
        var changes = new Dictionary<Compound, float>();

        // ReSharper disable once CollectionNeverUpdated.Local
        var cloudSizes = new Dictionary<Compound, float>();

        var random = new XoShiRo256starstar();

        foreach (var patch in map.Patches.Values)
        {
            var hasOxygen = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Oxygen,
                out var currentOxygen);

            if (!hasOxygen)
                continue;

            currentOxygen.Ambient = patch.IsSurfacePatch() ? random.Next(0.3f, 0.4f) : random.Next(0.05f, 0.15f);
            changes[Compound.Oxygen] = currentOxygen.Ambient;
            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, cloudSizes);

            changes.Clear();
            cloudSizes.Clear();
        }
    }

    private static MicrobeSpecies MakePlayerOrganellesMakeSenseForMulticellular(GameProperties game)
    {
        var simulationParameters = SimulationParameters.Instance;
        var playerSpecies = (MicrobeSpecies)game.GameWorld.PlayerSpecies;

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"),
            new Hex(0, -3), 0), workMemory1, workMemory2);
        playerSpecies.IsBacteria = false;

        var mitochondrion = simulationParameters.GetOrganelleType("mitochondrion");

        // Remove the original cytoplasm in the species and replace with hydrogenosome for a more efficient layout
        playerSpecies.Organelles.RemoveHexAt(new Hex(0, 0), workMemory1);

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(simulationParameters.GetOrganelleType("bindingAgent"),
            new Hex(0, 2), 0), workMemory1, workMemory2);

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(mitochondrion,
            new Hex(-1, 2), 0), workMemory1, workMemory2);

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(mitochondrion,
            new Hex(1, 1), 0), workMemory1, workMemory2);

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(mitochondrion,
            new Hex(0, 1), 0), workMemory1, workMemory2);

        var cytoplasm = simulationParameters.GetOrganelleType("cytoplasm");

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(cytoplasm,
            new Hex(1, -1), 0), workMemory1, workMemory2);

        playerSpecies.Organelles.AddFast(new OrganelleTemplate(cytoplasm,
            new Hex(-1, 0), 0), workMemory1, workMemory2);

        playerSpecies.OnEdited();
        return playerSpecies;
    }

    private static void MakeCellPlacementMakeSenseForMacroscopic(MulticellularSpecies species)
    {
        // We want at least COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC cells in a kind of long pattern
        species.ModifiableGameplayCells.Clear();

        var type = species.ModifiableCellTypes.First();

        int columns = 3;

        var inEachColumn = Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC / columns;

        var startHex = new Hex(0, 0);
        var columnCellOffset = new Hex(0, -1);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

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
                    if (species.ModifiableGameplayCells.CanPlace(template, workMemory1, workMemory2))
                    {
                        species.ModifiableGameplayCells.AddFast(template, workMemory1, workMemory2);
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
                if (species.ModifiableGameplayCells.CanPlace(template, workMemory1, workMemory2))
                {
                    species.ModifiableGameplayCells.AddFast(template, workMemory1, workMemory2);
                    --columnCellsLeft;

                    if (columnCellsLeft < 1)
                        break;
                }
            }
        }

        // Make sure we hit the required cell count
        while (species.ModifiableGameplayCells.Count < Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC)
        {
            var direction = new Vector2(0, -1);

            for (int distance = 1; distance < 1000; ++distance)
            {
                var finalPos = direction * distance;
                var template = new CellTemplate(type,
                    new Hex(MathUtils.RoundToInt(finalPos.X), MathUtils.RoundToInt(finalPos.Y)), 0);

                if (species.ModifiableGameplayCells.CanPlace(template, workMemory1, workMemory2))
                {
                    species.ModifiableGameplayCells.AddFast(template, workMemory1, workMemory2);
                    break;
                }
            }
        }

        species.RepositionToOrigin();
    }

    private static void AddBrainTissue(MacroscopicSpecies species, float brainTissueSize = 1)
    {
        var axonType = species.ModifiableCellTypes.First(c => c.IsBrainTissueType());

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

        var metaball = new MacroscopicMetaball(axonType)
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

                    var (overlap, parent) = species.ModifiableBodyLayout.CheckOverlapAndFindClosest(metaball);

                    if (overlap)
                        continue;

                    // Found a suitable place, adjust the position to be touching the parent
                    metaball.ModifiableParent = parent;
                    metaball.AdjustPositionToTouchParent();

                    // Skip if now the metaball would end up being inside something else
                    // TODO: a better approach would be to slide the metaball around its parent until it is no longer
                    // touching
                    if (species.ModifiableBodyLayout.CheckOverlapAndFindClosest(metaball).Overlap)
                    {
                        metaball.ModifiableParent = null;
                        continue;
                    }

                    species.ModifiableBodyLayout.Add(metaball);
                    return;
                }
            }
        }

        throw new Exception("Could not find a place to put more brain tissue");
    }

    private void ApplyDescensionPerks()
    {
        // TODO: implement the other perks
        float osmoregulationMultiplier = MathF.Pow(0.8f, AscensionCounter);

        // Need to ensure the world has a custom difficulty we can modify here
        var modifiedDifficulty = GameWorld.WorldSettings.Difficulty.Clone();

        modifiedDifficulty.OsmoregulationMultiplier *= osmoregulationMultiplier;

        GameWorld.WorldSettings.Difficulty = modifiedDifficulty;
    }
}
