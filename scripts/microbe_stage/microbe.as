// Was called everytime this object was created
// setupAbsorberForAllCompounds
#include "microbe_operations.as"
#include "hex.as"
#include "microbe_stage_hud.as"
#include "species_system.as"


//! Why is this needed? Is it for(the future when we don't want to
//! absorb everything (or does this skip toxins, which aren't in compound registry)
void setupAbsorberForAllCompounds(CompoundAbsorberComponent@ absorber){

    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint a = 0; a < compoundCount; ++a){

        auto compound = SimulationParameters::compoundRegistry().getTypeData(a);
    
        absorber.setCanAbsorbCompound(a, true);
    }
}

// Quantity of physics time between each loop distributing compounds
// to organelles. TODO: Modify to reflect microbe size.
const uint COMPOUND_PROCESS_DISTRIBUTION_INTERVAL = 100;

// Amount the microbes maxmimum bandwidth increases with per organelle
// added. This is a temporary replacement for microbe surface area
const float BANDWIDTH_PER_ORGANELLE = 1.0;

// The of time it takes for the microbe to regenerate an amount of
// bandwidth equal to maxBandwidth
const uint BANDWIDTH_REFILL_DURATION = 800;

// No idea what this does (if anything), but it isn't used in the
// process system, or when ejecting compounds.
const float STORAGE_EJECTION_THRESHHOLD = 0.8;

// The amount of time between each loop to maintaining a fill level
// below STORAGE_EJECTION_THRESHHOLD and eject useless compounds
const uint EXCESS_COMPOUND_COLLECTION_INTERVAL = 1000;

// The amount of hitpoints each organelle provides to a microbe.
const uint MICROBE_HITPOINTS_PER_ORGANELLE = 10;

// The minimum amount of oxytoxy (or any agent) needed to be able to shoot.
const float MINIMUM_AGENT_EMISSION_AMOUNT = 0.1;

// A sound effect thing for bumping with other cell i assume? Probably unused.
const float RELATIVE_VELOCITY_TO_BUMP_SOUND = 6.0;

// I think (emphasis on think) this is unused.
const float INITIAL_EMISSION_RADIUS = 0.5;

// The speed reduction when a cell is in rngulfing mode.
const uint ENGULFING_MOVEMENT_DIVISION = 3;

// The speed reduction when a cell is being engulfed.
const uint ENGULFED_MOVEMENT_DIVISION = 4;

// The amount of ATP per second spent on being on engulfing mode.
const float ENGULFING_ATP_COST_SECOND = 1.5;

// The minimum HP ratio between a cell and a possible engulfing victim.
const float ENGULF_HP_RATIO_REQ = 1.5 ;

// Cooldown between agent emissions, in milliseconds.
const uint AGENT_EMISSION_COOLDOWN = 1000;


// class Microbe{
    
    
// }
// Use "ObjectID microbeEntity" instead


////////////////////////////////////////////////////////////////////////////////
// MicrobeComponent
//
// Holds data common to all microbes. You probably shouldn't use this directly,
// use MicrobeOperations instead.
////////////////////////////////////////////////////////////////////////////////
class MicrobeComponent : ScriptComponent{

    //! This detaches all still attached organelles
    //! \todo There might be a more graceful way to do this
    ~MicrobeComponent(){

        LOG_INFO("MicrobeComponent destroyed: " + microbeEntity);

        for(uint i = 0; i < organelles.length(); ++i){

            organelles[i].onDestroyedWithMicrobe(microbeEntity);
        }

        organelles.resize(0);
    }

    //! This has to be called after creating this
    void init(ObjectID forEntity, bool isPlayerMicrobe, const string &in speciesName){
        
        this.speciesName = speciesName;
        this.isPlayerMicrobe = isPlayerMicrobe;
        this.microbeEntity = forEntity;

        // Microbe system update should initialize this component on next tick
    }

    // void load(storage){
    
    //     auto organelles = storage.get("organelles", {});
    //     for(i = 1,organelles.size()){
    //         auto organelleStorage = organelles.get(i);
    //         auto organelle = Organelle.loadOrganelle(organelleStorage);
    //         auto q = organelle.position.q;
    //         auto r = organelle.position.r;
    //         auto s = encodeAxial(q, r);
    //         this.organelles[s] = organelle;
    //     }
    //     this.hitpoints = storage.get("hitpoints", 0);
    //     this.speciesName = storage.get("speciesName", "Default");
    //     this.maxHitpoints = storage.get("maxHitpoints", 0);
    //     this.maxBandwidth = storage.get("maxBandwidth", 0);
    //     this.remainingBandwidth = storage.get("remainingBandwidth", 0);
    //     this.isPlayerMicrobe = storage.get("isPlayerMicrobe", false);
    //     this.speciesName = storage.get("speciesName", "");
        
