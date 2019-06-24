#include "configs.as"

// For system registering
#include "microbe.as"
#include "microbe_stage_hud.as"
#include "microbe_operations.as"
#include "microbe_ai.as"
#include "biome.as"


// This is a helper for calling all the setup functions at the same time
// This is the one called from C++
void setupScriptsForWorld(CellStageWorld@ world)
{
    setupSpecies(world);
    setupSystemsForWorld(world);
    setupSpawnSystem(world);
}

//! Server variant of setupScriptsForWorld
void setupScriptsForWorld_Server(CellStageWorld@ world)
{
    setupSpecies(world);
    setupSystemsForWorld_Server(world);
    // setupSpawnSystem_Server(world);
}

//! Server variant of setupScriptsForWorld
void setupScriptsForWorld_Client(CellStageWorld@ world)
{
    setupSpecies(world);
    setupSystemsForWorld_Client(world);
}

// This function should be the entry point for all player initial-species generation
// For now, it can go through the XML and instantiate all the species, but later this
// would be all procedural.
// Currently this goes through STARTER_MICROBES (defined in config.as) and makes entities with
// SpeciesComponents with the properties of the species
// The SpeciesSystem handles creating AI species
void setupSpecies(CellStageWorld@ world)
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    auto keys = STARTER_MICROBES.getKeys();

    for(uint i = 0; i < keys.length(); ++i){

        const string name = keys[i];

        MicrobeTemplate@ data = cast<MicrobeTemplate@>(STARTER_MICROBES[name]);

        ObjectID entity = Species::createSpecies(world, name, data);

        LOG_INFO("created starter microbe \"" + name + "\", species entity = " + entity);
    }

    LOG_INFO("setupSpecies created " + keys.length() + " species");
}

ScriptComponent@ MicrobeComponentFactory(GameWorld@ world)
{
    return MicrobeComponent();
}

ScriptComponent@ MicrobeAIControllerComponentFactory(GameWorld@ world)
{
    return MicrobeAIControllerComponent();
}

//! This function instantiates all script system types for a world
//! and registers all the microbe components that are defined in scripts to work
//! in a world
void setupSystemsForWorld(CellStageWorld@ world)
{
    // Fail if compound registry is empty (hud system caches the compound ids on startup) //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);
    world.RegisterScriptComponentType("MicrobeAIControllerComponent",
        @MicrobeAIControllerComponentFactory);

    // Add any new systems and component types that are defined in scripts here
    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
    world.RegisterScriptSystem("SpeciesSystem", SpeciesSystem());
    world.RegisterScriptSystem("MicrobeAISystem", MicrobeAISystem());
}

//! Server variant of setupSystemsForWorld
void setupSystemsForWorld_Server(CellStageWorld@ world)
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);
    world.RegisterScriptComponentType("MicrobeAIControllerComponent",
        @MicrobeAIControllerComponentFactory);

    // Add any new systems and component types that are defined in scripts here
    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("SpeciesSystem", SpeciesSystem());
    world.RegisterScriptSystem("MicrobeAISystem", MicrobeAISystem());
}

//! Client variant of setupSystemsForWorld
void setupSystemsForWorld_Client(CellStageWorld@ world)
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);

    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
}


//! This spawns the player
void setupPlayer(CellStageWorld@ world)
{
    assert(world !is null);
    setRandomBiome(world);
    GetThriveGame().playerData().lockedMap().addLock("Toxin");
    GetThriveGame().playerData().lockedMap().addLock("chloroplast");

    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");

    GetThriveGame().playerData().setActiveCreature(microbe);
}

//! This spawns a player in multiplayer
ObjectID spawnPlayer_Server(CellStageWorld@ world)
{
    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");
    return microbe;
}

//! This handles making a cell out of an entity received from the server
void setupClientSideReceivedCell(CellStageWorld@ world, ObjectID entity)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Create(entity));

    auto speciesName = "Default";

    auto speciesEntity = findSpeciesEntityByName(world, speciesName);

    assert(speciesEntity != NULL_OBJECT);

    auto species = world.GetComponent_SpeciesComponent(speciesEntity);

    assert(species !is null);

    microbeComponent.init(entity, true, species);

    auto shape = world.GetPhysicalWorld().CreateCompound();
    Species::applyTemplate(world, entity, species, shape);

    auto rigidBody = world.GetComponent_Physics(entity);

    MicrobeOperations::_applyMicrobeCollisionShape(world, rigidBody, microbeComponent, shape);

    microbeComponent.initialized = true;
}


