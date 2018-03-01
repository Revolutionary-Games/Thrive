// Operations on microbe entities
#include "biome.as"

namespace MicrobeOperations{

// Queries the currently stored amount of an compound
//
// @param compoundId
// The id of the compound to query
//
// @returns amount
// The amount stored in the microbe's storage oraganelles
double getCompoundAmount(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId){
    return world.GetComponent_CompoundBagComponent(microbeEntity).
        getCompoundAmount(compoundId);
}

// Getter for microbe species
// 
// returns the species component or null if it doesn't have a valid species
SpeciesComponent@ getSpeciesComponent(CellStageWorld@ world, ObjectID microbeEntity){

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, microbeComponent.speciesName);
    
    return world.GetComponent_SpeciesComponent(entity);
}

// Getter for species processor component
// 
// returns the processor component or null if such species doesn't have that component
// TODO: check what calls this and make it store the species entity id if it also calls
// getSpeciesComponent to save searching the whole species component index multiple times
ProcessorComponent@ getProcessorComponent(CellStageWorld@ world, const string &in speciesName){
    
    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, speciesName);
    
    return world.GetComponent_ProcessorComponent(entity);
}

// Retrieves the organelle occupying a hex cell
//
// @param q, r
// Axial coordinates, relative to the microbe's center
//
// @returns organelle
// The organelle at (q,r) or null if the hex is unoccupied
PlacedOrganelle@ getOrganelleAt(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){
    
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

    for(uint i = 0; i< microbeComponent.organelles.length(); ++i){
        auto organelle = microbeComponent.organelles[i];
        
        auto localQ = hex.X - organelle.q;
        auto localR = hex.Y - organelle.r;
        if(organelle.organelle.getHex(localQ, localR) !is null){
            return organelle;
        }
    }
    
    return null;
}
    
// Removes the organelle at a hex cell
// Note that this renders the organelle unusable as we destroy its underlying entity
//
// @param q, r
// Axial coordinates of the organelle's center
//
// @returns success
// True if an organelle has been removed, false if there was no organelle
// at (q,r)
// @note use a more specific version (for example damaged) if available
//
// This is responsible for updating the mass of the cell's physics body
bool removeOrganelle(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){

    auto organelle = getOrganelleAt(world, microbeEntity, hex);
    if(organelle is null){
        return false;
    }

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);

    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        if(microbeComponent.organelles[i] is organelle){

            microbeComponent.organelles.removeAt(i);
            break;
        }
    }
    
    organelle.onRemovedFromMicrobe(microbeEntity, rigidBodyComponent.Collision);

    // Need to recreate the body
    rigidBodyComponent.CreatePhysicsBody(world.GetPhysicalWorld());
    rigidBodyComponent.SetMass(rigidBodyComponent.Mass - organelle.organelle.mass);

    // And jump it to the current position
    auto position = world.GetComponent_Position(microbeEntity);
    rigidBodyComponent.JumpTo(position);

    // This refreshing these things could probably be somewhere else...
    calculateHealthFromOrganelles(world, microbeEntity);
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth -
        BANDWIDTH_PER_ORGANELLE ; // Temporary solution for decreasing max bandwidth
        
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;
    
    return true;
}

bool organelleDestroyedByDamage(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){

    // TODO: effects for destruction?
    return removeOrganelle(world, microbeEntity, hex);
}

// ------------------------------------ //
void respawnPlayer(CellStageWorld@ world){
    
    auto playerEntity = GetThriveGame().playerData().activeCreature();
    
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(playerEntity));
    auto rigidBodyComponent = world.GetComponent_Physics(playerEntity);
    auto sceneNodeComponent = world.GetComponent_RenderNode(playerEntity);

    microbeComponent.dead = false;
    microbeComponent.deathTimer = 0;

    // Reset the growth bins of the organelles to full health.
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        microbeComponent.organelles[i].reset();
    }
    calculateHealthFromOrganelles(world, playerEntity);

    // Reset position //
    rigidBodyComponent.SetPosition(Float3(0, 0, 0), Float4::IdentityQuaternion);

    // The physics body will set the Position on next tick

    // TODO: reset velocity like in the old lua code?

    // This set position is actually useless, but it was in the old lua code
    // sceneNodeComponent.Node.setPosition(Float3(0, 0, 0));
    sceneNodeComponent.Hidden = false;
    sceneNodeComponent.Marked = true;

    // TODO: give the microbe the values from some table instead.
    storeCompound(world, playerEntity,
        SimulationParameters::compoundRegistry().getTypeId("atp"), 50, false);

    setRandomBiome(world);
    cast<MicrobeStageHudSystem>(world.GetScriptSystem("MicrobeStageHudSystem")).
        suicideButtonreset();
}