    //     // auto compoundPriorities = storage.get("compoundPriorities", {})
    //     // for(i = 1,compoundPriorities.size()){
    //     //     auto compound = compoundPriorities.get(i)
    //     //     this.compoundPriorities[compound.get("compoundId", 0)] = compound.get("priority", 0)
    //     // }
    // }
    
    
    // void storage(storage){
    //     // Organelles
    //     auto organelles = StorageList()
    //         for(_, organelle in pairs(this.organelles)){
    //             auto organelleStorage = organelle.storage();
    //             organelles.append(organelleStorage);
    //         }
    //     storage.set("organelles", organelles);
    //     storage.set("hitpoints", this.hitpoints);
    //     storage.set("speciesName", this.speciesName);
    //     storage.set("maxHitpoints", this.maxHitpoints);
    //     storage.set("remainingBandwidth", this.remainingBandwidth);
    //     storage.set("maxBandwidth", this.maxBandwidth);
    //     storage.set("isPlayerMicrobe", this.isPlayerMicrobe);
    //     storage.set("speciesName", this.speciesName);

    //     // auto compoundPriorities = StorageList()
    //     // for(compoundId, priority in pairs(this.compoundPriorities)){
    //     //     compound = StorageContainer()
    //     //     compound.set("compoundId", compoundId)
    //     //     compound.set("priority", priority)
    //     //     compoundPriorities.append(compound)
    //     // }
    //     // storage.set("compoundPriorities", compoundPriorities)
    // }
    

    string speciesName;
    // TODO: initialize
    Float4 speciesColour;
    
    uint hitpoints;
    uint maxHitpoints = 0;
    bool dead = false;
    uint deathTimer = 0;
    array<PlacedOrganelle@> organelles;
    // Organelles with complete resonsiblity for a specific compound
    // (such as agentvacuoles)
    // Keys are the CompoundId of the agent and the value is int
    // specifying how many there are
    dictionary specialStorageOrganelles;  
    
    Float3 movementDirection = Float3(0, 0, 0);
    Float3 facingTargetPoint = Float3(0, 0, 0);
    float microbetargetdirection = 0;
    float movementFactor = 1.0; // Multiplied on the movement speed of the microbe.
    double capacity = 0;    // The amount that can be stored in the
                            // microbe. NOTE: This does not include
                            // special storage organelles.
    double stored = 0;  // The amount stored in the microbe. NOTE:
                        // This does not include special storage
                        // organelles.
    bool initialized = false;
    bool isPlayerMicrobe = false;
    float maxBandwidth = 10.0 * BANDWIDTH_PER_ORGANELLE; // wtf is a bandwidth anyway?
    float remainingBandwidth = 0.0;
    uint compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL;
    bool isCurrentlyEngulfing = false;
    bool isBeingEngulfed = false;
    bool wasBeingEngulfed = false;
    ObjectID hostileEngulfer = NULL_OBJECT;
    uint agentEmissionCooldown = 0;
    // Is this the place where the actual flash duration works?
    // The one in the organelle class doesn't work
    uint flashDuration = 0;
    Float4 flashColour = Float4(0, 0, 0, 0);
    uint reproductionStage = 0;


    // New state variables that MicrobeSystem also uses
    bool engulfMode = false;
    bool in_editor = false;

    // ObjectID microbe;
    ObjectID microbeEntity = NULL_OBJECT;
}

//! Helper for MicrobeSystem
class MicrobeSystemCached{

    MicrobeSystemCached(ObjectID entity, CompoundAbsorberComponent@ first,
        MicrobeComponent@ second, RenderNode@ third, Physics@ fourth,
        MembraneComponent@ fifth, CompoundBagComponent@ sixth
    ) {
        this.entity = entity;
        @this.first = first;
        @this.second = second;
        @this.third = third;
        @this.fourth = fourth;
        @this.fifth = fifth;
        @this.sixth = sixth;
    }

    ObjectID entity = -1;