// TODO: move this somewhere
// This is called from c++ system PlayerMicrobeControlSystem
void applyCellMovementControl(CellStageWorld@ world, ObjectID entity,
    const Float3 &in movement, const Float3 &in lookPosition)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));

    if(microbeComponent is null){
        return;
    }

    if(!microbeComponent.dead){
        microbeComponent.facingTargetPoint = lookPosition;
        microbeComponent.movementDirection = movement;
    }
}

// Activate Engulf Mode
void applyEngulfMode(CellStageWorld@ world, ObjectID entity)
{
    MicrobeOperations::toggleEngulfMode(world, entity);
}

// Player shoot toxin
void playerShootToxin(CellStageWorld@ world, ObjectID entity)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));
    CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
    MicrobeOperations::emitAgent(world, entity, oxytoxyId, 10.0f, 400*10.0f);
}

void onReturnFromEditor(CellStageWorld@ world)
{
    // Increase the population by 30 and increase the generation
    auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
    playerSpecies.population += 30;
    ++playerSpecies.generation;

    // Call event that checks win conditions
    GenericEvent@ event = GenericEvent("CheckWin");
    NamedVars@ vars = event.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("generation", playerSpecies.generation));
    vars.AddValue(ScriptSafeVariableBlock("population", playerSpecies.population));
    GetEngine().GetEventHandler().CallEvent(event);

    // The editor changes the cell template for the species so we won't have to do that here
    const auto player = GetThriveGame().playerData().activeCreature();
    auto pos = world.GetComponent_Position(player);

    assert(pos !is null);

    // Spawn another cell from the player species
    SpeciesComponent@ ourActualSpecies = MicrobeOperations::getSpeciesComponent(world, player);
    auto membraneComponent = world.GetComponent_MembraneComponent(player);

    // Can probabbly wrap this into the usual init to keep things clean

    PlayerSpeciesSpawner factory("Default");

    // Offset between cells
    pos._Position.X += membraneComponent.calculateEncompassingCircleRadius();
    pos._Position.Z += membraneComponent.calculateEncompassingCircleRadius();
    pos.Marked = true;

    auto spawned = factory.factorySpawn(world, pos._Position);

    LOG_WRITE("TODO: the spawned cell from the player species from the editor split will "
        "never be despawned");


    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(player));

    // Reset the player cell to be the same as the species template
    // This also sets the processor component
    Species::restoreOrganelleLayout(world, player, microbeComponent, playerSpecies);

    // Reset Players reproduction
    microbeComponent.reproductionStage = 0;

    // Halve the players Compounds
    for(uint64 compoundID = 0; compoundID <
            SimulationParameters::compoundRegistry().getSize(); ++compoundID)
    {
        auto amount = MicrobeOperations::getCompoundAmount(world, player,
            compoundID);

        if(amount != 0){
            MicrobeOperations::takeCompound(world, player, compoundID,
                amount / 2.0f /*, false*/ );
        }
    }

}

//
// TODO: also put these physics callback somewhere more sensible (maybe physics_callbacks.as?)
//

void cellHitFloatingOrganelle(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    // Determine which is the organelle
    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    auto model = asCellWorld.GetComponent_Model(firstEntity);
    auto floatingEntity = firstEntity;
    auto cellEntity = secondEntity;

    // Cell doesn't have a model
    if(model is null){

        @model = asCellWorld.GetComponent_Model(secondEntity);
        floatingEntity = secondEntity;
        cellEntity = firstEntity;
    }

    // // TODO: use this to detect stuff
    // LOG_INFO("Model: " + model.GraphicalObject.getMesh().getName());
    // LOG_INFO("TODO: organelle unlock progress if cell: " + cellEntity + " is the player");

    world.QueueDestroyEntity(floatingEntity);
}


