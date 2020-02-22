// Holds config file contents, translated into AngelScript objects
#include "agents.as"
#include "organelle.as"

// TODO: unifying these and options that the C++ side use would be nice
// Maybe some kind of wrapper class that is loaded from JSON once

// Global defines
const auto MICROBE_SPAWN_RADIUS = 150;
// Right now these are used for species split from the player
const auto INITIAL_SPLIT_POPULATION_MIN = 600;
const auto INITIAL_SPLIT_POPULATION_MAX = 2000;

//! Organelles won't take compounds if there is less available than this amount
const auto ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST = 0.f;

// Percentage of the compounds that compose the organelle released
// upon death (between 0.0 and 1.0).
const auto COMPOUND_MAKEUP_RELEASE_PERCENTAGE = 0.9f;
const auto COMPOUND_RELEASE_PERCENTAGE = 0.9f;


//Corpse info
const auto CORPSE_COMPOUND_COMPENSATION = 8.0f;
const int CORPSE_CHUNK_DIVISER = 3;
const auto CORPSE_CHUNK_AMOUNT_DIVISER = 3.0f;
const auto CHUNK_ENGULF_COMPOUND_DIVISOR = 30.0f;

// Cell Spawn Variation
const auto MIN_SPAWN_DISTANCE = -5000.0f;
const auto MAX_SPAWN_DISTANCE = 5000.0f;

// Cell Colors
const auto MIN_COLOR = 0.0f;
const auto MAX_COLOR = 0.9f;

// Too subtle?
const auto MIN_COLOR_MUTATION = -0.2f;
const auto MAX_COLOR_MUTATION = 0.2f;

const auto MIN_OPACITY = 0.5f;
const auto MAX_OPACITY = 1.8f;

const auto MIN_OPACITY_CHITIN = 0.4f;
const auto MAX_OPACITY_CHITIN = 1.2f;

// Min Opacity Mutation
const auto MIN_OPACITY_MUTATION = -0.01f;
const auto MAX_OPACITY_MUTATION = 0.01f;

// Mutation Variables
const auto MUTATION_BACTERIA_TO_EUKARYOTE = 1.0f;
const auto MUTATION_CREATION_RATE = 0.1f;
const auto MUTATION_EXTRA_CREATION_RATE = 0.1f;
const auto MUTATION_DELETION_RATE = 0.1f;
const auto MUTATION_REPLACEMENT_RATE = 0.1f;

// Genus splitting and name mutation
const auto MUTATION_CHANGE_GENUS = 33;
const auto MUTATION_WORD_EDIT = 10;

//Removal cost
const auto ORGANELLE_REMOVE_COST = 10;

// Max fear and agression and activity
const auto MAX_SPECIES_AGRESSION = 400.0f;
const auto MAX_SPECIES_FEAR = 400.0f;
const auto MAX_SPECIES_ACTIVITY = 400.0f;
const auto MAX_SPECIES_FOCUS = 400.0f;
const auto MAX_SPECIES_OPPORTUNISM = 400.0f;

// Personality Mutation
const auto MAX_SPECIES_PERSONALITY_MUTATION = 20.0f;
const auto MIN_SPECIES_PERSONALITY_MUTATION = -20.0f;

// Bacterial Colony configuration
const auto MIN_BACTERIAL_COLONY_SIZE = 2;
const auto MAX_BACTERIAL_COLONY_SIZE = 6;
const auto MIN_BACTERIAL_LINE_SIZE =  3;
const auto MAX_BACTERIAL_LINE_SIZE = 7;

// What is divided during fear and aggression calculations in the AI
const auto AGRESSION_DIVISOR = 25.0f;
const auto FEAR_DIVISOR = 25.0f;
const auto ACTIVITY_DIVISOR = 100.0f;
const auto FOCUS_DIVISOR = 100.0f;
const auto OPPORTUNISM_DIVISOR = 100.0f;


// Cooldown for AI for toggling engulfing
const uint AI_ENGULF_INTERVAL=300;

// if you are gaining less then this amount of compound per turn you are much more likely to turn randomly
const auto AI_COMPOUND_BIAS = -10.0f;
// So we dont run the AI system every single frame
const auto AI_TIME_INTERVAL = 0.2f;

const auto AI_CELL_THINK_INTERVAL = 3.f;

// Osmoregulation ATP cost
//! If you change this you must also change the value in process_system.cpp
const auto ATP_COST_FOR_OSMOREGULATION = 1.0f;

//Purge Divisor
const auto COMPOUND_PURGE_MODIFIER = 2.0f;

//! BASE MOVEMENT COST ATP cost
//! Cancels out a little bit more then one cytoplasm's glycolysis
//! Note: this is applied *per* hex
//! If you change this you must also change the value in process_system.cpp
const auto BASE_MOVEMENT_ATP_COST = 1.0f;

// The player's name
const auto PLAYER_NAME = "Player";

