// Operations on microbe entities
#include "biome.as"
#include "organelle_placement.as"
#include "setup.as"
#include "species_system.as"

namespace MicrobeOperations{

// Queries the currently stored amount of an compound
//
// @param compoundId
// The id of the compound to query
//
// @returns amount
// The amount stored in the microbe's storage oraganelles
double getCompoundAmount(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId)
{
    return world.GetComponent_CompoundBagComponent(microbeEntity).
        getCompoundAmount(compoundId);
}

// Getter for generic microbe component
//
// Returns handle to the microbe component with a given ID
MicrobeComponent@ getMicrobeComponent(CellStageWorld@ world, ObjectID microbeEntity)
{
 return cast<MicrobeComponent>(world.GetScriptComponentHolder("MicrobeComponent")
    .Find(microbeEntity));
}

// Getter for microbe species
//
// returns the species component or null if it doesn't have a valid species
SpeciesComponent@ getSpeciesComponent(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);

    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, microbeComponent.speciesName);

    return world.GetComponent_SpeciesComponent(entity);
}

MicrobeComponent@ getPlayerMicrobe(CellStageWorld@ world)
{
    auto playerMicrobe = GetThriveGame().playerData().activeCreature();
    return getMicrobeComponent(world, playerMicrobe);
}

// Getter for microbe species
//
// returns the species component or null if species with that name doesn't exist
SpeciesComponent@ getSpeciesComponent(CellStageWorld@ world, const string &in speciesName)
{
    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, speciesName);

    return world.GetComponent_SpeciesComponent(entity);
}

// Getter for species processor component
//
// returns the processor component or null if such species doesn't have that component
// TODO: check what calls this and make it store the species entity id if it also calls
// getSpeciesComponent to save searching the whole species component index multiple times
ProcessorComponent@ getProcessorComponent(CellStageWorld@ world, const string &in speciesName)
{
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
PlacedOrganelle@ getOrganelleAt(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    return OrganellePlacement::getOrganelleAt(microbeComponent.organelles, hex);
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

    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);

    if(!OrganellePlacement::removeOrganelleAt(microbeComponent.organelles, hex)){
        LOG_ERROR("Organelle remove failed (OrganellePlacement::removeOrganelleAt)");
    }

    auto position = world.GetComponent_Position(microbeEntity);

    organelle.onRemovedFromMicrobe(microbeEntity, rigidBodyComponent.Body.Shape);

    // TODO: there seriously needs to be some caching here to make this less expensive
    rigidBodyComponent.ChangeShape(world.GetPhysicalWorld(), rigidBodyComponent.Body.Shape);

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

    // This refreshing these things could probably be somewhere else...
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth -
        BANDWIDTH_PER_ORGANELLE ; // Temporary solution for decreasing max bandwidth

    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;

    return true;
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
// @param editShape If the cell doesn't yet have a physics body the shape can be edited without
//     worry
//
// @return
//  returns whether the organelle was added
bool addOrganelle(CellStageWorld@ world, ObjectID microbeEntity, PlacedOrganelle@ organelle,
    PhysicsShape@ editShape = null)
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

    auto position = world.GetComponent_Position(microbeEntity);

    microbeComponent.organelles.insertLast(@organelle);

    // Update collision shape
    if(editShape !is null){
        // Initial adding
        organelle.onAddedToMicrobe(microbeEntity, world, editShape);

    } else {
        // Adding after cell creation
        auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
        organelle.onAddedToMicrobe(microbeEntity, world, rigidBodyComponent.Body.Shape);

        // TODO: there seriously needs to be some caching here to make this less expensive
        rigidBodyComponent.ChangeShape(world.GetPhysicalWorld(),
            rigidBodyComponent.Body.Shape);
    }

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
    }

    return true;
}

bool organelleDestroyedByDamage(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex)
{
    return removeOrganelle(world, microbeEntity, hex);
}