// Used for chunks
void cellHitEngulfable(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    // Determine which is the chunk
    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    auto model = asCellWorld.GetComponent_Model(firstEntity);
    auto floatingEntity = firstEntity;
    auto cellEntity = secondEntity;

    // Cell doesn't have a model
    if(model is null){

        @model = asCellWorld.GetComponent_Model(secondEntity);
        floatingEntity = secondEntity;
        cellEntity = firstEntity;
    }
    auto microbeComponent = MicrobeOperations::getMicrobeComponent(asCellWorld,cellEntity);

    auto engulfableComponent = asCellWorld.GetComponent_EngulfableComponent(floatingEntity);

    auto compoundBagComponent = asCellWorld.GetComponent_CompoundBagComponent(cellEntity);

    auto floatBag = asCellWorld.GetComponent_CompoundBagComponent(floatingEntity);

    if (microbeComponent !is null && engulfableComponent !is null
        && compoundBagComponent !is null && floatBag !is null)
        {
        if (microbeComponent.engulfMode && microbeComponent.totalHexCountCache >=
            engulfableComponent.getSize()*ENGULF_HP_RATIO_REQ)
            {
            uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
            for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
                CompoundId realCompoundId = compoundId;
                double amountToTake =
                    floatBag.takeCompound(realCompoundId,floatBag.getCompoundAmount(realCompoundId));
                // Right now you get way too much compounds for engulfing the things but hey
                compoundBagComponent.giveCompound(realCompoundId, (amountToTake/CHUNK_ENGULF_COMPOUND_DIVISOR));
            }
            world.QueueDestroyEntity(floatingEntity);
            }
        }
}

// Used for chunks that also can damage
void cellHitDamageChunk(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    // Determine which is the chunk
    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    auto model = asCellWorld.GetComponent_Model(firstEntity);
    auto floatingEntity = firstEntity;
    auto cellEntity = secondEntity;
    bool disappear=false;
    // Cell doesn't have a model
    if(model is null){
        @model = asCellWorld.GetComponent_Model(secondEntity);
        floatingEntity = secondEntity;
        cellEntity = firstEntity;
    }
    auto damage = asCellWorld.GetComponent_DamageOnTouchComponent(floatingEntity);
    MicrobeComponent@ microbeComponent = MicrobeOperations::getMicrobeComponent(asCellWorld,cellEntity);

    if (damage !is null && microbeComponent !is null){
        if (damage.getDeletes() && !microbeComponent.dead){
            MicrobeOperations::damage(asCellWorld, cellEntity, double(damage.getDamage()), "toxin");
            disappear=true;
        }
        else if (!damage.getDeletes() && !microbeComponent.dead){
            MicrobeOperations::damage(asCellWorld, cellEntity, double(damage.getDamage()), "chunk");
        }
    }

    // Do engulfing stuff in the case that we have an engulfable component
    auto engulfableComponent = asCellWorld.GetComponent_EngulfableComponent(floatingEntity);
    auto compoundBagComponent = asCellWorld.GetComponent_CompoundBagComponent(cellEntity);
    auto floatBag = asCellWorld.GetComponent_CompoundBagComponent(floatingEntity);

    if (microbeComponent !is null && engulfableComponent !is null
        && compoundBagComponent !is null && floatBag !is null)
        {
        if (microbeComponent.engulfMode && microbeComponent.totalHexCountCache >=
            engulfableComponent.getSize()*ENGULF_HP_RATIO_REQ)
            {
            uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
            for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
                CompoundId realCompoundId = compoundId;
                double amountToTake =
                    floatBag.takeCompound(realCompoundId,floatBag.getCompoundAmount(realCompoundId));
                // Right now you get way too much compounds for engulfing the things but hey
                compoundBagComponent.giveCompound(realCompoundId, (amountToTake/CHUNK_ENGULF_COMPOUND_DIVISOR));
            }
            disappear=true;
            }
        }

    if (disappear){
        world.QueueDestroyEntity(floatingEntity);
    }
}


// Cell Hit Oxytoxy
// We can make this generic using the dictionary in agents.as
// eventually, but for now all we have is oxytoxy
void cellHitAgent(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    // Determine which is the organelle
    CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);

    auto model = asCellWorld.GetComponent_Model(firstEntity);
    auto floatingEntity = firstEntity;
    auto cellEntity = secondEntity;

    // Cell doesn't have a model
    if(model is null){
        @model = asCellWorld.GetComponent_Model(secondEntity);
        floatingEntity = secondEntity;
        cellEntity = firstEntity;
    }

    if(model is null){

        LOG_ERROR("cellHitAgent: neither body has a Model");
        return;
    }


    AgentProperties@ propertiesComponent =
        asCellWorld.GetComponent_AgentProperties(floatingEntity);

    MicrobeComponent@ microbeComponent = MicrobeOperations::getMicrobeComponent(asCellWorld,cellEntity);

    if (propertiesComponent !is null && microbeComponent !is null){
        if (propertiesComponent.getSpeciesName() != microbeComponent.speciesName && !microbeComponent.dead){
            MicrobeOperations::damage(asCellWorld, cellEntity, double(OXY_TOXY_DAMAGE), "toxin");
            world.QueueDestroyEntity(floatingEntity);
            }
        }

}