const auto DEFAULT_HEALTH = 100;
// Amount of health per second regenerated
const auto REGENERATION_RATE = 1.0f;

// Movement stuff
//! If you change this you must also change the value in process_system.cpp
const auto FLAGELLA_ENERGY_COST = 7.1f;
const auto FLAGELLA_BASE_FORCE = 0.7f;
const auto CELL_BASE_THRUST = 1.6f;

// is set by this and modified by applyCellMovement like the player later
const auto AI_BASE_MOVEMENT = 1.0f;
const auto AI_FOCUSED_MOVEMENT = 1.0f;

//! The drag force is calculated by taking the current velocity and multiplying it by this.
//! This must be negative!
const auto CELL_DRAG_MULTIPLIER = -0.12f;
const auto CELL_SIZE_DRAG_MULTIPLIER = -0.003f;
//! If drag is below this it isn't applied to let the cells come to a halt properly
const auto CELL_REQUIRED_DRAG_BEFORE_APPLY =  0.0033f;

// Turning is currently set to be instant to avoid gyrating around the correct heading


// Quantity of physics time between each loop distributing compounds
// to organelles. TODO: Modify to reflect microbe size.
const uint COMPOUND_PROCESS_DISTRIBUTION_INTERVAL = 100;

// Amount the microbes maxmimum bandwidth increases with per organelle
// added. This is a temporary replacement for microbe surface area
const float BANDWIDTH_PER_ORGANELLE = 1.0;

// The of time it takes for the microbe to regenerate an amount of
// bandwidth equal to maxBandwidth
const float BANDWIDTH_REFILL_DURATION = 0.8f;

// No idea what this does (if anything), but it isn't used in the
// process system, or when ejecting compounds.
const float STORAGE_EJECTION_THRESHHOLD = 0.8;

// The amount of time between each loop to maintaining a fill level
// below STORAGE_EJECTION_THRESHHOLD and eject useless compounds
const float EXCESS_COMPOUND_COLLECTION_INTERVAL = 1.f;

// The amount of hitpoints each organelle provides to a microbe.
const uint MICROBE_HITPOINTS_PER_ORGANELLE = 10;

// The minimum amount of oxytoxy (or any agent) needed to be able to shoot.
const float MINIMUM_AGENT_EMISSION_AMOUNT = 1;

// A sound effect thing for bumping with other cell i assume? Probably unused.
const float RELATIVE_VELOCITY_TO_BUMP_SOUND = 6.0;

// I think (emphasis on think) this is unused.
const float INITIAL_EMISSION_RADIUS = 0.5;

// The speed reduction when a cell is in rngulfing mode.
const double ENGULFING_MOVEMENT_DIVISION = 2.0f;

// The speed reduction when a cell is being engulfed.
const double ENGULFED_MOVEMENT_DIVISION = 7000.0f;

// The amount of ATP per second spent on being on engulfing mode.
const float ENGULFING_ATP_COST_SECOND = 1.5;

// The minimum HP ratio between a cell and a possible engulfing victim.
const float ENGULF_HP_RATIO_REQ = 1.5f;

// The amount of hp per second of damage
const float ENGULF_DAMAGE = 45.0f;

// Oxytoxy Damage
const float OXY_TOXY_DAMAGE = 10.0f;

// Cooldown between agent emissions, in seconds.
const float AGENT_EMISSION_COOLDOWN = 2.f;

// Iron amounts per chunk.
// big iron ejects ten per 20 clicks , so about 30 per second, so ill give it enough for 1000 seconds)
const double IRON_PER_BIG_CHUNK = 30000.0f;
const bool LARGE_IRON_DISSOLVES = false;
// small iron ejects 3 per 20 clicks , so about 9 per second
const double IRON_PER_SMALL_CHUNK = 100.0f;
const bool SMALL_IRON_DISSOLVES = true;

// Darwinian Evo Values
const int CREATURE_DEATH_POPULATION_LOSS = -60;
const int CREATURE_KILL_POPULATION_GAIN = 50;
const int CREATURE_SCAVENGE_POPULATION_GAIN = 10;
const int CREATURE_REPRODUCE_POPULATION_GAIN = 50;
const int CREATURE_ESCAPE_POPULATION_GAIN = 50;
const uint CREATURE_ESCAPE_INTERVAL = 5000;


// Auto-evo tweak variables (TODO: move to JSON)
const int AUTO_EVO_MINIMUM_MOVE_POPULATION = 250;
const float AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION = 0.1f;
const float AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION = 0.9f;

// Some (placeholder) auto-evo algorithm tweak parameters
const auto AUTO_EVO_LOW_SPECIES_THRESHOLD = 3;
const auto AUTO_EVO_LOW_SPECIES_BOOST = 500;
const auto AUTO_EVO_HIGH_SPECIES_THRESHOLD = 11;
const auto AUTO_EVO_HIGH_SPECIES_PENALTY = 500;
const auto AUTO_EVO_RANDOM_POPULATION_CHANGE = 500;