// ------------------------------------ //
void respawnPlayer(CellStageWorld@ world)
{
    auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
    auto playerEntity = GetThriveGame().playerData().activeCreature();

    if (playerSpecies.population > 20)
    {
        MicrobeComponent@ microbeComponent = getMicrobeComponent(world, playerEntity);
        auto rigidBodyComponent = world.GetComponent_Physics(playerEntity);
        auto sceneNodeComponent = world.GetComponent_RenderNode(playerEntity);

        microbeComponent.dead = false;
        microbeComponent.deathTimer = 0;

        // TODO: the cell template should be reapplied here

        // Reset the growth bins of the organelles to full health.
        for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
            microbeComponent.organelles[i].reset();
        }

        setupMicrobeHitpoints(microbeComponent, DEFAULT_HEALTH);
        // Setup compounds
        setupMicrobeCompounds(world,playerEntity);
        // Reset position //
        rigidBodyComponent.Body.SetPosition(Float3(GetEngine().GetRandom().GetNumber(MIN_SPAWN_DISTANCE, MAX_SPAWN_DISTANCE),
            0, GetEngine().GetRandom().GetNumber(MIN_SPAWN_DISTANCE, MAX_SPAWN_DISTANCE)),
            Float4::IdentityQuaternion);

        // The physics body will set the Position on next tick

        // TODO: reset velocity like in the old lua code?

        // This set position is actually useless, but it was in the old lua code
        // sceneNodeComponent.Node.setPosition(Float3(0, 0, 0));
        sceneNodeComponent.Hidden = false;
        sceneNodeComponent.Marked = true;

        setRandomBiome(world);

        cast<MicrobeStageHudSystem>(world.GetScriptSystem("MicrobeStageHudSystem")).
            suicideButtonreset();

        // Reset membrane color to fix the bug that made membranes sometimes red after you respawn.
        MicrobeOperations::applyMembraneColour(world, playerEntity);
        //If you died before entering the editor disable that
        microbeComponent.reproductionStage = 0;
        hideReproductionDialog(world);
        // Reset the player cell to be the same as the species template
        Species::restoreOrganelleLayout(world, playerEntity, microbeComponent, playerSpecies);
    }

    // Decrease the population by 20
    if (playerSpecies.population >= 20)
    {
        playerSpecies.population -= 20;
    }else
    {
        playerSpecies.population = 0;
    }

    // TODO: we already check if the player is extinct here. That logic shouldn't
    // be duplicated in the GUI
    // Creates an event that calls the function in javascript that checks extinction events
    GenericEvent@ checkExtinction = GenericEvent("CheckExtinction");
    NamedVars@ vars = checkExtinction.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("population", playerSpecies.population));
    GetEngine().GetEventHandler().CallEvent(checkExtinction);
}


void setupMicrobeHitpoints(CellStageWorld@ world, ObjectID microbeEntity, int health)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    setupMicrobeHitpoints(microbeComponent, health);
}

void setupMicrobeHitpoints(MicrobeComponent@ microbeComponent, int health)
{
    microbeComponent.maxHitpoints = health;
    microbeComponent.hitpoints = microbeComponent.maxHitpoints;
    microbeComponent.agentEmissionCooldown=uint(0);
}

//grabs compounds from template (starter_mcirobes) and stores them)
void setupMicrobeCompounds(CellStageWorld@ world, ObjectID microbeEntity)
{
    auto ids = getSpeciesComponent(world, microbeEntity).avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); ++i){
        CompoundId compoundId = parseUInt(ids[i]);
        InitialCompound amount = InitialCompound(getSpeciesComponent(world, microbeEntity).
            avgCompoundAmounts[ids[i]]);
        setCompound(world, microbeEntity, compoundId, amount.amount);
    }
}

// Default version of getBandwidth that takes an ObjectID paramater
float getBandwidth(CellStageWorld@ world, ObjectID microbeEntity, float maxAmount,
    CompoundId compoundId)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    return getBandwidth(microbeComponent, maxAmount, compoundId);
}

