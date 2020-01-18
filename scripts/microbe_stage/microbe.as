#include "configs.as"
#include "hex.as"
#include "microbe_operations.as"
#include "microbe_stage_hud.as"

//! Why is this needed? Is it for(the future when we don't want to
//! absorb everything (or does this skip toxins, which aren't in compound registry)
void setupAbsorberForAllCompounds(CompoundAbsorberComponent@ absorber)
{
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
class MicrobeComponent : ScriptComponent{

    //! This detaches all still attached organelles
    //! \todo There might be a more graceful way to do this
    ~MicrobeComponent()
    {
        //LOG_INFO("MicrobeComponent destroyed: " + microbeEntity);

        for(uint i = 0; i < organelles.length(); ++i){

            organelles[i].onDestroyedWithMicrobe(microbeEntity);
        }

        organelles.resize(0);
    }

    //! This has to be called after creating this
    void init(ObjectID forEntity, bool isPlayerMicrobe, Species@ species)
    {
        @this.species = species;

        this.isPlayerMicrobe = isPlayerMicrobe;
        this.engulfMode = false;
        this.isBeingEngulfed = false;
        this.hostileEngulfer = NULL_OBJECT;
        this.wasBeingEngulfed = false;
        this.isCurrentlyEngulfing = false;
        this.dead = false;
        this.microbeEntity = forEntity;
        this.agentEmissionCooldown = 0;

        //cache hexes in microbe
        for(uint i = 0; i < organelles.length(); ++i){
            totalHexCountCache += organelles[i].organelle.getHexCount();
        }

        // Microbe system update should initialize this component on next tick
    }

    //! Called from movement organelles to add movement force
    void addMovementForce(const Float3 &in force)
    {
        queuedMovementForce += force;
    }

    //! This is reference counted so this can be stored here
    //! \note This is directly read from C++ and MUST BE the first property
    Species@ species;

    // TODO: initialize
    float hitpoints = DEFAULT_HEALTH;
    float previousHitpoints = DEFAULT_HEALTH;
    float maxHitpoints = DEFAULT_HEALTH;
    bool dead = false;
    float deathTimer = 0;

    // The organelles in this microbe
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
    float compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL;
    float escapeInterval = 0;
    float agentEmissionCooldown = 0;

    // Is this the place where the actual flash duration works?
    // The one in the organelle class doesn't work
    float flashDuration = 0;
    Float4 flashColour = Float4(0, 0, 0, 0);
    //! \todo Change this into an enum
    uint reproductionStage = 0;


    //variables for engulfing
    bool engulfMode = false;
    bool isCurrentlyEngulfing = false;
    bool isBeingEngulfed = false;
    bool wasBeingEngulfed = false;
    bool hasEscaped = false;
    ObjectID hostileEngulfer = NULL_OBJECT;
    AudioSource@ engulfAudio;
    AudioSource@ otherAudio;
    // New state variables that MicrobeSystem also uses
    bool in_editor = false;

    // ObjectID microbe;
    ObjectID microbeEntity = NULL_OBJECT;

    Float3 queuedMovementForce = Float3(0, 0, 0);

    // This variable is used to cache the size of the hexes
    int totalHexCountCache = 0;
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

    void Run(float elapsed)
    {
        for(uint i = 0; i < CachedComponents.length(); ++i){
            updateMicrobe(CachedComponents[i], elapsed);
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
    void updateMicrobe(MicrobeSystemCached@ components, float elapsed)
    {
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

            updateDeadCell(components, elapsed);

        } else {

            updateAliveCell(components, elapsed);

            // As long as the player has been alive they can go to the editor in freebuild
            if(microbeComponent.isPlayerMicrobe &&
                GetThriveGame().playerData().isFreeBuilding())
            {
                showReproductionDialog(world);
            }
        }
    }

    private void updateAliveCell(MicrobeSystemCached@ &in components, float elapsed)
    {
        auto microbeEntity = components.entity;
        MembraneComponent@ membraneComponent = components.fifth;
        RenderNode@ sceneNodeComponent = components.third;
        CompoundAbsorberComponent@ compoundAbsorberComponent = components.first;
        CompoundBagComponent@ compoundBag = components.sixth;
        MicrobeComponent@ microbeComponent = components.second;
        microbeComponent.movementFactor = 1.0f;

        // Recalculating agent cooldown time.
        microbeComponent.agentEmissionCooldown -= elapsed;

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
        regenerateBandwidth(microbeEntity, elapsed);

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
        if(microbeComponent.flashDuration > 0 &&
            microbeComponent.flashColour != Float4(0, 0, 0, 0)
        ){
            microbeComponent.flashDuration -= elapsed;

            // How frequent it flashes, would be nice to update
            // the flash void to have this variable{
            if((microbeComponent.flashDuration % 0.6f) < 0.3f){
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

        //  Handle hitpoints
        if((microbeComponent.hitpoints < microbeComponent.maxHitpoints))
        {
            if(MicrobeOperations::getCompoundAmount(world, microbeEntity,
                    SimulationParameters::compoundRegistry().getTypeId("atp")) >= 1.0f)
            {
                microbeComponent.hitpoints += (REGENERATION_RATE * elapsed);
                if (microbeComponent.hitpoints > microbeComponent.maxHitpoints)
                {
                    microbeComponent.hitpoints =  microbeComponent.maxHitpoints;
                }
            }
        }

        doReproductionStep(components, elapsed);

        if(microbeComponent.engulfMode){
            // Drain atp
            auto cost = ENGULFING_ATP_COST_SECOND * elapsed;

            if(MicrobeOperations::takeCompound(world, microbeEntity,
                    SimulationParameters::compoundRegistry().getTypeId("atp"), cost) <
                cost - 0.001f)
            {
                //LOG_INFO("too little atp, disabling - engulfing");
                MicrobeOperations::toggleEngulfMode(microbeComponent);
            }

            // Play sound
            if (microbeComponent.isPlayerMicrobe &&  (@microbeComponent.engulfAudio is null ||
                !microbeComponent.engulfAudio.IsPlaying()))
            {
                @microbeComponent.engulfAudio = GetEngine().GetSoundDevice().Play2DSound(
                    "Data/Sound/soundeffects/engulfment.ogg", false);

                if(microbeComponent.engulfAudio !is null){

                    if (microbeComponent.isPlayerMicrobe)
                    {
                        microbeComponent.engulfAudio.SetVolume(1.0f);
                    }

                    // what about other sound level?

                } else {
                    LOG_ERROR("Failed to create engulfment sound player");
                }
            }

            // Flash the membrane blue.
            MicrobeOperations::flashMembraneColour(world, microbeEntity, 1,
                Float4(0.2,0.5,1.0,0.5));
        }

        if(microbeComponent.engulfMode){
            microbeComponent.movementFactor =  microbeComponent.movementFactor /
                ENGULFING_MOVEMENT_DIVISION;
        }

        if(microbeComponent.isBeingEngulfed){
            microbeComponent.movementFactor =  microbeComponent.movementFactor /
                ENGULFED_MOVEMENT_DIVISION;

            MicrobeOperations::damage(world,microbeEntity, ENGULF_DAMAGE * elapsed,
                "isBeingEngulfed - Microbe.update()s");
            microbeComponent.wasBeingEngulfed = true;
            // Else If we were but are no longer, being engulfed
        } else if(microbeComponent.wasBeingEngulfed && !microbeComponent.isBeingEngulfed){

            microbeComponent.wasBeingEngulfed = false;
            auto playerSpecies = MicrobeOperations::getSpecies(world, "Default");

            if (!microbeComponent.isPlayerMicrobe &&
                microbeComponent.species.name != playerSpecies.name)
            {
                 microbeComponent.hasEscaped = true;
                 microbeComponent.escapeInterval = 0;
            }

            MicrobeOperations::removeEngulfedEffect(world, microbeEntity);
        }

        // Still considered to be chased for CREATURE_ESCAPE_INTERVAL milliseconds
        if(microbeComponent.hasEscaped){
            microbeComponent.escapeInterval += elapsed;
            if(microbeComponent.escapeInterval >= CREATURE_ESCAPE_INTERVAL){
                microbeComponent.hasEscaped = false;
                microbeComponent.escapeInterval = 0;

                auto species = MicrobeOperations::getSpecies(world,
                    microbeComponent.species.name);

                if(species !is null)
                    MicrobeOperations::alterSpeciesPopulation(species,
                        CREATURE_ESCAPE_POPULATION_GAIN, "escape engulfing");
            }
        }

        // Check whether we should not be being engulfed anymore
        if (microbeComponent.hostileEngulfer != NULL_OBJECT){
            auto predatorPosition = world.GetComponent_Position(
                microbeComponent.hostileEngulfer);
            auto ourPosition = world.GetComponent_Position(microbeEntity);
            auto predatorMembraneComponent = world.GetComponent_MembraneComponent(
                microbeComponent.hostileEngulfer);

            if(predatorMembraneComponent is null){
                // Can't be engulfed by something with no membrane
                microbeComponent.hostileEngulfer = NULL_OBJECT;
                microbeComponent.isBeingEngulfed = false;
            } else {

                auto circleRad = predatorMembraneComponent.calculateEncompassingCircleRadius();

                MicrobeComponent@ hostileMicrobeComponent = cast<MicrobeComponent>(
                    world.GetScriptComponentHolder("MicrobeComponent").Find(
                        microbeComponent.hostileEngulfer));

                if (hostileMicrobeComponent.species.isBacteria){
                    circleRad = circleRad/2;
                }

                if ((hostileMicrobeComponent is null) ||
                    (!hostileMicrobeComponent.engulfMode) ||
                    (hostileMicrobeComponent.dead) ||
                    (ourPosition._Position -  predatorPosition._Position).LengthSquared() >=
                    circleRad){

                    microbeComponent.hostileEngulfer = NULL_OBJECT;
                    microbeComponent.isBeingEngulfed = false;
                }
            }
        }
        else {
            microbeComponent.hostileEngulfer = NULL_OBJECT;
            microbeComponent.isBeingEngulfed = false;
        }

        // There is an osmoregulation cost

        // This is per second logic time is the amount of ticks per second.
        // TODO:It seems to happen no matter what (even if it takes away less atp then
        // you generate per second), we should probably make it take into account the amount
        // of atp being generated so resources arent wasted
        auto osmoCost = (microbeComponent.totalHexCountCache * SimulationParameters::membraneRegistry().getTypeData(microbeComponent.species.membraneType).osmoregulationFactor * ATP_COST_FOR_OSMOREGULATION) *
            elapsed;

        MicrobeOperations::takeCompound(world, microbeEntity,
            SimulationParameters::compoundRegistry().getTypeId("atp"), osmoCost);

        // Reset compound absorption
        compoundAbsorberComponent.setAbsorbtionCapacity(microbeComponent.capacity);

        microbeComponent.compoundCollectionTimer += elapsed;

        // Moved this to right before atpDamage
        applyCellMovement(components, elapsed);

        while(microbeComponent.compoundCollectionTimer >
            EXCESS_COMPOUND_COLLECTION_INTERVAL)
        {
            // For every EXCESS_COMPOUND_COLLECTION_INTERVAL passed
            atpDamage(microbeEntity);

            microbeComponent.compoundCollectionTimer -= EXCESS_COMPOUND_COLLECTION_INTERVAL;
            MicrobeOperations::purgeCompounds(world, microbeEntity);
        }

        if(microbeComponent.hitpoints != microbeComponent.previousHitpoints)
            membraneComponent.setHealthFraction(microbeComponent.hitpoints /
                microbeComponent.maxHitpoints);

        microbeComponent.previousHitpoints = microbeComponent.hitpoints;
    }

    private void updateDeadCell(MicrobeSystemCached@ &in components, float elapsed)
    {
        auto microbeEntity = components.entity;

        MicrobeComponent@ microbeComponent = components.second;
        Physics@ physics = components.fourth;

        microbeComponent.deathTimer -= elapsed;
        // NOTE: this doesn't correctly restore the colour so not sure what this does here
        microbeComponent.flashDuration = 0;

        if(microbeComponent.deathTimer <= 0){
            if(microbeComponent.isPlayerMicrobe){
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
                    microbeComponent.organelles[i].onRemovedFromMicrobe(microbeEntity, null);
                }
            }
        }
    }

    private void applyCellMovement(MicrobeSystemCached@ &in components, float elapsed)
    {
        auto microbeEntity = components.entity;

        Physics@ physics = components.fourth;
        Position@ pos = components.seventh;
        MicrobeComponent@ microbeComponent = components.second;

        if(physics.Body is null){

            //LOG_ERROR("Cell is missing physics body: " + microbeEntity);
            return;
        }

        // Reset movement
        microbeComponent.queuedMovementForce = Float3(0.0f, 0.0f, 0.0f);

        // First add drag based on the velocity
        const Float3 velocity = physics.Body.GetVelocity();

        // There should be no Y velocity so it should be zero
        const Float3 drag(velocity.X * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
                    microbeComponent.totalHexCountCache)),
            velocity.Y * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
                    microbeComponent.totalHexCountCache)),
            velocity.Z * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
                    microbeComponent.totalHexCountCache)));

        // Only add drag if it is over CELL_REQUIRED_DRAG_BEFORE_APPLY
        if(abs(drag.X) >= CELL_REQUIRED_DRAG_BEFORE_APPLY){
            microbeComponent.queuedMovementForce.X += drag.X;
        }
        else if (abs(velocity.X) >  .001){
            microbeComponent.queuedMovementForce.X += -velocity.X;
        }

        if(abs(drag.Z) >= CELL_REQUIRED_DRAG_BEFORE_APPLY){
            microbeComponent.queuedMovementForce.Z += drag.Z;
        }
        else if (abs(velocity.Z) >  .001){
            microbeComponent.queuedMovementForce.Z += -velocity.Z;
        }

        // Add base movement. The movementDirection is the player or
        // AI input which is then rotated based on the cell
        // orientation
        if(microbeComponent.movementDirection.X != 0.f ||
            microbeComponent.movementDirection.Z != 0.f)
        {
            const auto cost = (BASE_MOVEMENT_ATP_COST * microbeComponent.totalHexCountCache)
                * elapsed;

            const auto got = MicrobeOperations::takeCompound(world, microbeEntity,
                SimulationParameters::compoundRegistry().getTypeId("atp"), cost);

            float force = CELL_BASE_THRUST;

            // Halve speed if out of ATP
            if(got < cost){
                // Not enough ATP to move at full speed
                force *= 0.5f;
            }

            microbeComponent.queuedMovementForce += pos._Orientation * (
                microbeComponent.movementDirection * force *
                microbeComponent.movementFactor *
                (SimulationParameters::membraneRegistry().getTypeData(microbeComponent.species.membraneType).movementFactor -
                microbeComponent.species.membraneRigidity * MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
        }

        // Update organelles and then apply the movement force that was generated
        for(uint i = 0; i < microbeComponent.organelles.length(); ++i){
            microbeComponent.organelles[i].update(elapsed);
        }

        // Apply movement
        if(microbeComponent.queuedMovementForce != Float3(0.0f, 0.0f, 0.0f)){
            if(physics.Body is null){

                LOG_WARNING(
                    "Skipping microbe movement apply for microbe without physics body");
            } else {

                // Scale movement by elapsed time (not by framerate). We aren't Fallout 4
                microbeComponent.queuedMovementForce *= elapsed * 10.f;
                physics.Body.GiveImpulse(microbeComponent.queuedMovementForce);
            }
        }

        // Rotation (this is unaffected by everything currently)
        {
            const auto target = Quaternion::LookAt(pos._Position,
                microbeComponent.facingTargetPoint);
            const auto current = pos._Orientation;
            // Slerp 50% of the way each call
            const auto interpolated = current.Slerp(target, 0.2f);
            // const auto interpolated = target;

            // Not sure if updating the Position component here does anything
            // The position should not be updated from here
            // pos._Orientation = interpolated;
            // pos.Marked = true;

            // LOG_WRITE("turn = " + pos._Orientation.X + ", " + pos._Orientation.Y + ", "
            //     + pos._Orientation.Z + ", " + pos._Orientation.W);

            physics.Body.SetOnlyOrientation(interpolated);

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
    //! \note If this or growOrganelle is changed,
    //! MicrobeOperations::calculateReproductionProgress must be changed as well
    //! \todo This currently does not use elapsed for anything
    void doReproductionStep(MicrobeSystemCached@ &in components, float elapsed)
    {
        auto microbeEntity = components.entity;

        MicrobeComponent@ microbeComponent = components.second;
        MembraneComponent@ membraneComponent = components.fifth;
        CompoundBagComponent@ compoundBag= components.sixth;

        if(microbeComponent.reproductionStage == 3){
            // Ready to reproduce already. Only the player gets here
            // as other cells split and reset automatically
            return;
        }

        auto reproductionStageComplete = true;

        if(microbeComponent.reproductionStage == 0 || microbeComponent.reproductionStage == 1){
            array<PlacedOrganelle@> organellesToAdd;

            // Grow all the organelles.
            for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

                auto organelle = microbeComponent.organelles[i];

                // Check if already done
                if(organelle.wasSplit)
                    continue;

                // We are in G1 phase of the cell cycle, duplicate all organelles.
                if(organelle.organelle.name != "nucleus" &&
                    microbeComponent.reproductionStage == 0)
                {
                    // If Give it some compounds to make it larger.
                    organelle.growOrganelle(compoundBag);

                    if(organelle.getGrowthProgress() >= 1.f){
                        // Queue this organelle for splitting after the loop.
                        organellesToAdd.insertLast(organelle);
                    } else {
                        // Needs more stuff
                        reproductionStageComplete = false;
                    }
                    // In the S phase, the nucleus grows as chromatin is duplicated.
                } else if (organelle.organelle.name == "nucleus" &&
                    microbeComponent.reproductionStage == 1)
                {
                    // The nucleus hasn't finished replicating
                    // its DNA, give it some compounds.
                    organelle.growOrganelle(compoundBag);

                    if(organelle.getGrowthProgress() < 1.f){
                        // Nucleus needs more compounds
                        reproductionStageComplete = false;
                    }
                }
            }

            // Splitting the queued organelles.
            for(uint i = 0; i < organellesToAdd.length(); ++i){
                PlacedOrganelle@ organelle = organellesToAdd[i];

                // LOG_INFO("ready to split " + organelle.organelle.name);

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

                // And allow the new organelles to do processes
                MicrobeOperations::rebuildProcessList(world, microbeEntity);
            }

            if(reproductionStageComplete){
                microbeComponent.reproductionStage += 1;
            }
        }

        if(microbeComponent.reproductionStage == 2)
        {
            microbeComponent.reproductionStage += 1;
            readyToReproduce(microbeEntity);
        }

        if(microbeComponent.reproductionStage == 3){
            // Nothing to do
        }

        // End of reproduction
    }

    // ------------------------------------ //
    // Microbe operations only done by this class
    //! Updates the used storage space in a microbe and stores it in the microbe component
    void calculateStorageSpace(ObjectID microbeEntity)
    {
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
    void updateCompoundAbsorber(ObjectID microbeEntity)
    {
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

    void regenerateBandwidth(ObjectID microbeEntity, float elapsed)
    {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        auto addedBandwidth = microbeComponent.remainingBandwidth + elapsed * 2700.f *
            (microbeComponent.maxBandwidth / BANDWIDTH_REFILL_DURATION);

        microbeComponent.remainingBandwidth = min(addedBandwidth,
            microbeComponent.maxBandwidth);
    }

    PlacedOrganelle@ splitOrganelle(ObjectID microbeEntity, PlacedOrganelle@ organelle)
    {
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
    void atpDamage(ObjectID microbeEntity)
    {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(MicrobeOperations::getCompoundAmount(world, microbeEntity,
                SimulationParameters::compoundRegistry().getTypeId("atp")) <= 0.0f)
        {
            // TODO: put this on a GUI notification.
            // if(microbeComponent.isPlayerMicrobe and not this.playerAlreadyShownAtpDamage){
            //     this.playerAlreadyShownAtpDamage = true
            //     showMessage("No ATP hurts you!")
            // }

            // Microbe takes 4% of max hp per second in damage
            MicrobeOperations::damage(world, microbeEntity,
                    0.04  * microbeComponent.maxHitpoints, "atpDamage");
        }
    }

    void divide(ObjectID microbeEntity)
    {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        // auto soundSourceComponent = world.GetComponent_SoundSourceComponent(microbeEntity);
        auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);
        auto position = world.GetComponent_Position(microbeEntity);
        auto rigidBodyComponent = world.GetComponent_Physics(microbeEntity);


        // Create the one daughter cell.
        auto copyEntity = MicrobeOperations::spawnMicrobe(world, position._Position,
            microbeComponent.species.name, true);

        // Grab its microbe_component
        MicrobeComponent@ microbeComponentCopy = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(copyEntity));

        // Separate the two cells.
        position._Position = Float3(position._Position.X +
            membraneComponent.calculateEncompassingCircleRadius(),
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
                    amount / 2.0f /*, false*/ );
                // Not sure what the false here means, it wasn't a
                // parameter in the original lua function so it did
                // nothing even then?
                MicrobeOperations::storeCompound(world, copyEntity, compoundID,
                    amount / 2.0f, false);
            }
        }

        microbeComponent.reproductionStage = 0;
        microbeComponentCopy.reproductionStage = 0;

        world.Create_SpawnedComponent(copyEntity, MICROBE_SPAWN_RADIUS * MICROBE_SPAWN_RADIUS);

        // Play the split sound
        MicrobeOperations::playSoundWithDistance(world, "Data/Sound/soundeffects/reproduction.ogg",microbeEntity);
    }

    // Copies this microbe (if this isn't the player). The new microbe
    // will not have the stored compounds of this one.
    void readyToReproduce(ObjectID microbeEntity)
    {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        if(microbeComponent.isPlayerMicrobe){
            // The player doesn't split automatically
            microbeComponent.reproductionStage = 3;
            showReproductionDialog(world);
        } else {

            // Return the first cell to its normal, non duplicated cell arrangement.
            auto species = MicrobeOperations::getSpecies(world, microbeEntity);
            if (species !is null)
            {
                auto playerSpecies = MicrobeOperations::getSpecies(world, "Default");
                if (!microbeComponent.isPlayerMicrobe &&
                    microbeComponent.species !is playerSpecies)
                {
                    MicrobeOperations::alterSpeciesPopulation(species,
                        CREATURE_REPRODUCE_POPULATION_GAIN, "reproduced");
                }

                Species::applyTemplate(world, microbeEntity,
                    MicrobeOperations::getSpecies(world, microbeEntity));

                divide(microbeEntity);

            } else {
                // It's extinct and can't split
                microbeComponent.reproductionStage = 3;
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