// Used in the cell collision callback to know if something hit was a pilus
const int PHYSICS_PILUS_TAG = 1;
const auto PILUS_BASE_DAMAGE = 1.f;
const auto PILUS_PENETRATION_DISTANCE_DAMAGE_MULTIPLIER = 10.f;


// For rigidity stat modifications
const int MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER = 30;
const float MEMBRANE_RIGIDITY_MOBILITY_MODIFIER = 0.1f;


//! Returns a material with a basic texture on it. For use on non-organelle models
Material@ getBasicMaterialWithTexture(const string &in textureName)
{
    Material@ material = Material(Shader("BuiltinShader::Standard"));
    material.SetTexture("gAlbedoTex", Texture(textureName));

    return material;
}

//! Returns a material with a basic texture on it. Supports transparency
Material@ getBasicTransparentMaterialWithTexture(const string &in textureName)
{
    Material@ material = Material(Shader("BuiltinShader::Transparent"));
    material.SetTexture("gAlbedoTex", Texture(textureName));

    return material;
}

//! Returns a material for organelles
Material@ getOrganelleMaterialWithTexture(const string &in textureName,
    const Float4 &in tint = Float4(1, 1, 1, 1))
{
    // TODO: loading the shader just once would be nice
    Shader@ shader = Shader("organelle.bsl");
    Material@ material = Material(shader);
    material.SetTexture("gAlbedoTex", Texture(textureName));

    updateMaterialTint(material, tint);
    return material;
}

void updateMaterialTint(Material@ material, const Float4 &in tint)
{
    material.SetFloat4("gTint", tint);
}


// Note this is an old comment
/*
Placing organelles can get downright annoying if you don't
map them out. To make it easier, download a few sheets of hexgrid
off the internet. Before you print them though, set up the axes
properly. See http://i.imgur.com/kTxHFMC.png for how. When you're
drawing out your microbe, keep in mind that it faces forward along
the +r direction.
0 degrees is considered up for the rotation (+r), and you can rotate
in 60 degree intervals counter clockwise.
The colour of the microbe should never be lower than (0.3, 0.3, 0.3)
*/

class OrganelleTemplatePlaced{

    OrganelleTemplatePlaced(const string &in type, int q, int r, int rotation){

        this.type = type;
        this.q = q;
        this.r = r;
        this.rotation = rotation;
    }

    string type;
    int q;
    int r;
    int rotation;
}

class MicrobeTemplate{

    MicrobeTemplate(
        float spawnDensity,
        dictionary compounds,
        array<OrganelleTemplatePlaced@> organelles,
        Float4 colour,
    bool isBacteria,
    string membraneType,
    float membraneRigidity,
    string genus,
    string epithet
    ) {
        this.spawnDensity = spawnDensity;
        this.compounds = compounds;
        this.organelles = organelles;
        this.colour = colour;
        this.isBacteria = isBacteria;
        this.membraneType = membraneType;
        this.membraneRigidity = membraneRigidity;
        this.genus = genus;
        this.epithet = epithet;
    }

    string genus;
    string epithet;
    float spawnDensity;
    dictionary compounds;
    array<OrganelleTemplatePlaced@> organelles;
    Float4 colour;
    bool isBacteria;
    string membraneType;
    float membraneRigidity;
}

class InitialCompound{
    InitialCompound(){

        this.amount = 0;
        this.priority = 1;
    }

    InitialCompound(float amount, int priority = 1){

        this.amount = amount;
        this.priority = priority;
    }

    float amount;
    int priority;
}


const dictionary STARTER_MICROBES = {
    {
        "Default",
        MicrobeTemplate(1/14000,
            {
                // For testing
                {"atp", InitialCompound(60)},
                {"glucose", InitialCompound(60)},
                {"ammonia", InitialCompound(0)},
                {"phosphates", InitialCompound(0)},
                {"hydrogensulfide", InitialCompound(0)},
                {"oxytoxy", InitialCompound(0)},
                {"iron", InitialCompound(3)}
            },
            {
                OrganelleTemplatePlaced("cytoplasm", 0, 0, 0)
            },
            Float4(1, 1, 1, 1),
            // Player starts as bacteria
            true,
            "single",
            0.f,
            "Primum",
            "Thrivium")
    }
};

// For normal microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS =
    {
        {"atp", InitialCompound(30, 300)},
        {"glucose", InitialCompound(30, 300)},
        {"ammonia", InitialCompound(30, 100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(0)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(0)}
    };

// For ferrophillic microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS_IRON =
    {
        {"atp", InitialCompound(30, 300)},
        {"glucose", InitialCompound(10, 30)},
        {"ammonia", InitialCompound(30, 100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(0)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(30, 300)}
    };

// For chemophillic microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS_CHEMO =
    {
        {"atp", InitialCompound(30, 300)},
        {"glucose", InitialCompound(10, 30)},
        {"ammonia", InitialCompound(30, 100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(30, 300)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(0)}
    };


