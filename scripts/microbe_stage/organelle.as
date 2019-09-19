#include "microbe.as"
#include "hex.as"

// How fast organelles grow.
const auto GROWTH_SPEED_MULTILPIER = 0.5f / 1000;

// Percentage of the compounds that compose the organelle released
// upon death (between 0.0 and 1.0).
const auto COMPOUND_MAKEUP_RELEASE_PERCENTAGE = 0.9f;
const auto COMPOUND_RELEASE_PERCENTAGE = 0.9f;


//! \todo Replace with Int2
class Hex{

    Hex(int q, int r){
        this.q = q;
        this.r = r;
    }

    int q;
    int r;
}

Float4 calculateHSLForOrganelle(const Float4 &in oldColour)
{
    // Get hue saturation and brightness for the colour
    float saturation = 0;
    float brightness = 0;
    float hue = 0;

    bs::Color(oldColour).getHSB(hue, saturation, brightness);
    return bs::Color::fromHSB(hue, saturation*2, brightness);
}


//! Class that is given a definition of organelle and it represents its data
//! \note Before there was an instance of this class for each microbe. Now this is global and
//! each microbe has a PlacedOrganelle instance instead (which also has many properties
//! that this class used to have)
class Organelle{

    Organelle(const OrganelleParameters &in parameters)
    {
        _name = parameters.name;
        mass = parameters.mass;
        mesh = parameters.mesh;
        texture = parameters.texture;
        gene = parameters.gene;
        chanceToCreate = parameters.chanceToCreate;
        prokaryoteChance = parameters.prokaryoteChance;
        mpCost = parameters.mpCost;

        initialComposition = parameters.initialComposition;
        components = parameters.components;
        processes = parameters.processes;

        // Sanity check processes //
        for(uint i = 0; i < processes.length(); ++i){

            if(processes[i] is null){

                assert(false, "Organelle created with null process at index: " + i);
            }
        }

        // Add hexes //
        for(uint i = 0; i < parameters.hexes.length(); ++i){

            addHex(parameters.hexes[i].X, parameters.hexes[i].Y);
        }

        // Calculate organelleCost and compoundsLeft//
        // This method sets organelleCost
        calculateCost(initialComposition);
    }

    ~Organelle()
    {

    }

    protected void calculateCost(dictionary composition)
    {
        organelleCost = 0;

        auto keys = composition.getKeys();

        for(uint i = 0; i < keys.length(); ++i){

            const auto compoundName = keys[i];
            int amount;

            if(!composition.get(keys[i], amount)){

                LOG_ERROR("Invalid value in calculateCost composition");
                continue;
            }

            // compoundsLeft[compoundName] = amount;
            initialComposition[compoundName] = amount;
            organelleCost += amount;
        }
    }

    // Adds a hex to this organelle
    //
    // @param q, r
    //  Axial coordinates of the new hex
    //
    // @returns success
    //  True if the hex could be added, false if there already is a hex at (q,r)
    // @note This needs to be done only once when this class is instantiated
    protected bool addHex(int q, int r)
    {
        int64 s = Hex::encodeAxial(q, r);
        if(hexes.exists(formatInt(s)))
            return false;

        Hex@ hex = Hex(q, r);

        @hexes[formatInt(s)] = hex;

        return true;
    }

    // Retrieves a hex
    //
    // @param q, r
    //  Axial coordinates of the hex
    //
    // @returns hex
    //  The hex at (q, r) or nil if there's no hex at that position
    Hex@ getHex(int q, int r) const
    {
        int64 s = Hex::encodeAxial(q, r);
        Hex@ hex;

        if(hexes.get(formatInt(s), @hex))
            return hex;
        return null;
    }

    array<Hex@>@ getHexes() const
    {
        array<Hex@>@ result = array<Hex@>();

        auto keys = hexes.getKeys();
        for(uint i = 0; i < keys.length(); ++i){

            result.insertLast(cast<Hex@>(hexes[keys[i]]));
        }

        return result;
    }

    //! \returns The hexes but rotated (rotation degrees)
    //! \todo Should this and the normal getHexes return handles to arrays
    array<Hex@>@ getRotatedHexes(int rotation) const
    {
        array<Hex@>@ result = array<Hex@>();

        int times = rotation / 60;

        auto keys = hexes.getKeys();
        for(uint i = 0; i < keys.length(); ++i){

            Hex@ hex = cast<Hex@>(hexes[keys[i]]);

            auto rotated = Hex::rotateAxialNTimes({hex.q, hex.r}, times);
            result.insertLast(Hex(rotated.X, rotated.Y));
        }

        return result;
    }

