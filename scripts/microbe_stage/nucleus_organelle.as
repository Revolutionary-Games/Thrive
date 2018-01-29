////////////////////////////////////////////////////////////////////////////////
// Class for the single core organelle of any microbe
////////////////////////////////////////////////////////////////////////////////
#include "organelle_component.as"

class NucleusOrganelle : OrganelleComponent{
    // Constructor
    NucleusOrganelle(dictionary arguments, dictionary data){

        //making sure this doesn't run when load() is called
        if(arguments == null && data == null){
            return;
        }

        // Moved to onAddedToMicrobe
    }

    // See organelle_component.as for more information about the 
    // organelle component methods and the arguments they receive.

    // Overridded from Organelle.onAddedToMicrobe
    void
    onAddedToMicrobe(
        Microbe@ microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        auto world = microbeEntity.getWorld();
        auto microbeNode = microbeEntity.getRenderNodeComponent();

        assert(microbeNode !is null, "microbe entity has no RenderNode");

        golgi = world.CreateEntity();
        ER = world.CreateEntity();
        
        Float2 xy = axialToCartesian(q-1, r-1);
        
        auto sceneNode1 = world.Create_RenderNode(golgi);
        auto model1 = world.Create_Model(golgi, "golgi.mesh");

        sceneNode1.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode1.Node.setPosition(Ogre::Vector3(xy.X, 0, xy.Y));
        sceneNode1.Node.setOrientation = Ogre::Quaternion(Ogre::Degree(rotation),
            Ogre::Vector3(0, 1, 0));
        sceneNode1.Marked = true;
        
        sceneNode1.Node.setParent(microbeNode.Node);

        world.SetEntityParent(microbeEntity.getEntityID(), golgi);

        auto sceneNode2 = world.Create_RenderNode(ER);
        auto model2 = world.Create_Model(ER, "ER.mesh");

        sceneNode2.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode2.Node.setOrientation = Ogre::Quaternion(Ogre::Degree(rotation + 10),
            Ogre::Vector3(0, 1, 0));
        sceneNode2.Marked = true;

        sceneNode2.Node.setParent(microbeNode.Node);
        
        // If we are not in the editor, get the color of this species.
        auto speciesComponent = microbeEntity.getSpeciesComponent();
        if(microbeEntity !is null){
            auto speciesColour = speciesComponent.colour;
            this.colourSuffix = "" + floor(speciesColour.X * 256) +
                floor(speciesColour.Y * 256) + floor(speciesColour.Z * 256);
        }
        
        organelle._needsColourUpdate = true;
    }

    void
    onRemovedFromMicrobe(
        Microbe@ microbeEntity,
        int q, int r
    ) override {

        world.DestroyEntity(golgi);
        world.DestroyEntity(ER);
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

