////////////////////////////////////////////////////////////////////////////////
// Class for organelles capable of producing compounds.
////////////////////////////////////////////////////////////////////////////////
class ProcessorOrganelle : OrganelleComponent{

    // Constructor
    ProcessorOrganelle() {}

    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

    }
}
