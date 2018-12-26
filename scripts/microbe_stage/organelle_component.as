
//! Base class for organelle components.
//! \note Unlike Organelle and PlacedOrganelle instanced of classes derived from this
//! are directly added to PlacedOrganelle so these classes may change state in
//! the update methods
abstract class OrganelleComponent{

    // Constructor.
    OrganelleComponent(){

    }

    // Event handler for an organelle added to a microbe.
    //
    // @param microbe
    //  The microbe this organelle is added to.
    //
    // @param q
    //  q component of the organelle relative position in the microbe,
    //  in axial coordinates (see hex.lua).
    //
    // @param r
    //  r component of the organelle relative position in the microbe,
    //  in axial coordinates (see hex.lua).
    //
    // @param rotation
    //  The rotation this organelle has on the microbe.
    //  it can be either 0, 60, 120, 180, 240 or 280.
    //
    // @param organelle
    //  The organelle object that is made up of these components.
    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) {
        assert(false, "OrganelleComponent::onAddedToMicrobe not overridden");
    }

    // Event handler for an organelle removed from a microbe.
    //
    // @param microbe
    //  The microbe this organelle is removed from.
    // These aren't passed, at least not when the microbe is dying so they are now removed
    // from here as well
    // @param organelle
    //  MUST BE THE SAME ORGANELLE this was added to
    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) {

    }

    //  Function executed at regular intervals
    //
    // @param microbe
    //  The microbe this organelle is attached.
    //
    // @param organelle
    //  The organelle that has this component.
    //
    // @param logicTime
    //  The time transcurred (in milliseconds) between this call
    //  to OrganelleComponent:update() and the previous one.
    void
    update(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle,
        int logicTime
    ) {

    }

    // Should hide any created entities of this component. Used to hide things on death
    void hideEntity(PlacedOrganelle@ organelle)
    {

    }


    // // Function for saving organelle information.
    // // If an organelle depends on an atribute, then it should be saved
    // // the data gets retrieved later by OrganelleComponent:load().
    // // The return value should be a new StorageContainer object
    // // filled with the data to save.
    // StorageContainer@ storage(){
    //     return StorageContainer();
    // }

    // // Function for loading organelle information.
    // //
    // // @param storage
    // //  The StorageContainer object that has the organelle information
    // //  (the one saved in OrganelleComponent:storage()).
    // void OrganelleComponent:load(StorageContainer@ storage){

    // }
}

