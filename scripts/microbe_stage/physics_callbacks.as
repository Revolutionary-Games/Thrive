// Callbacks for cell stage physics materials

// Used for chunks
bool cellHitEngulfable(GameWorld@ world,
    ObjectID otherEntity,
    ObjectID cellEntity,
    MicrobeComponent@ microbeComponent,
    const PhysicsShape@ cellShape,
    int cellSubCollision)
{
    const bool isPilus = cellShape.GetChildCustomTag(cellSubCollision) ==
        PHYSICS_PILUS_TAG;

    // Chunk can't be engulfed through a pilus
    if(isPilus)
        return false;

    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    auto engulfableComponent = asCellWorld.GetComponent_EngulfableComponent(otherEntity);

    auto compoundBagComponent = asCellWorld.GetComponent_CompoundBagComponent(cellEntity);

    auto floatBag = asCellWorld.GetComponent_CompoundBagComponent(otherEntity);

    // TODO: this code is almost an exact duplicate of block of code in cellHitDamageChunk
    if (microbeComponent !is null && engulfableComponent !is null
        && compoundBagComponent !is null && floatBag !is null)
    {
        if (microbeComponent.engulfMode && microbeComponent.totalHexCountCache >=
            engulfableComponent.getSize() * ENGULF_HP_RATIO_REQ)
        {
            uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
            for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
                CompoundId realCompoundId = compoundId;
                double amountToTake = floatBag.takeCompound(
                    realCompoundId, floatBag.getCompoundAmount(realCompoundId));
                // Right now you get way too much compounds for engulfing the things but hey
                compoundBagComponent.giveCompound(
                    realCompoundId, (amountToTake / CHUNK_ENGULF_COMPOUND_DIVISOR));
            }
            world.QueueDestroyEntity(otherEntity);
        }
    }

    return true;
}

// Used for chunks that also can damage
bool cellHitDamageChunk(GameWorld@ world,
    ObjectID otherEntity,
    ObjectID cellEntity,
    MicrobeComponent@ microbeComponent,
    const PhysicsShape@ cellShape,
    int cellSubCollision)
{
    const bool isPilus = cellShape.GetChildCustomTag(cellSubCollision) ==
        PHYSICS_PILUS_TAG;

    // Chunk can't hit through a pilus
    if(isPilus)
        return false;

    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    bool disappear = false;

    auto damage = asCellWorld.GetComponent_DamageOnTouchComponent(otherEntity);

    if (damage !is null && microbeComponent !is null){
        if (damage.getDeletes() && !microbeComponent.dead){
            MicrobeOperations::damage(asCellWorld, cellEntity, double(damage.getDamage()),
                "toxin");
            disappear = true;
        }
        else if (!damage.getDeletes() && !microbeComponent.dead){
            MicrobeOperations::damage(asCellWorld, cellEntity, double(damage.getDamage()),
                "chunk");
        }
    }

    // Do engulfing stuff in the case that the non-cell entity has an engulfable component
    auto engulfableComponent = asCellWorld.GetComponent_EngulfableComponent(otherEntity);
    auto compoundBagComponent = asCellWorld.GetComponent_CompoundBagComponent(cellEntity);
    auto floatBag = asCellWorld.GetComponent_CompoundBagComponent(otherEntity);

    if (microbeComponent !is null && engulfableComponent !is null
        && compoundBagComponent !is null && floatBag !is null)
    {
        if (microbeComponent.engulfMode && microbeComponent.totalHexCountCache >=
            engulfableComponent.getSize() * ENGULF_HP_RATIO_REQ)
        {
            uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
            for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
                CompoundId realCompoundId = compoundId;
                double amountToTake = floatBag.takeCompound(
                    realCompoundId, floatBag.getCompoundAmount(realCompoundId));
                // Right now you get way too much compounds for engulfing the things but hey
                compoundBagComponent.giveCompound(
                    realCompoundId, (amountToTake / CHUNK_ENGULF_COMPOUND_DIVISOR));
            }

            disappear = true;
        }
    }

    if (disappear){
        world.QueueDestroyEntity(otherEntity);
    }

    return true;
}

