// Holds config file contents, translated into AngelScript objects
#include "agents.as"
#include "organelle.as"

// Global defines
const auto CLOUD_SPAWN_RADIUS = 150;
const auto POWERUP_SPAWN_RADIUS = 85;
const auto DEFAULT_SPAWN_DENSITY = 1/25000.f;
const auto STARTING_SPAWN_DENSITY = 45000.0f;
const auto MAX_SPAWN_DENSITY = 20000.0f;

// Cell Spawn Variation
const auto MIN_SPAWN_DISTANCE = -5000.0f;
const auto MAX_SPAWN_DISTANCE = 5000.0f;

// Cell Colors
const auto MIN_COLOR = 0.0f;
const auto MAX_COLOR = 0.9f;
// Too subtle?
const auto MIN_COLOR_MUTATION = -0.005f;
const auto MAX_COLOR_MUTATION = 0.005f;

const auto MIN_OPACITY = 0.5f;
const auto MAX_OPACITY = 1.8f;

const auto MIN_OPACITY_CHITIN = 0.4f;
const auto MAX_OPACITY_CHITIN = 1.2f;
// Min Opacity Mutation
const auto MIN_OPACITY_MUTATION = -0.01f;
const auto MAX_OPACITY_MUTATION = 0.01f;

//not const because we want to change these
//current atmospheric oxygen percentage in modern times
auto OXYGEN_PERCENTAGE = 0.21f;
//co2 percentage (over expressed as .09% is the percenatge of all
//non-nitrogen-non-oxygen gasses in our atmosphere)
auto CARBON_DIOXIDE_PERCENTAGE = 0.009f;

// Mutation Variables
const auto MUTATION_BACTERIA_TO_EUKARYOTE = 1;
const auto MUTATION_CREATION_RATE = 0.1f;
const auto MUTATION_DELETION_RATE = 0.1f;

//Removal cost
const auto ORGANELLE_REMOVE_COST = 10;
// Spawn Radius
const auto MICROBE_SPAWN_RADIUS = 150;
const auto BACTERIA_SPAWN_RADIUS = 150;

// Max fear and agression and activity
const auto MAX_SPECIES_AGRESSION = 400.0f;
const auto MAX_SPECIES_FEAR = 400.0f;
const auto MAX_SPECIES_ACTIVITY = 400.0f;
const auto MAX_SPECIES_FOCUS = 400.0f;

// Personality Mutation
const auto MAX_SPECIES_PERSONALITY_MUTATION = 20.0f;
const auto MIN_SPECIES_PERSONALITY_MUTATION = -20.0f;

// Bacterial CXOlony configuration
const auto MIN_BACTERIAL_COLONY_SIZE = 1;
const auto MAX_BACTERIAL_COLONY_SIZE = 5;
const auto MIN_BACTERIAL_LINE_SIZE =  3;
const auto MAX_BACTERIAL_LINE_SIZE = 6;

// What is divided during fear and aggression calculations in the AI
const auto AGRESSION_DIVISOR = 25.0f;
const auto FEAR_DIVISOR = 25.0f;
const auto ACTIVITY_DIVISOR = 100.0f;
const auto FOCUS_DIVISOR = 100.0f;

// Cooldown for AI for toggling engulfing
const uint AI_ENGULF_INTERVAL=300;

// if you are gaining less then this amount of compound per turn you are much more likely to turn randomly
const auto AI_COMPOUND_BIAS = -10.0f;
// Osmoregulation ATP cost
const auto ATP_COST_FOR_OSMOREGULATION = 1.0f;

//Purge Divisor
const auto COMPOUND_PURGE_MODIFIER = 2.0f;

// BASE MOVEMENT COST ATP cost
// Cancels out a little bit more then one cytoplasm's glycolysis
const auto BASE_MOVEMENT_ATP_COST = 1.0f;

// The player's name
const auto PLAYER_NAME = "Player";

const auto DEFAULT_HEALTH = 100;
// Amount of health pers econd regened in percent
const auto REGENERATION_RATE = 1.0f;

// Movement stuff
const auto FLAGELLA_ENERGY_COST = 7.0f;
const auto FLAGELLA_BASE_FORCE = 0.7f;
const auto CELL_BASE_THRUST = 1.6f;
// is set by this and modified by applyCellMovement like the player later
const auto AI_BASE_MOVEMENT = 1.0f;
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
const uint BANDWIDTH_REFILL_DURATION = 800;

// No idea what this does (if anything), but it isn't used in the
// process system, or when ejecting compounds.
const float STORAGE_EJECTION_THRESHHOLD = 0.8;

// The amount of time between each loop to maintaining a fill level
// below STORAGE_EJECTION_THRESHHOLD and eject useless compounds
const uint EXCESS_COMPOUND_COLLECTION_INTERVAL = 1000;

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
const double ENGULFED_MOVEMENT_DIVISION = 1.7f;