// Attempts to obtain an amount of bandwidth for immediate use.
// This should be in conjunction with most operations ejecting  or absorbing compounds
// and agents for microbe.
//
// @param maicrobeEntity
// The entity of the microbe to get the bandwidth from.
//
// @param maxAmount
// The max amount of units that is requested.
//
// @param compoundId
// The compound being requested for volume considerations.
//
// @return
//  amount in units avaliable for use.
float getBandwidth(CellStageWorld@ world, ObjectID microbeEntity, float maxAmount,
    CompoundId compoundId
) {
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    
    auto compoundVolume = SimulationParameters::compoundRegistry().getTypeData(
        compoundId).volume;
    
    auto amount = min(maxAmount * compoundVolume, microbeComponent.remainingBandwidth);
    microbeComponent.remainingBandwidth = microbeComponent.remainingBandwidth - amount;
    return amount / compoundVolume;
}

// Stores an compound in the microbe's storage organelles
//
// @param compoundId
// The compound to store
//
// @param amount
// The amount to store
//
// @param bandwidthLimited
// Determines if the storage operation is to be limited by the bandwidth of the microbe
// 
// @returns leftover
// The amount of compound not stored, due to bandwidth or being full
float storeCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double amount, bool bandwidthLimited)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto storedAmount = amount;

    if(bandwidthLimited){
        storedAmount = getBandwidth(world, microbeEntity, amount, compoundId);
    }

    storedAmount = min(storedAmount,
        microbeComponent.capacity - microbeComponent.stored);
        
    world.GetComponent_CompoundBagComponent(microbeEntity).giveCompound(compoundId,
        storedAmount);
        
    microbeComponent.stored = microbeComponent.stored + storedAmount;
    return amount - storedAmount;
}

// Removes compounds from the microbe's storage organelles
//
// @param compoundId
// The compound to remove
//
// @param maxAmount
// The maximum amount to take
//
// @returns amount
// The amount that was actually taken, between 0.0 and maxAmount.
double takeCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double maxAmount)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto takenAmount = world.GetComponent_CompoundBagComponent(microbeEntity).
        takeCompound(compoundId, maxAmount);
    
    microbeComponent.stored = microbeComponent.stored - takenAmount;
    return takenAmount;
}

// Ejects compounds from the microbes behind position, into the enviroment
// Note that the compounds ejected are created in this world and not taken from the microbe
//
// @param compoundId
// The compound type to create and eject
//
// @param amount
// The amount to eject
void ejectCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double amount)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto position = world.GetComponent_Position(microbeEntity);

    // The back of the microbe
    Float3 exit = Hex::axialToCartesian(0, 1);
    auto membraneCoords = membraneComponent.GetExternalOrganelle(exit.X, exit.Z);

    //Get the distance to eject the compunds
    auto maxR = 0;
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        auto organelle = microbeComponent.organelles[i];
        auto hexes = organelle.organelle.getHexes();
        for(uint a = 0; a < hexes.length(); ++a){
            auto hex = hexes[a];
            if(hex.r + organelle.r > maxR){
                maxR = hex.r + organelle.r;
            }
        }
    }

    //The distance is two hexes away from the back of the microbe.
    //This distance could be precalculated when adding/removing an organelle
    //for more efficient pooping.
    auto ejectionDistance = (maxR + 3) * HEX_SIZE;

    auto angle = 180;
    // Find the direction the microbe is facing
    auto yAxis = Ogre::Quaternion(position._Orientation).yAxis();
    auto microbeAngle = atan2(yAxis.x, yAxis.y);
    if(microbeAngle < 0){
        microbeAngle = microbeAngle + 2 * PI;
    }
    microbeAngle = microbeAngle * 180 / PI;
    // Take the microbe angle into account so we get world relative degrees
    auto finalAngle = (angle + microbeAngle) % 360;
        
    auto s = sin(finalAngle/180*PI);
    auto c = cos(finalAngle/180*PI);

    auto xnew = -membraneCoords.x * c + membraneCoords.y * s;
    auto ynew = membraneCoords.x * s + membraneCoords.y * c;

    auto amountToEject = takeCompound(world, microbeEntity, compoundId,
        amount/10.0);
    createCompoundCloud(world, compoundId,
        position._Position.X + xnew * ejectionDistance,
        position._Position.Y + ynew * ejectionDistance,
        // TODO: Why is this multiplied by 5000?
        // And why amountToEject is ignored
        amount * 5000);
}

