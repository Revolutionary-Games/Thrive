#include "organelle_component.as"

////////////////////////////////////////////////////////////////////////////////
// Class for Organelles capable of producing and storing agents
////////////////////////////////////////////////////////////////////////////////
class AgentVacuole : OrganelleComponent{

    // Constructor
    //
    // @param compound
    //  The agent this organelle produces.
    //
    // @param process
    //  The process that creates the agent this organelle produces.
    AgentVacuole(const string &in compound, const string &in process){

        // Stored as a string as we only need it for dictionary operations
        this.compound = formatUInt(
            SimulationParameters::compoundRegistry().getTypeId(compound));
        this.process = process;
    }

    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(!microbeComponent.specialStorageOrganelles.exists(compound)){
            microbeComponent.specialStorageOrganelles[compound] = 1;
        } else {
            auto value = microbeComponent.specialStorageOrganelles[compound];
            // This needs to be applied like this otherwise it doesn't actually
            // apply the change
            microbeComponent.specialStorageOrganelles[compound] = int(value) + 1;
        }
    }

    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) override {

        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        auto value = microbeComponent.specialStorageOrganelles[compound];
        // This needs to be applied like this otherwise it doesn't actually apply the change
        microbeComponent.specialStorageOrganelles[compound] = int(value) - 1;
    }

    // void AgentVacuole.storage(){
    // auto storage = StorageContainer()
    // storage.set("compoundId", this.compoundId)
    // storage.set("q", this.position.q)
    // storage.set("r", this.position.r)
    // return storage
    // }

    // void AgentVacuole.load(storage){
    // this.compoundId = storage.get("compoundId", 0)
    // this.position = {}
    // this.position.q = storage.get("q", 0)
    // this.position.r = storage.get("r", 0)
    // }

    private string compound;
    private string process;
}