// The amount of ATP per second spent on being on engulfing mode.
const float ENGULFING_ATP_COST_SECOND = 1.5;

// The minimum HP ratio between a cell and a possible engulfing victim.
const float ENGULF_HP_RATIO_REQ = 1.5f;

// The amount of hp per second of damage
const float ENGULF_DAMAGE = 80.0f;

// Oxytoxy Damage
const float OXY_TOXY_DAMAGE = 10.0f;

// Cooldown between agent emissions, in milliseconds.
const uint AGENT_EMISSION_COOLDOWN = 2000;

// Iron amounts per chunk.
// big iron ejects ten per 20 clicks , so about 30 per second, so ill give it enough for 1000 seconds)
const double IRON_PER_BIG_CHUNK = 30000.0f;
// small iron ejects 3 per 20 clicks , so about 9 per second, so ill give it enough for 1000 seconds aswell
const double IRON_PER_SMALL_CHUNK = 9000.0f;
//Auto Evo Values
const int CREATURE_DEATH_POPULATION_LOSS = -60;
const int CREATURE_KILL_POPULATION_GAIN = 50;
const int CREATURE_REPRODUCE_POPULATION_GAIN = 50;
const int CREATURE_ESCAPE_POPULATION_GAIN = 50;

// TODO: move these into gamestate (this is very dirty)
// must be global
int chloroplast_Organelle_Number = 0;
int toxin_Organelle_Number = 0;
bool chloroplast_unlocked = false;
bool toxin_unlocked = false;


// this count the toxin Organelle Number
bool toxin_number(){
    toxin_Organelle_Number = toxin_Organelle_Number + 1;
    LOG_WRITE("toxin_Organelle_Number: " + toxin_Organelle_Number);
    if(toxin_Organelle_Number >= 3){ // 3 is an example
        unlockToxinIfStillLocked();
        toxin_call_Notification();
    }

    return true;
}

// this count the chloroplast Organelle Number
void chloroplast_number(){
    chloroplast_Organelle_Number = chloroplast_Organelle_Number + 1;
    LOG_WRITE("chloroplast_Organelle_Number: " + chloroplast_Organelle_Number);

    if(chloroplast_Organelle_Number >= 3){  // 3 is an example
        unlockChloroplastIfStillLocked();
        chloroplast_call_Notification();
    }
}

void playOrganellePickupSound(){
    GetEngine().GetSoundDevice().Play2DSoundEffect("Data/Sound/microbe-pickup-organelle.ogg");
}

// TODO: remove this code duplication

// this where the Unlock Happen
void unlockToxinIfStillLocked(){
    if(!GetThriveGame().playerData().lockedMap().isLocked("Toxin"))
        return;

    showMessage("Toxin Unlocked!");
    GetThriveGame().playerData().lockedMap().unlock("Toxin");

    playOrganellePickupSound();
}

//this where the Unlock Happen
void unlockChloroplastIfStillLocked(){

    if(!GetThriveGame().playerData().lockedMap().isLocked("chloroplast"))
        return;

    showMessage("Chloroplast Unlocked!");
    GetThriveGame().playerData().lockedMap().unlock("chloroplast");

    playOrganellePickupSound();
}


void chloroplast_call_Notification(){
    if(chloroplast_unlocked == false){
        // global_activeMicrobeStageHudSystem.chloroplastNotificationenable();
        GetEngine().GetEventHandler().CallEvent(GenericEvent("chloroplastNotificationenable"));
        chloroplast_unlocked = true;
    }
}

void toxin_call_Notification(){
    if(toxin_unlocked == false){
        // global_activeMicrobeStageHudSystem.toxinNotificationenable();
        GetEngine().GetEventHandler().CallEvent(GenericEvent("toxinNotificationenable"));
        toxin_unlocked = true;
    }
}

// TODO: move this to where axialToCartesian is defined
// We should use Int2 instead, or MAYBE a derived class defined in c++ if we wanna be really fancy...
/*
class AxialCoordinates{

    AxialCoordinates(int q, int r){

        this.q = q;
        this.r = r;
    }

    // q and r are radial coordinates instead of cartesian
    int q;
    int r;
}
*/

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
    MEMBRANE_TYPE speciesMembraneType
    ) {
        this.spawnDensity = spawnDensity;
        this.compounds = compounds;
        this.organelles = organelles;
        this.colour = colour;
        this.isBacteria = isBacteria;
        this.speciesMembraneType = speciesMembraneType;
    }

    float spawnDensity;
    dictionary compounds;
    array<OrganelleTemplatePlaced@> organelles;
    Float4 colour;
    bool isBacteria;
    MEMBRANE_TYPE speciesMembraneType;
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
            //for testing
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
            false,
            MEMBRANE_TYPE::MEMBRANE)
    }
};