void cellOnCellActualContact(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    //We are going to cheat here and set variables when you hit something, and hopefully the AABB will take care of the rest
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

        if(firstMicrobeComponent.isBacteria)
            firstMicrobeComponentHexCount /= 2;

        if(secondMicrobeComponent.isBacteria)
            secondMicrobeComponentHexCount /= 2;

        if (firstMicrobeComponent.engulfMode)
        {
            if(firstMicrobeComponentHexCount >
                (ENGULF_HP_RATIO_REQ * secondMicrobeComponentHexCount) &&
                firstMicrobeComponent.dead == false && secondMicrobeComponent.dead == false)
            {
                secondMicrobeComponent.isBeingEngulfed = true;
                secondMicrobeComponent.hostileEngulfer = firstEntity;
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
                firstMicrobeComponent.hostileEngulfer = secondEntity;
                firstMicrobeComponent.wasBeingEngulfed = true;
            }
        }
    }
}

// Targets player cell and kills it (For suicide button)
void killPlayerCellClicked(CellStageWorld@ world)
{
    auto playerEntity = GetThriveGame().playerData().activeCreature();
    //kill it hard
    MicrobeOperations::damage(world, playerEntity, 9999.0f, "suicide");
}

// Returns false if being engulfed, probabbly also damages the cell being
// engulfed, we should probabbly check cell size and such here aswell.
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

        if(firstMicrobeComponent.isBacteria)
            firstMicrobeComponentHexCount /= 2;

        if(secondMicrobeComponent.isBacteria)
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

// Returns false if you hit an agent and calls the hit effect code
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
            if (firstPropertiesComponent.getSpeciesName()==secondMicrobeComponent.speciesName ||
            firstPropertiesComponent.getParentEntity()==secondEntity)
            {
                shouldCollide = false;
                return shouldCollide;
            }
        }
        else if (secondPropertiesComponent !is null && firstMicrobeComponent !is null)
        {
            if (secondPropertiesComponent.getSpeciesName()==firstMicrobeComponent.speciesName ||
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

void createAgentCloud(CellStageWorld@ world, CompoundId compoundId,
    Float3 pos, Float3 direction, float amount, float lifetime,
    string speciesName, ObjectID creatorEntity)
{
    auto normalizedDirection = direction.Normalize();
    auto agentEntity = world.CreateEntity();

    auto position = world.Create_Position(agentEntity, pos + (direction * 1.5),
        bs::Quaternion(bs::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            bs::Vector3(0,1, 0)));

    // Agent
    auto agentProperties = world.Create_AgentProperties(agentEntity);
    agentProperties.setSpeciesName(speciesName);
    agentProperties.setParentEntity(creatorEntity);
    agentProperties.setAgentType("oxytoxy");

    auto rigidBody = world.Create_Physics(agentEntity, position);


    auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
        world.GetPhysicalWorld().CreateSphere(HEX_SIZE), 0.5,
        world.GetPhysicalMaterial("agentCollision"));

    body.ConstraintMovementAxises();

    // TODO: physics property applying here as well
    // rigidBody.properties.friction = 0.4;
    // rigidBody.properties.linearDamping = 0.4;

    body.SetVelocity(normalizedDirection * AGENT_EMISSION_VELOCITY);
    rigidBody.JumpTo(position);
    auto sceneNode = world.Create_RenderNode(agentEntity);
    auto model = world.Create_Model(agentEntity, "oxytoxy_fluid.mesh",
        getBasicMaterialWithTexture("oxytoxy_fluid.png"));

    // // Need to set the tint
    // model.GraphicalObject.setCustomParameter(1, bs::Vector4(1, 1, 1, 1));

    auto timedLifeComponent = world.Create_TimedLifeComponent(agentEntity, int(lifetime));
}

ObjectID createChunk(CellStageWorld@ world, uint chunkId,  Float3 pos)
{
    auto biome = getCurrentBiome();
    // chunk
    ObjectID chunkEntity = world.CreateEntity();
    const ChunkData@ chunk = biome.getChunk(chunkId);

    //Position and render node
    auto position = world.Create_Position(chunkEntity, pos,
        bs::Quaternion(bs::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            bs::Vector3(0,1,1)));


    auto renderNode = world.Create_RenderNode(chunkEntity);
    // Grab scale from json
    double chunkScale = chunk.chunkScale;
    renderNode.Scale = Float3(chunkScale, chunkScale, chunkScale);
    renderNode.Marked = true;
    renderNode.Node.setOrientation(bs::Quaternion(
            bs::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            bs::Vector3(0,1,1)));

    renderNode.Node.setPosition(pos);

    //Grab data
    double ventAmount= chunk.ventAmount;
    bool dissolves=chunk.dissolves;
    int radius = chunk.radius;
    int mass = chunk.mass;
    int chunkSize = chunk.size;
    auto meshListSize = chunk.getMeshListSize();
    string mesh=chunk.getMesh(GetEngine().GetRandom().GetNumber(0,
            meshListSize-1))+".mesh";

    //Set things
    auto venter = world.Create_CompoundVenterComponent(chunkEntity);
    venter.setVentAmount(ventAmount);
    venter.setDoDissolve(dissolves);
    auto bag = world.Create_CompoundBagComponent(chunkEntity);
    auto engulfable = world.Create_EngulfableComponent(chunkEntity);
    engulfable.setSize(chunkSize);


    auto chunkCompounds = chunk.getCompoundKeys();
    //LOG_INFO("chunkCompounds.length = " + chunkCompounds.length());

    for(uint i = 0; i < chunkCompounds.length(); ++i){
        auto compoundId = SimulationParameters::compoundRegistry().getTypeData(chunkCompounds[i]).id;
        //LOG_INFO("got here:");
        // And register new
        const double amount = chunk.getCompound(chunkCompounds[i]).amount;
        //LOG_INFO("amount:"+amount);
        bag.setCompound(compoundId,amount);
    }

    auto model = world.Create_Model(chunkEntity, mesh, getBasicMaterialWithTexture(
            chunk.name + ".png"));

    // Fluid mechanics.
    world.Create_FluidEffectComponent(chunkEntity);

    // Rigid Body
    auto rigidBody = world.Create_Physics(chunkEntity, position);

    //chunk properties
    if (chunk.damages > 0.0f || chunk.deleteOnTouch){
        auto damager = world.Create_DamageOnTouchComponent(chunkEntity);
        damager.setDamage(chunk.damages);
        damager.setDeletes(chunk.deleteOnTouch);
        //Damage
        auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
            world.GetPhysicalWorld().CreateSphere(radius),mass,
            world.GetPhysicalMaterial("chunkDamageMaterial"));

        body.ConstraintMovementAxises();
    }
    else {
        auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
            world.GetPhysicalWorld().CreateSphere(radius),mass,
            //engulfable
            world.GetPhysicalMaterial("engulfableMaterial"));
        body.ConstraintMovementAxises();
    }

    rigidBody.JumpTo(position);

    return chunkEntity;
}

