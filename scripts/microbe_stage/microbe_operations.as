// Operations on microbe entities
#include "biome.as"
#include "species_system.as"
#include "setup.as"

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

// Getter for microbe species
//
// returns the species component or null if species with that name doesn't exist
SpeciesComponent@ getSpeciesComponent(CellStageWorld@ world, const string &in speciesName){

    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, speciesName);

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

    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
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
bool removeOrganelle(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex)
{
    auto organelle = getOrganelleAt(world, microbeEntity, hex);

    if(organelle is null){
        return false;
    }

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);

    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        if(microbeComponent.organelles[i] is organelle){

            microbeComponent.organelles.removeAt(i);
            break;
        }
    }

    auto position = world.GetComponent_Position(microbeEntity);

    organelle.onRemovedFromMicrobe(microbeEntity, rigidBodyComponent.Collision);

    // Send the organelles to the membraneComponent so that the membrane can "shrink"
    // This is always 0?
    auto localQ = organelle.q - organelle.q;
    auto localR = organelle.r - organelle.r;

    // I guess this might skip sending organelles that have no hexes? to the membrane
    if(organelle.organelle.getHex(localQ, localR) !is null){

        auto hexes = organelle.organelle.getHexes();
        for(uint i = 0; i < hexes.length(); ++i){

            auto removedHex = hexes[i];

            auto q = removedHex.q + organelle.q;
            auto r = removedHex.r + organelle.r;
            Float3 membranePoint = Hex::axialToCartesian(q, r);

            // TODO: this is added here to make it impossible for our
            // caller to forget to call this, and this basically only
            // once does something and then on next tick the membrane
            // is initialized again
            membraneComponent.clear();
            membraneComponent.removeSentOrganelle(membranePoint.X, membranePoint.Z);
        }

        // What is this return?
        // return organelle;
        return true;
    }

    // // Need to recreate the body
    // if(rigidBodyComponent.Body !is null){

    //     LOG_INFO("Recreating physics body in removeOrganelle");
    //     rigidBodyComponent.CreatePhysicsBody(world.GetPhysicalWorld(),
    //         world.GetPhysicalMaterial("cell"));
    //     rigidBodyComponent.SetMass(rigidBodyComponent.Mass - organelle.organelle.mass);
    //     _applyMicrobePhysicsBodySettings(world, rigidBodyComponent);

    //     // And jump it to the current position
    //     rigidBodyComponent.JumpTo(position);
    // }

    // This refreshing these things could probably be somewhere else...
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth -
        BANDWIDTH_PER_ORGANELLE ; // Temporary solution for decreasing max bandwidth

    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;

    return true;
}

bool organelleDestroyedByDamage(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){
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

    // TODO: the cell template should be reapplied here

    // Reset the growth bins of the organelles to full health.
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        microbeComponent.organelles[i].reset();
    }
    setupMicrobeHitpoints(world, playerEntity,DEFAULT_HEALTH);
    //setup compounds
    setupMicrobeCompounds(world,playerEntity);
    // Reset position //
    rigidBodyComponent.SetPosition(Float3(0, 0, 0), Float4::IdentityQuaternion);

    // The physics body will set the Position on next tick

    // TODO: reset velocity like in the old lua code?

    // This set position is actually useless, but it was in the old lua code
    // sceneNodeComponent.Node.setPosition(Float3(0, 0, 0));
    sceneNodeComponent.Hidden = false;
    sceneNodeComponent.Marked = true;

    setRandomBiome(world);
    cast<MicrobeStageHudSystem>(world.GetScriptSystem("MicrobeStageHudSystem")).
        suicideButtonreset();

    //Decrease the population by 10
    auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
    playerSpecies.population -= 10;
}


void setupMicrobeHitpoints(CellStageWorld@ world, ObjectID microbeEntity, int health){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    microbeComponent.maxHitpoints = health;
    microbeComponent.hitpoints = microbeComponent.maxHitpoints;
    microbeComponent.agentEmissionCooldown=uint(0);
}

