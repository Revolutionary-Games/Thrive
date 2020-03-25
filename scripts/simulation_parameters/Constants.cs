using System;
using System.Reflection;

/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public class Constants
{
    /// <summary>
    ///   The (default) size of the hexagons, used in
    ///   calculations. Don't change this.
    /// </summary>
    public const float DEFAULT_HEX_SIZE = 0.75f;

    /// <summary>
    ///   Don't change this, so much stuff will break
    /// </summary>
    public const int CLOUDS_IN_ONE = 4;

    // NOTE: these 4 constants need to match what is setup in CompoundCloudPlane.tscn
    public const int CLOUD_WIDTH = 100;
    public const int CLOUD_X_EXTENT = CLOUD_WIDTH * 2;
    public const int CLOUD_HEIGHT = 100;

    // This is cloud local Y not world Y
    public const int CLOUD_Y_EXTENT = CLOUD_HEIGHT * 2;

    public const float CLOUD_Y_COORDINATE = 0;

    public const int MEMBRANE_RESOLUTION = 10;

    /// <summary>
    ///   BASE MOVEMENT ATP cost. Cancels out a little bit more then one cytoplasm's glycolysis
    /// </summary>
    /// <remarks>
    ///   this is applied *per* hex
    /// </remarks>
    public const float BASE_MOVEMENT_ATP_COST = 1.0f;

    public const float FLAGELLA_ENERGY_COST = 7.1f;

    public const float FLAGELLA_BASE_FORCE = 40.7f;

    /// <summary>
    ///   Used for energy balance calculations
    /// </summary>
    public const string FLAGELLA_COMPONENT_NAME = "movement";

    public const float CELL_BASE_THRUST = 50.6f;

    public const int PROCESS_OBJECTS_PER_TASK = 50;

    public const int MICROBE_SPAWN_RADIUS = 150;
    public const int CLOUD_SPAWN_RADIUS = 150;

    public const float STARTING_SPAWN_DENSITY = 70000.0f;
    public const float MAX_SPAWN_DENSITY = 20000.0f;

    /// <summary>
    ///   Added 2 seconds here to make the random implementation look a bit better
    /// </summary>
    public const float MICROBE_AI_THINK_INTERVAL = 2.3f;

    public const int MICROBE_AI_OBJECTS_PER_TASK = 15;

    public const int INITIAL_SPECIES_POPULATION = 100;

    /// <summary>
    ///   Controls with how much force agents are fired
    /// </summary>
    public const float AGENT_EMISSION_IMPULSE_STRENGTH = 20.0f;

    /// <summary>
    ///   How much of a compound is actually given to a cell when absorbed
    /// </summary>
    public const float ABSORPTION_RATIO = 0.0000125f;

    /// <summary>
    ///   Should be greater than ABSORPTION_RATIO
    /// </summary>
    public const float SKIP_TRYING_TO_ABSORB_RATIO = 0.0002f;

    /// <summary>
    ///   How much compounds a cell can vent per second
    /// </summary>
    public const float COMPOUNDS_TO_VENT_PER_SECOND = 5.0f;

    public const float CHUNK_VENT_COMPOUND_MULTIPLIER = 1000.0f;

    /// <summary>
    ///   This is used just as the default value for health and max
    ///   health of a microbe. The default membrane actually
    ///   determines the default health.
    /// </summary>
    public const float DEFAULT_HEALTH = 100.0f;

    /// <summary>
    ///   Amount of health per second regenerated
    /// </summary>
    public const float REGENERATION_RATE = 1.0f;

    /// <summary>
    ///   How often in seconds ATP damage is checked and applied if cell has no ATP
    /// </summary>
    public const float ATP_DAMAGE_CHECK_INTERVAL = 0.9f;

    /// <summary>
    ///   How much fully rigid membrane adds hitpoints
    /// </summary>
    public const float MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER = 30;

    /// <summary>
    ///   How much fully rigid membrane reduces movement factor of a cell
    /// </summary>
    public const float MEMBRANE_RIGIDITY_MOBILITY_MODIFIER = 0.1f;

    /// <summary>
    ///   How much ATP does engulf mode cost per second
    /// </summary>
    public const float ENGULFING_ATP_COST_SECOND = 1.5f;

    /// <summary>
    ///   The speed reduction when a cell is in engulfing mode.
    /// </summary>
    public const float ENGULFING_MOVEMENT_DIVISION = 2.0f;

    /// <summary>
    ///   The speed reduction when a cell is being engulfed.
    /// </summary>
    public const float ENGULFED_MOVEMENT_DIVISION = 10.0f;

    /// <summary>
    ///   The minimum HP ratio between a cell and a possible engulfing victim.
    /// </summary>
    public const float ENGULF_HP_RATIO_REQ = 1.5f;

    /// <summary>
    ///   The amount of hp per second of damage when being engulfed
    /// </summary>
    public const float ENGULF_DAMAGE = 45.0f;

    /// <summary>
    ///   Osmoregulation ATP cost per second per hex
    /// </summary>
    public const float ATP_COST_FOR_OSMOREGULATION = 1.0f;

    // Darwinian Evo Values
    public const int CREATURE_DEATH_POPULATION_LOSS = -60;
    public const int CREATURE_KILL_POPULATION_GAIN = 50;
    public const int CREATURE_SCAVENGE_POPULATION_GAIN = 10;
    public const int CREATURE_REPRODUCE_POPULATION_GAIN = 50;
    public const int CREATURE_ESCAPE_POPULATION_GAIN = 50;

    /// <summary>
    ///   How often a microbe can get the engulf escape population bonus
    /// </summary>
    public const float CREATURE_ESCAPE_INTERVAL = 5;

    /// <summary>
    ///   All Nodes tagged with this are handled by the spawn system for despawning
    /// </summary>
    public const string SPAWNED_GROUP = "spawned";

    /// <summary>
    ///   All Nodes tagged with this are handled by the timed life system for despawning
    /// </summary>
    public const string TIMED_GROUP = "timed";

    /// <summary>
    ///   All Nodes tagged with this are handled by the process system
    /// </summary>
    public const string PROCESS_GROUP = "process";

    /// <summary>
    ///   All Nodes tagged with this are handled by the ai system
    /// </summary>
    public const string AI_GROUP = "ai";

    public const string CONFIGURATION_FILE = "user://thrive_settings.json";

    private static readonly Constants INSTANCE = new Constants();

    static Constants()
    {
    }

    private Constants()
    {
    }

    public static Constants Instance
    {
        get
        {
            return INSTANCE;
        }
    }

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            var versionSuffix = (AssemblyInformationalVersionAttribute[])assembly.
                GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return version.ToString() + versionSuffix[0].InformationalVersion;
        }
    }
}
