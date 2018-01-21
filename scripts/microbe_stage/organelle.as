// How fast organelles grow.
const auto GROWTH_SPEED_MULTILPIER = 0.5 / 1000;

    // Percentage of the compounds that compose the organelle released
    // upon death (between 0.0 and 1.0).
const auto COMPOUND_RELEASE_PERCENTAGE = 0.3;


class Hex{

    Hex(int q, int r, NewtonCollision@ collision){
        this.q = q;
        this.r = r;
        this.collision = collision;
    }

    int q;
    int r;
    NewtonCollision@ collision;
}

//! Base class for all organelle types
//! \note Before there was an instance of this class for each microbe. Now this is global and
//! each microbe has a PlacedOrganelle instance instead (which also has many properties
//! that this class used to have)
abstract class Organelle{

    // This is world specific (at least the physics body) so this can
    // be used by only the world that is passed to this constructor
    Organelle(const string &in name, float mass, GameWorld@ world){

        this._name = name;
        this.mass = mass;

        // Calculate organelleCost and compoundsLeft//
        organelleCost = calculateCost(organelleTable[name].composition);

        // Setup physics body (this is now done just once here) //
        beingConstructed = true;

        @collisionShape = world.GetPhysicalWorld().CreateCompoundCollision();
        collisionShape.BeginAddRemove();
        
        setupPhysics();

        collisionShape.EndAddRemove();
        beingConstructed = false;
    }

    // Overwrite to make organelle do something at update time. Called from PlacedOrganelle
    //! \note This may not change the state of this Organelle object (or subclass) as they are
    //! global and only PlacedOrganelle has data regarding a specific organelle that is
    //! in a microbe
    void update(PlacedOrganelle@ instanceData) const{

        
    }

    protected setupPhysics(){
        assert(false, "Organelle::setupPhysics not overridden");
    }