// Attempts to obtain an amount of bandwidth for immediate use.
// This should be in conjunction with most operations ejecting  or absorbing compounds
// and agents for microbe.
//
// @param microbeEntity
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
float getBandwidth(MicrobeComponent@ microbeComponent, float maxAmount,
    CompoundId compoundId)
{
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
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto storedAmount = amount;

    if(bandwidthLimited){
        storedAmount = getBandwidth(microbeComponent, amount, compoundId);
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

void setCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double amount)
    {
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto storedAmount = amount;
    storedAmount = min(storedAmount, microbeComponent.capacity);
    world.GetComponent_CompoundBagComponent(microbeEntity).setCompound(compoundId,storedAmount);
    }


// Default Version of takeCompound that takes ObjectID
double takeCompound(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double maxAmount)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto compoundBag = world.GetComponent_CompoundBagComponent(microbeEntity);
    return takeCompound(microbeComponent, compoundBag, compoundId, maxAmount);
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
// The amount that was actually taken, between 0.0 and maxAmount
double takeCompound(MicrobeComponent@ microbeComponent, CompoundBagComponent@ compoundBag,
    CompoundId compoundId, double maxAmount)
{
    auto takenAmount = compoundBag.takeCompound(compoundId, maxAmount);
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
    auto ejectionDistance = (maxR) * HEX_SIZE;

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

    auto amountToEject = amount*10000;
    createCompoundCloud(world, uint64(compoundId),
        position._Position.X + xnew * ejectionDistance,
        position._Position.Z + ynew * ejectionDistance,
       amountToEject);
}

// Default version of purgeCompounds that takes ObjectID
void purgeCompounds(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto compoundBag = world.GetComponent_CompoundBagComponent(microbeEntity);
    purgeCompounds(world, microbeEntity, microbeComponent, compoundBag);
}

// Since we have individual storage now we dont need this
// (its functionally useless from a gameplay perspective since
// you no longer need to dump things because thngs can no longer
// "take up each others space"  However, it would be weird to store
// up compounds you dont use, so lets purge those.
void purgeCompounds(CellStageWorld@ world, ObjectID microbeEntity,
    MicrobeComponent@ microbeComponent, CompoundBagComponent@ compoundBag)
{
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){

        // Price is 1 if used, 0 if not
        auto price = compoundBag.getUsedLastTime(compoundId);
        auto useful = SimulationParameters::compoundRegistry().getTypeData(compoundId).
            isUseful;

        if (price == 0 && !useful)
        {
            // Dont remove everything immedately, give it some time so
            // people can see it happening
            double amountToEject = 2.0f;
            // Since we have a handle to the compoundBag component, skip calling the getCompoundAmount
            // in the microbe operations namespace
            double availableCompound = compoundBag.getCompoundAmount(compoundId);

            // This was also 'amount' so maybe this didn't work either?
            if(amountToEject > 0.0f && availableCompound-amountToEject >= 0.0f){
                amountToEject = takeCompound(microbeComponent, compoundBag,
                    compoundId, amountToEject);
                ejectCompound(world, microbeEntity, compoundId, amountToEject-1.0f);
            }
            // If we flagged the second one but we still have some left just get rid of it all
            else if (availableCompound > 0.0f)
            {
                amountToEject = takeCompound(microbeComponent, compoundBag,
                    compoundId, availableCompound);
                ejectCompound(world, microbeEntity, compoundId, amountToEject-1.0f);
            }
        }
    }
}

void flashMembraneColour(CellStageWorld@ world, ObjectID microbeEntity, uint duration,
    Float4 colour)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);

    if(microbeComponent.flashDuration <= 0){
        microbeComponent.flashColour = colour;
        microbeComponent.flashDuration = duration;
    }
}

