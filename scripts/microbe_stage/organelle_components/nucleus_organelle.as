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

        assert(microbeNode !is null, "microbe entity has no RenderNode");

        if(!IsInGraphicalMode())
            return;

        golgi = world.CreateEntity();
        ER = world.CreateEntity();

        auto sceneNode1 = world.Create_RenderNode(golgi);
        auto model1 = world.Create_Model(golgi, "golgi.fbx",
            getOrganelleMaterialWithTexture("GolgiApparatus.png", organelle.species.colour));

        sceneNode1.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode1.Node.setPosition(Hex::axialToCartesian(q + 0.9f, r + 0.9f));
        sceneNode1.Node.setOrientation(bs::Quaternion(bs::Degree(rotation+180),
                bs::Vector3(0, 1, 0)));
        sceneNode1.Marked = true;

        sceneNode1.Node.setParent(microbeNode.Node, false);

        world.SetEntitysParent(golgi, microbeEntity);

        auto sceneNode2 = world.Create_RenderNode(ER);
        auto model2 = world.Create_Model(ER, "ER.fbx",
            getOrganelleMaterialWithTexture("ER.png", organelle.species.colour));

        sceneNode2.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        sceneNode2.Node.setPosition(Hex::axialToCartesian(q, r + 1.6f));

        sceneNode2.Node.setOrientation(bs::Quaternion(bs::Degree(rotation+190),
                bs::Vector3(0, 1, 0)));
        sceneNode2.Marked = true;

        sceneNode2.Node.setParent(microbeNode.Node, false);

        world.SetEntitysParent(ER, microbeEntity);

        organelle._needsColourUpdate = true;
    }

    // Overridded from OrganelleComponent.onRemovedFromMicrobe
    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) override {

        auto world = organelle.world;
        // When nucleus is removed we have to manually destory this entities
        // They are attached to microbe so remove nucleus don't remove them
        if(golgi != NULL_OBJECT)
            world.QueueDestroyEntity(golgi);

        if(ER != NULL_OBJECT)
            world.QueueDestroyEntity(ER);
        // These also should be destroyed with the cell as they are parented
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

    void hideEntity(PlacedOrganelle@ organelle) override
    {
        auto renderNode = organelle.world.GetComponent_RenderNode(golgi);
        if(renderNode !is null && renderNode.Node.valid())
            renderNode.Node.removeFromParent();

        @renderNode = organelle.world.GetComponent_RenderNode(ER);
        if(renderNode !is null && renderNode.Node.valid())
            renderNode.Node.removeFromParent();
    }

    private ObjectID golgi = NULL_OBJECT;
    private ObjectID ER = NULL_OBJECT;
    //! Not sure if this is used for anything
    private string colourSuffix;
}