void purgeCompounds(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto compoundBag = world.GetComponent_CompoundBagComponent(microbeEntity);

    auto compoundAmountToDump = microbeComponent.stored - microbeComponent.capacity;

    // // Uncomment to print compound economic information to the console.
    // if(microbeComponent.isPlayerMicrobe){
    //     for(compound, _ in pairs(compoundTable)){
    //         compoundId = CompoundRegistry.getCompoundId(compound);
    //         print(compound, compoundBag.getPrice(compoundId),
    //             compoundBag.getDemand(compoundId));
    //     }
    // }
    // print("");

    // Dumping all the useless compounds (with price = 0).
    for(_, compoundId in pairs(CompoundRegistry.getCompoundList())){
        auto price = compoundBag.getPrice(compoundId);
        if(price <= 0){
            auto amountToEject = MicrobeSystem.getCompoundAmount(microbeEntity,
                compoundId);
                
            if(amount > 0){
                amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId,
                    amountToEject);
            }
            if(amount > 0){
                MicrobeSystem.ejectCompound(microbeEntity, compoundId, amountToEject);
            }
        }
    }

    if(compoundAmountToDump > 0){
        //Calculating each compound price to dump proportionally.
        auto compoundPrices = {};
        auto priceSum = 0;
        for(_, compoundId in pairs(CompoundRegistry.getCompoundList())){
            auto amount = MicrobeSystem.getCompoundAmount(microbeEntity, compoundId);

            if(amount > 0){
                auto price = compoundBag.getPrice(compoundId);
                compoundPrices[compoundId] = price;
                priceSum = priceSum + amount / price;
            }
        }

        //Dumping each compound according to it's price.
        for(compoundId, price in pairs(compoundPrices)){
            auto amountToEject = compoundAmountToDump * (MicrobeSystem.getCompoundAmount(
                    microbeEntity, compoundId) / price) / priceSum;
            if(amount > 0){ 
                amountToEject = MicrobeSystem.takeCompound(microbeEntity,
                    compoundId, amountToEject);
            }
            if(amount > 0){
                MicrobeSystem.ejectCompound(microbeEntity, compoundId, amountToEject);
            }
        }
    }
}


void calculateHealthFromOrganelles(CellStageWorld@ world, ObjectID microbeEntity){
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    microbeComponent.hitpoints = 0;
    microbeComponent.maxHitpoints = 0;
    for(_, organelle in pairs(microbeComponent.organelles)){

        if(organelle.getCompoundBin() < 1.0){
            microbeComponent.hitpoints += organelle.getCompoundBin() *
                MICROBE_HITPOINTS_PER_ORGANELLE;
        } else {
            microbeComponent.hitpoints += MICROBE_HITPOINTS_PER_ORGANELLE;
        }
            
        microbeComponent.maxHitpoints += MICROBE_HITPOINTS_PER_ORGANELLE;
    }
}

void flashMembraneColour(CellStageWorld@ world, ObjectID microbeEntity, uint duration,
    Float4 colour)
{
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    if(microbeComponent.flashDuration <= 0.0f){
        microbeComponent.flashColour = colour;
        microbeComponent.flashDuration = duration;
    }
}

// Applies the default membrane colour
void applyMembraneColour(CellStageWorld@ world, ObjectID microbeEntity){

    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    membraneComponent.setColour(microbeComponent.speciesColour);
}