//grabs compounds from template (starter_mcirobes) and stores them)
void setupMicrobeCompounds(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
                MicrobeTemplate@ data = cast<MicrobeTemplate@>(STARTER_MICROBES["Default"]);

    auto ids = getSpeciesComponent(world, microbeEntity).avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); ++i){
        CompoundId compoundId = parseUInt(ids[i]);
        InitialCompound amount = InitialCompound(getSpeciesComponent(world, microbeEntity).avgCompoundAmounts[ids[i]]);

        if(amount.amount != 0){
            MicrobeOperations::storeCompound(world, microbeEntity, compoundId, amount.amount, false);
        }
    }
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
// we need to remove this and have individual storage space
// The best way to do this is maybe have a variable for
// each possible compound, or  a list of floats for each
// possible compound, with maxes being based on Microbe.capacity

float storeCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double amount, bool bandwidthLimited)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto storedAmount = amount;

    if(bandwidthLimited){
        storedAmount = getBandwidth(world, microbeEntity, amount, compoundId);
    }
    //min it by capcity, so you cant go over capcity, maybe we dont need a bunch of variables
    storedAmount = min(storedAmount, microbeComponent.capacity);
    // This adds compounds, (it does not set but instead adds)
    if (getCompoundAmount(world,microbeEntity,compoundId)+amount <= microbeComponent.capacity)
    {
    world.GetComponent_CompoundBagComponent(microbeEntity).giveCompound(compoundId,storedAmount);
    //For run and tumble
    microbeComponent.stored = microbeComponent.stored + storedAmount;
    }

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
       amountToEject);
}

// Since we have individual storage now we dont need this
// (its functionally useless from a gameplay perspective since
// you no longer need to dump things because thngs can no longer
// "take up each others space"  However, it would be weird to store
// up compounds you dont use, so lets purge those.
void purgeCompounds(CellStageWorld@ world, ObjectID microbeEntity){
    //LOG_INFO("Enacting a purge of blue cells (also known as compounds)");
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto compoundBag = world.GetComponent_CompoundBagComponent(microbeEntity);

    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
    // Price is 1 if used, 0 if not
    auto price = compoundBag.getPrice(compoundId);
    auto useful = SimulationParameters::compoundRegistry().getTypeData(compoundId).isUseful;
    //LOG_INFO("ID:"+compoundId+" price:"+price);
    if (price == 0 && !useful)
    {
    // Dont remove everything immedately, give it some time so people can see it happening
        double amountToEject = 2;
    double availableCompound = getCompoundAmount(world,microbeEntity, compoundId);
            // This was also 'amount' so maybe this didn't work either?
            if(amountToEject > 0 && availableCompound-amountToEject >= 0){
                amountToEject = takeCompound(world, microbeEntity,
                    compoundId, amountToEject);
                ejectCompound(world, microbeEntity, compoundId, amountToEject);
            }
    // If we flagged the second one but we still have some left just get rid of it all
    else if (availableCompound > 0)
    {
        amountToEject = takeCompound(world, microbeEntity,
                    compoundId, availableCompound);
                ejectCompound(world, microbeEntity, compoundId, amountToEject);
    }
    }
    }
}


// Commented out all calls to this But keeping it in case we want to go back to the old system
/*void calculateHealthFromOrganelles(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    microbeComponent.hitpoints = 0;
    microbeComponent.maxHitpoints = 0;
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        auto organelle = microbeComponent.organelles[i];

        if(organelle.getCompoundBin() < 1.0){
            microbeComponent.hitpoints += round(organelle.getCompoundBin() *
                MICROBE_HITPOINTS_PER_ORGANELLE);
        } else {
            microbeComponent.hitpoints += MICROBE_HITPOINTS_PER_ORGANELLE;
        }

        microbeComponent.maxHitpoints += MICROBE_HITPOINTS_PER_ORGANELLE;
    }
}*/

void flashMembraneColour(CellStageWorld@ world, ObjectID microbeEntity, uint duration,
    Float4 colour)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

    if(microbeComponent.flashDuration <= 0){
        microbeComponent.flashColour = colour;
        microbeComponent.flashDuration = duration;
    }
}