void resetWorld(CellStageWorld@ world)
{
    // We have to call this so we get a new batch of species and so
    // that everything spawns properly.  I worry that not just
    // creating a whole new world will cause tons of problems later
    // aswell , for example when we want new planets and everything,
    // but eh. We can call it all in this method.

    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).resetAutoEvo();
}

//! AI species are spawned by Species in species_system
class PlayerSpeciesSpawner{
    PlayerSpeciesSpawner(const string &in speciesName){

        this.species = speciesName;
    }

    private string species;

    ObjectID factorySpawn(CellStageWorld@ world, Float3 pos){

        LOG_INFO("Spawning a cell from player species: " + species);
        return MicrobeOperations::spawnMicrobe(world, pos, species,
        // ai controlled
        true);
    }
}

// TODO: the player species handling would be more logically placed if
// it was in SpeciesSystem, so move it there
void setupSpawnSystem(CellStageWorld@ world){
    //spawn code is here, if it isnt obvious by the name
    SpawnSystem@ spawnSystem = world.GetSpawnSystem();

    // Clouds are handled by biome.as
    LOG_INFO("setting up player species to spawn");
    auto keys = STARTER_MICROBES.getKeys();
    for(uint n = 0; n < keys.length(); n++)
    {
        const string name = keys[n];

        PlayerSpeciesSpawner@ spawner = PlayerSpeciesSpawner(name);

        SpawnFactoryFunc@ factory = SpawnFactoryFunc(spawner.factorySpawn);

        LOG_INFO("adding spawn player species: " + name);

        const auto spawnerId = spawnSystem.addSpawnType(
            factory,
            //spawnDensity should depend on population
            DEFAULT_PLAYER_SPAWN_DENSITY,
            MICROBE_SPAWN_RADIUS);
    }
}