// Actual collision between agent and cell, applies damage and removes
// the agent if the hit was valid
bool cellHitAgent(GameWorld@ world,
    ObjectID otherEntity,
    ObjectID cellEntity,
    MicrobeComponent@ microbeComponent,
    const PhysicsShape@ cellShape,
    int cellSubCollision)
{
    const bool isPilus = cellShape.GetChildCustomTag(cellSubCollision) ==
        PHYSICS_PILUS_TAG;

    // Agent can't hit through a pilus
    if(isPilus)
        return false;

    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    AgentProperties@ propertiesComponent =
        asCellWorld.GetComponent_AgentProperties(otherEntity);

    if (propertiesComponent !is null){
        if (propertiesComponent.getSpeciesName() != microbeComponent.species.name &&
            !microbeComponent.dead){

            MicrobeOperations::damage(asCellWorld, cellEntity, double(OXY_TOXY_DAMAGE),
                "toxin");
            world.QueueDestroyEntity(otherEntity);
        }
    }

    return true;
}

bool cellOnCellActualContact(GameWorld@ world,
    const PhysicsShape@ firstShape,
    MicrobeComponent@ firstMicrobeComponent,
    const PhysicsShape@ secondShape,
    MicrobeComponent@ secondMicrobeComponent,
    int firstSubCollision, int secondSubCollision, float overlapAmount, bool handled)
{
    // Disallow cannibalism
    if(firstMicrobeComponent.species is secondMicrobeComponent.species)
        return true;

    // Handle stabbing
    const bool firstIsPilus = firstShape.GetChildCustomTag(firstSubCollision) ==
        PHYSICS_PILUS_TAG;
    const bool secondIsPilus = secondShape.GetChildCustomTag(secondSubCollision) ==
        PHYSICS_PILUS_TAG;

    // LOG_WRITE("first pilus: " + firstIsPilus + " second: " + secondIsPilus);

    if(firstIsPilus && secondIsPilus){
        // Pilus on pilus doesn't deal damage and you can't engulf
        // Maybe we should always return true here to prevent engulfing from happening
        // in any case. By not forcing true return here a subsequent call might allow
        // engulfing to happen (if the membranes are touching in addition to the piluses)
        return handled;
    } else if(firstIsPilus || secondIsPilus){

        // First attacking second OR second attacking first
        ObjectID target = firstIsPilus ? secondMicrobeComponent.microbeEntity :
            firstMicrobeComponent.microbeEntity;

        MicrobeOperations::damage(cast<CellStageWorld>(world),
            target,
            PILUS_BASE_DAMAGE * overlapAmount * PILUS_PENETRATION_DISTANCE_DAMAGE_MULTIPLIER,
            "pilus");

        return true;
    }

    // Engulf is handled just once
    if(handled)
        return true;

    // Get microbe sizes here
    int firstMicrobeComponentHexCount = firstMicrobeComponent.totalHexCountCache;
    int secondMicrobeComponentHexCount = secondMicrobeComponent.totalHexCountCache;

    if(firstMicrobeComponent.species.isBacteria)
        firstMicrobeComponentHexCount /= 2;

    if(secondMicrobeComponent.species.isBacteria)
        secondMicrobeComponentHexCount /= 2;

    if (firstMicrobeComponent.engulfMode)
    {
        if(firstMicrobeComponentHexCount >
            (ENGULF_HP_RATIO_REQ * secondMicrobeComponentHexCount) &&
            firstMicrobeComponent.dead == false && secondMicrobeComponent.dead == false)
        {
            secondMicrobeComponent.isBeingEngulfed = true;
            secondMicrobeComponent.hostileEngulfer = firstMicrobeComponent.microbeEntity;
            secondMicrobeComponent.wasBeingEngulfed = true;
        }
    }
    if (secondMicrobeComponent.engulfMode)
    {
        if(secondMicrobeComponentHexCount >
            (ENGULF_HP_RATIO_REQ * firstMicrobeComponentHexCount) &&
            secondMicrobeComponent.dead == false && firstMicrobeComponent.dead == false)
        {
            firstMicrobeComponent.isBeingEngulfed = true;
            firstMicrobeComponent.hostileEngulfer = secondMicrobeComponent.microbeEntity;
            firstMicrobeComponent.wasBeingEngulfed = true;
        }
    }

    return true;
}