    // Needs to be overwritten
    protected calculateCost(dictionary composition){

        organelleCost = 0;
        
        auto keys = composition.keys();

        for(uint i = 0; i < keys.length(); ++i){

            const auto compoundName = keys[i];
            int amount;

            if(!composition.get(keys[i], amount)){

                LOG_ERROR("Invalid value in calculateCost composition");
                continue;
            }
            
            compoundsLeft[compoundName] = amount;
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
    protected bool addHex(q, r, GameWorld@ world){

        assert(beingConstructed, "addHex called after organelle constructor");
        
        string s = encodeAxial(q, r);
        if(hexes.exists(s))
            return false;

        Float2 xz = axialToCartesian(q, r);
        Float3 translation = Float3(xz.X, 0, xz.Y);

        Ogre::Matrix offset;
        // Create the matrix with the offset
        assert(false, "TODO");
        
        Hex@ hex = Hex(q, r, world.GetPhysicalWorld().CreateSphere(2, offset));
        
        collisionShape.AddSubCollision(hex.collision);

        hexes[s] = hex;
        return true;
    }

    // Retrieves a hex
    //
    // @param q, r
    //  Axial coordinates of the hex
    //
    // @returns hex
    //  The hex at (q, r) or nil if there's no hex at that position
    Hex@ getHex(int q, int r){
        string s = encodeAxial(q, r);
        Hex@ hex;

        if(hexes.get(s, hex))
            return hex;
        return null;
    }

    Float3 calculateCenterOffset() const{
        int count = 0;

        auto keys = hexes.keys();
        for(uint i = 0; i < keys.length(); ++i){
            
            ++count;

            auto hex = hexes[i];
            Float2 coord = axialToCartesian(hex.q, hex.r);
            offset += Float3(coord.X, 0, coord.Y);
        }
        
        offset /= count;
        return offset;
    }

    // // Removes a hex from this organelle
    // //
    // // @param q,r
    // //  Axial coordinates of the hex to remove
    // //
    // // @returns success
    // //  True if the hex could be removed, false if there's no hex at (q,r)
    // function Organelle.removeHex(q, r)
    //     assert(not self.microbeEntity, "Cannot change organelle shape while it is in a microbe")
    //     local s = encodeAxial(q, r)
    //     local hex = table.remove(self._hexes, s)
    //     if hex {
    //         self.collisionShape.removeChildShape(hex.collisionShape)
    //         return true
    //         else
    //             return false
    //                 }
    // }
    

    // ------------------------------------ //

    // Prevent modification
    string name {

        get {
            return _name;
        }
    }

    private string _name;
    float mass;
    
    // The definition of the collision of this organelle, this is used
    // to create the actual physics body
    // This is only valid within a single GameWorld (as each have their own NewtonWorld)
    NewtonCollision@ collisionShape;

    // These are in PlacedOrganelle
    // self.position = {
    //     q = 0,
    //     r = 0
    // }
    // self.rotation = nil

    array<OrganelleComponent@> components;
    private dictionary hexes;

    // The initial amount of compounds this organelle consists of
    dictionary initialComposition;

    // The names in the processes need to match the ones in bioProcessRegistry
    // Or better yet, be loaded from the registry that reads the json files
    // so that the processes can be configured that way
    array<int> processes;

    // The deviation of the organelle color from the species color
    bool _needsColourUpdate = true;
        
    // The total number of compounds we need before we can split.
    int organelleCost;

    // True only in the constructor. Makes sure physics body cannot be
    // added to just like that
    private bool beingConstructed = false;
}

enum ORGANELLE_HEALTH{
    DEAD = 0,
    ALIVE = 1,
    // Organelle is ready to divide
    CAN_DIVIDE = 2
};

class PlacedOrganelle{

    PlacedOrganelle(Organelle@ organelle, int q, int r, int rotation){

        @this._organelle = organelle;
        this.q = q;
        this.r = r;
        this.rotation = rotation;

        resetHealth();

        // Create instances of components //
        for(uint i = 0; i < organelle.components.length(); ++i){

            components.push(PlacedOrganelleComponent(organelle.components[i]));
        }
    }

    void resetHealth(){

        // Copy //
        composition = initialComposition;
    }


    // Called by Microbe.update
    //
    // Override this to make your organelle class do something at regular intervals
    //
    // @param logicTime
    //  The time since the last call to update()
    void update(int logicTime){
        if(flashDuration >= 0){
            
            flashDuration -= logicTime;
            Float4 speciesColour = microbeEntity.getSpeciesComponent().colour;
            
            // How frequent it flashes, would be nice to update the
            // flash function to have this variable
            if(flashDuration % 600 < 300){
                
                colour = flashColour;
                
            } else {
                colour = speciesColour;
            }
        
            if(flashDuration <= 0){
                flashDuration = 0;
                colour = speciesColour;
            }
        
            _needsColourUpdate = true;
        }

        // If the organelle is supposed to be another color.
        if(_needsColourUpdate){
            // This method doesn't actually apply the colour so I have
            // no clue how the flashing works
            updateColour();
        }

        // Update main organelle derived class
        // This is a const method so we store all the state
        organelle.update(this);

        // Update each OrganelleComponent
        for(uint i = 0; i < components.size(); ++i){
            component.update(microbeEntity, this, logicTime);
        }
    }

    protected void updateColour(){

        if(organelleEntity == NULL_OBJECT || microbeEntity is null)
            return;

        auto model = microbeEntity.getWorld().Get_Model(organelleEntity);

        // local entity = this.sceneNode.entity;
        // //entity.tintColour(this.name, this.colour); //crashes game
        
        // model.Entity.SetColour(colour);
        
        _needsColourUpdate = false;
    }

    // Returns the meaning of compoundBin value
    ORGANELLE_HEALTH getHealth(){
        if(compoundBin <= ORGANELLE_HEALTH::DEAD)
            return ORGANELLE_HEALTH::DEAD;
        if(compoundBin < ORGANELLE_HEALTH::CAN_DIVIDE)
            return ORGANELLE_HEALTH::ALIVE;
        return ORGANELLE_HEALTH::CAN_DIVIDE;
    }

    // Gives organelles more compounds
    void growOrganelle(CompoundBagComponent@ compoundBagComponent, int logicTime){
        // Finds the total number of needed compounds.
        float sum = 0.0;

        auto compoundKeys = compoundsLeft.keys();
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
        if(sum <= 0.0)
            return;

        // Randomly choose which of the compounds are used in reproduction.
        // Uses a roulette selection.
        float id = GetEngine().GetRandom().GetFloat(0, 1) * sum;

        for(uint i = 0; i < compoundKeys.length(); ++i){

            float amount;
            if(!compoundsLeft.get(compoundKeys[i], amount)){

                LOG_ERROR("Invalid type in compoundsLeft");
                continue;
            }            
            
            if(id - amount < 0){
                // The random number is from this compound, so attempt to take it.
                float amountToTake = min(logicTime * GROWTH_SPEED_MULTILPIER, amount);
                amountToTake = compoundBagComponent.takeCompound(
                    SimulationParameters::compoundRegistry().getTypeId(compoundName),
                    amountToTake);
                compoundsLeft[compoundName] = cast<float>(compoundsLeft[compoundName]) -
                    amountToTake;
                break;

            } else {
                id -= amount;
            }
        }
        
        // Calculate the new growth value.
        recalculateBin();
    }

    void damageOrganelle(float damageAmount){
        // Flash the organelle that was damaged.
        flashOrganelle(3000, Float4(1, 0.2, 0.2, 1));

        // Calculate the total number of compounds we need
        // to divide now, so that we can keep this ratio.
        const float totalLeft = calculateCompoundsLeft();

        // Calculate how much compounds the organelle needs to have
        // to result in a health equal to compoundBin - amount.
        const float damageFactor = (2.0 - compoundBin + damageAmount) *
            (organelleCost / totalLeft);

        scaleCompoundsLeft(damageFactor);
        
        recalculateBin();
    }

    private void scaleCompoundsLeft(float scaleFactor){

        auto compoundKeys = compoundsLeft.keys();
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
    float calculateCompoundsLeft() const{

        float totalLeft = 0;
        
        auto compoundKeys = compoundsLeft.keys();
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

    private void recalculateBin(){
        // Calculate the new growth growth
        float totalCompoundsLeft = calculateCompoundsLeft();
    
        compoundBin = 2.0 - totalCompoundsLeft / organelleCost;

        // If the organelle is damaged...
        if(compoundBin < 1.0){
            // If it is dead
            if(compoundBin <= 0.0){
                // If it was split from a primary organelle, destroy it.
                if(isDuplicate == true){
                    microbeEntity.organelleDestroyedByDamage(q, r);
                    
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
        if organelleTable[this.name].components["NucleusOrganelle"] == nil {
                this.sceneNode.transform.scale = Vector3((1.0 + this.compoundBin)/2, (1.0 + this.compoundBin)/2, (1.0 + this.compoundBin)/2)*HEX_SIZE
                this.sceneNode.transform.touch()
            }
        
        // Darken the color. Will be updated on next call of update()
this.colourTint = Vector3((1.0 + this.compoundBin)/2, this.compoundBin, this.compoundBin)
    this._needsColourUpdate = true
        else
            // Scale the organelle model to reflect the new size.
            if organelleTable[this.name].components["NucleusOrganelle"] == nil {
                    this.sceneNode.transform.scale = Vector3(this.compoundBin, this.compoundBin, this.compoundBin)*HEX_SIZE
                    this.sceneNode.transform.touch()  
                }
    }
}

void reset(){
    // Return the compound bin to its original state
    this.compoundBin = 1.0
    for compoundName, amount in pairs(this.composition) ){
        this.compoundsLeft[compoundName] = amount
            }
    
// Scale the organelle model to reflect the new size.
this.sceneNode.transform.scale = Vector3(1, 1, 1) * HEX_SIZE
                                                             this.sceneNode.transform.touch()
        
                                                             // If it was split from a primary organelle, destroy it.
                                                             if this.isDuplicate == true {
                                                                     MicrobeSystem.removeOrganelle(this.microbeEntity, this.position.q, this.position.r)
                                                                     else
                                                                         this.wasSplit = false
                                                                             }
}


// Is this used? This will be quite difficult to do afterwards
function Organelle.removePhysics()
    this.collisionShape.clear()
    }

    


    

    // Called by a microbe when this organelle has been added to it
    //
    // @param microbe
    //  The organelle's new owner
    //
    // @param q, r
    //  Axial coordinates of the organelle's center
    // @note This is quite an expensive method as this creates a new entity with
    //  multiple components
    void onAddedToMicrobe(Microbe@ microbe, int q, int r, int rotation){

        if(@microbeEntity !is null){

            LOG_ERROR("onAddedToMicrobe called before this PlacedOrganelle was " +
                "removed from previous microbe");
            onRemovedFromMicrobe();
        }
        
        @microbeEntity = microbe;
        
        this.q = q;
        this.r = r;
        Float2 xz = axialToCartesian(q, r);
        this.position.cartesian = Vector3(xz.X, 0, xz.Y);
        this.rotation = rotation;

        assert(organelleEntity == NULL_ENTITY, "PlacedOrganelle already had an entity");

        auto@ world = microbe.GetWorld();
        
        organelleEntity = world.CreateEntity();

        // // Automatically destroyed if the parent is destroyed
        // world.SetEntityParent(microbe.getEntityID(), organelleEntity);
            
        // Change the colour of this species to be tinted by the membrane.
        auto species = microbeEntity.getSpeciesComponent();
        
        colour = species.colour;
        _needsColourUpdate = true;

        // Not sure which hexes these need to be
        //for _, hex in pairs(MicrobeSystem.getOrganelleAt(this.microbeEntity, q, r)._hexes) ){
        Float3 offset = organelle.calculateCenterOffset();

        auto renderNode = world.Create_RenderNode(organelleEntity);
        renderNode.Marked = true;
        renderNode.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        auto position = world.Create_Position(organelleEntity,
            offset + this.position.cartesian, Ogre::Quaternion(Ogre::Degree(rotation)));


        auto parentRenderNode = world.Get_RenderNode(microbeEntity.getEntityID());
        parentRenderNode.SceneNode.Attach(renderNode.SceneNode.Attach);
            
        //Adding a mesh for the organelle.
        world.Create_Model(organelleEntity, organelle.mesh);

        // TODO: create physics body
    
        // Add each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){
            
            components[i].onAddedToMicrobe(microbeEntity, q, r, rotation, this);
        }
    }

    // Called by a microbe when this organelle has been removed from it
    //
    // @param microbe
    //  The organelle's previous owner
    void onRemovedFromMicrobe(Microbe@ microbe){
        //iterating on each OrganelleComponent
        for(uint i = 0; i < components.length(); ++i){

            components[i].onRemovedFromMicrobe(microbeEntity);
        }
        
        world.DestroyEntity(organelleEntity);
        organelleEntity = NULL_OBJECT;
        @microbeEntity = null;
    }


    function Organelle.flashOrganelle(float duration, Float4 colour){
        if(flashDuration > 0)
            return;
        
        flashColour = colour;
        flashDuration = duration;
    }

// Sets the color of the organelle (used in editor for valid/nonvalid placement)
function OrganelleFactory.setColour(sceneNode, colour)
    sceneNode.entity.setColour(colour)
    }

    // ------------------------------------ //
    
    const Organelle@ organelle {
        get const{
            return _organelle;
        }
    }

    private Organelle@ _organelle;
    
    // q and r are radial coordinates instead of cartesian
    int q;
    int r;
    int rotation;

    // Whether or not this organelle has already divided.
    bool split = false;
    
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

    array<PlacedOrganelleComponent@> components;

    Microbe@ microbeEntity;
    ObjectID organelleEntity = NULL_OBJECT;

    float flashDuration = 0;
    Float4 flashColour;

    protected CompoundBin compoundBin;
    PlacedOrganelle@ sisterOrganelle = null;
}


class Nucleus : Organelle{

    Nucleus(){

        super("nucleus");
    }
}

class Mitochondrion : Organelle{

    Mitochondrion(){

        super("mitochondrion");
    }
}

class Vacuole : Organelle{

    Vacuole(){

        super("vacuole");
    }
}

class Flagellum : Organelle{

    Flagellum(){

        super("flagellum");
    }
}




// // Loading stored organelles
// function Organelle.loadOrganelle(storage){
//     local name = storage:get("name", "<nameless>");
//     local mass = storage:get("mass", 0.1);
//     local organelle = Organelle(mass, name);
//     organelle::load(storage);
//     return organelle;
// }

// function Organelle.load(storage){
//     local hexes = storage.get("hexes", {});
//     for(i = 1; i < hexes..size()){
//         local hexStorage = hexes.get(i);
//         local q = hexStorage.get("q", 0);
//         local r = hexStorage.get("r", 0);
//         this.addHex(q, r);
//     }
//     this.position.q = storage.get("q", 0);
//     this.position.r = storage.get("r", 0);
//     this.rotation = storage.get("rotation", 0);
    
//     local organelleInfo = organelleTable[this.name];
//     //adding all of the components.
//     for(componentName, _ in pairs(organelleInfo.components)){
//         local componentType = _G[componentName];
//         local componentData = storage.get(componentName, componentType());
//         local newComponent = componentType(nil, nil);
//         newComponent.load(componentData);
//         this.components[componentName] = newComponent;
//     }
// }


// function Organelle.storage(){
//     local storage = StorageContainer.new();
//     local hexes = StorageList.new();
//     for(_, hex in pairs(this._hexes)){
//         hexStorage = StorageContainer.new();
//         hexStorage.set("q", hex.q);
//         hexStorage.set("r", hex.r);
//         hexes.append(hexStorage);
//     }
//     storage.set("hexes", hexes);
//     storage.set("name", this.name);
//     storage.set("q", this.position.q);
//     storage.set("r", this.position.r);
//     storage.set("rotation", this.rotation);
//     storage.set("mass", this.mass);
//     //Serializing these causes some minor issues and ){esn't serve a purpose anyway
//     //storage.set("externalEdgeColour", this._externalEdgeColour)

//     //iterating on each OrganelleComponent
//     for(componentName, component in pairs(this.components) ){
//         local s = component.storage();
//         assert(isNotEmpty, componentName);
//         assert(s);
//         storage.set(componentName, s);
//     }

//     return storage;
// }


// The basic organelle maker
class OrganelleFactory{

 // Use the same named method in PlacedOrganelle
 // function OrganelleFactory.setColour(sceneNode, colour)

function OrganelleFactory.makeOrganelle(data)
    if not (data.name == "" or data.name == nil) {
        //retrieveing the organelle info from the table
        local organelleInfo = organelleTable[data.name]

            //creating an empty organelle
            local organelle = Organelle(organelleInfo.mass, data.name)

            //adding all of the components.
            for componentName, arguments in pairs(organelleInfo.components) ){
    local componentType = _G[componentName]
        organelle.components[componentName] = componentType.new(arguments, data)
        }

//getting the hex table of the organelle rotated by the angle
local hexes = OrganelleFactory.checkSize(data)

    //adding the hexes to the organelle
    for _, hex in pairs(hexes) ){
        organelle.addHex(hex.q, hex.r)
            }

return organelle
}
}