// Applies the default membrane colour
void applyMembraneColour(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto speciesColour = microbeComponent.speciesColour;
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
void emitAgent(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId,
    double maxAmount, float lifeTime)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);

    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    auto cellPosition = world.GetComponent_Position(microbeEntity);

    // Cooldown code
    if(microbeComponent.agentEmissionCooldown > 0)
        return;

    auto numberOfAgentVacuoles = int (microbeComponent.specialStorageOrganelles[
            formatUInt(compoundId)]);

    // Only shoot if you have an agent vacuole.
    if(numberOfAgentVacuoles == 0){
        // LOG_WARNING("Cell tries to shoot without agent vacuole");
        return;
    }

    auto compoundBag = world.GetComponent_CompoundBagComponent(microbeEntity);
    if(compoundBag.getCompoundAmount(compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT)
        {
        // The front of the microbe
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
        auto ejectionDistance = (maxR) * HEX_SIZE/2;
        auto angle = 180;
        // Find the direction the microbe is facing
        auto yAxis = Ogre::Quaternion(cellPosition._Orientation).zAxis();
        auto microbeAngle = atan2(yAxis.x, yAxis.z);
        if(microbeAngle < 0){
            microbeAngle = microbeAngle + 2 * PI;
        }
        microbeAngle = microbeAngle * 180 / PI;
        // Take the microbe angle into account so we get world relative degrees
        auto finalAngle = (angle + microbeAngle) % 360;
        auto s = sin(finalAngle/180*PI);
        auto c = cos(finalAngle/180*PI);
        // Membrane coords to world coords
        auto xnew = -membraneCoords.x * c + membraneCoords.z * s;
        auto ynew = membraneCoords.x * s + membraneCoords.z * c;
        // Find the direction the microbe is facing
        auto vec = ( microbeComponent.facingTargetPoint - cellPosition._Position);
        auto direction = vec.Normalize();

        auto amountToEject = takeCompound(microbeComponent, compoundBag, compoundId, maxAmount/10.0f);

        if (amountToEject >= MINIMUM_AGENT_EMISSION_AMOUNT)
        {
            playSoundWithDistance(world, "Data/Sound/soundeffects/microbe-release-toxin.ogg",microbeEntity);
            createAgentCloud(world, compoundId, cellPosition._Position+Float3(xnew*ejectionDistance,0,ynew*ejectionDistance),
                    direction, amountToEject, lifeTime, microbeComponent.speciesName);


            // The cooldown time is inversely proportional to the amount of agent vacuoles.
            microbeComponent.agentEmissionCooldown = uint(AGENT_EMISSION_COOLDOWN /
                numberOfAgentVacuoles);
        }
    }
}

void playSoundWithDistance(CellStageWorld@ world, const string &in soundPath, ObjectID microbeEntity)
    {
    auto location = world.GetComponent_Position(microbeEntity)._Position;
    auto playerEntity = GetThriveGame().playerData().activeCreature();
    Position@ thisPosition = world.GetComponent_Position(playerEntity);
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    // Length is squared so also square the variable we are dividing
    float thisVolume = 1000.0f/(sqrt(((thisPosition._Position-location).LengthSquared()))+1);
    float soundVolume = thisVolume;
    // Play sound
    if (@microbeComponent.otherAudio is null ||
                !microbeComponent.otherAudio.Get().isPlaying())
        {
         @microbeComponent.otherAudio = GetEngine().GetSoundDevice().Play2DSound(
            soundPath, false, true);
        if(microbeComponent.otherAudio !is null){
            if(microbeComponent.otherAudio.HasInternalSource()){
                    microbeComponent.otherAudio.Get().setVolume(soundVolume);
                    microbeComponent.otherAudio.Get().play();
                    } else {
                        LOG_ERROR("Created sound player doesn't have internal "
                            "sound source");
                    }
            } else {
                LOG_ERROR("Failed to create sound player");
            }
        }
    }

// Default version of toggleEngulfMode that takes ObjectID
void toggleEngulfMode(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    toggleEngulfMode(microbeComponent);
}

