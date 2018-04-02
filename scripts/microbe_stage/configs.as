// Holds config file contents, translated into AngelScript objects
#include "agents.as"
#include "organelle.as"

// Global defines
const auto CLOUD_SPAWN_RADIUS = 75;

const auto POWERUP_SPAWN_RADIUS = 85;

const auto DEFAULT_SPAWN_DENSITY = 1/25000.f;

const auto MIN_COLOR = 0.3f;
const auto MAX_COLOR = 1.0f;
const auto MIN_OPACITY = 0.3f;
const auto MAX_OPACITY = 5.0f;

const auto MUTATION_CREATION_RATE = 0.1f;
const auto MUTATION_DELETION_RATE = 0.1f;

const auto MICROBE_SPAWN_RADIUS = 85;

const auto PLAYER_NAME = "Player";


// TODO: move these into gamestate
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
        Float4 colour
    ) {
        this.spawnDensity = spawnDensity;
        this.compounds = compounds;
        this.organelles = organelles;
        this.colour = colour;
    }

    float spawnDensity;
    dictionary compounds;
    array<OrganelleTemplatePlaced@> organelles;
    Float4 colour;
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
        "Default", MicrobeTemplate(1/14000,
            {
                {"atp", InitialCompound(60)},
                {"glucose", InitialCompound(5)},
                {"oxygen", InitialCompound(10)}
            },
            {
                OrganelleTemplatePlaced("nucleus", 0, 0, 180),
                OrganelleTemplatePlaced("mitochondrion", -1, 3, 240),
                OrganelleTemplatePlaced("vacuole", 1, 2, 0),
                OrganelleTemplatePlaced("flagellum", 1, 3, 0),
                OrganelleTemplatePlaced("flagellum", -1, 4, 0)
            },
            Float4(1, 0, 1, 1))
    }
};






