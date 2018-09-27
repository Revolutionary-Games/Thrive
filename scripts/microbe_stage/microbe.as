#include "configs.as"
#include "hex.as"
#include "microbe_operations.as"
#include "microbe_stage_hud.as"
#include "organelle_container.as"
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


////////////////////////////////////////////////////////////////////////////////
// MicrobeComponent
//
// Holds data common to all microbes. You probably shouldn't use this directly,
// use MicrobeOperations instead.
////////////////////////////////////////////////////////////////////////////////
class MicrobeComponent : ScriptComponent, OrganelleContainer{

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
        this.agentEmissionCooldown = 0;

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

    //! Called from movement organelles to add movement force
    void addMovementForce(const Float3 &in force)
    {
        queuedMovementForce += force;
    }


    string speciesName;
    // TODO: initialize
    float hitpoints = DEFAULT_HEALTH;
    float maxHitpoints = DEFAULT_HEALTH;
    bool dead = false;
    uint deathTimer = 0;
    // Organelles with complete resonsiblity for a specific compound
    // (such as agentvacuoles)
    // Keys are the CompoundId of the agent and the value is int
    // specifying how many there are
    dictionary specialStorageOrganelles;

    Float3 movementDirection = Float3(0, 0, 0);
    Float3 facingTargetPoint = Float3(0, 0, 0);
    float microbetargetdirection = 0;
    float movementFactor = 1.0; // Multiplied on the movement speed of the microbe.
    // The amount that can be stored in the
    // microbe. NOTE: This does not include
    // special storage organelles.
    // This is also the amount of each
    // individual compound you can hold
    double capacity = 0;
    // The amount stored in the microbe. NOTE:
    // This does not include special storage
    // organelles.
    double stored = 0;
    bool initialized = false;
    bool isPlayerMicrobe = false;
    float maxBandwidth = 10.0 * BANDWIDTH_PER_ORGANELLE; // wtf is a bandwidth anyway?
    float remainingBandwidth = 0.0;
    uint compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL;
    int agentEmissionCooldown = 0;

    // Is this the place where the actual flash duration works?
    // The one in the organelle class doesn't work
    uint flashDuration = 0;
    Float4 flashColour = Float4(0, 0, 0, 0);
    uint reproductionStage = 0;


    //variables for engulfing
    bool engulfMode = false;
    bool isCurrentlyEngulfing = false;
    bool isBeingEngulfed = false;
    bool wasBeingEngulfed = false;
    ObjectID hostileEngulfer = NULL_OBJECT;
    AudioSource@ engulfAudio;

    // New state variables that MicrobeSystem also uses
    bool in_editor = false;

    // ObjectID microbe;
    ObjectID microbeEntity = NULL_OBJECT;

    Float3 queuedMovementForce = Float3(0, 0, 0);
}

//! Helper for MicrobeSystem
class MicrobeSystemCached{

    MicrobeSystemCached(ObjectID entity, CompoundAbsorberComponent@ first,
        MicrobeComponent@ second, RenderNode@ third, Physics@ fourth,
        MembraneComponent@ fifth, CompoundBagComponent@ sixth, Position@ seventh)
    {
        this.entity = entity;
        @this.first = first;
        @this.second = second;
        @this.third = third;
        @this.fourth = fourth;
        @this.fifth = fifth;
        @this.sixth = sixth;
        @this.seventh = seventh;
    }

    ObjectID entity = -1;

    CompoundAbsorberComponent@ first;
    MicrobeComponent@ second;
    RenderNode@ third;
    Physics@ fourth;
    MembraneComponent@ fifth;
    CompoundBagComponent@ sixth;
    Position@ seventh;
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