// Disables or enables engulfmode for a microbe, allowing or
// disallowing it to absorb other microbes
void toggleEngulfMode(MicrobeComponent@ microbeComponent)
{
    // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
    if(microbeComponent.engulfMode && !microbeComponent.isBeingEngulfed){
        microbeComponent.movementFactor = 1.0f;
        // soundSourceComponent.stopSound("microbe-engulfment"); // Possibly comment out.
    }
    microbeComponent.movementFactor = microbeComponent.movementFactor /
        ENGULFING_MOVEMENT_DIVISION;

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

    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
    if (microbeComponent !is null)
        {
        if(damageType == "toxin"){
            // Play the toxin sound
            playSoundWithDistance(world, "Data/Sound/soundeffects/microbe-toxin-damage.ogg", microbeEntity);
        }

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
}


// TODO: we have a similar method in procedural_microbes.lua and another one
// in microbe_editor.lua.
// They probably should all use the same one.
// We'll probably need a rotation for this, although maybe it should be done in c++ where
// sets are a thing?
bool validPlacement(CellStageWorld@ world, ObjectID microbeEntity, const Organelle@ organelle,
    Int2 posToCheck)
{
    auto touching = false;
    //TODO: should this hex list here be rotated, this doesn't seem to
    //take a rotation parameter in
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

// speciesName decides the template to use, while individualName is
// used for referencing the instance
ObjectID spawnMicrobe(CellStageWorld@ world, Float3 pos, const string &in speciesName,
    bool aiControlled)
{
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

    auto microbeEntity = _createMicrobeEntity(world, aiControlled, speciesName,
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

    if(IsInGraphicalMode())
        node.Node.setPosition(pos);

    auto speciesEntity = findSpeciesEntityByName(world, speciesName);
    auto species = world.GetComponent_SpeciesComponent(speciesEntity);

    // TODO: Why is this here with the separate spawnBacteria function existing?
    // Bacteria get scaled to half size
    if(species.isBacteria){
        // TODO: wow, this is a big hack and no way guarantees that
        // the physics size matches the rendered size
        node.Scale = Float3(0.5, 0.5, 0.5);
        node.Marked = true;
        // This call is also not the cheapest. So would be much better
        // if the physics generation actually did the right then when
        // species.isBacteria is true
        physics.ChangeShape(world.GetPhysicalWorld(),
            world.GetPhysicalWorld().CreateSphere(HEX_SIZE/2.0f));
    }

    return microbeEntity;
}

// TODO: merge common parts with spawnMicrobe
ObjectID spawnBacteria(CellStageWorld@ world, Float3 pos, const string &in speciesName,
    bool aiControlled, bool partOfColony)
{
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

    auto microbeEntity = _createMicrobeEntity(world, aiControlled, speciesName,
        // in_editor
        false);

    // Teleport the cell to the right position
    auto microbePos = world.GetComponent_Position(microbeEntity);
    microbePos._Position = pos;
    microbePos.Marked = true;

    auto physics = world.GetComponent_Physics(microbeEntity);
    physics.Body.SetMass(physics.Body.Mass * 10);
    physics.JumpTo(microbePos);

    // Try setting the position immediately as well (as otherwise it
    // takes until the next tick for this to take effect)
    auto node = world.GetComponent_RenderNode(microbeEntity);
    node.Node.setPosition(pos);

    // Bacteria get scaled to half size
    // TODO: wow, this is a big hack and no way guarantees that
    // the physics size matches the rendered size
    node.Scale = Float3(0.5, 0.5, 0.5);
    node.Marked = true;
    // This call is also not the cheapest. So would be much better
    // if the physics generation actually did the right then when
    // species.isBacteria is true
    physics.ChangeShape(world.GetPhysicalWorld(),
        world.GetPhysicalWorld().CreateSphere(HEX_SIZE/2.0f));

    // Need to set bacteria spawn and it needs to be squared like it
    // is in the spawn system. code, if part of colony but not
    // directly spawned give a spawned component
    if (partOfColony){
        world.Create_SpawnedComponent(microbeEntity, BACTERIA_SPAWN_RADIUS *
            BACTERIA_SPAWN_RADIUS);
    }

    return microbeEntity;
}

// Creates and applies microbe collision shape
void _applyMicrobeCollisionShape(CellStageWorld@ world, Physics@ rigidBody,
    MicrobeComponent@ microbeComponent, PhysicsShape@ shape)
{
    // This compensates for the lack of a nucleus for the player cell at the beginning and makes eukaryotes alot heavier.
    float mass = 0.7f;

    // Organelles
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        auto organelle = microbeComponent.organelles[i];

        // organelles are already initialized when they are added
        // Not sure if this reset is needed here
        organelle.reset();

        mass += organelle.organelle.mass;
    }

    assert(mass != 0, "creating cell with zero mass");

    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(), shape, mass,
        world.GetPhysicalMaterial("cell"));

    assert(rigidBody.Body !is null);

    _applyMicrobePhysicsBodySettings(world, rigidBody);
}

// Creates a new microbe with all required components. Use spawnMicrobe from other
// code instead of this function
//
// @returns microbe
// An object of type Microbe
// TODO: this should take in the initial position
ObjectID _createMicrobeEntity(CellStageWorld@ world, bool aiControlled,
    const string &in speciesName, bool in_editor)
{
    assert(speciesName != "", "Empty species name for create microbe");

    auto speciesEntity = findSpeciesEntityByName(world, speciesName);
    auto species = world.GetComponent_SpeciesComponent(speciesEntity);

    if(speciesEntity == NULL_OBJECT)
        assert(false, "Trying to create a microbe with invalid species");

    ObjectID entity = world.CreateEntity();

    // TODO: movement sound for microbes
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

    auto position = world.Create_Position(entity, Float3(0, 0, 0), Float4::IdentityQuaternion);

    auto membraneComponent = world.Create_MembraneComponent(entity,
        species.speciesMembraneType);

    auto compoundAbsorberComponent = world.Create_CompoundAbsorberComponent(entity);

    world.Create_RenderNode(entity);
    auto compoundBag = world.Create_CompoundBagComponent(entity);

    auto processorComponent = world.Create_ProcessorComponent(entity);

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Create(entity));

    microbeComponent.init(entity, not aiControlled, species);

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
    } else {
        // Each microbe now has their own processor component to allow
        // the process system to run safely while species are deleted
        Species::copyProcessesFromSpecies(world, species, entity);
    }

    if(microbeComponent.organelles.length() > 0)
        assert(false, "Freshly created microbe has organelles in it");

    // Apply the template //
    auto shape = world.GetPhysicalWorld().CreateCompound();

    // TODO: as now each microbe has a separate processor component they no longer stay
    // up to date with the species so either this should apply the species processes OR
    // there should be a ProcessConfiguration object that would be shared between the
    // ProcessorComponent both in the species and individual cells
    Species::applyTemplate(world, entity, species, shape);

    // ------------------------------------ //
    // Initialization logic taken from MicrobeSystem and put here now
    assert(microbeComponent.organelles.length() > 0, "Microbe has no "
        "organelles in initializeMicrobe");

    auto rigidBody = world.Create_Physics(entity, position);

    _applyMicrobeCollisionShape(world, rigidBody, microbeComponent, shape);

    // Allowing the microbe to absorb all the compounds.
    setupAbsorberForAllCompounds(compoundAbsorberComponent);

    microbeComponent.initialized = true;
    return entity;
}