// Checks if cells should collide or not and applies the being engulfed status
bool beingEngulfed(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    bool shouldCollide = false;

    // Grab the microbe components
    MicrobeComponent@ firstMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(firstEntity));
    MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(secondEntity));
    //Check if they were null *because if null the cast failed)
    if (firstMicrobeComponent !is null && secondMicrobeComponent !is null)
    {
        // Get microbe sizes here
        int firstMicrobeComponentHexCount = firstMicrobeComponent.totalHexCountCache;
        int secondMicrobeComponentHexCount = secondMicrobeComponent.totalHexCountCache;

        if(firstMicrobeComponent.species.isBacteria)
            firstMicrobeComponentHexCount /= 2;

        if(secondMicrobeComponent.species.isBacteria)
            secondMicrobeComponentHexCount /= 2;

        // If either cell is engulfing we need to do things
        //return false;
        //LOG_INFO(""+firstMicrobeComponent.engulfMode);
        // LOG_INFO(""+secondMicrobeComponent.engulfMode);
        if (firstMicrobeComponent.engulfMode)
        {
            if(firstMicrobeComponentHexCount >
                (ENGULF_HP_RATIO_REQ * secondMicrobeComponentHexCount) &&
                firstMicrobeComponent.dead == false && secondMicrobeComponent.dead == false)
            {
                secondMicrobeComponent.isBeingEngulfed = true;
                secondMicrobeComponent.hostileEngulfer = firstEntity;
                secondMicrobeComponent.wasBeingEngulfed = true;
                firstMicrobeComponent.isCurrentlyEngulfing = true;
                return false;
            }
        }
        if (secondMicrobeComponent.engulfMode)
        {
            if(secondMicrobeComponentHexCount >
                (ENGULF_HP_RATIO_REQ * firstMicrobeComponentHexCount) &&
                secondMicrobeComponent.dead == false && firstMicrobeComponent.dead == false)
            {
                firstMicrobeComponent.isBeingEngulfed = true;
                firstMicrobeComponent.hostileEngulfer = secondEntity;
                firstMicrobeComponent.wasBeingEngulfed = true;
                secondMicrobeComponent.isCurrentlyEngulfing = true;
                return false;
            }
        }

        if (secondMicrobeComponent.hostileEngulfer == firstEntity || firstMicrobeComponent.hostileEngulfer == secondEntity) {
            return false;
        }
    }

    return true;
}

//! \brief Skips collision if agent shouldn't damange the given cell
bool hitAgent(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    // TODO: why is this used here when each place that sets this return immediately?
    bool shouldCollide = true;

    // Grab the microbe components
    MicrobeComponent@ firstMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(firstEntity));
    MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(secondEntity));
    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);
    AgentProperties@ firstPropertiesComponent =
        asCellWorld.GetComponent_AgentProperties(firstEntity);
    AgentProperties@ secondPropertiesComponent =
        asCellWorld.GetComponent_AgentProperties(secondEntity);

    if (firstPropertiesComponent !is null || secondPropertiesComponent !is null)
    {
        if (firstPropertiesComponent !is null && secondMicrobeComponent !is null)
        {
            if (firstPropertiesComponent.getSpeciesName()==secondMicrobeComponent.species.name ||
                firstPropertiesComponent.getParentEntity()==secondEntity)
            {
                shouldCollide = false;
                return shouldCollide;
            }
        }
        else if (secondPropertiesComponent !is null && firstMicrobeComponent !is null)
        {
            if (secondPropertiesComponent.getSpeciesName()==firstMicrobeComponent.species.name ||
                secondPropertiesComponent.getParentEntity()==firstEntity)
            {
                shouldCollide = false;
                return shouldCollide;
            }
        }
    }

    // Check if one is a microbe, and the other is not
    if ((firstMicrobeComponent !is null || secondMicrobeComponent !is null) &&
        !(firstMicrobeComponent !is null && secondMicrobeComponent !is null))
    {
        shouldCollide = true;
    }

    return shouldCollide;
}
