#include "organelle_component.as"

////////////////////////////////////////////////////////////////////////////////
// Class for organelles capable of producing compounds.
// TODO: Make this handle adding and removing processores from the microbes.
// Right now this does nothing!
////////////////////////////////////////////////////////////////////////////////
class ProcessorOrganelle : OrganelleComponent{

    // Constructor
    //
    // @param arguments.colourChangeFactor
    //  I got absolutely no idea
    //  what this does :P. Also it doesn't seem to be used anymore
    ProcessorOrganelle(float colourChangeFactor = 1.0f){

    }

    // // See organelle_component.lua for more information about the
    // // organelle component methods and the arguments they receive.

    // // Adds a processor to the processoring organelle
    // // The organelle will distribute its capacity between processores
    // //
    // // @param processor
    // // The processor to add
    // void ProcessorOrganelle.addProcessor(processor){
    //     table.insert(this.processes, processor);
    //}

    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

    }
}