void _applyMicrobePhysicsBodySettings(CellStageWorld@ world, Physics@ rigidBody)
{
    // Constraint to 2d movement
    rigidBody.Body.ConstraintMovementAxises();

    rigidBody.Body.SetDamping(0.2, 0.2);

    rigidBody.Body.SetFriction(0.2);
}

//! Helper for Invoking an operation for destroying the physics body of a cell
class DestroyPhysicsBodyHelper{

    DestroyPhysicsBodyHelper(ObjectID id, CellStageWorld@ world){
        m_id = id;
        @m_world = world;
    }

    void execute(){

        auto rigidBodyComponent = m_world.GetComponent_Physics(m_id);

        if(rigidBodyComponent !is null){
            rigidBodyComponent.Release(m_world.GetPhysicalWorld());
        } else {
            LOG_ERROR("No Physics for DestroyPhysicsBodyHelper");
        }
    }

    ObjectID m_id;
    CellStageWorld@ m_world;
}

// Kills the microbe, releasing stored compounds into the enviroment
void kill(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);
    auto microbeSceneNode = world.GetComponent_RenderNode(microbeEntity);
    auto position = world.GetComponent_Position(microbeEntity);

    if(microbeComponent.dead){
        LOG_ERROR("Trying to kill a dead microbe");
        return;
    }

    // Releasing all the agents.
    // To not completely deadlock in this there is a maximum of 5 of these created
    const int maxAgentsToShoot = 5;
    int createdAgents = 0;

    auto storageTypes = microbeComponent.specialStorageOrganelles.getKeys();
    for(uint i = 0; i < storageTypes.length(); ++i){
        CompoundId compoundId = parseInt(storageTypes[i]);
        auto _amount = getCompoundAmount(world, microbeEntity, compoundId);
        while(_amount > 0){
            // Eject up to 5 units per particle
            auto ejectedAmount = 5.0f;
            auto direction = Float3(GetEngine().GetRandom().GetNumber(0.0f, 1.0f) * 2 - 1,
                0, GetEngine().GetRandom().GetNumber(0.0f, 1.0f) * 2 - 1);

            createAgentCloud(world, compoundId, position._Position, direction, ejectedAmount,
                2000, microbeComponent.speciesName);
            ++createdAgents;

            if(createdAgents >= maxAgentsToShoot)
                break;

            _amount -= ejectedAmount;
        }
    }

    dictionary compoundsToRelease;

    // Eject the compounds that was in the microbe
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();

    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
        auto total = getCompoundAmount(world, microbeEntity, compoundId)*COMPOUND_RELEASE_PERCENTAGE;
        compoundsToRelease[formatInt(compoundId)] = float(total);
        //LOG_INFO(""+float(compoundsToRelease[formatInt(compoundId)]));
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
                compoundsToRelease[key] = amount * COMPOUND_MAKEUP_RELEASE_PERCENTAGE;
            } else {
                compoundsToRelease[key] = float(compoundsToRelease[key]) +
                    (amount * COMPOUND_MAKEUP_RELEASE_PERCENTAGE);
            }
        }
    }

    // They were added in order already so looping through this other thing is fine
    for(uint64 compoundID = 0; compoundID <
                SimulationParameters::compoundRegistry().getSize(); ++compoundID)
        {
            //LOG_INFO(""+float(compoundsToRelease[formatInt(compoundID)]));
            //LOG_INFO(""+float(compoundsToRelease[formatUInt(compoundID)]));
           if (SimulationParameters::compoundRegistry().getTypeData(compoundID).isCloud &&
                float(compoundsToRelease[formatUInt(compoundID)]) > 0.0f)
            {
            //Earlier we added all of the keys to the list by ID,in order,  so this is fine
            //LOG_INFO("Releasing "+float(compoundsToRelease[formatUInt(compoundID)]));
            if (SimulationParameters::compoundRegistry().getTypeData(compoundID).isCloud)
                {
                ejectCompound(world, microbeEntity, uint64(compoundID),float(compoundsToRelease[formatUInt(compoundID)]));
                }
            }
        }

    // Play the death sound
    playSoundWithDistance(world, "Data/Sound/soundeffects/microbe-death.ogg", microbeEntity);

    //TODO: Get this working
    //auto deathAnimationEntity = world.CreateEntity();
    //auto lifeTimeComponent = world.Create_TimedLifeComponent(deathAnimationEntity, 4000);
    //auto deathAnimSceneNode = world.Create_RenderNode(deathAnimationEntity);
    //auto deathAnimModel = world.Create_Model(deathAnimationEntity, deathAnimSceneNode.Node,
    //     "MicrobeDeath.mesh");
    //deathAnimSceneNode.Node.setPosition(position._Position);

    //LOG_WRITE("TODO: play animation deathAnimModel");
    // deathAnimModel.GraphicalObject.playAnimation("Death", false);
    //subtract population
    auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
    if (!microbeComponent.isPlayerMicrobe &&
        microbeComponent.speciesName != playerSpecies.name)
    {
        alterSpeciesPopulation(world, microbeEntity, CREATURE_DEATH_POPULATION_LOSS);
    }


    microbeComponent.dead = true;
    microbeComponent.deathTimer = 5000;
    microbeComponent.movementDirection = Float3(0,0,0);

    if(rigidBodyComponent.Body !is null)
        rigidBodyComponent.Body.ClearVelocity();

    if(!microbeComponent.isPlayerMicrobe){
        // We can't destroy the body while in a physics callback
        // So we queue it to happen before the next tick
        DestroyPhysicsBodyHelper obj(microbeEntity, world);

        GetEngine().Invoke(InvokeCallbackFunc(obj.execute));

        // Hide organelles
        for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
            // The organelles are hidden here as otherwise the extra
            // entities like the ER stay visible for a while until the
            // cell entity is destroyed
            microbeComponent.organelles[i].hideEntity();
        }
    }

    if(microbeComponent.wasBeingEngulfed){
        removeEngulfedEffect(world, microbeEntity);
    }

    microbeSceneNode.Hidden = true;
    microbeSceneNode.Marked = true;
}