// Draws the hexes and uploads the models in the editor
function OrganelleFactory.renderOrganelles(data)
if data.name == "remove" {
        return {}
        else
            //Getting the list hexes occupied by this organelle.
            occupiedHexList = OrganelleFactory.checkSize(data)

                //Used to get the average x and y values.
                local xSum = 0
                local ySum = 0

                //Rendering a cytoplasm in each of those hexes.
                //Note: each scenenode after the first one is considered a cytoplasm by the engine automatically.
                local i = 2
                for _, hex in pairs(occupiedHexList) ){
    local organelleX, organelleY = axialToCartesian(data.q, data.r)
        local hexX, hexY = axialToCartesian(hex.q, hex.r)
        local x = organelleX + hexX
        local y = organelleY + hexY
        local translation = Vector3(-x, -y, 0)
        data.sceneNode[i].transform.position = translation
        data.sceneNode[i].transform.orientation = Quaternion.new(
            Radian.new(Degree(data.rotation)), Vector3(0, 0, 1))
        xSum = xSum + x
        ySum = ySum + y
        i = i + 1
        }

//Getting the average x and y values to render the organelle mesh in the middle.
local xAverage = xSum / (i - 2) // Number of occupied hexes = (i - 2).
                            local yAverage = ySum / (i - 2)

                            //R}ering the organelle mesh (if it has one).
                            local mesh = organelleTable[data.name].mesh
                            if(mesh ~= nil) {
                                data.sceneNode[1].meshName = mesh
                                data.sceneNode[1].transform.position = Vector3(-xAverage, -yAverage, 0)
                                data.sceneNode[1].transform.orientation = Quaternion.new(
                                    Radian.new(Degree(data.rotation)), Vector3(0, 0, 1))
                            }
}
}

// Checks which hexes an organelle occupies
function OrganelleFactory.checkSize(data)
    if data.name == "remove" {
            return {}
            else
                //getting the angle the organelle has
                //(and setting one if it doesn't have one).
                if data.rotation == nil {
                        data.rotation = 0
                    }
            local angle = data.rotation / 60

            //getting the hex table of the organelle rotated by the angle
            local hexes = rotateHexListNTimes(organelleTable[data.name].hexes, angle)
            return hexes
        }
}
}