    Float3 calculateCenterOffset() const
    {
        int count = 0;

        Float3 offset = Float3(0, 0, 0);

        auto keys = hexes.getKeys();
        for(uint i = 0; i < keys.length(); ++i){

            ++count;

            auto hex = cast<Hex@>(hexes[keys[i]]);
            offset += Hex::axialToCartesian(hex.q, hex.r);
        }

        offset /= count;
        return offset;
    }
    
    Float3 calculateModelOffset() const
    {
        return ((calculateCenterOffset()/=hexes.getSize()))*HEX_SIZE;
    }
    
    bool hasComponent(const string &in name) const{

        for(uint i = 0; i < components.length(); ++i){
            if(components[i].name == name)
                return true;
        }

        return false;
    }

    // ------------------------------------ //

    // Prevent modification
    string name {

        get const{
            return _name;
        }
    }

    int getHexCount() const
    {
        return hexes.getSize();
    }

    private string _name;
    float mass;
    string gene;

    array<OrganelleComponentFactory@> components;
    private dictionary hexes;


    // The initial amount of compounds this organelle consists of
    dictionary initialComposition;

    // The names in the processes need to match the ones in bioProcessRegistry
    // Or better yet, be loaded from the registry that reads the json files
    // so that the processes can be configured that way
    array<TweakedProcess@> processes;

    // The total number of compounds we need before we can split.
    int organelleCost;

    // Name of the model used for this organelle. For example "nucleus.fbx"
    string mesh;

    //! Name of the texture used for this model
    string texture;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float chanceToCreate = 0.0;
    float prokaryoteChance = 0.0;

    //! Cost in mutation points
    int mpCost = 0;
}

enum ORGANELLE_HEALTH{
    DEAD = 0,
    ALIVE = 1,
    // Organelle is ready to divide
    CAN_DIVIDE = 2
};

//! These are placed either on an actual microbe where they are
//! onAddedToMicrobe() OR on templates where these just have positions
//! set and should be duplicated for the purpose of adding to a
//! microbe
class PlacedOrganelle : SpeciesStoredOrganelleType{

    PlacedOrganelle(Organelle@ organelle, int q, int r, int rotation)
    {
        @this._organelle = organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;

        _commonConstructor();
    }

    //! Takes type from another PlacedOrganelle
    PlacedOrganelle(PlacedOrganelle@ typefromother, int q, int r, int rotation)
    {
        @this._organelle = typefromother._organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;

        _commonConstructor();
    }

    //! Takes everything that's sensible to copy from another PlacedOrganelle
    PlacedOrganelle(PlacedOrganelle@ other)
    {
        @this._organelle = other._organelle;
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
        if(_organelle is null)
            assert(false, "PlacedOrganelle created with null Organelle");

        resetHealth();

        // Create instances of components //
        for(uint i = 0; i < organelle.components.length(); ++i){

            components.insertLast(organelle.components[i].factory());
        }

        compoundsLeft = organelle.initialComposition;
    }

    void resetHealth()
    {
        // Copy //
        composition = _organelle.initialComposition;
    }