// Applies the default membrane colour
void applyMembraneColour(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto speciesColour =  world.GetComponent_SpeciesComponent(findSpeciesEntityByName(world, microbeComponent.speciesName)).colour;
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    membraneComponent.setColour(speciesColour);
}


    // // Drains an agent from the microbes special storage and emits it
    // //
    // // @param compoundId
    // // The compound id of the agent to emit
    // //
    // // @param maxAmount
    // // The maximum amount to try to emit
void emitAgent(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId, double maxAmount, float lifeTime){
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    //     auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity, SoundSourceComponent);
        auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
        auto cellPosition = world.GetComponent_Position(microbeEntity);

    // Cooldown code
    LOG_INFO("Cooldown "+microbeComponent.agentEmissionCooldown);

    if(microbeComponent.agentEmissionCooldown > 0){ return; }

    auto numberOfAgentVacuoles = int (microbeComponent.specialStorageOrganelles[formatUInt(compoundId)]);
    // Only shoot if you have an agent vacuole.
    if(numberOfAgentVacuoles == 0){ return; }

    if(MicrobeOperations::getCompoundAmount(world, microbeEntity, compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT)
        {
        // Calculate the emission angle of the agent emitter
         // The front of the microbe
         Float3 exit = Hex::axialToCartesian(1, 0);
        auto membraneCoords = membraneComponent.GetExternalOrganelle(exit.X, exit.Z);

        //Get the distance to eject the agent
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
        auto angle =  atan2(exit.Z, exit.X);
            if(angle < 0){
                 angle = angle + 2*PI;
            }
             angle = -(angle * 180/PI-90 ) % 360;
        // Find the direction the microbe is facing
        auto ejectionDistance = (maxR+1.7f) * HEX_SIZE;
        auto yAxis = Ogre::Quaternion(cellPosition._Orientation).yAxis();
        auto microbeAngle = atan2(yAxis.x,yAxis.z);
        if(microbeAngle < 0){
            microbeAngle = microbeAngle + 2 * PI;
        }
        microbeAngle = microbeAngle * 180 / PI;
        // Take the microbe angle into account so we get world relative degrees
        auto finalAngle = (angle + microbeAngle) % 360;
        auto s = sin(finalAngle/180*PI);
        auto c = cos(finalAngle/180*PI);
        auto xnew = -membraneCoords.x * c + membraneCoords.z * s;
        auto ynew = membraneCoords.x * s + membraneCoords.z * c;

        auto amountToEject = takeCompound(world, microbeEntity,compoundId, maxAmount/10.0);

        auto vec = ( microbeComponent.facingTargetPoint - cellPosition._Position);
        auto direction = vec.Normalize();
        if (amountToEject >= MINIMUM_AGENT_EMISSION_AMOUNT)
            {
            GetEngine().GetSoundDevice().Play2DSoundEffect("Data/Sound/soundeffects/microbe-release-toxin.ogg");
            createAgentCloud(world, compoundId, cellPosition._Position+(Float3(xnew*ejectionDistance,0,ynew*ejectionDistance)), direction,amountToEject * 10.0f,lifeTime);
            // The cooldown time is inversely proportional to the amount of agent vacuoles.
            microbeComponent.agentEmissionCooldown = uint(AGENT_EMISSION_COOLDOWN/numberOfAgentVacuoles);
            }
        }
    }

// Disables or enables engulfmode for a microbe, allowing or
// disallowing it to absorb other microbes
void toggleEngulfMode(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

    // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
    if(microbeComponent.engulfMode && !microbeComponent.isBeingEngulfed){
        microbeComponent.movementFactor = 1.0f;
        // soundSourceComponent.stopSound("microbe-engulfment"); // Possibly comment out.
    }
    else  if (microbeComponent.isBeingEngulfed)
    {
        microbeComponent.movementFactor = microbeComponent.movementFactor /  ENGULFED_MOVEMENT_DIVISION;
    }
    else
    {
        microbeComponent.movementFactor = microbeComponent.movementFactor /
            ENGULFING_MOVEMENT_DIVISION;
    }

    microbeComponent.engulfMode = !microbeComponent.engulfMode;
}


// Damages the microbe, killing it if its hitpoints drop low enough
// @param amount
//  amount of hitpoints to substract
void damage(CellStageWorld@ world, ObjectID microbeEntity, double amount, const string &in
    damageType)
{
    if(damageType == ""){
        assert(false, "Damage type is empty");
    }

    if(amount < 0.0f){
        assert(false, "Can't deal negative damage. Use MicrobeOperations::heal instead");
    }

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);

    if(damageType == "toxin"){
    // Play the toxin sound
    GetEngine().GetSoundDevice().Play2DSoundEffect("Data/Sound/soundeffects/microbe-toxin-damage.ogg");
    }

    // Choose a random organelle or membrane to damage.
    // TODO: CHANGE TO USE AGENT CODES FOR DAMAGE.
   /* int rand = GetEngine().GetRandom().GetNumber(0, int(microbeComponent.maxHitpoints /
            MICROBE_HITPOINTS_PER_ORGANELLE) - 1);
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        // If this is the organelle we have chosen...
        if(int(i) == rand){
            // Deplete its health/compoundBin.
            microbeComponent.organelles[i].damageOrganelle(amount);
            break;
        }
    }
    */

    microbeComponent.hitpoints -= amount;
    // Flash the microbe red
    //LOG_INFO("DAMAGE FLASH");
    flashMembraneColour(world, microbeEntity, 1000,
                    Float4(1,0,0,0.5));

    // Find out the amount of health the microbe has.
    if(microbeComponent.hitpoints <= 0.0f){
        microbeComponent.hitpoints = 0.0f;
        kill(world, microbeEntity);
    }
}


