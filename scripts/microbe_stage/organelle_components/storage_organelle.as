#include "organelle_component.as"
#include "microbe_operations.as"

////////////////////////////////////////////////////////////////////////////////
// A storage organelle class
////////////////////////////////////////////////////////////////////////////////
class StorageOrganelle : OrganelleComponent{

    StorageOrganelle(float capacity){

        this.capacity = capacity;
    }

    // See organelle_component.lua for more information about the
    // organelle component methods and the arguments they receive.

    // void StorageOrganelle.load(storage){
    // this.capacity = storage.get("capacity", 100)
    // }

    // void StorageOrganelle.storage(){
    // auto storage = StorageContainer()
    // storage.set("capacity", this.capacity)
    // return storage
    // }

    // Overridded from Organelle.onAddedToMicrobe
    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        microbeComponent.capacity += this.capacity;
    }

    // Overridded from Organelle.onRemovedFromMicrobe
    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) override {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        microbeComponent.capacity -= this.capacity;
    }

    private float capacity;
}