// Disables or enabled engulfmode for a microbe, allowing or
// disallowed it to absorb other microbes
void toggleEngulfMode(ObjectID microbeEntity){
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    auto rigidBodyComponent = world.GetComponent_RigidBodyComponent(microbeEntity);
    auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);

    if(microbeComponent.engulfMode){
        microbeComponent.movementFactor = microbeComponent.movementFactor *
            ENGULFING_MOVEMENT_DIVISION;
        soundSourceComponent.stopSound("microbe-engulfment"); // Possibly comment out.
            // If version > 0.3.2 delete. //> We're way past 0.3.2, do we still need this?
        rigidBodyComponent.reenableAllCollisions();
    } else {
        microbeComponent.movementFactor = microbeComponent.movementFactor /
            ENGULFING_MOVEMENT_DIVISION;
    }

    microbeComponent.engulfMode = !microbeComponent.engulfMode;
}


// Damages the microbe, killing it if its hitpoints drop low enough
//
// @param amount
//  amount of hitpoints to substract
void damage(CellStageWorld@ world, ObjectID microbeEntity, uint amount, const string &in
    damageType)
{
    if(damageType == ""){
        assert(false, "Damage type is empty");
    }

    if(amount < 0){
        assert(false, "Can't deal negative damage. Use MicrobeSystem.heal instead");
    }

    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity, MicrobeComponent);
    auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity, SoundSourceComponent);
    
    if(damageType == "toxin"){
        soundSourceComponent.playSound("microbe-toxin-damage");
    }
    
    // Choose a random organelle or membrane to damage.
    // TODO: CHANGE TO USE AGENT CODES FOR DAMAGE.
    auto rand = math.random(1, microbeComponent.maxHitpoints /
        MICROBE_HITPOINTS_PER_ORGANELLE);
    auto i = 1;
    for(_, organelle in pairs(microbeComponent.organelles)){
        // If this is the organelle we have chosen...
        if(i == rand){
            // Deplete its health/compoundBin.
            organelle.damageOrganelle(amount);
        }
        i = i + 1;
    }
    
    // Find out the amount of health the microbe has.
    calculateHealthFromOrganelles(microbeEntity);
        
    if(microbeComponent.hitpoints <= 0){
        microbeComponent.hitpoints = 0;
        MicrobeSystem.kill(microbeEntity);
    }
}


// TODO: we have a similar method in procedural_microbes.lua and another one
// in microbe_editor.lua.
// They probably should all use the same one.
// We'll probably need a rotation for this, although maybe it should be done in c++ where
// sets are a thing?
bool validPlacement(CellStageWorld@ world, ObjectID microbeEntity, const Organelle@ organelle,
    Int2 hex
) {  
    auto touching = false;
    assert(false, "TODO: should this hex list here be rotated, this doesn't seem to take "
        "a rotation parameter in");
    for(s, hex in pairs(organelle._hexes)){
        
        auto organelle = MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q, hex.r + r);
        if(organelle){
            if(organelle.name != "cytoplasm"){
                return false ;
            }
        }
        
        if(MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 0, hex.r + r - 1) ||
            MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 1, hex.r + r - 1) ||
            MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 1, hex.r + r + 0) ||
            MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 0, hex.r + r + 1) ||
            MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q - 1, hex.r + r + 1) ||
            MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q - 1, hex.r + r + 0))
        {
            touching = true;
        }
    }
    
    return touching;
}