// TODO: we have a similar method in procedural_microbes.lua and another one
// in microbe_editor.lua.
// They probably should all use the same one.
// We'll probably need a rotation for this, although maybe it should be done in c++ where
// sets are a thing?
bool validPlacement(CellStageWorld@ world, ObjectID microbeEntity, const Organelle@ organelle,
    Int2 posToCheck
) {
    auto touching = false;
    //assert(false, "TODO: should this hex list here be rotated, this doesn't seem to take "
      //  "a rotation parameter in");
    auto hexes = organelle.getHexes();
    for(uint i = 0; i < hexes.length(); ++i){

        auto hex = hexes[i];

        auto existingOrganelle = getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X,
                    hex.r + posToCheck.Y});
        if(existingOrganelle !is null){
            if(existingOrganelle.organelle.name != "cytoplasm"){
                return false ;
            }
        }

        // These are pretty expensive methods
        if(getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X + 0,
                        hex.r + posToCheck.Y - 1}) !is null ||
            getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X + 1,
                        hex.r + posToCheck.Y - 1}) !is null ||
            getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X + 1,
                        hex.r + posToCheck.Y + 0}) !is null ||
            getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X + 0,
                        hex.r + posToCheck.Y + 1}) !is null ||
            getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X - 1,
                        hex.r + posToCheck.Y + 1}) !is null ||
            getOrganelleAt(world, microbeEntity, {hex.q + posToCheck.X - 1,
                        hex.r + posToCheck.Y + 0})  !is null)
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
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

    // Exact coordinate check //
    // This isn't perfect so that's why it needs to have been checked before that this
    // place isn't full
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
        if(microbeComponent.organelles[i].q == organelle.q &&
            microbeComponent.organelles[i].r == organelle.r)
        {
            return false;
        }
    }

    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    auto position = world.GetComponent_Position(microbeEntity);

    microbeComponent.organelles.insertLast(@organelle);
    // Float3 translation = Hex::axialToCartesian(organelle.q, organelle.r);

    // Collision shape
    // Need to begin update for our physics body as this adds the
    // hexes of the organelle as a sub collision
    // This isn't optimal if multiple are added but simplifies calling this
    NewtonCollision@ collision = rigidBodyComponent.Collision;
    collision.CompoundCollisionBeginAddRemove();

    organelle.onAddedToMicrobe(microbeEntity, world, collision);

    collision.CompoundCollisionEndAddRemove();

    // // The body is recreated if it existed already
    // if(rigidBodyComponent.Body !is null){

    //     // Need to recreate the body
    //     LOG_INFO("Recreating physics body in addOrganelle");
    //     rigidBodyComponent.CreatePhysicsBody(world.GetPhysicalWorld(),
    //         world.GetPhysicalMaterial("cell"));

    //     rigidBodyComponent.SetMass(rigidBodyComponent.Mass + organelle.organelle.mass);

    //     _applyMicrobePhysicsBodySettings(world, rigidBodyComponent);

    //     // And jump it to the current position
    //     rigidBodyComponent.JumpTo(position);
    // }

    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth +
        BANDWIDTH_PER_ORGANELLE; // Temporary solution for increasing max bandwidth
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;

    // Send the organelles to the membraneComponent so that the membrane can "grow"
    // This is always 0?
    auto localQ = organelle.q - organelle.q;
    auto localR = organelle.r - organelle.r;

    // I guess this might skip sending organelles that have no hexes? to the membrane
    if(organelle.organelle.getHex(localQ, localR) !is null){

        auto hexes = organelle.organelle.getHexes();
        for(uint i = 0; i < hexes.length(); ++i){

            auto hex = hexes[i];

            auto q = hex.q + organelle.q;
            auto r = hex.r + organelle.r;
            Float3 membranePoint = Hex::axialToCartesian(q, r);
            // TODO: this is added here to make it impossible for our
            // caller to forget to call this, and this basically only
            // once does something and then on next tick the membrane
            // is initialized again
            membraneComponent.clear();
            membraneComponent.sendOrganelles(membranePoint.X, membranePoint.Z);
        }

        // What is this return?
        // return organelle;
        return true;
    }

    return true;
}


