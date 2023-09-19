using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using Newtonsoft.Json;
using Path = System.IO.Path;

/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public static class Constants
{
    /// <summary>
    ///   Default length in seconds for an in-game day. If this is changed, the placeholder values in
    ///   NewGameSettings.tscn should also be changed.
    /// </summary>
    public const int DEFAULT_DAY_LENGTH = 180;

    /// <summary>
    ///   How long the player stays dead before respawning
    /// </summary>
    public const float PLAYER_RESPAWN_TIME = 5.0f;

    /// <summary>
    ///   How long the initial compounds should last (in seconds)
    /// </summary>
    public const float INITIAL_COMPOUND_TIME = 40.0f;

    public const float MULTICELLULAR_INITIAL_COMPOUND_MULTIPLIER = 1.5f;

    public const int FULL_INITIAL_GLUCOSE_SMALL_SIZE_LIMIT = 3;

    /// <summary>
    ///   The maximum duration the player is shown being ingested before they are auto respawned.
    /// </summary>
    public const float PLAYER_ENGULFED_DEATH_DELAY_MAX = 10.0f;

    // Variance in the player position when respawning
    public const float MIN_SPAWN_DISTANCE = -5000.0f;
    public const float MAX_SPAWN_DISTANCE = 5000.0f;

    /// <summary>
    ///   Size of "chunks" used for spawning entities
    /// </summary>
    public const float SPAWN_SECTOR_SIZE = 120.0f;

    public const float MIN_DISTANCE_FROM_PLAYER_FOR_SPAWN = SPAWN_SECTOR_SIZE - 10;

    /// <summary>
    ///   Scale factor for density of compound cloud spawns
    /// </summary>
    public const int CLOUD_SPAWN_DENSITY_SCALE_FACTOR = 10000;

    /// <summary>
    ///   Scale factor for amount of compound in each spawned cloud
    /// </summary>
    public const float CLOUD_SPAWN_AMOUNT_SCALE_FACTOR = 0.75f;

    /// <summary>
    ///   Threshold under which entities start to spawn around the player
    ///   The value is in the range 0-1 and is the fraction of the maximum
    ///   allowed entities.
    /// </summary>
    public const float ENTITY_SPAWNING_AROUND_PLAYER_THRESHOLD = 0.8f;

    /// <summary>
    ///   Scale factor for how dense microbes spawn (also affected by their populations).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Due to an earlier problem, old species spawners were never cleared so they accumulated a lot.
    ///     This multiplier has now been increased quite a bit to try to make the lower number of species spawners
    ///     result in the same level of microbe spawning.
    ///   </para>
    /// </remarks>
    public const float MICROBE_SPAWN_DENSITY_SCALE_FACTOR = 0.022f;

    /// <summary>
    ///   Along with <see cref="MICROBE_SPAWN_DENSITY_SCALE_FACTOR"/> affects spawn density of microbes.
    ///   The lower this multiplier is set the more evenly species with different populations are spawned.
    /// </summary>
    public const float MICROBE_SPAWN_DENSITY_POPULATION_MULTIPLIER = 1 / 25.0f;

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

    public const float CLOUD_CHEAT_DENSITY = 16000.0f;

    public const int MEMBRANE_RESOLUTION = 10;

    public const float MEMBRANE_ROOM_FOR_ORGANELLES = 1.9f;
    public const float MEMBRANE_NUMBER_OF_WAVES = 9.0f;
    public const float MEMBRANE_WAVE_HEIGHT_DEPENDENCE_ON_SIZE = 0.3f;
    public const float MEMBRANE_WAVE_HEIGHT_MULTIPLIER = 0.025f;
    public const float MEMBRANE_WAVE_HEIGHT_MULTIPLIER_CELL_WALL = 0.015f;

    /// <summary>
    ///   BASE MOVEMENT ATP cost. Cancels out a little bit more then one cytoplasm's glycolysis
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     this is applied *per* hex
    ///   </para>
    /// </remarks>
    public const float BASE_MOVEMENT_ATP_COST = 1.0f;

    public const float FLAGELLA_ENERGY_COST = 4.0f;

    public const float FLAGELLA_BASE_FORCE = 75.7f;

    public const float BASE_MOVEMENT_FORCE = 910.0f;

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

    public const float CILIA_PULLING_FORCE_FIELD_RADIUS = 8.5f;
    public const float CILIA_PULLING_FORCE_GROW_STEP = 2.0f;
    public const float CILIA_PULLING_FORCE = 20.0f;
    public const float CILIA_PULLING_FORCE_FALLOFF_FACTOR = 0.1f;
    public const float CILIA_CURRENT_GENERATION_ANIMATION_SPEED = 5.0f;

    public const int MICROBE_SPAWN_RADIUS = 350;
    public const int CLOUD_SPAWN_RADIUS = 350;

    /// <summary>
    ///   This controls how many entities over the entity limit we allow things to reproduce. This is so that even when
    ///   the spawn system has spawned things until the limit is full, the spawned things can still reproduce.
    /// </summary>
    public const float REPRODUCTION_ALLOW_EXCEED_ENTITY_LIMIT_MULTIPLIER = 1.15f;

    /// <summary>
    ///   If the entity limit is over this once the player has reproduced, force despawning will happen
    /// </summary>
    public const float REPRODUCTION_PLAYER_ALLOWED_ENTITY_LIMIT_EXCEED = 1.25f;

    /// <summary>
    ///   Once reproduced player copies take this much or more of the overall entity limit, they are preferred to
    ///   despawn first.
    /// </summary>
    public const float PREFER_DESPAWN_PLAYER_REPRODUCED_COPY_AFTER = 0.30f;

    /// <summary>
    ///   Multiplier for how much cells in a colony contribute to the entity limit. Actually colonies seem quite a bit
    ///   heavier than normal microbes, as such this is set pretty high.
    /// </summary>
    public const float MICROBE_COLONY_MEMBER_ENTITY_WEIGHT_MULTIPLIER = 0.95f;

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
    public const float MAX_FORCE_APPLIED_BY_CURRENTS = 5.25f;

    public const int TRANSLATION_VERY_INCOMPLETE_THRESHOLD = 30;
    public const int TRANSLATION_INCOMPLETE_THRESHOLD = 70;

    public const float LIGHT_LEVEL_UPDATE_INTERVAL = 0.1f;

    /// <summary>
    ///   How often the microbe AI processes each microbe
    /// </summary>
    public const float MICROBE_AI_THINK_INTERVAL = 0.3f;

    /// <summary>
    ///   This is how often entities for emitted signals from other entities.
    ///   This is set relatively high to reduce the performance impact. This is used for example for AI microbes to
    ///   detect signaling agents.
    /// </summary>
    public const float ENTITY_SIGNAL_UPDATE_INTERVAL = 0.15f;

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

    public const string MICROBE_MOVEMENT_SOUND = "res://assets/sounds/soundeffects/microbe-movement-ambience.ogg";
    public const string MICROBE_ENGULFING_MODE_SOUND = "res://assets/sounds/soundeffects/engulfment.ogg";
    public const string MICROBE_BINDING_MODE_SOUND = "res://assets/sounds/soundeffects/binding.ogg";

    public const float MICROBE_MOVEMENT_SOUND_MAX_VOLUME = 0.4f;

    // TODO: should this volume be actually 0?
    public const float MICROBE_MOVEMENT_SOUND_START_VOLUME = 1;

    /// <summary>
    ///   Max number of concurrent audio players that may be used per entity.
    /// </summary>
    public const int MAX_CONCURRENT_SOUNDS_PER_ENTITY = 10;

    public const float MICROBE_SOUND_MAX_DISTANCE = 100;
    public const float MICROBE_SOUND_MAX_DISTANCE_SQUARED = MICROBE_SOUND_MAX_DISTANCE * MICROBE_SOUND_MAX_DISTANCE;

    public const int MAX_CONCURRENT_SOUNDS = 100;

    /// <summary>
    ///   Max number of concurrent audio players that may be spawned for UI sounds.
    /// </summary>
    public const int MAX_CONCURRENT_UI_AUDIO_PLAYERS = 10;

    public const float CONTACT_PENETRATION_TO_BUMP_SOUND = 0.1f;

    public const float INTERVAL_BETWEEN_SOUND_CACHE_CLEAR = 0.321f;

    /// <summary>
    ///   How long to keep a played sound in memory in case it will be shortly played again
    /// </summary>
    public const float DEFAULT_SOUND_CACHE_TIME = 30;

    /// <summary>
    ///   Controls with how much speed agents are fired
    /// </summary>
    public const float AGENT_EMISSION_VELOCITY = 10.0f;

    public const float OXYTOXY_DAMAGE = 15.0f;

    /// <summary>
    ///   How much a cell's speed is slowed when travelling through slime
    /// </summary>
    public const float MUCILAGE_IMPEDE_FACTOR = 4.0f;

    /// <summary>
    ///   How much a cell's speed is increased when secreting slime (scaling with secreted compound amount)
    /// </summary>
    public const float MUCILAGE_JET_FACTOR = 600.0f;

    /// <summary>
    ///   Minimum stored slime needed to start secreting
    /// </summary>
    public const float MUCILAGE_MIN_TO_VENT = 0.01f;

    /// <summary>
    ///   Length in seconds for slime secretion cooldown
    /// </summary>
    public const float MUCILAGE_COOLDOWN_TIMER = 1.5f;

    /// <summary>
    ///   How long a toxin projectile can fly for before despawning if it doesn't hit anything before that
    /// </summary>
    public const float TOXIN_PROJECTILE_TIME_TO_LIVE = 3;

    public const float TOXIN_PROJECTILE_PHYSICS_SIZE = 1;

    public const float TOXIN_PROJECTILE_PHYSICS_DENSITY = 700;

    public const float CHUNK_PHYSICS_DAMPING = 0.1f;
    public const float MICROBE_PHYSICS_DAMPING = 0.95f;

    /// <summary>
    ///   Delay when a toxin hits or expires until it is destroyed. This is used to give some time for the effect to
    ///   fade so this must always be at least as long as how long the despawn effect takes visually
    /// </summary>
    public const float EMITTER_DESPAWN_DELAY = 3;

    public const float AGENT_EMISSION_DISTANCE_OFFSET = 0.5f;

    public const float EMITTED_AGENT_LIFETIME = 5.0f;

    public const int MAX_EMITTED_AGENTS_ON_DEATH = 5;

    /// <summary>
    ///   Percentage of the compounds that compose the organelle
    ///   released upon death (between 0.0 and 1.0).
    /// </summary>
    public const float COMPOUND_MAKEUP_RELEASE_FRACTION = 0.9f;

    public const float COMPOUND_RELEASE_FRACTION = 0.9f;

    // TODO: delete after the conversion to custom physics
    /// <summary>
    ///   Base mass all microbes have on top of their organelle masses
    /// </summary>
    public const float MICROBE_BASE_MASS = 0.7f;

    public const float PHYSICS_ALLOWED_Y_AXIS_DRIFT = 0.1f;

    /// <summary>
    ///   Buffers bigger than this number of elements will never be cached so if many entities track more than this
    ///   many collisions that's going to be bad in terms of memory allocations
    /// </summary>
    public const int MAX_COLLISION_CACHE_BUFFER_RETURN_SIZE = 50;

    /// <summary>
    ///   How many buffers of similar length can be in the collision cache. This is quite high to ensure that basically
    ///   all entities' buffers can go to the cache for example when loading a save while in game. That is required
    ///   because most entities have the exact same buffer length.
    /// </summary>
    public const int MAX_COLLISION_CACHE_BUFFERS_OF_SIMILAR_LENGHT = 500;

    /// <summary>
    ///   How many collisions each normal entity can detect at once (if more collisions happen during an update the
    ///   rest are lost and can't be detected by the game logic)
    /// </summary>
    public const int MAX_SIMULTANEOUS_COLLISIONS_SMALL = 8;

    /// <summary>
    ///   A very small limit of collisions for entities that don't need to be able to detect many collisions. Note
    ///   that this is specifically picked to be lower by a power of two than the small limit to make collision
    ///   recording buffer cache work better (as it should hopefully put these two categories to separate buckets)
    /// </summary>
    public const int MAX_SIMULTANEOUS_COLLISIONS_TINY = 4;

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

    public const float DEFAULT_MICROBE_VENT_THRESHOLD = 2.0f;

    /// <summary>
    ///   If more chunks exist at once than this, then some are forced to despawn immediately
    /// </summary>
    public const int FLOATING_CHUNK_MAX_COUNT = 50;

    public const float CHUNK_VENT_COMPOUND_MULTIPLIER = 3000.0f;

    public const float MICROBE_VENT_COMPOUND_MULTIPLIER = 10000.0f;

    public const float FLOATING_CHUNKS_DISSOLVE_SPEED = 0.3f;

    public const int DESPAWNING_CHUNK_LIFETIME = 150;

    public const float MEMBRANE_DISSOLVE_SPEED = 0.3f;

    public const float INTERACTION_BUTTONS_FULL_UPDATE_INTERVAL = 0.1f;

    public const int INTERACTION_BUTTONS_MAX_COUNT = 50;

    public const float INTERACTION_BUTTON_DEFAULT_Y_OFFSET = 1.0f;

    public const int INTERACTION_BUTTON_SIZE = 32;
    public const int INTERACTION_BUTTON_X_PIXEL_OFFSET = -INTERACTION_BUTTON_SIZE / 2;
    public const int INTERACTION_BUTTON_Y_PIXEL_OFFSET = -INTERACTION_BUTTON_SIZE / 2;

    public const float INTERACTION_DEFAULT_VISIBILITY_DISTANCE = 20.0f;
    public const float INTERACTION_DEFAULT_INTERACT_DISTANCE = 8.5f;

    public const float INTERACTION_MAX_ANGLE_TO_VIEW = Mathf.Pi;

    public const float WORLD_PROGRESS_BAR_FULL_UPDATE_INTERVAL = 0.1f;
    public const float WORLD_PROGRESS_BAR_MAX_DISTANCE = 15.0f;
    public const float WORLD_PROGRESS_BAR_MAX_COUNT = 15;
    public const float WORLD_PROGRESS_BAR_DEFAULT_WIDTH = 125;
    public const float WORLD_PROGRESS_BAR_MIN_WIDTH_TO_SHOW = 20;
    public const float WORLD_PROGRESS_BAR_DEFAULT_HEIGHT = 18;
    public const float WORLD_PROGRESS_BAR_MIN_HEIGHT = 6;
    public const float WORLD_PROGRESS_BAR_DISTANCE_SIZE_SCALE = 1.0f;
    public const float WORLD_PROGRESS_DEFAULT_Y_OFFSET = 3.5f;

    public const float INVENTORY_DRAG_START_ALLOWANCE = 0.15f;

    public const float NAME_LABELS_FULL_UPDATE_INTERVAL = 0.2f;
    public const int NAME_LABELS_MAX_COUNT_PER_CATEGORY = 30;
    public const float NAME_LABEL_VISIBILITY_DISTANCE = 300.0f;

    /// <summary>
    ///   Maximum number of damage events allowed for an entity. Any more are not recorded and is an error.
    /// </summary>
    public const int MAX_DAMAGE_EVENTS = 1000;

    /// <summary>
    ///   Amount of health per second regenerated
    /// </summary>
    public const float HEALTH_REGENERATION_RATE = 1.5f;

    /// <summary>
    ///   Cells need at least this much ATP to regenerate health passively
    /// </summary>
    public const float HEALTH_REGENERATION_ATP_THRESHOLD = 1;

    /// <summary>
    ///   How often in seconds ATP damage is checked and applied if cell has no ATP
    /// </summary>
    public const float ATP_DAMAGE_CHECK_INTERVAL = 0.9f;

    // TODO: remove if unused with ECS
    public const float MICROBE_REPRODUCTION_PROGRESS_INTERVAL = 0.05f;

    /// <summary>
    ///   Used to prevent lag / loading causing big jumps in reproduction progress
    /// </summary>
    public const float MICROBE_REPRODUCTION_MAX_DELTA_FRAME = 0.2f;

    /// <summary>
    ///   Because reproduction progress is most often time limited,
    ///   the bars can go to the reproduction ready state way too early, so this being false prevents that.
    /// </summary>
    public const bool ALWAYS_SHOW_STORED_COMPOUNDS_IN_REPRODUCTION_PROGRESS = false;

    /// <summary>
    ///   Multiplier on how much total compounds can be absorbed by organelles to grow per second compared to the free
    ///   compounds amount. Value of 2 means that having available compounds in storage can make reproduction 2x the
    ///   speed of just using free compounds.
    /// </summary>
    public const float MICROBE_REPRODUCTION_MAX_COMPOUND_USE = 2.25f;

    /// <summary>
    ///   Controls how many "free" compounds a microbe absorbs out of thin air (or water, really) per second for
    ///   reproduction use. Note this limit applies to all compounds combined, not to each individual compound type.
    ///   This is because it is way easier to implement that way.
    /// </summary>
    public const float MICROBE_REPRODUCTION_FREE_COMPOUNDS = 0.25f;

    /// <summary>
    ///   Bonus per hex given to the free compound rate (<see cref="MICROBE_REPRODUCTION_FREE_COMPOUNDS"/>)
    /// </summary>
    public const float MICROBE_REPRODUCTION_FREE_RATE_FROM_HEX = 0.02f;

    /// <summary>
    ///   A multiplier for <see cref="MICROBE_REPRODUCTION_MAX_COMPOUND_USE"/> and
    ///   <see cref="MICROBE_REPRODUCTION_FREE_COMPOUNDS"/> for early multicellular microbes
    /// </summary>
    public const float EARLY_MULTICELLULAR_REPRODUCTION_COMPOUND_MULTIPLIER = 2;

    /// <summary>
    ///   How much ammonia a microbe needs on top of the organelle initial compositions to reproduce
    /// </summary>
    public const float MICROBE_REPRODUCTION_COST_BASE_AMMONIA = 16;

    public const float MICROBE_REPRODUCTION_COST_BASE_PHOSPHATES = 16;

    public const float EARLY_MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER = 1.3f;

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
    public const float MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER = 0.1f;

    /// <summary>
    ///   How much ATP does engulf mode cost per second
    /// </summary>
    public const float ENGULFING_ATP_COST_PER_SECOND = 1.5f;

    /// <summary>
    ///   The speed reduction when a cell is in engulfing mode.
    /// </summary>
    public const float ENGULFING_MOVEMENT_DIVISION = 1.7f;

    /// <summary>
    ///   The minimum size ratio between a cell and a possible engulfing victim.
    /// </summary>
    public const float ENGULF_SIZE_RATIO_REQ = 1.5f;

    /// <summary>
    ///   The duration for which an engulfable object can't be engulfed after being expelled.
    /// </summary>
    public const float ENGULF_EJECTED_COOLDOWN = 2.0f;

    public const float ENGULF_EJECTION_FORCE = 20.0f;

    /// <summary>
    ///   Offsets how far should the chunks for expelled partially digested objects be spawned from the membrane.
    ///   0 means no offset and chunks are spawned directly on the membrane point.
    /// </summary>
    public const float EJECTED_PARTIALLY_DIGESTED_CELL_CORPSE_CHUNKS_SPAWN_OFFSET = 2.0f;

    /// <summary>
    ///   The measure of which beyond this threshold an engulfable is considered partially digested.
    ///   Used to determine whether a cell should be able to heal after being expelled from engulfment.
    /// </summary>
    public const float PARTIALLY_DIGESTED_THRESHOLD = 0.5f;

    /// <summary>
    ///   The maximum digestion progress in which an engulfable is considered fully digested. Do not change this.
    ///   It is assumed elsewhere that 1 means fully digested so this will break a bunch of stuff if you change this.
    /// </summary>
    public const float FULLY_DIGESTED_LIMIT = 1.0f;

    /// <summary>
    ///   The speed of which a cell can absorb compounds from digestible engulfed objects.
    /// </summary>
    public const float ENGULF_COMPOUND_ABSORBING_PER_SECOND = 0.5f;

    /// <summary>
    ///   How much compounds a cell can absorb per second from digestible engulfed objects.
    /// </summary>
    public const float ENGULF_BASE_COMPOUND_ABSORPTION_YIELD = 0.3f;

    /// <summary>
    ///   How often in seconds damage is checked and applied when cell digests a toxic cell
    /// </summary>
    public const float TOXIN_DIGESTION_DAMAGE_CHECK_INTERVAL = 0.9f;

    /// <summary>
    ///   Determines how big of a fraction of damage (of total health)
    ///   is dealt to a microbe at a time when it digests a toxic cell.
    /// </summary>
    public const float TOXIN_DIGESTION_DAMAGE_FRACTION = 0.09f;

    /// <summary>
    ///   Each enzyme addition grants a fraction, set by this variable, increase in digestion speed.
    /// </summary>
    public const float ENZYME_DIGESTION_SPEED_UP_FRACTION = 0.1f;

    /// <summary>
    ///   Each enzyme addition grants this fraction increase in compounds yield.
    /// </summary>
    public const float ENZYME_DIGESTION_EFFICIENCY_BUFF_FRACTION = 0.15f;

    /// <summary>
    ///   The maximum cap for efficiency of digestion.
    /// </summary>
    public const float ENZYME_DIGESTION_EFFICIENCY_MAXIMUM = 0.6f;

    public const float ADDITIONAL_DIGESTIBLE_GLUCOSE_AMOUNT_MULTIPLIER = 0.25f;

    public const string LYSOSOME_DEFAULT_ENZYME_NAME = "lipase";

    public const string VACUOLE_DEFAULT_COMPOUND_NAME = "glucose";

    /// <summary>
    ///   How much the capacity of a specialized vacuole should be multiplied
    /// </summary>
    public const float VACUOLE_SPECIALIZED_MULTIPLIER = 2.0f;

    /// <summary>
    ///   How much ATP does binding mode cost per second
    /// </summary>
    public const float BINDING_ATP_COST_PER_SECOND = 2.0f;

    /// <summary>
    ///   Damage a single pilus stab does
    /// </summary>
    public const float PILUS_BASE_DAMAGE = 20.0f;

    public const float PILUS_PHYSICS_SIZE = 4.7f;

    // TODO: remove
    /// <summary>
    ///   Damage a single injectisome stab does
    /// </summary>
    public const float INJECTISOME_BASE_DAMAGE = 20.0f;

    /// <summary>
    ///   How much time (in seconds) a pilus applies invulnerability upon damage.
    /// </summary>
    public const float PILUS_INVULNERABLE_TIME = 0.25f;

    /// <summary>
    ///   Osmoregulation ATP cost per second per hex
    /// </summary>
    public const float ATP_COST_FOR_OSMOREGULATION = 1.0f;

    public const float MICROBE_FLASH_DURATION = 0.6f;

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
    public const int PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_CONSTANT = -35;
    public const float PLAYER_PATCH_EXTINCTION_POPULATION_LOSS_COEFFICIENT = 1 / 1.2f;

    /// <summary>
    ///   How often a microbe can get the engulf escape population bonus
    /// </summary>
    public const float CREATURE_ESCAPE_INTERVAL = 5;

    public const int BASE_MUTATION_POINTS = 100;

    public const int ORGANELLE_REMOVE_COST = 10;
    public const int ORGANELLE_MOVE_COST = 5;

    public const string ORGANELLE_UPGRADE_SPECIAL_NONE = "none";

    public const int METABALL_ADD_COST = 7;
    public const int METABALL_REMOVE_COST = 5;
    public const int METABALL_MOVE_COST = 3;
    public const int METABALL_RESIZE_COST = 3;

    public const float DIVIDE_EXTRA_DAUGHTER_OFFSET = 3.0f;

    // Corpse info
    public const float CORPSE_COMPOUND_COMPENSATION = 8.0f;
    public const int CORPSE_CHUNK_DIVISOR = 3;
    public const float CORPSE_CHUNK_AMOUNT_DIVISOR = 3.0f;
    public const float CHUNK_ENGULF_COMPOUND_DIVISOR = 30.0f;
    public const string DEFAULT_CHUNK_MODEL_NAME = "cytoplasm";

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
    public const float CHEMORECEPTOR_SEARCH_UPDATE_INTERVAL = 0.25f;
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

    /// <summary>
    ///   Minimum extra microbes to spawn
    /// </summary>
    public const int MIN_BACTERIAL_COLONY_SIZE = 0;

    /// <summary>
    ///   Maximum extra microbes to spawn
    /// </summary>
    public const int MAX_BACTERIAL_COLONY_SIZE = 1;

    // What is divided during fear and aggression calculations in the AI
    public const float AGGRESSION_DIVISOR = 25.0f;
    public const float FEAR_DIVISOR = 25.0f;
    public const float ACTIVITY_DIVISOR = 100.0f;
    public const float FOCUS_DIVISOR = 100.0f;
    public const float OPPORTUNISM_DIVISOR = 100.0f;

    // Cooldown for AI for toggling engulfing
    public const float AI_ENGULF_INTERVAL = 300;

    /// <summary>
    ///   Probability, rolled at each AI step (which happens very often), that the AI will try to engulf something
    ///   it can't eat
    /// </summary>
    public const float AI_BAD_ENGULF_CHANCE = 0.15f;

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

    public const float AI_BASE_TOXIN_SHOOT_ANGLE_PRECISION = 5;

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
    public const float AUTO_EVO_MUCILAGE_PREDATION_SCORE = 100;
    public const float AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY = 0.1f;
    public const float AUTO_EVO_CHUNK_LEAK_MULTIPLIER = 0.1f;
    public const float AUTO_EVO_PREDATION_ENERGY_MULTIPLIER = 0.4f;
    public const float AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT = 150000;
    public const float AUTO_EVO_THERMOSYNTHESIS_ENERGY_AMOUNT = 500;
    public const float AUTO_EVO_COMPOUND_ENERGY_AMOUNT = 2400;
    public const float AUTO_EVO_CHUNK_ENERGY_AMOUNT = 90000000;
    public const float AUTO_EVO_CHUNK_AMOUNT_NERF = 0.01f;

    public const float AUTO_EVO_MINIMUM_VIABLE_RESERVE_PER_TIME_UNIT = 1.0f;
    public const float AUTO_EVO_NON_VIABLE_RESERVE_PENALTY = 10;

    public const int AUTO_EVO_MINIMUM_SPECIES_SIZE_BEFORE_SPLIT = 80;
    public const bool AUTO_EVO_ALLOW_SPECIES_SPLIT_ON_NO_MUTATION = true;

    public const double AUTO_EVO_COMPOUND_RATIO_POWER_BIAS = 1;
    public const double AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS = 0.5;

    /// <summary>
    ///   How much auto-evo affects the player species compared to the normal amount
    /// </summary>
    public const float AUTO_EVO_PLAYER_STRENGTH_FRACTION = 0.2f;

    public const int EDITOR_TIME_JUMP_MILLION_YEARS = 100;
    public const float GLUCOSE_MIN = 0.0f;

    // These control how many game entities can exist at once
    // TODO: bump these back up once we resolve the performance bottleneck
    public const int TINY_MAX_SPAWNED_ENTITIES = 50;
    public const int VERY_SMALL_MAX_SPAWNED_ENTITIES = 100;
    public const int SMALL_MAX_SPAWNED_ENTITIES = 200;
    public const int NORMAL_MAX_SPAWNED_ENTITIES = 300;
    public const int LARGE_MAX_SPAWNED_ENTITIES = 400;
    public const int VERY_LARGE_MAX_SPAWNED_ENTITIES = 500;
    public const int HUGE_MAX_SPAWNED_ENTITIES = 600;
    public const int EXTREME_MAX_SPAWNED_ENTITIES = 800;

    /// <summary>
    ///   Controls how fast entities are allowed to spawn
    /// </summary>
    public const int MAX_SPAWNS_PER_FRAME = 1;

    /// <summary>
    ///   Delete a max of this many entities per step to reduce lag from deleting tons of entities at once.
    ///   Note that this is a raw count and not a weighted count as game instability is probably related to the number
    ///   of deleted world child Nodes and not their complexity.
    /// </summary>
    public const int MAX_DESPAWNS_PER_FRAME = 4;

    /// <summary>
    ///   Multiplier for how much organelles inside spawned cells contribute to the entity count.
    /// </summary>
    public const float ORGANELLE_ENTITY_WEIGHT = 0.1f;

    public const float MICROBE_BASE_ENTITY_WEIGHT = 2;

    public const float FLOATING_CHUNK_ENTITY_WEIGHT = 1;

    /// <summary>
    ///   How often despawns happen on top of the normal despawns that are part of the spawn cycle
    /// </summary>
    public const float DESPAWN_INTERVAL = 0.08f;

    public const float CHANCE_MULTICELLULAR_SPAWNS_GROWN = 0.1f;
    public const float CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN = 0.3f;
    public const float CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE = 0.4f;

    public const float TIME_BEFORE_TUTORIAL_CAN_PAUSE = 0.01f;

    public const float MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY = 12.0f;
    public const float MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY_CONTROLLER = 1.0f;
    public const float MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME = 2.2f;
    public const float TUTORIAL_ENTITY_POSITION_UPDATE_INTERVAL = 0.2f;
    public const float GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE = 0.14f;
    public const float GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE = 0.21f;
    public const float MICROBE_REPRODUCTION_TUTORIAL_DELAY = 10;
    public const float HIDE_MICROBE_STAYING_ALIVE_TUTORIAL_AFTER = 60;
    public const float HIDE_MICROBE_DAY_NIGHT_TUTORIAL_AFTER = 20;
    public const float HIDE_MICROBE_ENGULFED_TUTORIAL_AFTER = 35;
    public const float OPEN_MICROBE_BECOME_MULTICELLULAR_TUTORIAL_AFTER = 30;
    public const float MICROBE_EDITOR_BUTTON_TUTORIAL_DELAY = 20;

    public const float DAY_NIGHT_TUTORIAL_LIGHT_MIN = 0.01f;

    /// <summary>
    ///   Used to limit how often the hover indicator panel are
    ///   updated. Default value is every 0.1 seconds.
    /// </summary>
    public const float HOVER_PANEL_UPDATE_INTERVAL = 0.1f;

    public const int MAX_RAY_HITS_FOR_INSPECT = 20;

    public const float TOOLTIP_OFFSET = 20;
    public const float TOOLTIP_DEFAULT_DELAY = 1.0f;
    public const float TOOLTIP_FADE_SPEED = 0.25f;

    public const float EDITOR_ARROW_OFFSET = 3.5f;
    public const float EDITOR_ARROW_INTERPOLATE_SPEED = 0.5f;

    public const float EDITOR_DEFAULT_CAMERA_HEIGHT = 10;

    public const float MULTICELLULAR_EDITOR_PREVIEW_MICROBE_SCALE_MULTIPLIER = 0.80f;

    public const float MAX_SPECIES_NAME_LENGTH_PIXELS = 230.0f;

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

    public const float BRAIN_POWER_REQUIRED_FOR_AWARE = 0.5f;
    public const float BRAIN_POWER_REQUIRED_FOR_AWAKENING = 5;

    /// <summary>
    ///   Squared distance after which a timed action is canceled due to moving too much
    /// </summary>
    public const float ACTION_CANCEL_DISTANCE = 5;

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

    public const int CUSTOM_FOCUS_DRAWER_RADIUS = 12;
    public const int CUSTOM_FOCUS_DRAWER_RADIUS_POINTS = 12;
    public const int CUSTOM_FOCUS_DRAWER_WIDTH = 3;
    public const bool CUSTOM_FOCUS_DRAWER_ANTIALIAS = true;

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

    /// <summary>
    ///   Buffs small bacteria
    /// </summary>
    public const float MICROBE_MIN_ABSORB_RADIUS = 3;

    public const float PROCEDURAL_CACHE_CLEAN_INTERVAL = 9.3f;
    public const float PROCEDURAL_CACHE_MEMBRANE_KEEP_TIME = 500;
    public const float PROCEDURAL_CACHE_LOADED_SHAPE_KEEP_TIME = 1000;

    // TODO: convert prototypes over to an ECS system as well

    public const string ENTITY_TAG_CREATURE = "creature";

    public const string INTERACTABLE_GROUP = "interactable";

    public const string CITY_ENTITY_GROUP = "city";
    public const string NAME_LABEL_GROUP = "labeled";

    public const string PLANET_ENTITY_GROUP = "planet";
    public const string SPACE_FLEET_ENTITY_GROUP = "fleet";
    public const string SPACE_STRUCTURE_ENTITY_GROUP = "s_structure";

    /// <summary>
    ///   Group for entities that can show a progress bar above them in the GUI
    /// </summary>
    public const string PROGRESS_ENTITY_GROUP = "progress";

    public const string STRUCTURE_ENTITY_GROUP = "structure";

    public const string CITIZEN_GROUP = "citizen";

    public const string DELETION_HOLD_LOAD = "load";
    public const string DELETION_HOLD_MICROBE_EDITOR = "microbe_editor";

    public const string CONFIGURATION_FILE = "user://thrive_settings.json";
    public const string WORKSHOP_DATA_FILE = "user://workshop_data.json";

    public const string SAVE_FOLDER = "user://saves";
    public const string FOSSILISED_SPECIES_FOLDER = "user://fossils";
    public const string AUTO_EVO_EXPORT_FOLDER = "user://auto-evo_exports";

    public const string EXPLICIT_PATH_PREFIX = "file://";

    /// <summary>
    ///   This is used in Steam mode, so don't remove even if this shows as unused
    /// </summary>
    public const int MAX_PATH_LENGTH = 1024;

    public const string SCREENSHOT_FOLDER = "user://screenshots";

    public const string LOGS_FOLDER_NAME = "logs";
    public const string LOGS_FOLDER = "user://" + LOGS_FOLDER_NAME;

    public const string JSON_DEBUG_OUTPUT_FILE = LOGS_FOLDER + "/" + JSON_DEBUG_OUTPUT_FILE_NAME;
    public const string JSON_DEBUG_OUTPUT_FILE_NAME = "json_debug.txt";

    public const string STARTUP_ATTEMPT_INFO_FILE = "user://startup_attempt.json";

    public const string LAST_PLAYED_VERSION_FILE = "user://last_played_version.txt";

    public const string LICENSE_FILE = "res://LICENSE.txt";
    public const string STEAM_LICENSE_FILE = "res://doc/steam_license_readme.txt";
    public const string ASSETS_README = "res://assets/README.txt";
    public const string ASSETS_LICENSE_FILE = "res://assets/LICENSE.txt";
    public const string GODOT_LICENSE_FILE = "res://doc/GodotLicense.txt";
    public const string OFL_LICENSE_FILE = "res://assets/OFL.txt";
    public const string GPL_LICENSE_FILE = "res://gpl.txt";

    public const string ASSETS_GUI_BEVEL_FOLDER = "res://assets/textures/gui/bevel";

    public const float GUI_FOCUS_GRABBER_PROCESS_INTERVAL = 0.1f;
    public const float GUI_FOCUS_SETTER_PROCESS_INTERVAL = 0.2f;

    public const string BUILD_INFO_FILE = "res://simulation_parameters/revision.json";

    public const string PHYSICS_DUMP_PATH = LOGS_FOLDER + "/physics_dump.bin";

    public const bool VERBOSE_SIMULATION_PARAMETER_LOADING = false;

    /// <summary>
    ///   Internal Godot name for the default audio output device
    /// </summary>
    public const string DEFAULT_AUDIO_OUTPUT_DEVICE_NAME = "Default";

    public const string OS_WINDOWS_NAME = "Windows";

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
    public const int FOSSILISED_PREVIEW_IMAGE_HEIGHT = 400;

    public const string FOSSIL_EXTENSION = "thrivefossil";
    public const string FOSSIL_EXTENSION_WITH_DOT = "." + FOSSIL_EXTENSION;

    /// <summary>
    ///   How long the main menu needs to be ready before game startup is considered successful
    /// </summary>
    public const float MAIN_MENU_TIME_BEFORE_STARTUP_SUCCESS = 1.25f;

    public const int KIBIBYTE = 1024;
    public const int MEBIBYTE = 1024 * KIBIBYTE;

    /// <summary>
    ///   Max bytes to allocate on the stack, any bigger data needs to allocate heap memory
    /// </summary>
    public const int MAX_STACKALLOC = 1024;

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
    public const float COMPOUND_DENSITY_CATEGORY_LITTLE = 10.0f;

    /// <summary>
    ///   Minimum amount for the some category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_SOME = 50.0f;

    /// <summary>
    ///   Minimum amount for the fair amount category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT = 200.0f;

    /// <summary>
    ///   Minimum amount for the quite a bit category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT = 800.0f;

    /// <summary>
    ///   Minimum amount for the an abundance category in the hover info.
    /// </summary>
    public const float COMPOUND_DENSITY_CATEGORY_AN_ABUNDANCE = 3000.0f;

    public const float PHOTO_STUDIO_CAMERA_FOV = 70;
    public const float PHOTO_STUDIO_CAMERA_HALF_ANGLE = PHOTO_STUDIO_CAMERA_FOV / 2.0f;
    public const float PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER = 0.80f;

    public const int RESOURCE_LOAD_TARGET_MIN_FPS = 60;
    public const float RESOURCE_TIME_BUDGET_PER_FRAME = 1.0f / RESOURCE_LOAD_TARGET_MIN_FPS;
    public const bool TRACK_ACTUAL_RESOURCE_LOAD_TIMES = false;
    public const float REPORT_LOAD_TIMES_OF_BY = 0.1f;

    public const int GALLERY_THUMBNAIL_MAX_WIDTH = 500;

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

    // TODO: combine to a common module with launcher as these are there as well
    public const string DISABLE_VIDEOS_LAUNCH_OPTION = "--thrive-disable-videos";
    public const string OPENED_THROUGH_LAUNCHER_OPTION = "--thrive-started-by-launcher";
    public const string OPENING_LAUNCHER_IS_HIDDEN = "--thrive-launcher-hidden";
    public const string THRIVE_LAUNCHER_STORE_PREFIX = "--thrive-store=";

    public const string STARTUP_SUCCEEDED_MESSAGE = "------------ Thrive Startup Succeeded ------------";
    public const string USER_REQUESTED_QUIT = "User requested program exit, Thrive will close shortly";
    public const string REQUEST_LAUNCHER_OPEN = "------------ SHOWING LAUNCHER REQUESTED ------------";

    // Min/max values for each customisable difficulty option
    public const float MIN_MP_MULTIPLIER = 0.2f;
    public const float MAX_MP_MULTIPLIER = 2;
    public const float MIN_AI_MUTATION_RATE = 0.5f;
    public const float MAX_AI_MUTATION_RATE = 5;
    public const float MIN_COMPOUND_DENSITY = 0.2f;
    public const float MAX_COMPOUND_DENSITY = 2;
    public const float MIN_PLAYER_DEATH_POPULATION_PENALTY = 1;
    public const float MAX_PLAYER_DEATH_POPULATION_PENALTY = 5;
    public const float MIN_GLUCOSE_DECAY = 0.3f;
    public const float MAX_GLUCOSE_DECAY = 0.95f;
    public const float MIN_OSMOREGULATION_MULTIPLIER = 0.2f;
    public const float MAX_OSMOREGULATION_MULTIPLIER = 2;

    // Constants for procedural patch map
    public const float PATCH_NODE_RECT_LENGTH = 64.0f;
    public const float PATCH_AND_REGION_MARGIN = 2 * 3.0f;
    public const float PATCH_REGION_CONNECTION_LINE_WIDTH = 4.0f;
    public const float PATCH_REGION_BORDER_WIDTH = 6.0f;
    public const int PATCH_GENERATION_MAX_RETRIES = 100;

    /// <summary>
    ///   If set to true then physics debug draw gets enabled when the game starts
    /// </summary>
    public const bool AUTOMATICALLY_TURN_ON_PHYSICS_DEBUG_DRAW = false;

    /// <summary>
    ///   Extra time passed to <see cref="HUDMessages"/> when exiting the editor. Needs to be close to (or higher)
    ///   than the long message time as defined in <see cref="HUDMessages.TimeToFadeFromDuration"/>
    /// </summary>
    public const float HUD_MESSAGES_EXTRA_ELAPSE_TIME_FROM_EDITOR = 11.2f;

    public const float SOCIETY_STAGE_ENTER_ANIMATION_DURATION = 15;

    public const float SOCIETY_STAGE_BUILDING_PROCESS_INTERVAL = 0.05f;

    public const float SOCIETY_STAGE_CITIZEN_PROCESS_INTERVAL = 0.05f;

    public const float SOCIETY_STAGE_CITIZEN_SPAWN_INTERVAL = 5.0f;

    public const float SOCIETY_STAGE_RESEARCH_PROGRESS_INTERVAL = 1.0f;

    public const float SOCIETY_CAMERA_ZOOM_INDUSTRIAL_EQUIVALENT = INDUSTRIAL_STAGE_SIZE_MULTIPLIER;

    /// <summary>
    ///   Scale of the world in industrial stage compared to the society stage
    /// </summary>
    public const float INDUSTRIAL_STAGE_SIZE_MULTIPLIER = 5.0f;

    public const float INDUSTRIAL_STAGE_CITY_PROCESS_INTERVAL = 0.1f;

    public const float CITY_SCREEN_UPDATE_INTERVAL = 0.1f;

    public const int CITY_MAX_BUILD_QUEUE_LENGTH = 10;

    public const int CITY_MAX_GARRISONED_UNITS = 10;

    public const float SPACE_TO_INDUSTRIAL_SCALE_FACTOR = 0.1f;

    public const float INDUSTRIAL_TO_SPACE_CAMERA_PAN_DURATION = 2.5f;

    public const float INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_START = 12;
    public const float INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_SPEED = 0.1f;
    public const float INDUSTRIAL_TO_SPACE_CAMERA_MIN_HEIGHT_MULTIPLIER = 0.6f;
    public const float INDUSTRIAL_TO_SPACE_CAMERA_ZOOM_SPEED = 0.6f;
    public const float INDUSTRIAL_TO_SPACE_FADE_DURATION = 4;

    public const float INDUSTRIAL_TO_SPACE_ROCKET_ACCELERATION = 0.005f;

    public const float INDUSTRIAL_TO_SPACE_END_ROCKET_HEIGHT = 300;

    public const float PLANET_SCREEN_UPDATE_INTERVAL = 0.1f;
    public const float UNIT_SCREEN_UPDATE_INTERVAL = 0.05f;

    public const float SPACE_STAGE_PLANET_PROCESS_INTERVAL = 0.1f;
    public const float SPACE_STAGE_STRUCTURE_PROCESS_INTERVAL = 0.1f;

    public const float SPACE_FLEET_MODEL_SCALE = 0.1f;

    public const float SPACE_INITIAL_ANIMATION_MIN_ZOOM_SCALE = 0.3f;
    public const float SPACE_INITIAL_ANIMATION_ZOOM_SPEED = 0.08f;

    public const float SPACE_ASCEND_ANIMATION_MIN_ZOOM_SCALE = 0.2f;
    public const float SPACE_ASCEND_ANIMATION_DURATION = 2.5f;
    public const float SPACE_ASCEND_ANIMATION_ZOOM_SPEED = 0.5f;
    public const float SPACE_ASCEND_SCREEN_FADE = 0.8f;

    public const float SPACE_FLEET_SELECTION_RADIUS = 1.7f;

    /// <summary>
    ///   Names like "Pangonia Primus" are cool so we use those until it makes more sense to switch to roman numerals
    /// </summary>
    public const int NAMING_SWITCH_TO_ROMAN_NUMERALS_AFTER = 10;

    /// <summary>
    ///   How many pixels the cursor needs to be from a screen edge to activate edge panning
    /// </summary>
    public const int EDGE_PAN_PIXEL_THRESHOLD = 4;

    public const ControllerType DEFAULT_CONTROLLER_TYPE = ControllerType.XboxSeriesX;
    public const float MINIMUM_DELAY_BETWEEN_INPUT_TYPE_CHANGE = 0.3f;

    // If we update our Godot project base resolution these *may* need to be adjusted for mouse input to feel the same
    public const float BASE_VERTICAL_RESOLUTION_FOR_INPUT = 720;
    public const float BASE_HORIZONTAL_RESOLUTION_FOR_INPUT = 1280;

    public const float MOUSE_INPUT_SENSITIVITY_STEP = 0.0001f;
    public const float CONTROLLER_INPUT_SENSITIVITY_STEP = 0.04f;

    public const float CONTROLLER_AXIS_REBIND_REQUIRED_STRENGTH = 0.5f;

    public const float CONTROLLER_DEFAULT_DEADZONE = 0.2f;

    /// <summary>
    ///   How big fraction of extra margin is added on top of a calibrated deadzone
    /// </summary>
    public const float CONTROLLER_DEADZONE_CALIBRATION_MARGIN = 0.1f;

    /// <summary>
    ///   Constant value added to the calibration value to make the deadzones not as tight, especially at low values
    /// </summary>
    public const float CONTROLLER_DEADZONE_CALIBRATION_MARGIN_CONSTANT = 0.007f;

    public const int FORCE_CLOSE_AFTER_TRIES = 3;

    /// <summary>
    ///   Controls whether benchmarks start off showing the hardware info, or only after some results are generated
    /// </summary>
    public const bool BENCHMARKS_SHOW_HARDWARE_INFO_IMMEDIATELY = true;

    public const int MAX_NEWS_FEED_ITEMS_TO_SHOW = 15;
    public const int MAX_NEWS_FEED_ITEM_LENGTH = 1000;

    public const string CLICKABLE_TEXT_BBCODE = "[color=#3796e1]";
    public const string CLICKABLE_TEXT_BBCODE_END = "[/color]";

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
    ///   Colour of the custom focus highlight elements. Should be the same as what it set in Thrive theme
    /// </summary>
    public static readonly Color CustomFocusDrawerColour = new("#00bfb6");

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

    // Regex expressions to categorize different file types.
    public static readonly Regex BackupRegex = new(@"^.*\.backup\." + SAVE_EXTENSION + "$");
    public static readonly Regex AutoSaveRegex = new(@"^auto_save_\d+\." + SAVE_EXTENSION + "$");
    public static readonly Regex QuickSaveRegex = new(@"^quick_save_\d+\." + SAVE_EXTENSION + "$");

    /// <summary>
    ///   When any action is triggered matching any of these, input method change is prevented.
    ///   This is used to allow taking screenshots with the keyboard while playing with a controller, for example.
    /// </summary>
    public static readonly IReadOnlyCollection<string> ActionsThatDoNotChangeInputMethod = new[]
    {
        "screenshot",
        "toggle_FPS",
    };

    // TODO: switch to https once our runtime supports it: https://github.com/Revolutionary-Games/Thrive/issues/4100
    // See: https://github.com/Revolutionary-Games/Thrive/pull/4097#issuecomment-1415301373
    public static readonly Uri MainSiteFeedURL = new("http://thrivefeeds.b-cdn.net/feed.rss");

    public static readonly Regex NewsFeedRegexDeleteContent =
        new(@"\s*The\spost\s*.*appeared\sfirst\son.*Revolutionary\sGames\sStudio.*$");

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

    private const uint FreeCompoundAmountIsLessThanUsePerSecond =
        (MICROBE_REPRODUCTION_FREE_COMPOUNDS < MICROBE_REPRODUCTION_MAX_COMPOUND_USE) ? 0 : -42;

    private const uint ReproductionProgressIntervalLessThanMaxDelta =
        (MICROBE_REPRODUCTION_PROGRESS_INTERVAL < MICROBE_REPRODUCTION_MAX_DELTA_FRAME) ? 0 : -42;

    private const uint ReproductionTutorialDelaysAreSensible =
        (MICROBE_REPRODUCTION_TUTORIAL_DELAY + 1 < MICROBE_EDITOR_BUTTON_TUTORIAL_DELAY) ? 0 : -42;

    // Needed to be true by InputManager
    private const uint GodotJoystickAxesStartAtZero = (JoystickList.Axis0 == 0) ? 0 : -42;

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