    CompoundAbsorberComponent@ first;
    MicrobeComponent@ second;
    RenderNode@ third;
    Physics@ fourth;
    MembraneComponent@ fifth;
    CompoundBagComponent@ sixth;
    // TODO: determine if this is accessed frequently enough that it should be here
    // Physics ;
}



////////////////////////////////////////////////////////////////////////////////
// MicrobeSystem
//
// Updates microbes
////////////////////////////////////////////////////////////////////////////////
// TODO: This system is HUUUUUUGE! D:
// We should try to separate it into smaller systems.
// For example, the agents should be handled in another system
// (however we're going to redo agents so we should wait until then for that one)
// This is now also split into MicrobeOperations which implements all the methods that don't
// necessarily need instance data in this class (this is most things) so that they can be
// called from different places. Functions that shouldn't be called from any other place are
// kept here
class MicrobeSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "MicrobeSystem expected CellStageWorld");
    }

    void Release(){}

    void Run(){
        // // Note that this triggers every frame there is a collision
        // for(_, collision in pairs(this.microbeCollisions.collisions())){
        //     auto entity1 = Entity(collision.entityId1, this.gameState.wrapper);
        //     auto entity2 = Entity(collision.entityId2, this.gameState.wrapper);
        //     if(entity1.exists() and entity2.exists()){
        //         // Engulf initiation
        //         MicrobeSystem.checkEngulfment(entity1, entity2);
        //         MicrobeSystem.checkEngulfment(entity2, entity1);
        //     }
        // }
        
        // this.microbeCollisions.clearCollisions()
        
        // // TEMP, DELETE FOR 0.3.3!!!!!!!!
        // for(_, collision in pairs(this.agentCollisions.collisions())){
        //     auto entity = Entity(collision.entityId1, this.gameState.wrapper);
        //     auto agent = Entity(collision.entityId2, this.gameState.wrapper);
            
        //     if(entity.exists() and agent.exists()){
        //         MicrobeSystem.damage(entity, .5, "toxin");
        //         agent.destroy();
        //     }
        // }
        
        // this.agentCollisions.clearCollisions()
        
        // for(_, collision in pairs(this.bacteriaCollisions.collisions())){
        //     local microbe_entity = Entity(collision.entityId1, this.gameState.wrapper);
        //     local bacterium_entity = Entity(collision.entityId2, this.gameState.wrapper);

        //     if(microbe_entity.exists() and bacterium_entity.exists()){
        //         if(world.GetComponent_Bacterium(bacterium_entity) !is null){
        //             auto bacterium = Bacterium(bacterium_entity);
        //             bacterium.damage(4);
        //         }
        //     }
        // }
        // this.bacteriaCollisions.clearCollisions();

        for(uint i = 0; i < CachedComponents.length(); ++i){
            updateMicrobe(CachedComponents[i], TICKSPEED);
        }
    }

    void Clear(){

        CachedComponents.resize(0);
    }

    void CreateAndDestroyNodes(){

        
        // Delegate to helper //
        ScriptSystemNodeHelper(world, @CachedComponents, SystemComponents);
    }

    // TODO: make sure these work fine after converting
        // this.microbeCollisions = CollisionFilter(
        //     "microbe",
        //     "microbe"
        // )
        // // Temporary for 0.3.2, should be moved to separate system.
        // this.agentCollisions = CollisionFilter(
        //     "microbe",
        //     "agent"
        // )

        // this.bacteriaCollisions = CollisionFilter(
        //     "microbe",
        //     "bacteria"
        // )

        // this.microbes = {}
        // }

    // Updates the microbe's state
    void updateMicrobe(MicrobeSystemCached@ components, uint logicTime){
        auto microbeEntity = components.entity;

        if(microbeEntity == -1){

            LOG_ERROR("MicrobeSystem: updateMicrobe: invalid microbe entity hasn't "
                "set a ObjectID, did someone forget to call 'init'?");
            return;
        }
        
        MicrobeComponent@ microbeComponent = components.second;
        MembraneComponent@ membraneComponent = components.fifth;
        RenderNode@ sceneNodeComponent = components.third;
        CompoundAbsorberComponent@ compoundAbsorberComponent = components.first;
        CompoundBagComponent@ compoundBag = components.sixth;

        if(!microbeComponent.initialized){

            LOG_ERROR("Microbe is not initialized: " + microbeEntity);
            return;
        }

        if(microbeComponent.dead){
            microbeComponent.deathTimer = microbeComponent.deathTimer - logicTime;
            microbeComponent.flashDuration = 0;
            if(microbeComponent.deathTimer <= 0){
                if(microbeComponent.isPlayerMicrobe == true){
                    MicrobeOperations::respawnPlayer(world);
                } else {

                    auto physics = world.GetComponent_Physics(microbeEntity);

                    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

                        // This Collision doesn't really need to be
                        // updated here, but this keeps us from
                        // changing onRemovedFromMicrobe to allow
                        // skipping it
                        microbeComponent.organelles[i].onRemovedFromMicrobe(microbeEntity,
                            physics.Collision);
                    }
                    
                    // Safe destroy before next tick
                    world.QueueDestroyEntity(microbeEntity);
                }
            }
        } else {
            // Recalculating agent cooldown time.
            microbeComponent.agentEmissionCooldown = max(
                microbeComponent.agentEmissionCooldown - logicTime, 0);

            //calculate storage.
            calculateStorageSpace(microbeEntity);

            compoundBag.storageSpace = microbeComponent.capacity;

            // StorageOrganelles
            updateCompoundAbsorber(microbeEntity);
            
            // Regenerate bandwidth
            regenerateBandwidth(microbeEntity, logicTime);
            
            // Attempt to absorb queued compounds
            auto absorbed = compoundAbsorberComponent.getAbsorbedCompounds();
            for(uint i = 0; i < absorbed.length(); ++i){
                CompoundId compound = absorbed[i];
                auto amount = compoundAbsorberComponent.absorbedCompoundAmount(compound);
                if(amount > 0.0){
                    MicrobeOperations::storeCompound(world, microbeEntity, compound,
                        amount, true);
                }
            }
            // Flash membrane if something happens.
            if(microbeComponent.flashDuration != 0 &&
                microbeComponent.flashColour != Float4(0, 0, 0, 0)
            ){
                if(microbeComponent.flashDuration >= logicTime){
                    
                    microbeComponent.flashDuration = microbeComponent.flashDuration -
                        logicTime;
                    
                } else {
                    // Would wrap over to very large number
                    microbeComponent.flashDuration = 0;
                }
            
                // How frequent it flashes, would be nice to update
                // the flash void to have this variable{
                
                if((microbeComponent.flashDuration % 600.0f) < 300){
                    membraneComponent.setColour(microbeComponent.flashColour);
                } else {
                    // Restore colour
                    MicrobeOperations::applyMembraneColour(world, microbeEntity);
                }

                if(microbeComponent.flashDuration <= 0){
                    microbeComponent.flashDuration = 0;
                    // Restore colour
                    MicrobeOperations::applyMembraneColour(world, microbeEntity);
                }
            }
        
            microbeComponent.compoundCollectionTimer =
                microbeComponent.compoundCollectionTimer + logicTime;
            
            while(microbeComponent.compoundCollectionTimer >
                EXCESS_COMPOUND_COLLECTION_INTERVAL)
            {
                // For every COMPOUND_DISTRIBUTION_INTERVAL passed

                microbeComponent.compoundCollectionTimer =
                    microbeComponent.compoundCollectionTimer -
                    EXCESS_COMPOUND_COLLECTION_INTERVAL;

                MicrobeOperations::purgeCompounds(world, microbeEntity);

                atpDamage(microbeEntity);
            }
        
            // First organelle run: updates all the organelles and heals the broken ones.
            if(microbeComponent.hitpoints < microbeComponent.maxHitpoints){
                for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
                    
                    auto organelle = microbeComponent.organelles[i];
                    // Update the organelle.
                    organelle.update(logicTime);
                
                    // If the organelle is hurt.
                    if(organelle.getCompoundBin() < 1.0){
                        // Give the organelle access to the compound bag to take some compound.
                        organelle.growOrganelle(
                            world.GetComponent_CompoundBagComponent(microbeEntity), logicTime);
                        
                        // An organelle was damaged and we tried to
                        // heal it, so our health might be different.
                        MicrobeOperations::calculateHealthFromOrganelles(world, microbeEntity);
                    }
                }
            } else {
                auto reproductionStageComplete = true;
                array<PlacedOrganelle@> organellesToAdd;

                // Grow all the large organelles.
                for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
                    
                    auto organelle = microbeComponent.organelles[i];
                    
                    // Update the organelle.
                    organelle.update(logicTime);
        
                    // We are in G1 phase of the cell cycle, duplicate all organelles.
                    if(organelle.organelle.name != "nucleus" &&
                        microbeComponent.reproductionStage == 0)
                    {
                        // If the organelle is not split, give it some
                        // compounds to make it larger.
                        if(organelle.getCompoundBin() < 2.0 && !organelle.wasSplit){
                            // Give the organelle access to the
                            // compound bag to take some compound.
                            organelle.growOrganelle(
                                world.GetComponent_CompoundBagComponent(microbeEntity),
                                logicTime);
                            
                            reproductionStageComplete = false;
                            
                            // if the organelle was split and has a
                            // bin less 1, it must have been damaged.
                        } else if(organelle.getCompoundBin() < 1.0 && organelle.wasSplit){
                            // Give the organelle access to the
                            // compound bag to take some compound.
                            organelle.growOrganelle(
                                world.GetComponent_CompoundBagComponent(microbeEntity),
                                logicTime);
                            
                            // If the organelle is twice its size...
                        } else if(organelle.getCompoundBin() >= 2.0){
                            
                            //Queue this organelle for splitting after the loop.
                            //(To avoid "cutting down the branch we're sitting on").
                            organellesToAdd.insertLast(organelle);
                        }
                        // In the S phase, the nucleus grows as chromatin is duplicated.
                    } else if (organelle.organelle.name == "nucleus" &&
                        microbeComponent.reproductionStage == 1)
                    {
                        // If the nucleus hasn't finished replicating
                        // its DNA, give it some compounds.
                        if(organelle.getCompoundBin() < 2.0){
                            // Give the organelle access to the compound
                            // back to take some compound.
                            organelle.growOrganelle(
                                world.GetComponent_CompoundBagComponent(microbeEntity),
                                logicTime);
                            reproductionStageComplete = false;
                        }
                    }
                }
                                
                //Splitting the queued organelles.
                for(uint i = 0; i < organellesToAdd.length(); ++i){
                    
                    PlacedOrganelle@ organelle = organellesToAdd[i];
                    
                    LOG_INFO("ready to split " + organelle.organelle.name);

                    // Mark this organelle as done and return to its normal size.
                    organelle.reset();
                    organelle.wasSplit = true;
                    // Create a second organelle.
                    auto organelle2 = splitOrganelle(microbeEntity, organelle);
                    organelle2.wasSplit = true;
                    organelle2.isDuplicate = true;
                    @organelle2.sisterOrganelle = organelle;
                }

                if(organellesToAdd.length() > 0){
                    // Redo the cell membrane.
                    membraneComponent.clear();
                }
            
                if(reproductionStageComplete && microbeComponent.reproductionStage < 2){
                    microbeComponent.reproductionStage += 1;
                }
                
                // To finish the G2 phase we just need more than a threshold of compounds.
                if(microbeComponent.reproductionStage == 2 ||
                    microbeComponent.reproductionStage == 3)
                {
                    readyToReproduce(microbeEntity);
                }
            }
            
            if(microbeComponent.engulfMode){
                // Drain atp and if(we run out){ disable engulfmode
                auto cost = ENGULFING_ATP_COST_SECOND/1000*logicTime;
                
                if(MicrobeOperations::takeCompound(world, microbeEntity,
                        SimulationParameters::compoundRegistry().getTypeId("atp"), cost) <
                    cost - 0.001)
                {
                    LOG_INFO("too little atp, disabling - engulfing");
                    MicrobeOperations::toggleEngulfMode(world, microbeEntity);
                }
                // Flash the membrane blue.
                MicrobeOperations::flashMembraneColour(world, microbeEntity, 3000,
                    Float4(0.2,0.5,1.0,0.5));
            }
            
            if(microbeComponent.isBeingEngulfed && microbeComponent.wasBeingEngulfed){
                MicrobeOperations::damage(world, microbeEntity, int(logicTime * 0.000025  *
                        microbeComponent.maxHitpoints), "isBeingEngulfed - Microbe.update()s");
                // Else If we were but are no longer, being engulfed
            } else if(microbeComponent.wasBeingEngulfed){
                MicrobeOperations::removeEngulfedEffect(world, microbeEntity);
            }
            // Used to detect when engulfing stops
            microbeComponent.isBeingEngulfed = false;
            compoundAbsorberComponent.setAbsorbtionCapacity(min(microbeComponent.capacity -
                    microbeComponent.stored + 10, microbeComponent.remainingBandwidth));
        }
    }
    
    // ------------------------------------ //
    // Microbe operations only done by this class
    //! Updates the used storage space in a microbe and stores it in the microbe component
    void calculateStorageSpace(ObjectID microbeEntity){
        
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        microbeComponent.stored = 0;
        uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
        for(uint a = 0; a < compoundCount; ++a){

            microbeComponent.stored += MicrobeOperations::getCompoundAmount(world,
                microbeEntity, a);
        }
    }

        
    // For updating the compound absorber
    //
    // Toggles the absorber on and off depending on the remaining storage
    // capacity of the storage organelles.
    void updateCompoundAbsorber(ObjectID microbeEntity){
        
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        
        auto compoundAbsorberComponent = world.GetComponent_CompoundAbsorberComponent(
            microbeEntity);

        if(//microbeComponent.stored >= microbeComponent.capacity or 
            microbeComponent.remainingBandwidth < 1 ||
            microbeComponent.dead)
        {
            compoundAbsorberComponent.disable();
        } else {
            compoundAbsorberComponent.enable();
        }
    }

    void regenerateBandwidth(ObjectID microbeEntity, int logicTime){
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        
        auto addedBandwidth = microbeComponent.remainingBandwidth + logicTime *
            (microbeComponent.maxBandwidth / BANDWIDTH_REFILL_DURATION);
        
        microbeComponent.remainingBandwidth = min(addedBandwidth,
            microbeComponent.maxBandwidth);
    }

    // Stuff that should be maybe moved out of here:
    // void checkEngulfment(ObjectID engulferMicrobeEntity, ObjectID engulfedMicrobeEntity){
    //     auto body = world.GetComponent_RigidBodyComponent(engulferMicrobeEntity, RigidBodyComponent);
    //     auto microbe1Comp = world.GetComponent_MicrobeComponent(engulferMicrobeEntity, MicrobeComponent);
    //     auto microbe2Comp = world.GetComponent_MicrobeComponent(engulfedMicrobeEntity, MicrobeComponent);
    //     auto soundSourceComponent = world.GetComponent_SoundSourceComponent(engulferMicrobeEntity, SoundSourceComponent);
    //     auto bodyEngulfed = world.GetComponent_RigidBodyComponent(engulfedMicrobeEntity, RigidBodyComponent);

    //     // That actually happens sometimes, and i think it shouldn't. :/
    //     // Probably related to a collision detection bug.
    //     // assert(body !is null, "Microbe without a rigidBody tried to engulf.")
    //     // assert(bodyEngulfed !is null, "Microbe without a rigidBody tried to be engulfed.")
    //     if(body is null || bodyEngulfed is null)
    //         return;

    //     if(microbe1Comp.engulfMode && microbe1Comp.maxHitpoints > (
    //             ENGULF_HP_RATIO_REQ * microbe2Comp.maxHitpoints) && 
    //         microbe1Comp.dead == false && microbe2Comp.dead == false)
    //     {
    //         if(!microbe1Comp.isCurrentlyEngulfing){
    //             //We have just started engulfing
    //             microbe2Comp.movementFactor = microbe2Comp.movementFactor /
    //                 ENGULFED_MOVEMENT_DIVISION;
    //             microbe1Comp.isCurrentlyEngulfing = true;
    //             microbe2Comp.wasBeingEngulfed = true;
    //             microbe2Comp.hostileEngulfer = engulferMicrobeEntity;
    //             body.disableCollisionsWith(engulfedMicrobeEntity.id);
    //             soundSourceComponent.playSound("microbe-engulfment");
    //         }

    //         //isBeingEngulfed is set to false every frame
    //         // we detect engulfment stopped by isBeingEngulfed being
    //         // false while wasBeingEngulfed is true
    //         microbe2Comp.isBeingEngulfed = true;
    //     }
    // }


    PlacedOrganelle@ splitOrganelle(ObjectID microbeEntity, PlacedOrganelle@ organelle){
        auto q = organelle.q;
        auto r = organelle.r;

        //Spiral search for space for the organelle
        int radius = 1;
        while(true){
            //Moves into the ring of radius "radius" and center the old organelle
            Int2 radiusOffset = Int2(HEX_NEIGHBOUR_OFFSET[
                    formatInt(int(HEX_SIDE::BOTTOM_LEFT))]);
            q = q + radiusOffset.X;
            r = r + radiusOffset.Y;

            //Iterates in the ring
            for(int side = 1; side <= 6; ++side){
                Int2 offset = Int2(HEX_NEIGHBOUR_OFFSET[formatInt(side)]);
                //Moves "radius" times into each direction
                for(int i = 1; i <= radius; ++i){
                    q = q + offset.X;
                    r = r + offset.Y;

                    //Checks every possible rotation value.
                    for(int j = 0; j <= 5; ++j){
                        
                        // auto rotation = 360 * j / 6;

                        // In the lua code the rotation is i * 60 here
                        // and not in fact the rotation variable

                        // Why doesn't this take a rotation parameter?
                        // Does it incorrectly assume that the Organelle type has a rotated
                        // hex list?
                        if(MicrobeOperations::validPlacement(world, microbeEntity,
                                organelle.organelle, {q, r}))
                        {
                            auto newOrganelle = PlacedOrganelle(organelle, q, r, i*60);
                            
                            LOG_INFO("placed " + organelle.organelle.name + " at " +
                                q + ", " + r);
                            MicrobeOperations::addOrganelle(world, microbeEntity,
                                newOrganelle);
                            return newOrganelle;
                        }
                    }
                }
            }

            radius = radius + 1;
        }

        return null;
    }


    // Damage the microbe if its too low on ATP.
    void atpDamage(ObjectID microbeEntity){
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(MicrobeOperations::getCompoundAmount(world, microbeEntity,
                SimulationParameters::compoundRegistry().getTypeId("atp")) < 1.0)
        {
            // TODO: put this on a GUI notification.
            // if(microbeComponent.isPlayerMicrobe and not this.playerAlreadyShownAtpDamage){
            //     this.playerAlreadyShownAtpDamage = true
            //     showMessage("No ATP hurts you!")
            // }
            MicrobeOperations::damage(world, microbeEntity,
                int(EXCESS_COMPOUND_COLLECTION_INTERVAL *
                    0.000002  * microbeComponent.maxHitpoints), "atpDamage");
            // Microbe takes 2% of max hp per second in damage
        }
    }

    // // Drains an agent from the microbes special storage and emits it
    // //
    // // @param compoundId
    // // The compound id of the agent to emit
    // //
    // // @param maxAmount
    // // The maximum amount to try to emit
    // void emitAgent(ObjectID microbeEntity, CompoundId compoundId, double maxAmount){
    //     auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity, MicrobeComponent);
    //     auto sceneNodeComponent = world.GetComponent_OgreSceneNodeComponent(microbeEntity, OgreSceneNodeComponent);
    //     auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity, SoundSourceComponent);
    //     auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity, MembraneComponent);

    //     // Cooldown code
    //     if(microbeComponent.agentEmissionCooldown > 0){ return; }
    //     auto numberOfAgentVacuoles = microbeComponent.specialStorageOrganelles[compoundId];
    
    //     // Only shoot if you have agent vacuoles.
    //     if(numberOfAgentVacuoles == 0 or numberOfAgentVacuoles == 0){ return; }

    //     // The cooldown time is inversely proportional to the amount of agent vacuoles.
    //     microbeComponent.agentEmissionCooldown = AGENT_EMISSION_COOLDOWN /
    //         numberOfAgentVacuoles;

    //     if(MicrobeSystem.getCompoundAmount(microbeEntity, compoundId) >
    //         MINIMUM_AGENT_EMISSION_AMOUNT)
    //     {
    //         soundSourceComponent.playSound("microbe-release-toxin");

    //         // Calculate the emission angle of the agent emitter
    //         local organelleX, organelleY = axialToCartesian(0, -1); // The front of the microbe
    //         auto membraneCoords = membraneComponent.getExternOrganellePos(
    //             organelleX, organelleY);

    //         auto angle =  math.atan2(organelleY, organelleX);
    //         if(angle < 0){
    //             angle = angle + 2*math.pi;
    //         }
    //         angle = -(angle * 180/math.pi -90 ) % 360;

    //         // Find the direction the microbe is facing
    //         auto yAxis = sceneNodeComponent.transform.orientation.yAxis();
    //         auto microbeAngle = math.atan2(yAxis.x, yAxis.y);
    //         if(microbeAngle < 0){
    //             microbeAngle = microbeAngle + 2*math.pi;
    //         }
    //         microbeAngle = microbeAngle * 180/math.pi;
    //         // Take the microbe angle into account so we get world relative degrees
    //         auto finalAngle = (angle + microbeAngle) % 360;

    //         auto s = math.sin(finalAngle/180*math.pi);
    //         auto c = math.cos(finalAngle/180*math.pi);

    //         auto xnew = -membraneCoords[1] * c + membraneCoords[2] * s;
    //         auto ynew = membraneCoords[1] * s + membraneCoords[2] * c;
        
    //         auto direction = Vector3(xnew, ynew, 0);
    //         direction.normalise();
    //         auto amountToEject = MicrobeSystem.takeCompound(microbeEntity,
    //             compoundId, maxAmount/10.0);
    //         createAgentCloud(compoundId, sceneNodeComponent.transform.position.x + xnew,
    //             sceneNodeComponent.transform.position.y + ynew, direction,
    //             amountToEject * 10);
    //     }
    // }

    // void transferCompounds(ObjectID fromEntity, ObjectID toEntity){
    //     for(_, compoundID in pairs(SimulationParameters::compoundRegistry().getCompoundList())){
    //         auto amount = MicrobeSystem.getCompoundAmount(fromEntity, compoundID);
    
    //         if(amount != 0){
    //             // Is it possible that compounds are created or destroyed here as
    //             // the actual amounts aren't checked (that these functions should return)
    //             MicrobeSystem.takeCompound(fromEntity, compoundID, amount, false);
    //             MicrobeSystem.storeCompound(toEntity, compoundID, amount, false);
    //         }
    //     }
    // }

    void divide(ObjectID microbeEntity){
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
        auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
        auto position = world.GetComponent_Position(microbeEntity);
        auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);

        // Create the one daughter cell.
        // The empty string here is the name of the new cell, which could be more descriptive
        // to set to something based on the original cell
        auto copyEntity = MicrobeOperations::_createMicrobeEntity(world, "", true,
            microbeComponent.speciesName, false);
        MicrobeComponent@ microbeComponentCopy = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(copyEntity));
        auto rigidBodyComponentCopy = world.GetComponent_Physics(copyEntity);
        auto positionCopy = world.GetComponent_Position(copyEntity);

        //Separate the two cells.
        positionCopy._Position = Float3(position._Position.X - 
            membraneComponent.getCellDimensions() / 2,
            0, position._Position.Z);
        rigidBodyComponentCopy.JumpTo(positionCopy);

        position._Position = Float3(position._Position.X + 
            membraneComponent.getCellDimensions() / 2,
            0, position._Position.Z);
        rigidBodyComponent.JumpTo(position);

        
        // Split the compounds evenly between the two cells.
        for(uint64 compoundID = 0; compoundID <
                SimulationParameters::compoundRegistry().getSize(); ++compoundID)
        {
            auto amount = MicrobeOperations::getCompoundAmount(world, microbeEntity,
                compoundID);

            if(amount != 0){
                MicrobeOperations::takeCompound(world, microbeEntity, compoundID,
                    amount / 2/*, false*/ );
                // Not sure what the false here means, it wasn't a
                // parameter in the original lua function so it did
                // nothing even then?
                MicrobeOperations::storeCompound(world, copyEntity, compoundID,
                    amount / 2, false);
            }
        }
    
        microbeComponent.reproductionStage = 0;
        microbeComponentCopy.reproductionStage = 0;

        world.Create_SpawnedComponent(copyEntity, MICROBE_SPAWN_RADIUS);
            
        LOG_WRITE("TODO: soundSourceComponent.playSound(\"microbe-reproduction\");"); 
        // soundSourceComponent.playSound("microbe-reproduction");
    }

    // Copies this microbe. The new microbe will not have the stored compounds of this one.
    void readyToReproduce(ObjectID microbeEntity){
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(microbeComponent.isPlayerMicrobe){
            showReproductionDialog(world);
            microbeComponent.reproductionStage = 0;
        } else {
            // Return the first cell to its normal, non duplicated cell arangement.
            Species::applyTemplate(world, microbeEntity,
                MicrobeOperations::getSpeciesComponent(world, microbeEntity));
            
            divide(microbeEntity);
        }
    }

    private array<MicrobeSystemCached@> CachedComponents;
    private CellStageWorld@ world;
        
    private array<ScriptSystemUses> SystemComponents = {
        ScriptSystemUses(CompoundAbsorberComponent::TYPE),
        ScriptSystemUses("MicrobeComponent"),
        ScriptSystemUses(RenderNode::TYPE),
        ScriptSystemUses(Physics::TYPE),
        ScriptSystemUses(MembraneComponent::TYPE),
        ScriptSystemUses(CompoundBagComponent::TYPE)
    };
}
