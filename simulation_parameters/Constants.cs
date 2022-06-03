﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Path = System.IO.Path;

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
    ///   The (default) size of the hexagons, used in calculations. Don't change this.
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

    // Should be the same as its counterpart in shaders/CompoundCloudPlane.shader
    public const float CLOUD_NOISE_UV_OFFSET_MULTIPLIER = 2.5f;

    public const float CLOUD_CHEAT_DENSITY = 16000.0f;

    public const int MEMBRANE_RESOLUTION = 10;

    /// <summary>
    ///   BASE MOVEMENT ATP cost. Cancels out a little bit more then one cytoplasm's glycolysis
    /// </summary>
    /// <remarks>
    ///   this is applied *per* hex
    /// </remarks>
    public const float BASE_MOVEMENT_ATP_COST = 1.0f;

    public const float FLAGELLA_ENERGY_COST = 4.0f;

    public const float FLAGELLA_BASE_FORCE = 75.7f;

    public const float CELL_BASE_THRUST = 50.6f;

    public const float MICROBE_MOVEMENT_SOUND_EMIT_COOLDOWN = 1.3f;

    public const float CELL_BASE_ROTATION = 0.2f;
    public const float CELL_MAX_ROTATION = 0.40f;
    public const float CELL_MIN_ROTATION = 0.005f;
    public const float CELL_MOMENT_OF_INERTIA_DISTANCE_MULTIPLIER = 0.5f;
    public const float CILIA_ROTATION_FACTOR = 0.008f;
    public const float CILIA_RADIUS_FACTOR_MULTIPLIER = 0.7f;

    public const float CELL_COLONY_MAX_ROTATION_MULTIPLIER = 2.5f;
    public const float CELL_COLONY_MIN_ROTATION_MULTIPLIER = 0.05f;
    public const float CELL_COLONY_MAX_ROTATION_HELP = 2.5f;
    public const float CELL_COLONY_MEMBER_ROTATION_FACTOR_MULTIPLIER = 45.0f;

    public const float CILIA_ENERGY_COST = 2.0f;
    public const float CILIA_ROTATION_NEEDED_FOR_ATP_COST = 0.03f;
    public const float CILIA_ROTATION_ENERGY_BASE_MULTIPLIER = 4.0f;

    public const float CILIA_DEFAULT_ANIMATION_SPEED = 0.3f;
    public const float CILIA_MIN_ANIMATION_SPEED = 0.15f;
    public const float CILIA_MAX_ANIMATION_SPEED = 1.2f;
    public const float CILIA_ROTATION_ANIMATION_SPEED_MULTIPLIER = 7.0f;
    public const float CILIA_ROTATION_SAMPLE_INTERVAL = 0.1f;

    public const int PROCESS_OBJECTS_PER_TASK = 15;

    public const int MICROBE_SPAWN_RADIUS = 170;
    public const int CLOUD_SPAWN_RADIUS = 170;

    /// <summary>
    ///   Extra radius added to the spawn radius of things to allow them to move in the "wrong" direction a bit
    ///   without causing them to despawn instantly. Things despawn outside the despawn radius.
    /// </summary>
    public const int DESPAWN_RADIUS_OFFSET = 50;

    public const int MICROBE_DESPAWN_RADIUS_SQUARED = (MICROBE_SPAWN_RADIUS + DESPAWN_RADIUS_OFFSET) *
        (MICROBE_SPAWN_RADIUS + DESPAWN_RADIUS_OFFSET);

    public const float STARTING_SPAWN_DENSITY = 70000.0f;
    public const float MAX_SPAWN_DENSITY = 20000.0f;
    public const float MIN_SPAWN_RADIUS_RATIO = 0.95f;

    /// <summary>
    ///   Radius of the zone where the player is considered immobile as he remains inside.
    ///   Used to not overgenerate when the player doesn't move.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The value is squared for faster computation.
    ///   </para>
    ///   <para>
    ///     The non-squared radius should roughly be (1-MIN_SPAWN_RADIUS_RATIO)*max(spawn_radius), as defined above,
    ///     to make spawn zone match when moving.
    ///   </para>
    /// </remarks>
    public const int PLAYER_IMMOBILITY_ZONE_RADIUS_SQUARED = 100;

    /// <summary>
    ///   The maximum force that can be applied by currents in the fluid system
    /// </summary>
    public const float MAX_FORCE_APPLIED_BY_CURRENTS = 0.0525f;

    public const int TRANSLATION_VERY_INCOMPLETE_THRESHOLD = 30;
    public const int TRANSLATION_INCOMPLETE_THRESHOLD = 70;

    /// <summary>
    ///   How often the microbe AI processes each microbe
    /// </summary>
    public const float MICROBE_AI_THINK_INTERVAL = 0.3f;

    /// <summary>
    ///   This is how often the AI microbes look for emitted signaling agent signals from members of their species.
    ///   This is set pretty high to reduce the performance impact.
    /// </summary>
    public const float MICROBE_AI_SIGNAL_REACT_INTERVAL = 1.2f;

    public const int MICROBE_AI_OBJECTS_PER_TASK = 12;

    public const int INITIAL_SPECIES_POPULATION = 100;

    public const int INITIAL_FREEBUILD_POPULATION_VARIANCE_MIN = 0;
    public const int INITIAL_FREEBUILD_POPULATION_VARIANCE_MAX = 400;

    // Right now these are used for species split from the player
    public const int INITIAL_SPLIT_POPULATION_MIN = 600;
    public const int INITIAL_SPLIT_POPULATION_MAX = 2000;

    /// <summary>
    ///   If true a mutated copy of the (player) species is created when entering the editor
    /// </summary>
    public const bool CREATE_COPY_OF_EDITED_SPECIES = false;

    /// <summary>
    ///   Max number of concurrent audio players that may be spawned per entity.
    /// </summary>
    public const int MAX_CONCURRENT_SOUNDS_PER_ENTITY = 10;

    /// <summary>
    ///   Max number of concurrent audio players that may be spawned for UI sounds.
    /// </summary>
    public const int MAX_CONCURRENT_UI_AUDIO_PLAYERS = 10;

    public const float CONTACT_IMPULSE_TO_BUMP_SOUND = 8;

    /// <summary>
    ///   Controls with how much force agents are fired
    /// </summary>
    public const float AGENT_EMISSION_IMPULSE_STRENGTH = 20.0f;

    public const float OXYTOXY_DAMAGE = 15.0f;

    /// <summary>
    ///   Delay when a toxin hits or expires until it is destroyed. This is used to give some time for the effect to
    ///   fade so this must always be at least as long as how long the despawn effect takes visually
    /// </summary>
    public const float PROJECTILE_DESPAWN_DELAY = 3;

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
    ///   The minimum amount of oxytoxy (or any agent) fired in one shot.
    /// </summary>
    public const float MINIMUM_AGENT_EMISSION_AMOUNT = MathUtils.EPSILON;

    /// <summary>
    ///   The maximum amount of oxytoxy (or any agent) fired in one shot.
    /// </summary>
    public const float MAXIMUM_AGENT_EMISSION_AMOUNT = 2.0f;

    /// <summary>
    ///   The time (in seconds) it takes a cloud being absorbed to halve its compounds.
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

    /// <summary>
    ///   Limits how often floating chunks are processed to save on some performance
    /// </summary>
    public const float FLOATING_CHUNK_PROCESS_INTERVAL = 0.05f;

    /// <summary>
    ///   If more chunks exist at once than this, then some are forced to dissolve immediately
    /// </summary>
    public const int FLOATING_CHUNK_MAX_COUNT = 35;

    public const float CHUNK_VENT_COMPOUND_MULTIPLIER = 3000.0f;

    public const float MICROBE_VENT_COMPOUND_MULTIPLIER = 10000.0f;

    public const float FLOATING_CHUNKS_DISSOLVE_SPEED = 0.3f;

    public const int DESPAWNING_CHUNK_LIFETIME = 150;

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
    public const float REGENERATION_RATE = 1.5f;

    /// <summary>
    ///   How often in seconds ATP damage is checked and applied if cell has no ATP
    /// </summary>
    public const float ATP_DAMAGE_CHECK_INTERVAL = 0.9f;

    public const float MICROBE_REPRODUCTION_PROGRESS_INTERVAL = 0.05f;

    /// <summary>
    ///   Determines how big of a fraction of damage (of total health)
    ///   is dealt to a microbe at a time when it is out of ATP.
    /// </summary>
    public const float NO_ATP_DAMAGE_FRACTION = 0.04f;

    /// <summary>
    ///   Organelles won't take compounds if there is less available than this amount
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is no longer zero as rounding can otherwise make compounds just disappear
    ///   </para>
    /// </remarks>
    public const float ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST = 0.0001f;

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
    public const float ENGULFING_ATP_COST_PER_SECOND = 1.5f;

    /// <summary>
    ///   The speed reduction when a cell is in engulfing mode.
    /// </summary>
    public const float ENGULFING_MOVEMENT_DIVISION = 1.7f;

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
    ///   How much ATP does binding mode cost per second
    /// </summary>
    public const float BINDING_ATP_COST_PER_SECOND = 2.0f;

    /// <summary>
    ///   Damage a single pilus stab does
    /// </summary>
    public const float PILUS_BASE_DAMAGE = 3.0f;

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
    public const int ORGANELLE_MOVE_COST = 5;

    public const float COLONY_DIVIDE_EXTRA_DAUGHTER_OFFSET = 1;

    // Corpse info
    public const float CORPSE_COMPOUND_COMPENSATION = 8.0f;
    public const int CORPSE_CHUNK_DIVISOR = 3;
    public const float CORPSE_CHUNK_AMOUNT_DIVISOR = 3.0f;
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

    public const float CHEMORECEPTOR_RANGE_MIN = 2;
    public const float CHEMORECEPTOR_RANGE_MAX = 700;
    public const float CHEMORECEPTOR_RANGE_DEFAULT = 350;
    public const float CHEMORECEPTOR_AMOUNT_MIN = 1;
    public const float CHEMORECEPTOR_AMOUNT_MAX = 5000;
    public const float CHEMORECEPTOR_AMOUNT_DEFAULT = 100;
    public const float CHEMORECEPTOR_COMPOUND_UPDATE_INTERVAL = 0.25f;
    public const string CHEMORECEPTOR_DEFAULT_COMPOUND_NAME = "glucose";

    /// <summary>
    ///   Size, in radians, of the gaps between directions the chemoreceptor checks for compounds
    /// </summary>
    public const double CHEMORECEPTOR_ARC_SIZE = Math.PI / 24.0;

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
    public const float MUTATION_BACTERIA_TO_EUKARYOTE = 0.01f;
    public const float MUTATION_CREATION_RATE = 0.25f;
    public const float MUTATION_NEW_ORGANELLE_CHANCE = 0.25f;
    public const float MUTATION_DELETION_RATE = 0.05f;
    public const float MUTATION_REPLACEMENT_RATE = 0.1f;

    // Max fear and aggression and activity
    public const float MAX_SPECIES_AGGRESSION = 400.0f;
    public const float MAX_SPECIES_FEAR = 400.0f;
    public const float MAX_SPECIES_ACTIVITY = 400.0f;
    public const float MAX_SPECIES_FOCUS = 400.0f;
    public const float MAX_SPECIES_OPPORTUNISM = 400.0f;

    public const float DEFAULT_BEHAVIOUR_VALUE = 100.0f;

    // Bacterial Colony configuration
    public const int MIN_BACTERIAL_COLONY_SIZE = 2;
    public const int MAX_BACTERIAL_COLONY_SIZE = 6;
    public const int MIN_BACTERIAL_LINE_SIZE = 3;
    public const int MAX_BACTERIAL_LINE_SIZE = 7;

    // What is divided during fear and aggression calculations in the AI
    public const float AGGRESSION_DIVISOR = 25.0f;
    public const float FEAR_DIVISOR = 25.0f;
    public const float ACTIVITY_DIVISOR = 100.0f;
    public const float FOCUS_DIVISOR = 100.0f;
    public const float OPPORTUNISM_DIVISOR = 100.0f;

    // Cooldown for AI for toggling engulfing
    public const float AI_ENGULF_INTERVAL = 300;

    // Average number of calls to think method before doing expensive cloud-finding calculations
    public const int AI_STEPS_PER_SMELL = 20;

    // if you are gaining less then this amount of compound per turn you are much more likely to turn randomly
    public const float AI_COMPOUND_BIAS = -10.0f;

    /// <summary>
    ///   Threshold to not be stuck in tiny local maxima during gradient ascent algorithms.
    /// </summary>
    public const float AI_GRADIENT_DETECTION_THRESHOLD = 0.005f;

    public const float AI_BASE_MOVEMENT = 1.0f;
    public const float AI_FOCUSED_MOVEMENT = 1.0f;
    public const float AI_ENGULF_STOP_DISTANCE = 0.8f;

    public const float AI_FOLLOW_DISTANCE_SQUARED = 60 * 60;
    public const float AI_FLEE_DISTANCE_SQUARED = 85 * 85;

    // Personality Mutation
    public const float MAX_SPECIES_PERSONALITY_MUTATION = 40.0f;
    public const float MIN_SPECIES_PERSONALITY_MUTATION = -40.0f;

    // Genus splitting and name mutation
    public const int MUTATION_WORD_EDIT = 10;
    public const int DIFFERENCES_FOR_GENUS_SPLIT = 1;

    /// <summary>
    ///   How many steps forward of the population simulation to do when auto-evo looks at the results of mutations
    ///   etc. for which is the most beneficial
    /// </summary>
    public const int AUTO_EVO_VARIANT_SIMULATION_STEPS = 15;

    /// <summary>
    ///   Populations of species that are under this will be killed off by auto-evo
    /// </summary>
    public const int AUTO_EVO_MINIMUM_VIABLE_POPULATION = 20;

    // Auto evo population algorithm tweak variables
    // TODO: move all of these into auto-evo_parameters.json
    public const int AUTO_EVO_MINIMUM_MOVE_POPULATION = 200;
    public const float AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION = 0.1f;
    public const float AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION = 0.8f;
    public const float AUTO_EVO_ATP_USE_SCORE_MULTIPLIER = 0.0033f;
    public const float AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER = 20;
    public const float AUTO_EVO_ENGULF_PREDATION_SCORE = 100;
    public const float AUTO_EVO_PILUS_PREDATION_SCORE = 20;
    public const float AUTO_EVO_TOXIN_PREDATION_SCORE = 100;
    public const float AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY = 0.1f;
    public const float AUTO_EVO_CHUNK_LEAK_MULTIPLIER = 0.1f;
    public const float AUTO_EVO_PREDATION_ENERGY_MULTIPLIER = 0.4f;
    public const float AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT = 100000;
    public const float AUTO_EVO_COMPOUND_ENERGY_AMOUNT = 2400;
    public const float AUTO_EVO_CHUNK_ENERGY_AMOUNT = 90000000;
    public const float AUTO_EVO_CHUNK_AMOUNT_NERF = 0.01f;
    public const int AUTO_EVO_MINIMUM_SPECIES_SIZE_BEFORE_SPLIT = 80;
    public const bool AUTO_EVO_ALLOW_SPECIES_SPLIT_ON_NO_MUTATION = true;

    public const double AUTO_EVO_COMPOUND_RATIO_POWER_BIAS = 1;
    public const double AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS = 0.5;

    /// <summary>
    ///   How much auto-evo affects the player species compared to the normal amount
    /// </summary>
    public const float AUTO_EVO_PLAYER_STRENGTH_FRACTION = 0.2f;

    public const int EDITOR_TIME_JUMP_MILLION_YEARS = 100;

    public const float GLUCOSE_REDUCTION_RATE = 0.8f;
    public const float GLUCOSE_MIN = 0.0f;

    // These control how many game entities can exist at once and how fast they are allowed to spawn / despawn
    // TODO: bump this back up once we resolve the performance bottleneck
    public const int DEFAULT_MAX_SPAWNED_ENTITIES = 140;
    public const int MAX_SPAWNS_PER_FRAME = 1;

    /// <summary>
    ///   Delete a max of this many entities per step to reduce lag from deleting tons of entities at once.
    /// </summary>
    public const int MAX_DESPAWNS_PER_FRAME = 2;

    /// <summary>
    ///   How often despawns happen on top of the normal despawns that are part of the spawn cycle
    /// </summary>
    public const float DESPAWN_INTERVAL = 0.08f;

    public const float CHANCE_MULTICELLULAR_SPAWNS_GROWN = 0.1f;
    public const float CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN = 0.3f;
    public const float CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE = 0.4f;

    public const float TIME_BEFORE_TUTORIAL_CAN_PAUSE = 0.01f;

    public const float MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY = 17.0f;
    public const float MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME = 2.2f;
    public const float TUTORIAL_COMPOUND_POSITION_UPDATE_INTERVAL = 0.2f;
    public const float GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE = 0.14f;
    public const float GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE = 0.21f;
    public const float MICROBE_REPRODUCTION_TUTORIAL_DELAY = 180;
    public const float HIDE_MICROBE_STAYING_ALIVE_TUTORIAL_AFTER = 60;
    public const float MICROBE_EDITOR_BUTTON_TUTORIAL_DELAY = 20;

    /// <summary>
    ///   Used to limit how often the hover indicator panel are
    ///   updated. Default value is every 0.1 seconds.
    /// </summary>
    public const float HOVER_PANEL_UPDATE_INTERVAL = 0.1f;

    public const float TOOLTIP_OFFSET = 20;
    public const float TOOLTIP_DEFAULT_DELAY = 1.0f;
    public const float TOOLTIP_FADE_SPEED = 0.25f;

    public const float EDITOR_ARROW_OFFSET = 3.5f;
    public const float EDITOR_ARROW_INTERPOLATE_SPEED = 0.5f;

    public const float EDITOR_DEFAULT_CAMERA_HEIGHT = 10;

    public const float MULTICELLULAR_EDITOR_PREVIEW_MICROBE_SCALE_MULTIPLIER = 0.80f;

    /// <summary>
    ///   Scale used for one frame while membrane data is not ready yet
    /// </summary>
    public const float MULTICELLULAR_EDITOR_PREVIEW_PLACEHOLDER_SCALE = 0.18f;

    /// <summary>
    ///   Multiplier for cell editor actions in multicellular editor
    /// </summary>
    public const float MULTICELLULAR_EDITOR_COST_FACTOR = 0.5f;

    public const float MINIMUM_RUNNABLE_PROCESS_FRACTION = 0.00001f;

    public const float DEFAULT_PROCESS_SPINNER_SPEED = 365.0f;
    public const float DEFAULT_PROCESS_STATISTICS_AVERAGE_INTERVAL = 0.4f;

    public const int COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR = 5;
    public const int COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC = 20;

    /// <summary>
    ///   Main menu cancel priority. Main menu handles the cancel action for sub menus that don't have special needs
    ///   regarding exiting them <see cref="PAUSE_MENU_CANCEL_PRIORITY"/>
    /// </summary>
    public const int MAIN_MENU_CANCEL_PRIORITY = -3;

    /// <summary>
    ///   Pause menu has lower cancel priority to avoid handling canceling being in the menu if a an open sub menu
    ///   has special actions it needs to do
    /// </summary>
    public const int PAUSE_MENU_CANCEL_PRIORITY = -2;

    public const int SUBMENU_CANCEL_PRIORITY = -1;

    /// <summary>
    ///   Popups have a highest priority to ensure they can react first.
    /// </summary>
    public const int POPUP_CANCEL_PRIORITY = int.MaxValue;

    /// <summary>
    ///   Maximum amount of snapshots to store in patch history.
    /// </summary>
    public const int PATCH_HISTORY_RANGE = 10;

    /// <summary>
    ///   The maximum limit for amount of events by time period to store in <see cref="GameWorld"/>.
    /// </summary>
    public const int GLOBAL_EVENT_LOG_CAP = 20;

    /// <summary>
    ///   Extra margin used to show cells that the player hovers over with the mouse. This is done to make it easier
    ///   to see what small cells are.
    ///   Specifically for use with LengthSquared.
    /// </summary>
    public const float MICROBE_HOVER_DETECTION_EXTRA_RADIUS_SQUARED = 2 * 2;

    public const float PROCEDURAL_CACHE_CLEAN_INTERVAL = 9.3f;
    public const float PROCEDURAL_CACHE_MEMBRANE_KEEP_TIME = 500;

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
    ///   All Nodes tagged with this are handled by the process system. Can't be just "process" as that conflicts with
    ///   godot idle_process and process, at least I think it does.
    /// </summary>
    public const string PROCESS_GROUP = "run_processes";

    /// <summary>
    ///   All Nodes tagged with this are handled by the ai system
    /// </summary>
    public const string AI_GROUP = "ai";

    /// <summary>
    ///   All Nodes tagged with this are considered Microbes that the AI can react to
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: quite a few of these AI_TAG starting constants need to be renamed as these are generally used to
    ///     find relevant entities for things that aren't the AI system
    ///   </para>
    /// </remarks>
    public const string AI_TAG_MICROBE = "microbe";

    /// <summary>
    ///   All Nodes tagged with this are considered FloatingChunks that the AI can react to
    /// </summary>
    public const string AI_TAG_CHUNK = "chunk";

    public const string DELETION_HOLD_LOAD = "load";
    public const string DELETION_HOLD_MICROBE_EDITOR = "microbe_editor";

    public const string CONFIGURATION_FILE = "user://thrive_settings.json";
    public const string WORKSHOP_DATA_FILE = "user://workshop_data.json";

    public const string SAVE_FOLDER = "user://saves";

    public const string EXPLICIT_PATH_PREFIX = "file://";

    public const int MAX_PATH_LENGTH = 1024;

    public const string SCREENSHOT_FOLDER = "user://screenshots";

    public const string LOGS_FOLDER_NAME = "logs";
    public const string LOGS_FOLDER = "user://" + LOGS_FOLDER_NAME;

    public const string JSON_DEBUG_OUTPUT_FILE = LOGS_FOLDER + "/json_debug.txt";

    public const string LICENSE_FILE = "res://LICENSE.txt";
    public const string STEAM_LICENSE_FILE = "res://doc/steam_license_readme.txt";
    public const string ASSETS_README = "res://assets/README.txt";
    public const string ASSETS_LICENSE_FILE = "res://assets/LICENSE.txt";
    public const string GODOT_LICENSE_FILE = "res://doc/GodotLicense.txt";
    public const string OFL_LICENSE_FILE = "res://assets/OFL.txt";
    public const string GPL_LICENSE_FILE = "res://gpl.txt";

    public const string ASSETS_GUI_BEVEL_FOLDER = "res://assets/textures/gui/bevel";

    /// <summary>
    ///   Internal Godot name for the default audio output device
    /// </summary>
    public const string DEFAULT_AUDIO_OUTPUT_DEVICE_NAME = "Default";

    /// <summary>
    ///   This is just here to make it easier to debug saves
    /// </summary>
    public const Formatting SAVE_FORMATTING = Formatting.None;

    /// <summary>
    ///   If set to false, saving related errors are re-thrown to make debugging easier
    /// </summary>
    public const bool CATCH_SAVE_ERRORS = true;

    /// <summary>
    ///   JSON traces longer than this are not printed to the console
    /// </summary>
    public const int MAX_JSON_ERROR_LENGTH_FOR_CONSOLE = 20000;

    public const string FILE_NAME_DISALLOWED_CHARACTERS = "<>:\"/\\|?*\0";
    public const string SAVE_EXTENSION = "thrivesave";
    public const string SAVE_EXTENSION_WITH_DOT = "." + SAVE_EXTENSION;
    public const string SAVE_BACKUP_SUFFIX = ".backup" + SAVE_EXTENSION_WITH_DOT;

    public const int SAVE_LIST_SCREENSHOT_HEIGHT = 720;

    public const int KIBIBYTE = 1024;
    public const int MEBIBYTE = 1024 * KIBIBYTE;

    /// <summary>
    ///   Delay for the compound row to hide when standing still and compound amount is 0.
    /// </summary>
    public const float COMPOUND_HOVER_INFO_REMOVE_DELAY = 0.5f;

    /// <summary>
    ///   Compound changes below this value are ignored while mouse world position doesn't change.
    /// </summary>
    public const float COMPOUND_HOVER_INFO_THRESHOLD = 2.5f;

    /// <summary>
    ///   Minimum amount for the very little category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_VERY_LITTLE = 0.5f;

    /// <summary>
    ///   Minimum amount for the little category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_LITTLE = 10f;

    /// <summary>
    ///   Minimum amount for the some category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_SOME = 50f;

    /// <summary>
    ///   Minimum amount for the fair amount category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT = 200f;

    /// <summary>
    ///   Minimum amount for the quite a bit category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT = 400f;

    /// <summary>
    ///   Minimum amount for the an abundance category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_AN_ABUNDANCE = 600f;

    public const float PHOTO_STUDIO_CAMERA_FOV = 70;
    public const float PHOTO_STUDIO_CAMERA_HALF_ANGLE = PHOTO_STUDIO_CAMERA_FOV / 2.0f;
    public const float PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER = 0.80f;

    /// <summary>
    ///   Regex for species name validation.
    /// </summary>
    public const string SPECIES_NAME_REGEX = "^(?<genus>[a-zA-Z0-9]+) (?<epithet>[a-zA-Z0-9]+)$";

    public const string MOD_INFO_FILE_NAME = "thrive_mod.json";

    /// <summary>
    ///   Minimum hex distance before the same render priority.
    /// </summary>
    public const int HEX_RENDER_PRIORITY_DISTANCE = 4;

    public const float COLOUR_PICKER_PICK_INTERVAL = 0.2f;

    public const string DISABLE_VIDEOS_LAUNCH_OPTION = "--thrive-disable-videos";

    /// <summary>
    ///   The duration for which a save is considered recently performed.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Not a const because TimeSpan is not a primitive.
    ///   </para>
    /// </remarks>
    public static readonly TimeSpan RecentSaveTime = TimeSpan.FromSeconds(15);

    /// <summary>
    ///   Locations mods are searched in. The last location is considered to be the user openable and editable folder
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     These must be preprocessed with GlobalizePath as otherwise relative paths will break when the first mod
    ///     .pck file is loaded.
    ///     TODO: might be nice to move to some other place as this got pretty long and complicated due to pck loading
    ///     messing with the current working directory.
    ///   </para>
    /// </remarks>
    public static readonly IReadOnlyList<string> ModLocations = new[]
    {
        OS.HasFeature("standalone") ?
            Path.Combine(
                Path.GetDirectoryName(OS.GetExecutablePath()) ??
                throw new InvalidOperationException("no current executable path"), "mods") :
            ProjectSettings.GlobalizePath("res://mods"),
        "user://mods",
    };

    // Following is a hacky way to ensure some conditions apply on the constants defined here.
    // When the constants don't follow a set of conditions a warning is raised, which CI treats as an error.
    // Or maybe it raises an actual error. Anyway this seems good enough for now to do some stuff

#pragma warning disable CA1823 // unused fields

    // ReSharper disable UnreachableCode HeuristicUnreachableCode
    private const uint MinimumMovePopIsHigherThanMinimumViable =
        (AUTO_EVO_MINIMUM_MOVE_POPULATION * AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION >=
            AUTO_EVO_MINIMUM_VIABLE_POPULATION) ?
            0 :
            -42;

    private const uint MinimumRunnableProcessFractionIsAboveEpsilon =
        (MINIMUM_RUNNABLE_PROCESS_FRACTION > MathUtils.EPSILON) ? 0 : -42;

    // ReSharper restore UnreachableCode HeuristicUnreachableCode
#pragma warning restore CA1823

    /// <summary>
    ///   This needs to be a separate field to make this only be calculated once needed the first time
    /// </summary>
    private static readonly string GameVersion = FetchVersion();

    /// <summary>
    ///   Game version
    /// </summary>
    public static string Version => GameVersion;

    public static string UserFolderAsNativePath => OS.GetUserDataDir().Replace('\\', '/');

    private static string FetchVersion()
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
