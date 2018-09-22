#include "organelle_component.as"
#include "microbe_operations.as"

////////////////////////////////////////////////////////////////////////////////
// Class for the single core organelle of any microbe
////////////////////////////////////////////////////////////////////////////////
class NucleusOrganelle : OrganelleComponent{
    // Constructor
    NucleusOrganelle(){

        // Moved to onAddedToMicrobe
    }

    // Overridded from OrganelleComponent.onAddedToMicrobe
    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        auto world = organelle.world;
        auto microbeNode = world.GetComponent_RenderNode(microbeEntity);
    auto speciesComponent = MicrobeOperations::getSpeciesComponent(world, microbeEntity);

        assert(microbeNode !is null, "microbe entity has no RenderNode");

        golgi = world.CreateEntity();
        ER = world.CreateEntity();

        auto sceneNode1 = world.Create_RenderNode(golgi);
        auto model1 = world.Create_Model(golgi, sceneNode1.Node, "golgi.mesh");

        // Tint must be set
        model1.GraphicalObject.setCustomParameter(1, Ogre::Vector4(speciesComponent.colour));

        sceneNode1.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode1.Node.setPosition(Hex::axialToCartesian(q + 1, r + 1));
            //sceneNode1.Node.setOrientation(Ogre::Quaternion(Ogre::Radian(rotation),
               // Ogre::Vector3(0, .5, 1)));
        sceneNode1.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(rotation),
                Ogre::Vector3(0, 1, -1)));
        sceneNode1.Marked = true;

        sceneNode1.Node.removeFromParent();
        microbeNode.Node.addChild(sceneNode1.Node);

        world.SetEntitysParent(golgi, microbeEntity);

        auto sceneNode2 = world.Create_RenderNode(ER);
        auto model2 = world.Create_Model(ER, sceneNode2.Node, "ER.mesh");

        // Tint must be set
        model2.GraphicalObject.setCustomParameter(1, Ogre::Vector4(speciesComponent.colour));

        sceneNode2.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode2.Node.setPosition(Hex::axialToCartesian(q, r+.4));

        sceneNode2.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(rotation+10),
                Ogre::Vector3(0, 1, -1)));
        sceneNode2.Marked = true;

        sceneNode2.Node.removeFromParent();
        microbeNode.Node.addChild(sceneNode2.Node);

        world.SetEntitysParent(ER, microbeEntity);


        // This does nothing...
        // auto speciesColour = speciesComponent.colour;
        // this.colourSuffix = "" + floor(speciesColour.X * 256) +
        //     floor(speciesColour.Y * 256) + floor(speciesColour.Z * 256);

        organelle._needsColourUpdate = true;
    }

    // Overridded from OrganelleComponent.onRemovedFromMicrobe
    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) override {

        auto world = organelle.world;

        world.QueueDestroyEntity(golgi);
        world.QueueDestroyEntity(ER);
        golgi = NULL_OBJECT;
        ER = NULL_OBJECT;
    }

    // void NucleusOrganelle.storage(){
    // return StorageContainer()
    // }

    // void NucleusOrganelle.load(storage){
    // this.golgi = Entity(g_luaEngine.currentGameState.wrapper)
    // this.ER = Entity(g_luaEngine.currentGameState.wrapper)
    // }

    private ObjectID golgi = NULL_OBJECT;
    private ObjectID ER = NULL_OBJECT;
    //! Not sure if this is used for anything
    private string colourSuffix;
}

