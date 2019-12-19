#include "microbe.as"
#include "hex.as"



Float4 calculateHSLForOrganelle(const Float4 &in oldColour)
{
    // Get hue saturation and brightness for the colour
    float saturation = 0;
    float brightness = 0;
    float hue = 0;

    bs::Color(oldColour).getHSB(hue, saturation, brightness);
    return bs::Color::fromHSB(hue, saturation*2, brightness);
}


//! These are placed either on an actual microbe where they are
//! onAddedToMicrobe() OR on templates where these just have positions
//! set and should be duplicated for the purpose of adding to a
//! microbe
class PlacedOrganelle : SpeciesStoredOrganelleType{

    PlacedOrganelle(OrganelleTemplate@ organelle, int q, int r, int rotation)
    {
        @this.organelle = organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;

        _commonConstructor();
    }

    //! Takes type from another PlacedOrganelle
    PlacedOrganelle(PlacedOrganelle@ typefromother, int q, int r, int rotation)
    {
        @this.organelle = typefromother.organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;

        _commonConstructor();
    }

    //! Takes everything that's sensible to copy from another PlacedOrganelle
    PlacedOrganelle(PlacedOrganelle@ other)
    {
        @this.organelle = other.organelle;
        this.q = other.q;
        this.r = other.r;
        this.rotation = other.rotation;

        _commonConstructor();
    }

    ~PlacedOrganelle()
    {
        if(microbeEntity != NULL_OBJECT){

            LOG_ERROR("PlacedOrganelle (" + organelle.name + ") not removed from microbe "
                "before it was destroyed, microbe: " + microbeEntity);
        }
    }

    private void _commonConstructor()
    {
        // Sanity check
        if(organelle is null)
            assert(false, "PlacedOrganelle created with null OrganelleTemplate");

        // Create instances of components //
        for(uint i = 0; i < organelle.getComponentCount(); ++i){
            auto@ component = cast<OrganelleComponent>(organelle.createComponent(i));

            if(component is null)
                assert(false,
                    "failed to cast created organelle component to OrganelleComponent");

            components.insertLast(component);
        }

        compoundsLeft = organelle.initialComposition;
    }

