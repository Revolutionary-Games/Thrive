using System;
using System.Reflection;

/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public class Constants
{
    /// <summary>
    ///   How long the player stays dead before respawning
    /// </summary>
    public const float PLAYER_RESPAWN_TIME = 5.0f;

    // Variance in the player position when respawning
    public const float MIN_SPAWN_DISTANCE = -5000.0f;
    public const float MAX_SPAWN_DISTANCE = 5000.0f;

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

    public const float FLAGELLA_BASE_FORCE = 75.7f;

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
    ///   The maximum force that can be applied by currents in the fluid system
    /// </summary>
    public const float MAX_FORCE_APPLIED_BY_CURRENTS = 0.525f;

    /// <summary>
    ///   Added 2 seconds here to make the random implementation look a bit better
    /// </summary>
    public const float MICROBE_AI_THINK_INTERVAL = 2.3f;

    public const int MICROBE_AI_OBJECTS_PER_TASK = 15;

    public const int INITIAL_SPECIES_POPULATION = 100;

    public const int INITIAL_FREEBUILD_POPULATION_VARIANCE_MIN = 0;
    public const int INITIAL_FREEBUILD_POPULATION_VARIANCE_MAX = 400;

    // Right now these are used for species split from the player
    public const int INITIAL_SPLIT_POPULATION_MIN = 600;
    public const int INITIAL_SPLIT_POPULATION_MAX = 2000;

    /// <summary>
    ///   Controls with how much force agents are fired
    /// </summary>
    public const float AGENT_EMISSION_IMPULSE_STRENGTH = 20.0f;

    public const float OXYTOXY_DAMAGE = 10.0f;

    public const float AGENT_EMISSION_DISTANCE_OFFSET = 0.5f;

    public const float EMITTED_AGENT_LIFETIME = 5.0f;

    public const int MAX_EMITTED_AGENTS_ON_DEATH = 5;

    /// <summary>
    ///   Percentage of the compounds that compose the organelle
    ///   released upon death (between 0.0 and 1.0).
    /// </summary>
    public const float COMPOUND_MAKEUP_RELEASE_PERCENTAGE = 0.9f;
    public const float COMPOUND_RELEASE_PERCENTAGE = 0.9f;

    /// <summary>
    ///   Base mass all microbes have on top of their organelle masses
    /// </summary>
    public const float MICROBE_BASE_MASS = 0.7f;

    /// <summary>
    ///   Cooldown between agent emissions, in seconds.
    /// </summary>
    public const float AGENT_EMISSION_COOLDOWN = 2.0f;

    /// <summary>
    ///   The minimum amount of oxytoxy (or any agent) needed to be able to shoot.
    /// </summary>
    public const float MINIMUM_AGENT_EMISSION_AMOUNT = 1;

    /// <summary>
    ///   The time (in seconds) it takes a cloud being absorbed to halve its compunds.
    /// </summary>
    public const float CLOUD_ABSORPTION_HALF_LIFE = 0.02291666666f;

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

    public const float MICROBE_VENT_COMPOUND_MULTIPLIER = 10000.0f;

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
    ///   Determines how big of a fraction of damage (of total health)
    ///   is dealt to a microbe at a time when it is out of ATP.
    /// </summary>
    public const float NO_ATP_DAMAGE_FRACTION = 0.04f;

    /// <summary>
    ///   Organelles won't take compounds if there is less available than this amount
    /// </summary>
    public const float ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST = 0.0f;

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
    ///   The minimum size ratio between a cell and a possible engulfing victim.
    /// </summary>
    public const float ENGULF_SIZE_RATIO_REQ = 1.5f;

    /// <summary>
    ///   The amount of hp per second of damage when being engulfed
    /// </summary>
    public const float ENGULF_DAMAGE = 45.0f;

    /// <summary>
    ///   Damage a single pilus stab does
    /// </summary>
    public const float PILUS_BASE_DAMAGE = 5.0f;

    /// <summary>
    ///   Osmoregulation ATP cost per second per hex
    /// </summary>
    public const float ATP_COST_FOR_OSMOREGULATION = 1.0f;

    /// <summary>
    ///   The default contact store count for objects using contact reporting
    /// </summary>
    public const int DEFAULT_STORE_CONTACTS_COUNT = 4;

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

    public const int BASE_MUTATION_POINTS = 100;

    public const int ORGANELLE_REMOVE_COST = 10;

    // Corpse info
    public const float CORPSE_COMPOUND_COMPENSATION = 8.0f;
    public const int CORPSE_CHUNK_DIVISER = 3;
    public const float CORPSE_CHUNK_AMOUNT_DIVISER = 3.0f;
    public const float CHUNK_ENGULF_COMPOUND_DIVISOR = 30.0f;

    /// <summary>
    ///   The drag force is calculated by taking the current velocity
    ///   and multiplying it by this. This must be negative!
    /// </summary>
    public const float CELL_DRAG_MULTIPLIER = -0.12f;
    public const float CELL_SIZE_DRAG_MULTIPLIER = -0.003f;

    /// <summary>
    ///   If drag is below this it isn't applied to let the cells come to a halt properly
    /// </summary>
    public const float CELL_REQUIRED_DRAG_BEFORE_APPLY = 0.0033f;

    /// <summary>
    ///   This should be the max needed hexes (nucleus {10} * 6-way symmetry)
    /// </summary>
    public const int MAX_HOVER_HEXES = 60;
    public const int MAX_SYMMETRY = 6;

    // Cell Colors
    public const float MIN_COLOR = 0.0f;
    public const float MAX_COLOR = 0.9f;

    public const float MIN_COLOR_MUTATION = -0.2f;
    public const float MAX_COLOR_MUTATION = 0.2f;

    public const float MIN_OPACITY = 0.5f;
    public const float MAX_OPACITY = 1.8f;

    public const float MIN_OPACITY_CHITIN = 0.4f;
    public const float MAX_OPACITY_CHITIN = 1.2f;

    // Min Opacity Mutation
    public const float MIN_OPACITY_MUTATION = -0.01f;
    public const float MAX_OPACITY_MUTATION = 0.01f;

    // Mutation Variables
    public const float MUTATION_BACTERIA_TO_EUKARYOTE = 1.0f;
    public const float MUTATION_CREATION_RATE = 0.1f;
    public const float MUTATION_EXTRA_CREATION_RATE = 0.1f;
    public const float MUTATION_DELETION_RATE = 0.1f;
    public const float MUTATION_REPLACEMENT_RATE = 0.1f;

    // Max fear and agression and activity
    public const float MAX_SPECIES_AGRESSION = 400.0f;
    public const float MAX_SPECIES_FEAR = 400.0f;
    public const float MAX_SPECIES_ACTIVITY = 400.0f;
    public const float MAX_SPECIES_FOCUS = 400.0f;
    public const float MAX_SPECIES_OPPORTUNISM = 400.0f;

    // Personality Mutation
    public const float MAX_SPECIES_PERSONALITY_MUTATION = 20.0f;
    public const float MIN_SPECIES_PERSONALITY_MUTATION = -20.0f;

    // Genus splitting and name mutation
    public const int MUTATION_CHANGE_GENUS = 33;
    public const int MUTATION_WORD_EDIT = 10;

    /// <summary>
    ///   How many steps forward of the population simulation to do when auto-evo looks at the results of mutations
    ///   etc. for which is the most beneficial
    /// </summary>
    public const int AUTOEVO_VARIANT_SIMULATION_STEPS = 10;

    public const int AUTO_EVO_MINIMUM_MOVE_POPULATION = 250;
    public const float AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION = 0.1f;
    public const float AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION = 0.9f;

    // Some (placeholder) auto-evo algorithm tweak parameters
    public const int AUTO_EVO_LOW_SPECIES_THRESHOLD = 3;
    public const int AUTO_EVO_LOW_SPECIES_BOOST = 500;
    public const int AUTO_EVO_HIGH_SPECIES_THRESHOLD = 11;
    public const int AUTO_EVO_HIGH_SPECIES_PENALTY = 500;
    public const int AUTO_EVO_RANDOM_POPULATION_CHANGE = 500;

    public const float GLUCOSE_REDUCTION_RATE = 0.8f;

    /// <summary>
    ///   All Nodes tagged with this are handled by the spawn system for despawning
    /// </summary>
    public const string SPAWNED_GROUP = "spawned";

    /// <summary>
    ///   All Nodes tagged with this are handled by the timed life system for despawning
    /// </summary>
    public const string TIMED_GROUP = "timed";

    /// <summary>
    ///   All RigidBody nodes tagged with this are affected by currents by the fluid system
    /// </summary>
    public const string FLUID_EFFECT_GROUP = "fluid_effect";

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
