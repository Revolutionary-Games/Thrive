// Holds config file contents, translated into AngelScript objects
#include "agents.as"

// TODO: move these into gamestate
// must be global
int chloroplast_Organelle_Number = 0;
int toxin_Organelle_Number = 0;
bool chloroplast_unlocked = false;
bool toxin_unlocked = false;

void showMessage(const string &in message){

    LOG_WRITE("TODO: showMessage needs implementing, this is the message: " + message);
}


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

    GetEngine().GetSoundDevice().PlaySoundEffect("microbe-pickup-organelle.ogg");
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

abstract class Organelle{

    Organelle(string name){

        this._name = name;
    }

    // Prevent modification
    string name {

        get {
            return _name;
        }
    }

    private string _name;
}

class Nucleus : Organelle{

    Nucleus(){

        super("nucleus");
    }
}

class Mitochondrion : Organelle{

    Mitochondrion(){

        super("mitochondrion");
    }
}

class Vacuole : Organelle{

    Vacuole(){

        super("vacuole");
    }
}

class Flagellum : Organelle{

    Flagellum(){

        super("flagellum");
    }
}


class PlacedOrganelle{

    PlacedOrganelle(Organelle@ organelle, int q, int r, int rotation){

        @this.organelle = @organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;
    }
    
    Organelle@ organelle;
    // No clue what q and r are
    int q;
    int r;
    int rotation;
}

class MicrobeTemplate{

    MicrobeTemplate(
        float spawnDensity,
        dictionary compounds,
        array<PlacedOrganelle@> organelles,
        Float4 colour
    ) {
        this.spawnDensity = spawnDensity;
        this.compounds = compounds;
        this.organelles = organelles;
        this.colour = colour;
    }

    float spawnDensity;
    dictionary compounds;
    array<PlacedOrganelle@> organelles;
    Float4 colour;
}

const dictionary STARTER_MICROBES = {
    {
        "Default", MicrobeTemplate(1/14000,
            {
                {"atp", 60},
                {"glucose", 5},
                {"oxygen", 10}
            },
            {
                PlacedOrganelle(Nucleus(), 0, 0, 0),
                PlacedOrganelle(Mitochondrion(), -1, -2, 240),
                PlacedOrganelle(Vacuole(), 1, -3, 0),
                PlacedOrganelle(Flagellum(), -1, -3, 0),
                PlacedOrganelle(Flagellum(), 1, -4, 0)
            },
            Float4(1, 1, 1, 1))
    }
};






