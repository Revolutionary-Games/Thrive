// Operations on microbe entities

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
    
    auto microbeComponent = getComponent(microbeEntity, MicrobeComponent);
    // This needs to loop all the components and get matching one
    return getComponent(microbeComponent.speciesName, g_luaEngine.currentGameState,
        SpeciesComponent);
}

// Retrieves the organelle occupying a hex cell
//
// @param q, r
// Axial coordinates, relative to the microbe's center
//
// @returns organelle
// The organelle at (q,r) or null if the hex is unoccupied
PlacedOrganelle@ getOrganelleAt(ObjectID microbeEntity, Int2 hex){
    auto microbeComponent = getComponent(microbeEntity, MicrobeComponent);

    for(_, organelle in pairs(microbeComponent.organelles)){
        auto localQ = q - organelle.position.q;
        auto localR = r - organelle.position.r;
        if(organelle.getHex(localQ, localR) !is null){
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
bool removeOrganelle(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    auto rigidBodyComponent = world.GetComponent_RigidBodyComponent(microbeEntity);

    auto organelle = getOrganelleAt(microbeEntity, q, r);
    if(not organelle){
        return false
            }
    
    auto s = encodeAxial(organelle.position.q, organelle.position.r);
    microbeComponent.organelles[s] = null;
    
    rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass - organelle.mass;
    rigidBodyComponent.properties.touch();
    // TODO: cache for performance
    auto compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape);
    compoundShape.removeChildShape(
        organelle.collisionShape
    );
    
    organelle.onRemovedFromMicrobe();
    
    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity);
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth -
        BANDWIDTH_PER_ORGANELLE ; // Temporary solution for decreasing max bandwidth
        
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;
    
    return true;
}

bool organelleDestroyedByDamage(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){

    // TODO: effects for destruction?
    return removeOrganelle(world, microbeEntity, hex);
}

void respawnPlayer(CellStageWorld@ world){
    auto playerEntity = Entity("player", g_luaEngine.currentGameState.wrapper);
    auto microbeComponent = getComponent(playerEntity, MicrobeComponent);
    auto rigidBodyComponent = getComponent(playerEntity, RigidBodyComponent);
    auto sceneNodeComponent = getComponent(playerEntity, OgreSceneNodeComponent);

    microbeComponent.dead = false;
    microbeComponent.deathTimer = 0;

    // Reset the growth bins of the organelles to full health.
    for(_, organelle in pairs(microbeComponent.organelles)){
        organelle.reset();
    }
    MicrobeSystem.calculateHealthFromOrganelles(playerEntity);

    rigidBodyComponent.setDynamicProperties(
        Vector3(0,0,0), // Position
        Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), // Orientation
        Vector3(0, 0, 0), // Linear velocity
        Vector3(0, 0, 0)  // Angular velocity
    );

    sceneNodeComponent.visible = true;
    sceneNodeComponent.transform.position = Vector3(0, 0, 0);
    sceneNodeComponent.transform.touch();

    // TODO: give the microbe the values from some table instead.
    MicrobeSystem.storeCompound(playerEntity, CompoundRegistry.getCompoundId("atp"),
        50, false);

    setRandomBiome(g_luaEngine.currentGameState);
    global_activeMicrobeStageHudSystem.suicideButtonreset();
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
void storeCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double amount, bool bandwidthLimited)
{
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    auto storedAmount = amount;

    if(bandwidthLimited){
        storedAmount = MicrobeSystem.getBandwidth(microbeEntity, amount, compoundId);
    }

    storedAmount = min(storedAmount,
        microbeComponent.capacity - microbeComponent.stored);
        
    world.GetComponent_CompoundBagComponent(microbeEntity).giveCompound(tonumber(compoundId),
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
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
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
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto sceneNodeComponent = world.GetComponent_OgreSceneNodeComponent(microbeEntity);

    // The back of the microbe
    local exitX, exitY = axialToCartesian(0, 1);
    auto membraneCoords = membraneComponent.getExternOrganellePos(exitX, exitY);

    //Get the distance to eject the compunds
    auto maxR = 0;
    for(_, organelle in pairs(microbeComponent.organelles)){
        for(_, hex in pairs(organelle._hexes)){
            if(hex.r + organelle.position.r > maxR){
                maxR = hex.r + organelle.position.r;
            }
        }
    }

    //The distance is two hexes away from the back of the microbe.
    //This distance could be precalculated when adding/removing an organelle
    //for more efficient pooping.
    auto ejectionDistance = (maxR + 3) * HEX_SIZE;

    auto angle = 180;
    // Find the direction the microbe is facing
    auto yAxis = sceneNodeComponent.transform.orientation.yAxis();
    auto microbeAngle = math.atan2(yAxis.x, yAxis.y);
    if(microbeAngle < 0){
        microbeAngle = microbeAngle + 2 * math.pi;
    }
    microbeAngle = microbeAngle * 180 / math.pi;
    // Take the microbe angle into account so we get world relative degrees
    auto finalAngle = (angle + microbeAngle) % 360;
        
    auto s = math.sin(finalAngle/180*math.pi);
    auto c = math.cos(finalAngle/180*math.pi);

    auto xnew = -membraneCoords[1] * c + membraneCoords[2] * s;
    auto ynew = membraneCoords[1] * s + membraneCoords[2] * c;

    auto amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId,
        amount/10.0);
    createCompoundCloud(CompoundRegistry.getCompoundInternalName(compoundId),
        sceneNodeComponent.transform.position.x + xnew * ejectionDistance,
        sceneNodeComponent.transform.position.y + ynew * ejectionDistance,
        amount * 5000);
}

}