// speciesName decides the template to use, while individualName is
// used for referencing the instance
ObjectID spawnMicrobe(CellStageWorld@ world, Float3 pos, const string &in speciesName,
    bool aiControlled, const string &in individualName
) {
    assert(world !is null);
    assert(speciesName != "");

    if(pos.Y != 0)
        LOG_WARNING("spawnMicrobe: spawning at y-coordinate: " + pos.Y);

    auto processor = getProcessorComponent(world, speciesName);

    if(processor is null){
        LOG_ERROR("Skipping microbe spawn because species '" + speciesName +
            "' doesn't have a processor component");

        return NULL_OBJECT;
    }

    auto microbeEntity = _createMicrobeEntity(world, individualName, aiControlled, speciesName,
        // in_editor
        false);

    // Teleport the cell to the right position
    auto microbePos = world.GetComponent_Position(microbeEntity);
    microbePos._Position = pos;
    microbePos.Marked = true;

    auto physics = world.GetComponent_Physics(microbeEntity);
    physics.JumpTo(microbePos);

    // Try setting the position immediately as well (as otherwise it
    // takes until the next tick for this to take effect)
    auto node = world.GetComponent_RenderNode(microbeEntity);
    node.Node.setPosition(pos);

    auto speciesEntity = findSpeciesEntityByName(world, speciesName);
    auto species = world.GetComponent_SpeciesComponent(speciesEntity);

    // Bacteria get scaled to half size
    if(species.isBacteria){
        node.Scale = Float3(0.5, 0.5, 0.5);
        node.Marked = true;
        physics.SetCollision(world.GetPhysicalWorld().CreateSphere(HEX_SIZE/2.0f));
    }

    return microbeEntity;
}