// Adds a new organelle
//
// The space at (q,r) must not be occupied by another organelle already.
//
// @param q,r
// Offset of the organelle's center relative to the microbe's center in
// axial coordinates. These are now in the organelle object
//
// @param organelle
// The organelle to add
//
// @return
//  returns whether the organelle was added
bool addOrganelle(CellStageWorld@ world, ObjectID microbeEntity, PlacedOrganelle@ organelle)
{
    // Faster to first check can we add and then get the components //
    auto s = encodeAxial(organelle.q, organelle.r);
    if(microbeComponent.organelles[s]){
        return false;
    }

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    
    microbeComponent.organelles[s] = organelle;
    local x, y = axialToCartesian(q, r);
    auto translation = Vector3(x, y, 0);
    // Collision shape
    // TODO: cache for performance
    auto compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape);
    compoundShape.addChildShape(
        translation,
        Quaternion(Radian(0), Vector3(1,0,0)),
        organelle.collisionShape
    );
    rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass +
        organelle.mass;
    rigidBodyComponent.properties.touch();

    // Need to begin update for our physics body as this adds the
    // hexes of the organelle as a sub collision
    // This isn't optimal if multiple are added but simplifies calling this
    NewtonCollision@ collision;
    collision.CompoundShapeBeginAddRemove();
    organelle.onAddedToMicrobe(microbeEntity, world, collision);
    
    calculateHealthFromOrganelles(microbeEntity);
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth +
        BANDWIDTH_PER_ORGANELLE; // Temporary solution for increasing max bandwidth
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;
    
    // Send the organelles to the membraneComponent so that the membrane can "grow"
    auto localQ = q - organelle.position.q;
    auto localR = r - organelle.position.r;
    if(organelle.getHex(localQ, localR) !is null){
        for(_, hex in pairs(organelle._hexes)){
            auto q = hex.q + organelle.position.q;
            auto r = hex.r + organelle.position.r;
            local x, y = axialToCartesian(q, r);
            membraneComponent.sendOrganelles(x, y);
        }
        // What is this return?
        return organelle;
    }
       
    return true;
}


// Creates a new microbe with all required components
//
// @param name
// The entity's name. If null, the entity will be unnamed.
//
// @returns microbe
// An object of type Microbe
ObjectID createMicrobeEntity(CellStageWorld@ world, const string &in name, bool aiControlled,
    const string &in speciesName, bool in_editor)
{
    assert(speciesName != "", "Empty species name for create microbe");

    local entity;
    if(name){
        entity = Entity(name, g_luaEngine.currentGameState.wrapper);
    } else {
        entity = Entity(g_luaEngine.currentGameState.wrapper);
    }

    auto rigidBody = RigidBodyComponent();
    rigidBody.properties.shape = CompoundShape();
    rigidBody.properties.linearDamping = 0.5;
    rigidBody.properties.friction = 0.2;
    rigidBody.properties.mass = 0.0;
    rigidBody.properties.linearFactor = Vector3(1, 1, 0);
    rigidBody.properties.angularFactor = Vector3(0, 0, 1);
    rigidBody.properties.touch();

    auto reactionHandler = CollisionComponent();
    reactionHandler.addCollisionGroup("microbe");

    auto membraneComponent = MembraneComponent();

    auto soundComponent = SoundSourceComponent();
    auto s1 = null;
    soundComponent.addSound("microbe-release-toxin",
        "soundeffects/microbe-release-toxin.ogg");
    soundComponent.addSound("microbe-toxin-damage",
        "soundeffects/microbe-toxin-damage.ogg");
    soundComponent.addSound("microbe-death", "soundeffects/microbe-death.ogg");
    soundComponent.addSound("microbe-pickup-organelle",
        "soundeffects/microbe-pickup-organelle.ogg");
    soundComponent.addSound("microbe-engulfment", "soundeffects/engulfment.ogg");
    soundComponent.addSound("microbe-reproduction", "soundeffects/reproduction.ogg");

    s1 = soundComponent.addSound("microbe-movement-1",
        "soundeffects/microbe-movement-1.ogg");
    s1.properties.volume = 0.4;
    s1.properties.touch();
    s1 = soundComponent.addSound("microbe-movement-turn",
        "soundeffects/microbe-movement-2.ogg");
    s1.properties.volume = 0.1;
    s1.properties.touch();
    s1 = soundComponent.addSound("microbe-movement-2",
        "soundeffects/microbe-movement-3.ogg");
    s1.properties.volume = 0.4;
    s1.properties.touch();

    auto components = {
        CompoundAbsorberComponent(),
        OgreSceneNodeComponent(),
        CompoundBagComponent(),
        MicrobeComponent(not aiControlled, speciesName),
        reactionHandler,
        rigidBody,
        soundComponent,
        membraneComponent
    }

    if(aiControlled){
        auto aiController = MicrobeAIControllerComponent();
        table.insert(components, aiController);
    }

    for(_, component in ipairs(components)){
        entity.addComponent(component);
    }
    
    MicrobeSystem.initializeMicrobe(entity, in_editor, g_luaEngine.currentGameState);

    return entity;
}

}