    // Called by Microbe.update
    //
    // Override this to make your organelle class do something at regular intervals
    //
    // @param elapsed
    //  The time since the last call to update()
    void update(float elapsed)
    {
        // If the organelle is supposed to be another color.
        if(_needsColourUpdate){
            updateColour();
        }

        // Update each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){
            components[i].update(microbeEntity, this, elapsed);
        }
    }

    protected void updateColour()
    {
        if(organelleEntity == NULL_OBJECT)
            return;

        auto model = world.GetComponent_Model(organelleEntity);

        if(model !is null && IsInGraphicalMode()){

            updateMaterialTint(model.Material, calculateHSLForOrganelle(this.species.colour));
        }

        _needsColourUpdate = false;
    }

    // Gives organelles more compounds to grow
    void growOrganelle(CompoundBagComponent@ compoundBagComponent)
    {
        float totalTaken = 0;

        const auto compoundKeys = compoundsLeft.getKeys();
        for(uint i = 0; i < compoundKeys.length(); ++i){

            const auto compoundKey = compoundKeys[i];

            float amountNeeded;
            if(!compoundsLeft.get(compoundKey, amountNeeded)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }

            if(amountNeeded <= 0.f)
                continue;

            // Take compounds if the cell has what we need
            // TODO: caching the types would be nice
            const auto compoundId = parseInt(compoundKey);

            const auto amountAvailable = compoundBagComponent.getCompoundAmount(compoundId)
                // This controls how much of a certain compound must exist before we take some
                - ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if(amountAvailable <= 0.f)
                continue;

            // We can take some
            const auto amountToTake = min(amountNeeded, amountAvailable);

            const float amount = compoundBagComponent.takeCompound(compoundId, amountToTake);
            float left = float(compoundsLeft[compoundKey]);
            left -= amount;

            if(left < 0.0001)
                left = 0;
            compoundsLeft[compoundKey] = left;

            totalTaken += amount;
        }

        if(totalTaken > 0){
            // Calculate the new growth value.
            recalculateGrowthValue();
        }
    }

    void recalculateGrowthValue()
    {
        duplicateProgress = 1.f - (calculateCompoundsLeft() / organelle.organelleCost);

        applyScale();
    }

    float getGrowthProgress() const
    {
        return duplicateProgress;
    }

    // Calculates total number of compounds left until this organelle can divide
    float calculateCompoundsLeft() const
    {
        float totalLeft = 0;

        auto compoundKeys = compoundsLeft.getKeys();
        for(uint i = 0; i < compoundKeys.length(); ++i){

            float amount;
            if(!compoundsLeft.get(compoundKeys[i], amount)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }

            totalLeft += amount;
        }

        return totalLeft;
    }

    //! Calculates how much compounds this organelle has absorbed
    //! already, adds to the dictionary
    float calculateAbsorbedCompounds(dictionary &inout result) const
    {
        float totalAbsorbed = 0;

        const auto compoundKeys = compoundsLeft.getKeys();
        for(uint i = 0; i < compoundKeys.length(); ++i){

            float amountLeft;
            if(!compoundsLeft.get(compoundKeys[i], amountLeft)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }

            float amountTotal;
            if(!organelle.initialComposition.get(compoundKeys[i], amountTotal)){
                LOG_ERROR("Invalid type in organelle.initialComposition");
                continue;
            }

            float alreadyInResult;
            if(!result.get(compoundKeys[i], alreadyInResult))
                alreadyInResult = 0;

            const auto absorbed = amountTotal - amountLeft;

            result.set(compoundKeys[i], alreadyInResult + absorbed);
            totalAbsorbed += absorbed;
        }

        return totalAbsorbed;
    }

    // Resets the state. Used after dividing?
    void reset()
    {
        // Return the compound bin to its original state
        duplicateProgress = 0.f;

        // Assign (doesn't only copy a reference)
        compoundsLeft = organelle.initialComposition;

        applyScale();

        // If it was split from a primary organelle, destroy it.
        if(isDuplicate){
            MicrobeOperations::removeOrganelle(world, microbeEntity,
                {this.q, this.r});
        } else {
            wasSplit = false;
        }
    }

    void applyScale()
    {
        // Nucleus isn't scaled
        if(organelle.hasComponent("NucleusOrganelle"))
            return;

        if(IsInGraphicalMode()){
            // Scale the organelle model to reflect the new size.
            // This might be able to be skipped as the recalculateBin method will always set
            // the correct scale
            RenderNode@ sceneNode = world.GetComponent_RenderNode(
                organelleEntity);

            sceneNode.Scale = Float3(1 + duplicateProgress, 1 + duplicateProgress,
                1 + duplicateProgress) * HEX_SIZE;
            sceneNode.Marked = true;
        }
    }


    // Called by a microbe when this organelle has been added to it
    //
    // @param microbe
    //  The organelle's new owner
    //
    // @param q, r
    //  Axial coordinates of the organelle's center
    // @param world
    //  the world the microbe entity is in. This is used to retrieve various components
    // @note This is quite an expensive method as this creates a new entity with
    //  multiple components
    void onAddedToMicrobe(ObjectID microbe, CellStageWorld@ world,
        PhysicsShape@ collisionShape)
    {
        if(microbeEntity != NULL_OBJECT){

            // It would be a huge mess to handle this here so we don't bother.
            // call MicrobeOperations::removeOrganelle
            assert(false, "onAddedToMicrobe called before this PlacedOrganelle was " +
                "removed from previous microbe. Previous entity: " + microbeEntity);
        }

        @this.beingConstructedShape = collisionShape;

        @this.world = world;

        assert(this.world !is null, "trying to create placed organelle without world");

        microbeEntity = microbe;

        // This should be the only species check any organelle ever makes.
        // TODO: Species is now a reference counted type and it should be safe to
        // store a handle to it now.
        @this.species = MicrobeOperations::getSpecies(world, microbeEntity);

        // Our coordinates are already set when this is called
        // so just cache this
        this.cartesianPosition = Hex::axialToCartesian(q, r);

        float hexSize = HEX_SIZE;

        // Scale the hex size down for bacteria
        if(this.species.isBacteria)
            hexSize /= 2.f;

        assert(organelleEntity == NULL_OBJECT, "PlacedOrganelle already had an entity");


        RenderNode@ renderNode;

        // Graphics setup
        if(IsInGraphicalMode()){

            organelleEntity = world.CreateEntity();

            // Automatically destroyed if the parent is destroyed
            world.SetEntitysParent(organelleEntity, microbeEntity);

            _needsColourUpdate = true;

            @renderNode = world.Create_RenderNode(organelleEntity);
            renderNode.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
            renderNode.Marked = true;

            // For performance reasons we set the position here directly
            // instead of with the position system
            const Float3 offset = organelle.calculateModelOffset();
            renderNode.Node.setPosition(offset + this.cartesianPosition);
            renderNode.Node.setOrientation(bs::Quaternion(bs::Degree(180),
                    bs::Vector3(0, 1, 0))*bs::Quaternion(bs::Degree(rotation),
                    bs::Vector3(0, -1, 0)));

            auto parentRenderNode = world.GetComponent_RenderNode(
                microbeEntity);

            renderNode.Node.setParent(parentRenderNode.Node, false);

            // Adding a mesh for the organelle.
            if(organelle.mesh != ""){
                auto model = world.Create_Model(organelleEntity, organelle.mesh,
                    getOrganelleMaterialWithTexture(organelle.texture,
                        calculateHSLForOrganelle(this.species.colour)));
            }
        }

        // Add hex collision shapes
        // Applying the rotation here is new, hopefully doesn't break collisions
        auto hexes = organelle.getRotatedHexes(rotation);

        for(uint i = 0; i < hexes.length(); ++i){

            // Also add our offset to the hex offset
            Float3 translation = Hex::axialToCartesian(hexes[i].X, hexes[i].Y) +
                this.cartesianPosition;

            // Scale for bacteria physics. This might be pretty expensive to be in this loop...
            if(this.species.isBacteria)
                translation /= 2.f;

            PhysicsShape@ hexCollision = world.GetPhysicalWorld().CreateSphere(hexSize * 2);

            collisionShape.AddChildShape(hexCollision, translation);
            _addedCollisions.insertLast(hexCollision);
        }

        // Add each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){

            // This cannot affect the collision here. It needs to do
            // Organelle::addHex during construction
            components[i].onAddedToMicrobe(microbeEntity, q, r, rotation, this);
        }

        @this.beingConstructedShape = null;
    }

    //! Alternative to onRemovedFromMicrobe called when the microbe
    //! and this is being destroyed at the same time. For example when
    //! closing the game
    void onDestroyedWithMicrobe(ObjectID microbe){

        // TODO: do these need handling?
        // //iterating on each OrganelleComponent
        // for(uint i = 0; i < components.length(); ++i){

        //     components[i].onDestroyedWithMicrobe(microbeEntity, this);
        // }

        // The organelle entity is automatically destroyed because it
        // is parented to the microbe entity
        organelleEntity = NULL_OBJECT;
        microbeEntity = NULL_OBJECT;
        @world = null;
    }

    // Called by a microbe when this organelle has been removed from it
    //
    // @param microbe
    //  The organelle's previous owner
    // @todo This actually crashes the game in
    //  collisionShape.CompoundCollisionRemoveSubCollision so someone should figure out how
    //  to fix that if this is needed (currently species are just destroyed so this isn't used)
    void onRemovedFromMicrobe(ObjectID microbe, PhysicsShape@ collisionShape)
    {
        //LOG_INFO("PlacedOrganelle (" + organelle.name + ") removed from: " + microbeEntity);
        // PrintCallStack();

        //iterating on each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){
            components[i].onRemovedFromMicrobe(microbeEntity, this /*, q, r*/);
        }

        // We can do a quick remove from the destructor
        // Remove our sub collisions //
        // This null check is here because this is called when cels are killed and
        // the physics bodies are long gone
        if(collisionShape !is null){
            for(uint i = 0; i < _addedCollisions.length(); ++i){

                collisionShape.RemoveChildShape(_addedCollisions[i]);
            }
        }

        _addedCollisions.resize(0);

        world.QueueDestroyEntity(organelleEntity);
        organelleEntity = NULL_OBJECT;
        microbeEntity = NULL_OBJECT;
        @world = null;
    }

    //! This method is provided for OrganelleComponents to be able to add extra
    //! collision shapes
    //! \note Untested
    void addChildCollision(PhysicsShape@ shape, Float3 offset, Float4 orientation)
    {
        assert(beingConstructedShape !is null,
            "addChildCollision shape called while not being added to a microbe");
        beingConstructedShape.AddChildShape(shape, offset, orientation);
        _addedCollisions.insertLast(shape);
    }

    //! \todo This might not work anymore
    void hideEntity()
    {
        auto renderNode = world.GetComponent_RenderNode(organelleEntity);
        if(renderNode !is null && renderNode.Node.valid()){
            renderNode.Hidden = true;
            renderNode.Marked = true;
        }

        // Also hide components as they also can have entities
        for(uint i = 0; i < components.length(); ++i){
            components[i].hideEntity(this);
        }
    }
    // ------------------------------------ //

    const OrganelleTemplate@ organelle;

    // q and r are radial coordinates instead of cartesian
    // Could use the class AxialCoordinates here
    int q;
    int r;
    int rotation;

    // Filled from the above parameters when added to a microbe
    Float3 cartesianPosition = Float3(0, 0, 0);

    // Whether or not this organelle has already divided.
    bool wasSplit = false;

    // If this organelle is a duplicate of another organelle caused by splitting.
    bool isDuplicate = false;

    //! Fraction towards duplicating progress
    float duplicateProgress = 0.f;

    // The compounds left to divide this organelle.
    // Decreases every time a required compound is absorbed.
    private dictionary compoundsLeft;

    array<OrganelleComponent@> components;

    ObjectID microbeEntity = NULL_OBJECT;
    ObjectID organelleEntity = NULL_OBJECT;
    const Species@ species;

    // This is the world in which the entities for this organelle exists
    CellStageWorld@ world;

    PlacedOrganelle@ sisterOrganelle = null;

    // Used for removing the added sub collisions when we are removed from a microbe
    private array<PhysicsShape@> _addedCollisions;

    // Used for addChildCollision, only valid during onAddedToMicrobe
    private PhysicsShape@ beingConstructedShape;

    bool _needsColourUpdate = false;
}