    // Called by Microbe.update
    //
    // Override this to make your organelle class do something at regular intervals
    //
    // @param logicTime
    //  The time since the last call to update()
    void update(int logicTime)
    {
        // If the organelle is supposed to be another color.
        if(_needsColourUpdate){
            updateColour();
        }

        // Update each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){
            components[i].update(microbeEntity, this, logicTime);
        }
    }

    protected void updateColour()
    {
        if(organelleEntity == NULL_OBJECT)
            return;

        auto model = world.GetComponent_Model(organelleEntity);

        if(model !is null && IsInGraphicalMode()){

            updateMaterialTint(model.Material, calculateHSLForOrganelle(this.speciesColour));
        }

        _needsColourUpdate = false;
    }

    // Returns the meaning of compoundBin value
    ORGANELLE_HEALTH getHealth()
    {
        if(compoundBin <= ORGANELLE_HEALTH::DEAD)
            return ORGANELLE_HEALTH::DEAD;
        if(compoundBin < ORGANELLE_HEALTH::CAN_DIVIDE)
            return ORGANELLE_HEALTH::ALIVE;
        return ORGANELLE_HEALTH::CAN_DIVIDE;
    }

    // This doesnt seem ideal
    //! \returns compoundBin
    float getCompoundBin()
    {
        return compoundBin;
    }

    // Gives organelles more compounds
    void growOrganelle(CompoundBagComponent@ compoundBagComponent, int logicTime)
    {
        // Finds the total number of needed compounds.
        float sum = 0.0f;

        auto compoundKeys = compoundsLeft.getKeys();
        for(uint i = 0; i < compoundKeys.length(); ++i){

            // Finds which compounds the cell currently has.
            if(compoundBagComponent.getCompoundAmount(
                    SimulationParameters::compoundRegistry().getTypeId(compoundKeys[i])) >= 1)
            {
                float amount;
                if(!compoundsLeft.get(compoundKeys[i], amount)){

                    LOG_ERROR("Invalid type in compoundsLeft");
                    continue;
                }

                sum += amount;
            }
        }

        // If sum is 0, we either have no compounds, in which case we
        // cannot grow the organelle, or the organelle is ready to
        // split (i.e. compoundBin = 2), in which case we wait for the
        // microbe to handle the split.
        if(sum <= 0.0f)
            return;

        // Randomly choose which of the compounds are used in reproduction.
        // Uses a roulette selection.
        float id = GetEngine().GetRandom().GetFloat(0, 1) * sum;

        for(uint i = 0; i < compoundKeys.length(); ++i){

            const auto compoundName = compoundKeys[i];

            float amount;
            if(!compoundsLeft.get(compoundName, amount)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }

            if(id - amount < 0.0f){

                // The random number is from this compound, so attempt to take it.
                float amountToTake = min(logicTime * GROWTH_SPEED_MULTILPIER, amount);
                amountToTake = compoundBagComponent.takeCompound(
                    SimulationParameters::compoundRegistry().getTypeId(compoundName),
                    amountToTake);
                compoundsLeft[compoundName] = float(compoundsLeft[compoundName]) -
                    amountToTake;
                break;

            } else {
                id -= amount;
            }
        }

        // Calculate the new growth value.
        recalculateBin();
    }

    private void scaleCompoundsLeft(float scaleFactor)
    {
        auto compoundKeys = compoundsLeft.getKeys();
        for(uint i = 0; i < compoundKeys.length(); ++i){
            float amount;
            if(!compoundsLeft.get(compoundKeys[i], amount)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }

            compoundsLeft[compoundKeys[i]] = amount * scaleFactor;
        }
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

    private void recalculateBin()
    {
        // Calculate the new growth growth
        float totalCompoundsLeft = calculateCompoundsLeft();

        compoundBin = 2.0 - totalCompoundsLeft / organelle.organelleCost;

        // If the organelle is damaged...
        if(compoundBin < 1.0){
            // If it is dead
            if(compoundBin <= 0.0){
                // If it was split from a primary organelle, destroy it.
                if(isDuplicate == true){

                    // Calls different method for possible sound and effects
                    MicrobeOperations::organelleDestroyedByDamage(world,
                        microbeEntity, {q, r});

                    // Notify the organelle the sister organelle it is no longer split.
                    sisterOrganelle.wasSplit = false;
                    return;

                } else {
                    // If it is a primary organelle, make sure that
                    // it's compound bin is not less than 0.
                    compoundBin = 0.0;

                    scaleCompoundsLeft(2);
                }
            }

            // Scale the model at a slower rate (so that 0.0 is half size).
            // Nucleus isn't scaled
            // TODO: This isn't the cheapest call so maybe this should be cached
            if(!organelle.hasComponent("NucleusOrganelle")){

                RenderNode@ sceneNode = world.GetComponent_RenderNode(
                    organelleEntity);

                sceneNode.Scale = Float3((1.0 + compoundBin)/2,
                    (1.0 + compoundBin)/2,
                    (1.0 + compoundBin)/2) * HEX_SIZE;
                sceneNode.Marked = true;
            }

            // See update and updateColour for as to why this doesn't work
            // Darken the color. Will be updated on next call of update()
            // colourTint = Float4((1.0 + compoundBin)/2, compoundBin, compoundBin, 1);
            // _needsColourUpdate = true;

        } else{
            // Nucleus isn't scaled
            if(organelle.hasComponent("NucleusOrganelle"))
                return;

            // Scale the organelle model to reflect the new size.
            // Only if it is different
            const Float3 newScale = Float3(compoundBin, compoundBin, compoundBin) * HEX_SIZE;

            RenderNode@ sceneNode = world.GetComponent_RenderNode(
                organelleEntity);

            if(sceneNode is null){

                LOG_ERROR("Trying to scale organelle that is missing RenderNode component");
                return;
            }

            if(newScale != sceneNode.Scale){
                sceneNode.Scale = newScale;
                sceneNode.Marked = true;
            }
        }
    }

    // Resets the state. Used after dividing?
    void reset()
    {
        // Return the compound bin to its original state
        this.compoundBin = 1.0;

        // Assign (doesn't only copy a reference)
        compoundsLeft = organelle.initialComposition;

        if(IsInGraphicalMode()){
            // Scale the organelle model to reflect the new size.
            // This might be able to be skipped as the recalculateBin method will always set
            // the correct scale
            RenderNode@ sceneNode = world.GetComponent_RenderNode(
                organelleEntity);

            sceneNode.Scale = Float3(1, 1, 1) * HEX_SIZE;
            sceneNode.Marked = true;

            // If it was split from a primary organelle, destroy it.
            if(isDuplicate){
                MicrobeOperations::removeOrganelle(world, microbeEntity,
                    {this.q, this.r});
            } else {
                wasSplit = false;
            }
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

        @this.world = world;

        assert(this.world !is null, "trying to create placed organelle without world");

        microbeEntity = microbe;

        bool bacteria = false;

        // This should be the only species check any organelle ever makes.
        auto species = MicrobeOperations::getSpeciesComponent(world, microbeEntity);
        if (species !is null){
            this.speciesColour = species.colour;
            bacteria = species.isBacteria;
        }

        // Our coordinates are already set when this is called
        // so just cache this
        this.cartesianPosition = Hex::axialToCartesian(q, r);

        float hexSize = HEX_SIZE;

        // Scale the hex size down for bacteria
        if(bacteria)
            hexSize /= 2.f;

        assert(organelleEntity == NULL_OBJECT, "PlacedOrganelle already had an entity");

        const Float3 offset = organelle.calculateModelOffset();

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
                        calculateHSLForOrganelle(this.speciesColour)));
            }
        }

        // Add hex collision shapes
        auto hexes = organelle.getHexes();

        for(uint i = 0; i < hexes.length(); ++i){

            Hex@ hex = hexes[i];

            // Also add our offset to the hex offset
            Float3 translation = Hex::axialToCartesian(hex.q, hex.r) + this.cartesianPosition;

            // Scale for bacteria physics. This might be pretty expensive to be in this loop...
            if(bacteria)
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

    //! \todo This might not work anymore
    void hideEntity()
    {
        auto renderNode = world.GetComponent_RenderNode(organelleEntity);
        if(renderNode !is null && renderNode.Node.valid())
            renderNode.Node.removeFromParent();

        // Also hide components as they also can have entities
        for(uint i = 0; i < components.length(); ++i){
            components[i].hideEntity(this);
        }
    }
    // ------------------------------------ //

    const Organelle@ organelle {
        get const{
            return _organelle;
        }
    }

    private Organelle@ _organelle;

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

    // The "Health Bar" of the organelle constrained to [0, 2],
    // ORGANELLE_HEALTH tells what different ranges mean
    float compoundBin = ORGANELLE_HEALTH::ALIVE;

    // The compounds left to divide this organelle.
    // Decreases every time a required compound is absorbed.
    dictionary compoundsLeft;

    // The compounds that make up this organelle. They get reduced each time
    // the organelle gets damaged.
    dictionary composition;

    array<OrganelleComponent@> components;

    ObjectID microbeEntity = NULL_OBJECT;
    ObjectID organelleEntity = NULL_OBJECT;

    // This is the world in which the entities for this organelle exists
    CellStageWorld@ world;

    // Target colour for this
    Float4 speciesColour = Float4(0.0f, 0.0f, 0.0f, 0.0f);

    PlacedOrganelle@ sisterOrganelle = null;

    // Used for removing the added sub collisions when we are removed from a microbe
    private array<PhysicsShape@> _addedCollisions;

    bool _needsColourUpdate = false;
}