    void Init(GameWorld@ w)
    {
        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "MicrobeSystem expected CellStageWorld");
    }

    void Release(){}

    void Run()
    {
        for(uint i = 0; i < CachedComponents.length(); ++i){
            updateMicrobe(CachedComponents[i], TICKSPEED);
        }
    }

    void Clear()
    {
        CachedComponents.resize(0);
    }

    void CreateAndDestroyNodes()
    {
        // Delegate to helper //
        ScriptSystemNodeHelper(world, @CachedComponents, SystemComponents);
    }

    // Updates the microbe's state
    void updateMicrobe(MicrobeSystemCached@ components, uint logicTime){
        auto microbeEntity = components.entity;

        if(microbeEntity == -1){

            LOG_ERROR("MicrobeSystem: updateMicrobe: invalid microbe entity hasn't "
                "set a ObjectID, did someone forget to call 'init'?");
            return;
        }

        MicrobeComponent@ microbeComponent = components.second;

        if(!microbeComponent.initialized){

            LOG_ERROR("Microbe is not initialized: " + microbeEntity);
            return;
        }

        if(microbeComponent.dead){

            updateDeadCell(components, logicTime);

        } else {

            updateAliveCell(components, logicTime);
        }
    }

    private void updateAliveCell(MicrobeSystemCached@ &in components, uint logicTime)
    {
        auto microbeEntity = components.entity;

        MembraneComponent@ membraneComponent = components.fifth;
        RenderNode@ sceneNodeComponent = components.third;
        CompoundAbsorberComponent@ compoundAbsorberComponent = components.first;
        CompoundBagComponent@ compoundBag = components.sixth;
        MicrobeComponent@ microbeComponent = components.second;

        // Recalculating agent cooldown time.
        microbeComponent.agentEmissionCooldown = int(max(
            microbeComponent.agentEmissionCooldown - logicTime, 0));

        // Calculate storage.
        calculateStorageSpace(microbeEntity);

        // Get amount of compounds
        uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
        // This is only used in the process sytem to make sure you
        // dont add anymore when out of space for a specific
        // compound
        compoundBag.storageSpace = microbeComponent.capacity;

        // StorageOrganelles
        updateCompoundAbsorber(microbeEntity);

        // Regenerate bandwidth
        regenerateBandwidth(microbeEntity, logicTime);

        // Attempt to absorb queued compounds
        auto absorbed = compoundAbsorberComponent.getAbsorbedCompounds();

        // Loop through compounds and add if you can
        for(uint i = 0; i < absorbed.length(); ++i){
            CompoundId compound = absorbed[i];
            auto amount = compoundAbsorberComponent.absorbedCompoundAmount(compound);

            if(amount > 0.0 && (amount + MicrobeOperations::getCompoundAmount(world,
                        microbeEntity, compound) <= microbeComponent.capacity)){
                // Only fill up the microbe if they can hold more of a specific compound
                MicrobeOperations::storeCompound(world, microbeEntity, compound,
                    min(microbeComponent.capacity,amount), true);
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
                //LOG_INFO("Flashed");
                MicrobeOperations::setMembraneColour(world, microbeEntity,
                    microbeComponent.flashColour);
            } else {
                //Restore colour
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
        //	Handle hitpoints
        if((microbeComponent.hitpoints < microbeComponent.maxHitpoints))
        {
            if(MicrobeOperations::getCompoundAmount(world, microbeEntity,
                    SimulationParameters::compoundRegistry().getTypeId("atp")) > 0)
            {
                microbeComponent.hitpoints += (REGENERATION_RATE/1000.0*logicTime);
                if (microbeComponent.hitpoints > microbeComponent.maxHitpoints)
                {
                    microbeComponent.hitpoints =  microbeComponent.maxHitpoints;
                }
            }
        }

        doReproductionStep(components,logicTime);

        if(microbeComponent.engulfMode){
            // Drain atp
            auto cost = ENGULFING_ATP_COST_SECOND/1000*logicTime;

            if(MicrobeOperations::takeCompound(world, microbeEntity,
                    SimulationParameters::compoundRegistry().getTypeId("atp"), cost) <
                cost - 0.001)
            {
                LOG_INFO("too little atp, disabling - engulfing");
                MicrobeOperations::toggleEngulfMode(world, microbeEntity);
            }

            // Play sound
            if (@microbeComponent.engulfAudio is null ||
                !microbeComponent.engulfAudio.Get().isPlaying())
            {
                @microbeComponent.engulfAudio = GetEngine().GetSoundDevice().Play2DSound(
                    "Data/Sound/soundeffects/engulfment.ogg",false,true);
                if (microbeComponent.isPlayerMicrobe)
                {
                    microbeComponent.engulfAudio.Get().setVolume(70.0f);
                }
                else {
                    // NPC microbes are less loud
                    microbeComponent.engulfAudio.Get().setVolume(40.0f);
                }
                microbeComponent.engulfAudio.Get().play();
            }

            // Flash the membrane blue.
            MicrobeOperations::flashMembraneColour(world, microbeEntity, 1000,
                Float4(0.2,0.5,1.0,0.5));
        }

        if(microbeComponent.isBeingEngulfed && microbeComponent.wasBeingEngulfed){
            //LOG_INFO("doing engulf damage");
            MicrobeOperations::damage(world,microbeEntity, microbeComponent.maxHitpoints / 5.0f
                / 1000.0f * logicTime,
                "isBeingEngulfed - Microbe.update()s");

            microbeComponent.wasBeingEngulfed = true;

            // Else If we were but are no longer, being engulfed
        } else if(microbeComponent.wasBeingEngulfed && !microbeComponent.isBeingEngulfed){
            LOG_INFO("removing engulf effect");
            microbeComponent.wasBeingEngulfed=false;
            MicrobeOperations::removeEngulfedEffect(world, microbeEntity);
        }

        applyCellMovement(components, logicTime);

        microbeComponent.isBeingEngulfed = false;
        compoundAbsorberComponent.setAbsorbtionCapacity(microbeComponent.capacity);
    }

    private void updateDeadCell(MicrobeSystemCached@ &in components, uint logicTime)
    {
        auto microbeEntity = components.entity;

        MicrobeComponent@ microbeComponent = components.second;
        Physics@ physics = components.fourth;

        microbeComponent.deathTimer = microbeComponent.deathTimer - logicTime;
        microbeComponent.flashDuration = 0;
        if(microbeComponent.deathTimer <= 0){
            if(microbeComponent.isPlayerMicrobe == true){
                MicrobeOperations::respawnPlayer(world);
            } else {

                // Safe destroy before next tick
                // This is done before removing the organelles as that seems to cause a lot
                // of null pointer accesses
                world.QueueDestroyEntity(microbeEntity);

                // Remove organelles from the microbe
                for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

                    // This Collision doesn't really need to be
                    // updated here, but this keeps us from
                    // changing onRemovedFromMicrobe to allow
                    // skipping it
                    microbeComponent.organelles[i].onRemovedFromMicrobe(microbeEntity,
                        physics.Collision);
                }
            }
        }
    }

    private void applyCellMovement(MicrobeSystemCached@ &in components, uint logicTime)
    {
        auto microbeEntity = components.entity;

        Physics@ physics = components.fourth;
        Position@ pos = components.seventh;
        MicrobeComponent@ microbeComponent = components.second;

        if(physics.Body is null){

            LOG_ERROR("Cell is missing physics body: " + microbeEntity);
            return;
        }

        // Reset movement
        microbeComponent.queuedMovementForce = Float3(0, 0, 0);

        // First add drag based on the velocity
        const Float3 velocity = physics.GetVelocity();

        // There should be no Y velocity so it should be zero
        const Float3 drag(velocity.X * CELL_DRAG_MULTIPLIER,
            velocity.Y * CELL_DRAG_MULTIPLIER,
            velocity.Z * CELL_DRAG_MULTIPLIER);

        // Only add drag if it is over CELL_REQUIRED_DRAG_BEFORE_APPLY
        if(drag.HAddAbs() >= CELL_REQUIRED_DRAG_BEFORE_APPLY){

            microbeComponent.queuedMovementForce += drag;
        }

        // Add base movement
        // The movementDirection is the player or AI input
        // Rotate the 'thrust' based on our orientation
        microbeComponent.queuedMovementForce += pos._Orientation.RotateVector(
            microbeComponent.movementDirection * CELL_BASE_THRUST);

        // Update organelles and then apply the movement force that was generated
        for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
            microbeComponent.organelles[i].update(logicTime);
        }

        // Apply movement
        if(microbeComponent.queuedMovementForce != Float3(0, 0, 0)){

            if(physics.Body is null){

                LOG_WARNING(
                    "Skipping microbe movement apply for microbe without physics body");
            } else {

                // LOG_WRITE("cell thrust: " + microbeComponent.queuedMovementForce.X + ", " +
                //     microbeComponent.queuedMovementForce.Y + ", " +
                //     microbeComponent.queuedMovementForce.Z);
                physics.GiveImpulse(microbeComponent.queuedMovementForce,
                    pos._Position);
            }
        }

        // Rotation (this is unaffected by everything currently)
        {
            const auto target = Float4::QuaternionLookAt(pos._Position,
                microbeComponent.facingTargetPoint);
            const auto current = pos._Orientation;
            // Slerp 50% of the way each call
            const auto interpolated = current.Slerp(target, 0.5f);
            // const auto interpolated = target;

            // Not sure if updating the Position component here does anything
            pos._Orientation = interpolated;
            pos.Marked = true;

            // LOG_WRITE("turn = " + pos._Orientation.X + ", " + pos._Orientation.Y + ", "
            //     + pos._Orientation.Z + ", " + pos._Orientation.W);

            physics.SetOnlyOrientation(interpolated);

            // auto targetDirection = microbeComponent.facingTargetPoint - pos._Position;
            // // TODO: direct multiplication was also used here
            // // Float3 localTargetDirection = pos._Orientation.Inverse().RotateVector(
            //      targetDirection);
            // Float3 localTargetDirection = pos._Orientation.Inverse().RotateVector(
            //      targetDirection);

            // // Float3 localTargetDirection = pos._Orientation.ToAxis() - targetDirection;
            // // localTargetDirection.Y = 0;
            // // improper fix. facingTargetPoint somehow gets a non-zero y value.
            // LOG_WRITE("local direction = " + localTargetDirection.X + ", " +
            //     localTargetDirection.Y + ", " + localTargetDirection.Z);

            // assert(localTargetDirection.Y < 0.01,
            //     "Microbes should only move in the 2D plane with y = 0");

            // // This doesn't help with the major jitter
            // // // Round to zero if either is too small
            // // if(abs(localTargetDirection.X) < 0.01)
            // //     localTargetDirection.X = 0;
            // // if(abs(localTargetDirection.Z) < 0.01)
            // //     localTargetDirection.Z = 0;

            // float alpha = atan2(-localTargetDirection.X, -localTargetDirection.Z);
            // float absAlpha = abs(alpha) * RADIANS_TO_DEGREES;
            // microbeComponent.microbetargetdirection = absAlpha;
            // if(absAlpha > 1){

            //     LOG_WRITE("Alpha is: " + alpha);
            //     Float3 torqueForces = Float3(0, this.torque * alpha * logicTime *
            //         microbeComponent.movementFactor * 0.00001f, 0);
            //     rigidBodyComponent.AddOmega(torqueForces);

            //     // Rotation is the same for each flagella so doing this
            //     // makes things less likely to break and still work. Only
            //     // tweak should be that there should be
            //     // microbeComponent.movementFactor alternative for
            //     // rotation that depends on flagella and cilia. The
            //     // problem with this is that there are weird spots where
            //     // this gets stuck at (hopefully works better with the
            //     // rounding of X and Z)
            //     // Float3 torqueForces = Float3(0, this.torque * alpha * logicTime *
            //     //     microbeComponent.movementFactor * 0.0001f, 0);
            //     // rigidBodyComponent.SetOmega(torqueForces);

            // } else {
            //     // Doesn't work
            //     // // Slow down rotation if there is some
            //     // auto omega = rigidBodyComponent.GetOmega();
            //     // rigidBodyComponent.SetOmega(Float3(0, 0, 0));

            //     // if(abs(omega.X) > 1 || abs(omega.Z) > 1){

            //     //     rigidBodyComponent.AddOmega(Float3(-omega.X * 0.01f, 0,
            //     //         -omega.Z * 0.01f));
            //     // }
            // }
        }
    }

    //! This method handles reproduction for the cell
    //! It makes calls to many other places to achieve this
    void doReproductionStep(MicrobeSystemCached@ &in components, uint logicTime){
        auto microbeEntity = components.entity;
        //! Reproduction
        MicrobeComponent@ microbeComponent = components.second;
        MembraneComponent@ membraneComponent = components.fifth;
        auto reproductionStageComplete = true;
        array<PlacedOrganelle@> organellesToAdd;

        // Grow all the large organelles.
        for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

            auto organelle = microbeComponent.organelles[i];

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

        // End of reproduction
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
            // Again this variable is only really nessessary for run and tumble
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

        if(microbeComponent.remainingBandwidth < 1 ||
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
                SimulationParameters::compoundRegistry().getTypeId("atp")) <= 0)
        {
            // TODO: put this on a GUI notification.
            // if(microbeComponent.isPlayerMicrobe and not this.playerAlreadyShownAtpDamage){
            //     this.playerAlreadyShownAtpDamage = true
            //     showMessage("No ATP hurts you!")
            // }

            // Microbe takes 4% of max hp per second in damage
            MicrobeOperations::damage(world, microbeEntity,
                EXCESS_COMPOUND_COLLECTION_INTERVAL *
                    0.00004  * microbeComponent.maxHitpoints, "atpDamage");
        }
    }

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
        LOG_INFO("Divide called");
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
        // Will also need to be changed for individual storage
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

        //play the split sound
        GetEngine().GetSoundDevice().Play2DSoundEffect(
            "Data/Sound/soundeffects/reproduction.ogg");
    }

    // Copies this microbe. The new microbe will not have the stored compounds of this one.
    void readyToReproduce(ObjectID microbeEntity)
    {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(microbeComponent.isPlayerMicrobe){
            showReproductionDialog(world);
            microbeComponent.reproductionStage = 0;
        } else {

            // Return the first cell to its normal, non duplicated cell arrangement.
            if (MicrobeOperations::getSpeciesComponent(world, microbeEntity) !is null)
                {
                Species::applyTemplate(world, microbeEntity,
                    MicrobeOperations::getSpeciesComponent(world, microbeEntity));
                divide(microbeEntity);
                }
            else
                {
                // You are extinct so just die okay.
                MicrobeOperations::kill(world, microbeEntity);
                }
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
        ScriptSystemUses(CompoundBagComponent::TYPE),
        ScriptSystemUses(Position::TYPE)
    };
}
