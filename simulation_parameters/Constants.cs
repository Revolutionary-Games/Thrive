using System;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public static class Constants
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

    public const int CLOUD_SQUARES_PER_SIDE = 3;
    public const int CLOUD_EDGE_WIDTH = 2;

    // NOTE: these 4 constants need to match what is setup in CompoundCloudPlane.tscn
    public const int CLOUD_WIDTH = 300;
    public const int CLOUD_X_EXTENT = CLOUD_WIDTH * 2;
    public const int CLOUD_HEIGHT = 300;

    // This is cloud local Y not world Y
    public const int CLOUD_Y_EXTENT = CLOUD_HEIGHT * 2;

    public const float CLOUD_Y_COORDINATE = 0;

    public const float CLOUD_DIFFUSION_RATE = 0.007f;

    // Should be the same as its counterpart in shaders/CompoundCloudPlane.shader
    public const float CLOUD_MAX_INTENSITY_SHOWN = 1000;

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

    public const int MICROBE_SPAWN_RADIUS = 170;
    public const int CLOUD_SPAWN_RADIUS = 170;

    public const float STARTING_SPAWN_DENSITY = 70000.0f;
    public const float MAX_SPAWN_DENSITY = 20000.0f;
    public const float MIN_SPAWN_RADIUS_RATIO = 0.95f;

    /// <summary>
    ///   The maximum force that can be applied by currents in the fluid system
    /// </summary>
    public const float MAX_FORCE_APPLIED_BY_CURRENTS = 0.0525f;

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
    ///   Max number of concurrent audio players that may be spawned per entity.
    /// </summary>
    public const int MAX_CONCURRENT_SOUNDS_PER_ENTITY = 10;

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

    public const float CHUNK_VENT_COMPOUND_MULTIPLIER = 3000.0f;

    public const float MICROBE_VENT_COMPOUND_MULTIPLIER = 10000.0f;

    public const float FLOATING_CHUNKS_DISSOLVE_SPEED = 0.3f;

    public const float MEMBRANE_DISSOLVE_SPEED = 0.3f;

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
    ///   Cost of moving the rigidity slider by one step in the microbe editor
    /// </summary>
    public const int MEMBRANE_RIGIDITY_COST_PER_STEP = 2;

    /// <summary>
    ///   Number used to convert between the value from the rigidity slider and the actual value
    /// </summary>
    public const float MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO = 10;

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

    public const int PLAYER_DEATH_POPULATION_LOSS_CONSTANT = -20;
    public const float PLAYER_DEATH_POPULATION_LOSS_COEFFICIENT = 1 / 1.5f;
    public const int PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT = 50;
    public const float PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT = 1.2f;

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

    // Bacterial Colony configuration
    public const int MIN_BACTERIAL_COLONY_SIZE = 2;
    public const int MAX_BACTERIAL_COLONY_SIZE = 6;
    public const int MIN_BACTERIAL_LINE_SIZE = 3;
    public const int MAX_BACTERIAL_LINE_SIZE = 7;

    // What is divided during fear and aggression calculations in the AI
    public const float AGRESSION_DIVISOR = 25.0f;
    public const float FEAR_DIVISOR = 25.0f;
    public const float ACTIVITY_DIVISOR = 100.0f;
    public const float FOCUS_DIVISOR = 100.0f;
    public const float OPPORTUNISM_DIVISOR = 100.0f;

    // Cooldown for AI for toggling engulfing
    public const float AI_ENGULF_INTERVAL = 300;

    // if you are gaining less then this amount of compound per turn you are much more likely to turn randomly
    public const float AI_COMPOUND_BIAS = -10.0f;

    public const float AI_BASE_MOVEMENT = 1.0f;
    public const float AI_FOCUSED_MOVEMENT = 1.0f;

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

    public const int MAX_SPAWNS_PER_FRAME = 2;
    public const int MAX_DESPAWNS_PER_FRAME = 2;

    public const float TIME_BEFORE_TUTORIAL_CAN_PAUSE = 0.01f;

    public const float MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY = 15.0f;
    public const float MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME = 2.2f;
    public const float TUTORIAL_COMPOUND_POSITION_UPDATE_INTERVAL = 0.2f;
    public const float GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE = 0.14f;
    public const float GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE = 0.21f;
    public const float MICROBE_REPRODUCTION_TUTORIAL_DELAY = 180;
    public const float HIDE_MICROBE_STAYING_ALIVE_TUTORIAL_AFTER = 60;

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

    /// <summary>
    ///   All Nodes tagged with this are considered Microbes that the AI can react to
    /// </summary>
    public const string AI_TAG_MICROBE = "microbe";

    /// <summary>
    ///   All Nodes tagged with this are considered FloatingChunks that the AI can react to
    /// </summary>
    public const string AI_TAG_CHUNK = "chunk";

    public const string DELETION_HOLD_LOAD = "load";
    public const string DELETION_HOLD_MICROBE_EDITOR = "microbe_editor";

    public const string CONFIGURATION_FILE = "user://thrive_settings.json";

    public const string SAVE_FOLDER = "user://saves";

    public const string SCREENSHOT_FOLDER = "user://screenshots";

    public const string LOGS_FOLDER_NAME = "logs";

    /// <summary>
    ///   This is just here to make it easier to debug saves
    /// </summary>
    public const Formatting SAVE_FORMATTING = Formatting.None;

    public const string SAVE_EXTENSION = "thrivesave";
    public const string SAVE_EXTENSION_WITH_DOT = "." + SAVE_EXTENSION;

    public const int SAVE_LIST_SCREENSHOT_HEIGHT = 720;

    public const int KIBIBYTE = 1024;
    public const int MEBIBYTE = 1024 * KIBIBYTE;

    public static string Version
    {
        get
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var versionSuffix =
                    (AssemblyInformationalVersionAttribute[])assembly.GetCustomAttributes(
                        typeof(AssemblyInformationalVersionAttribute), false);
                return $"{version}" + versionSuffix[0].InformationalVersion;
            }
            catch (Exception error)
            {
                GD.Print("Error getting version: ", error);
                return "error (" + error.GetType().Name + ")";
            }
        }
    }
}