ObjectID spawnBacteria(CellStageWorld@ world, Float3 pos, const string &in speciesName,
    bool aiControlled, const string &in individualName, bool partOfColony
) {
    assert(world !is null);
    assert(speciesName != "");

    if(pos.Y != 0)
        LOG_WARNING("spawnBacteria: spawning at y-coordinate: " + pos.Y);

    auto processor = getProcessorComponent(world, speciesName);

    if(processor is null){

        LOG_ERROR("Skipping microbe spawn because species '" + speciesName +
            "' doesn't have a processor component");

        return NULL_OBJECT;
    }

    auto microbeEntity = _createMicrobeEntity(world, individualName, aiControlled, speciesName,
        // in_editor
        false);

    // Teleport the cell to the right position
    auto microbePos = world.GetComponent_Position(microbeEntity);
    microbePos._Position = pos;
    microbePos.Marked = true;

    auto physics = world.GetComponent_Physics(microbeEntity);
    physics.SetMass(physics.Mass*10);
    physics.JumpTo(microbePos);

    // Try setting the position immediately as well (as otherwise it
    // takes until the next tick for this to take effect)
    auto node = world.GetComponent_RenderNode(microbeEntity);
    node.Node.setPosition(pos);

    // Bacteria get scaled to half size
    node.Scale = Float3(0.5, 0.5, 0.5);
    node.Marked = true;
    physics.SetCollision(world.GetPhysicalWorld().CreateSphere(HEX_SIZE/2.0f));
    // Need to set bacteria spawn and it needs to be squared like it is in the spawn system. code, if part of colony but not directly spawned give a spawned component
    if (partOfColony){
    world.Create_SpawnedComponent(microbeEntity,BACTERIA_SPAWN_RADIUS*BACTERIA_SPAWN_RADIUS);
    }
    //LOG_WARNING("spawning bacterium radius "+ BACTERIA_SPAWN_RADIUS);
    return microbeEntity;
}
// Creates a new microbe with all required components. Use spawnMicrobe from other
// code instead of this function
//
// @param name
// The entity's name. If null, the entity will be unnamed.
//
// @returns microbe
// An object of type Microbe
ObjectID _createMicrobeEntity(CellStageWorld@ world, const string &in name, bool aiControlled,
    const string &in speciesName, bool in_editor)
{
    assert(speciesName != "", "Empty species name for create microbe");

    auto speciesEntity = findSpeciesEntityByName(world, speciesName);
    auto species = world.GetComponent_SpeciesComponent(speciesEntity);

    if(speciesEntity == NULL_OBJECT)
        assert(false, "Trying to create a microbe with invalid species");

    ObjectID entity = world.CreateEntity();

    auto position = world.Create_Position(entity, Float3(0, 0, 0), Float4::IdentityQuaternion);

    auto rigidBody = world.Create_Physics(entity, world, position, null);
    auto collision = world.GetPhysicalWorld().CreateCompoundCollision();

    rigidBody.SetCollision(collision);

    auto membraneComponent = world.Create_MembraneComponent(entity, species.speciesMembraneType);

    // auto soundComponent = SoundSourceComponent();
    // auto s1 = null;
    // soundComponent.addSound("microbe-release-toxin",
    //     "soundeffects/microbe-release-toxin.ogg");
    // soundComponent.addSound("microbe-toxin-damage",
    //     "soundeffects/microbe-toxin-damage.ogg");
    // soundComponent.addSound("microbe-death", "soundeffects/microbe-death.ogg");
    // soundComponent.addSound("microbe-pickup-organelle",
    //     "soundeffects/microbe-pickup-organelle.ogg");
    // soundComponent.addSound("microbe-engulfment", "soundeffects/engulfment.ogg");
    // soundComponent.addSound("microbe-reproduction", "soundeffects/reproduction.ogg");

    // s1 = soundComponent.addSound("microbe-movement-1",
    //     "soundeffects/microbe-movement-1.ogg");
    // s1.properties.volume = 0.4;
    // s1.properties.touch();
    // s1 = soundComponent.addSound("microbe-movement-turn",
    //     "soundeffects/microbe-movement-2.ogg");
    // s1.properties.volume = 0.1;
    // s1.properties.touch();
    // s1 = soundComponent.addSound("microbe-movement-2",
    //     "soundeffects/microbe-movement-3.ogg");
    // s1.properties.volume = 0.4;
    // s1.properties.touch();

    auto compoundAbsorberComponent = world.Create_CompoundAbsorberComponent(entity);

    world.Create_RenderNode(entity);
    auto compoundBag = world.Create_CompoundBagComponent(entity);

    auto processorComponent = world.Create_ProcessorComponent(entity);

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Create(entity));

    microbeComponent.init(entity, not aiControlled, speciesName);

    if(aiControlled){
        world.GetScriptComponentHolder("MicrobeAIControllerComponent").Create(entity);
    }

    // Rest of the stuff doesn't really work in_editor
    // TODO: verify that this is actually the case
    if(in_editor){

        return entity;
    }

    auto processor = world.GetComponent_ProcessorComponent(speciesEntity);

    if(processor is null){
        LOG_ERROR("Microbe species '" + microbeComponent.speciesName +
            "' doesn't have a processor component");
        // assert(processor !is null);
    } else {
    LOG_INFO("Added processor");
        compoundBag.setProcessor(processor, microbeComponent.speciesName);
    }

    if(microbeComponent.organelles.length() > 0)
        assert(false, "Freshly created microbe has organelles in it");

    // Apply the template //
    Species::applyTemplate(world, entity, species);

    // ------------------------------------ //
    // Initialization logic taken from MicrobeSystem and put here now
    assert(microbeComponent.organelles.length() > 0, "Microbe has no "
        "organelles in initializeMicrobe");

    // We create physics body after adding the organelles as that
    // requires the physics body to be recreated when any organelle is
    // added (if the body already exists at that point) so we do it
    // here after that
    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(), world.GetPhysicalMaterial("cell"));

    assert(rigidBody.Body !is null);

    // Allowing the microbe to absorb all the compounds.
    setupAbsorberForAllCompounds(compoundAbsorberComponent);

    // We don't add the sub collisions here. Instead they are
    // individually added in addOrganelle, which is not very efficient
    // rigidBody.Collision.CompoundCollisionBeginAddRemove();
    // rigidBody.Collision.CompoundCollisionEndAddRemove();

    float mass = 0.f;

    // Organelles
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        auto organelle = microbeComponent.organelles[i];

        // organelles are already initialized when they are added
        // Not sure if this reset is needed here
        organelle.reset();

        mass += organelle.organelle.mass;
    }

    rigidBody.SetMass(mass);
    _applyMicrobePhysicsBodySettings(world, rigidBody);

    microbeComponent.initialized = true;
    return entity;
}