// Default version of alterSpeciesPopulation that takes an ObjectID
void alterSpeciesPopulation(CellStageWorld@ world, ObjectID microbeEntity, int popChange)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    SpeciesComponent@ ourSpecies = getSpeciesComponent(world, microbeEntity);
    alterSpeciesPopulation(world, ourSpecies, microbeComponent, popChange);
}

void alterSpeciesPopulation(CellStageWorld@ world,
                            SpeciesComponent@ ourSpecies,
                            MicrobeComponent@ microbeComponent,
                            int popChange)
{
    if (ourSpecies !is null)
    {
        cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).
            updatePopulationForSpecies(microbeComponent.speciesName,popChange);
    }
}

// Default version of removeEngulfedEffect
void removeEngulfedEffect(CellStageWorld@ world, ObjectID microbeEntity)
{
    MicrobeComponent@ microbeComponent = getMicrobeComponent(world, microbeEntity);
    MicrobeComponent@ hostileMicrobeComponent = getMicrobeComponent(world, microbeComponent.hostileEngulfer);
    removeEngulfedEffect(microbeComponent, hostileMicrobeComponent);
}

void removeEngulfedEffect(MicrobeComponent@ microbeComponent, MicrobeComponent@ hostileMicrobeComponent)
{
    // This kept getting doubled for some reason, so i just set it to default
    microbeComponent.movementFactor = 1.0f;
    microbeComponent.wasBeingEngulfed = false;
    microbeComponent.isBeingEngulfed = false;

    if(hostileMicrobeComponent !is null){
        hostileMicrobeComponent.isCurrentlyEngulfing = false;
    }

    microbeComponent.hostileEngulfer=NULL_OBJECT;
}

// Sets the colour of the microbe's membrane.
void setMembraneColour(CellStageWorld@ world, ObjectID microbeEntity, Float4 colour)
{
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    membraneComponent.setColour(colour);
}

// Sets the type of the microbe's membrane.
void setMembraneType(CellStageWorld@ world, ObjectID microbeEntity, MEMBRANE_TYPE type)
{
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
    membraneComponent.setMembraneType(type);
}

}//Namespace MicrobeOperations