void _applyMicrobePhysicsBodySettings(CellStageWorld@ world, Physics@ rigidBody){

    // TODO: apply all these properties
    rigidBody.SetLinearDamping(0.2);
    // This is new
    rigidBody.SetAngularDamping(0.2);

    // rigidBody.properties.friction = 0.2;
    // rigidBody.properties.mass = 0.0;
    // rigidBody.properties.linearFactor = Vector3(1, 1, 0);
    // rigidBody.properties.angularFactor = Vector3(0, 0, 1);
    // rigidBody.properties.touch();

    // auto reactionHandler = CollisionComponent();
    // reactionHandler.addCollisionGroup("microbe");


    // Constraint to 2d movement
    if(!rigidBody.CreatePlaneConstraint(world.GetPhysicalWorld(), Float3(0, 1, 0))){
        LOG_ERROR("Failed to constraint cell to 2d plane");
    }
}

// Kills the microbe, releasing stored compounds into the enviroment
void kill(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
    auto microbeSceneNode = world.GetComponent_RenderNode(microbeEntity);
    auto position = world.GetComponent_Position(microbeEntity);

    // Hacky but meh.
    if(microbeComponent.dead){
        LOG_ERROR("Trying to kill a dead microbe");
        return;
    }

    // Releasing all the agents.
    auto storageTypes = microbeComponent.specialStorageOrganelles.getKeys();
    for(uint i = 0; i < storageTypes.length(); ++i){
        CompoundId compoundId = parseInt(storageTypes[i]);
        auto _amount = getCompoundAmount(world, microbeEntity, compoundId);
        while(_amount > 0){
            // Eject up to 3 units per particle
            auto ejectedAmount = takeCompound(world, microbeEntity, compoundId, 2);
            auto direction = Float3(GetEngine().GetRandom().GetNumber(0.0f, 1.0f) * 2 - 1,
                0, GetEngine().GetRandom().GetNumber(0.0f, 1.0f) * 2 - 1);
            createAgentCloud(world, compoundId, position._Position, direction, ejectedAmount, 2000);
            _amount = _amount - ejectedAmount;
        }
    }
    dictionary compoundsToRelease;
    // Eject the compounds that was in the microbe
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){

        auto total = getCompoundAmount(world, microbeEntity, compoundId);
        auto ejectedAmount = takeCompound(world, microbeEntity,
            compoundId, total);
        compoundsToRelease[formatInt(compoundId)] = ejectedAmount;
    }

    // Eject some part of the build cost of all the organelles
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        auto organelle = microbeComponent.organelles[i];

        auto keys = organelle.organelle.initialComposition.getKeys();

        for(uint a = 0; a < keys.length(); ++a){

            float amount = float(organelle.organelle.initialComposition[keys[a]]);

            auto compoundId = SimulationParameters::compoundRegistry().getTypeId(keys[a]);

            auto key = formatInt(compoundId);

            if(!compoundsToRelease.exists(key)){
                compoundsToRelease[key] = amount * COMPOUND_RELEASE_PERCENTAGE;
            } else {
                compoundsToRelease[key] = float(compoundsToRelease[key]) +
                    amount * COMPOUND_RELEASE_PERCENTAGE;
            }
        }
    }

    // TODO: make the compounds be released inside of the microbe and not in the back.
    auto keys = compoundsToRelease.getKeys();
    for(uint i = 0; i < keys.length(); ++i){

        ejectCompound(world, microbeEntity, parseInt(keys[i]),
            float(compoundsToRelease[keys[i]]));
    }

    // Play the death sound
    GetEngine().GetSoundDevice().Play2DSoundEffect("Data/Sound/soundeffects/microbe-death.ogg");


    //TODO: Get this working
    //auto deathAnimationEntity = world.CreateEntity();
    //auto lifeTimeComponent = world.Create_TimedLifeComponent(deathAnimationEntity, 4000);
    //auto deathAnimSceneNode = world.Create_RenderNode(deathAnimationEntity);
    //auto deathAnimModel = world.Create_Model(deathAnimationEntity, deathAnimSceneNode.Node,
   //     "MicrobeDeath.mesh");

    LOG_WRITE("TODO: play animation deathAnimModel");
    // deathAnimModel.GraphicalObject.playAnimation("Death", false);

    //deathAnimSceneNode.Node.setPosition(position._Position);

    microbeComponent.dead = true;
    microbeComponent.deathTimer = 5000;
    microbeComponent.movementDirection = Float3(0,0,0);

    rigidBodyComponent.ClearVelocity();

    if(!microbeComponent.isPlayerMicrobe){
        // Destroy the physics state //
        rigidBodyComponent.Release();
    }

    if(microbeComponent.wasBeingEngulfed){
        removeEngulfedEffect(world, microbeEntity);
    }

    microbeSceneNode.Hidden = true;
    microbeSceneNode.Marked = true;
}

// TODO: Confirm this method works
void removeEngulfedEffect(CellStageWorld@ world, ObjectID microbeEntity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));


    // This kept getting doubled for some reason, so i just set it to default
    microbeComponent.movementFactor = 1.0f;


    microbeComponent.wasBeingEngulfed = false;
    microbeComponent.isBeingEngulfed = false;

    MicrobeComponent@ hostileMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(
            microbeComponent.hostileEngulfer));

    if(hostileMicrobeComponent !is null){
        hostileMicrobeComponent.isCurrentlyEngulfing = false;
    }

    microbeComponent.hostileEngulfer=NULL_OBJECT;

}

// Sets the colour of the microbe's membrane.
void setMembraneColour(CellStageWorld@ world, ObjectID microbeEntity, Float4 colour){
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    membraneComponent.setColour(colour);
}

// Sets the type of the microbe's membrane.
void setMembraneType(CellStageWorld@ world, ObjectID microbeEntity, MEMBRANE_TYPE type){
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    membraneComponent.setMembraneType(type);
}

}

