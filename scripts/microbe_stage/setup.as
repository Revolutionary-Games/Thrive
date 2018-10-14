#include "configs.as"

// For system registering
#include "microbe.as"
#include "microbe_stage_hud.as"
#include "microbe_operations.as"
#include "microbe_ai.as"
#include "biome.as"


// This is a helper for calling all the setup functions at the same time
// This is the one called from C++
void setupScriptsForWorld(CellStageWorld@ world){
    setupSpecies(world);
    setupSystemsForWorld(world);
    setupSpawnSystem(world);
    setupSound(world);
}

// This function should be the entry point for all player initial-species generation
// For now, it can go through the XML and instantiate all the species, but later this
// would be all procedural.
// Currently this goes through STARTER_MICROBES (defined in config.as) and makes entities with
// SpeciesComponents with the properties of the species
// The SpeciesSystem handles creating AI species
void setupSpecies(CellStageWorld@ world){

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

ScriptComponent@ MicrobeComponentFactory(GameWorld@ world){

    return MicrobeComponent();
}

ScriptComponent@ MicrobeAIControllerComponentFactory(GameWorld@ world){

    return MicrobeAIControllerComponent();
}

//! This function instantiates all script system types for a world
//! and registers all the microbe components that are defined in scripts to work
//! in a world
void setupSystemsForWorld(CellStageWorld@ world){

    // Fail if compound registry is empty (hud system caches the compound ids on startup) //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);
    world.RegisterScriptComponentType("MicrobeAIControllerComponent",
        @MicrobeAIControllerComponentFactory);

    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
    world.RegisterScriptSystem("SpeciesSystem", SpeciesSystem());
    world.RegisterScriptSystem("MicrobeAISystem", MicrobeAISystem());

    // TODO: add the rest of the systems and component types that are defined in scripts here
}


//! This spawns the player
void setupPlayer(CellStageWorld@ world){
    assert(world !is null);
    setRandomBiome(world);
    GetThriveGame().playerData().lockedMap().addLock("Toxin");
    GetThriveGame().playerData().lockedMap().addLock("chloroplast");

    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false, PLAYER_NAME);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");
    // TODO: powerupable
    //microbe.collisionHandler.addCollisionGroup("powerupable");

    GetThriveGame().playerData().setActiveCreature(microbe);

    // // Testing spawning extra cell
    // MicrobeOperations::spawnMicrobe(world, Float3(10, 0, 0), "Default",
    //     false, "extra player");

    // // Test model spawn
    // auto testModel = world.CreateEntity();

    // auto position = world.Create_Position(testModel, Float3(5, 35, 5),
    //     Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
    //         Ogre::Vector3(0, 1, 0)));
    // auto sceneNode = world.Create_RenderNode(testModel);
    // auto model = world.Create_Model(testModel, sceneNode.Node, "UnitCube.mesh");
}


// TODO: move this somewhere
// This is called from c++ system PlayerMicrobeControlSystem
void applyCellMovementControl(CellStageWorld@ world, ObjectID entity, const Float3 &in movement,
    const Float3 &in lookPosition)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));

    if(!microbeComponent.dead){
        microbeComponent.facingTargetPoint = lookPosition;
        microbeComponent.movementDirection = movement;
    }
}

// Activate Engulf Mode
void applyEngulfMode(CellStageWorld@ world, ObjectID entity){
    MicrobeOperations::toggleEngulfMode(world, entity);
}

// Player shoot toxin
void playerShootToxin(CellStageWorld@ world, ObjectID entity){
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));
    CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
    MicrobeOperations::emitAgent(world,entity, oxytoxyId,10.0f,400*10.0f);
}

void onReturnFromEditor(CellStageWorld@ world)
{
    // Increase the population by 30 and increase the generation
    auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
    playerSpecies.population += 30;
    ++playerSpecies.generation;

    //Call event that checks win conditions
    GenericEvent@ event = GenericEvent("CheckWin");
    NamedVars@ vars = event.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("generation", playerSpecies.generation));
    GetEngine().GetEventHandler().CallEvent(event);

    // The editor changes the cell template for the species so we won't have to do that here
    const auto player = GetThriveGame().playerData().activeCreature();
    auto pos = world.GetComponent_Position(player);

    assert(pos !is null);

    // Spawn another cell from the player species
    PlayerSpeciesSpawner factory("Default");
    auto spawned = factory.factorySpawn(world, pos._Position);

    LOG_WRITE("TODO: the spawned cell from the player species from the editor split will "
        "never be despawned");

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(player));

    // Reset the player cell to be the same as the species template
    Species::restoreOrganelleLayout(world, player, microbeComponent, playerSpecies);

    // TODO: the player compound amount should be halved
}

// TODO: also put these physics callback somewhere
void cellHitFloatingOrganelle(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity){

    //LOG_INFO("Cell hit a floating organelle: object ids: " + firstEntity + " and " +
    //    secondEntity);

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

    // TODO: use this to detect stuff
    LOG_INFO("Model: " + model.GraphicalObject.getMesh().getName());
    LOG_INFO("TODO: organelle unlock progress if cell: " + cellEntity + " is the player");

    world.QueueDestroyEntity(floatingEntity);
}

// Cell Hit Oxytoxy
// We can make this generic using the dictionary in agents.as eventually, but for now all we have is oxytoxy
void cellHitAgent(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity){

    //LOG_INFO("Cell hit an agaent: object ids: " + firstEntity + " and " +
    //    secondEntity);

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

    MicrobeOperations::damage(asCellWorld, cellEntity, double(OXY_TOXY_DAMAGE), "toxin");

    world.QueueDestroyEntity(floatingEntity);

}

// SO what should we use this method for?
void cellOnCellActualContact(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity){
    // LOG_INFO("Cell hit another cell, thats cool i guess");
}

// Targets player cell and kills it (For suicide button)
void killPlayerCellClicked(CellStageWorld@ world){
    auto playerEntity = GetThriveGame().playerData().activeCreature();
    //kill it hard
     MicrobeOperations::damage(world,playerEntity, 9999.0f, "suicide");
}

// Returns 0 if being engulfed, probabbly also damages the cell being
// engulfed, we should probabbly check cell size and such here aswell.
int beingEngulfed(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    int shouldCollide = 1;


    // Grab the microbe components
    MicrobeComponent@ firstMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(firstEntity));
    MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(secondEntity));
    //Check if they were null *because if null the cast failed)
    if (firstMicrobeComponent !is null && secondMicrobeComponent !is null)
    {
    // Get microbe sizes here
    int firstMicrobeComponentOrganelles = firstMicrobeComponent.organelles.length();
    int secondMicrobeComponentOrganelles = secondMicrobeComponent.organelles.length();

    // If either cell is engulfing we need to do things
    if (firstMicrobeComponent.engulfMode)
    {
        if(firstMicrobeComponent.engulfMode && firstMicrobeComponentOrganelles >
            (ENGULF_HP_RATIO_REQ * secondMicrobeComponentOrganelles) &&
            firstMicrobeComponent.dead == false && secondMicrobeComponent.dead == false)
        {

            secondMicrobeComponent.isBeingEngulfed = true;
            secondMicrobeComponent.hostileEngulfer = firstEntity;
            secondMicrobeComponent.wasBeingEngulfed = true;

            if(!firstMicrobeComponent.isCurrentlyEngulfing){
                // We have just started engulfing
                secondMicrobeComponent.movementFactor = secondMicrobeComponent.movementFactor /
                    ENGULFED_MOVEMENT_DIVISION;
            }

            firstMicrobeComponent.isCurrentlyEngulfing = true;
            shouldCollide = 0;
        }
    }

    // Looks like it doenst work sometimes if i dont check the second
    // one aswell, so i have to, theres gotta be a way to improve this
    if (secondMicrobeComponent.engulfMode)
    {
        if(secondMicrobeComponent.engulfMode && secondMicrobeComponentOrganelles >
            (ENGULF_HP_RATIO_REQ * firstMicrobeComponentOrganelles) &&
            secondMicrobeComponent.dead == false && firstMicrobeComponent.dead == false)
        {

            firstMicrobeComponent.isBeingEngulfed = true;
            firstMicrobeComponent.hostileEngulfer = secondEntity;
            firstMicrobeComponent.wasBeingEngulfed = true;

            if(!secondMicrobeComponent.isCurrentlyEngulfing){
                //We have just started engulfing
                firstMicrobeComponent.movementFactor = firstMicrobeComponent.movementFactor /
                    ENGULFED_MOVEMENT_DIVISION;
            }

            secondMicrobeComponent.isCurrentlyEngulfing = true;
            shouldCollide = 0;
        }
    }
    }

    return shouldCollide;
}

// Returns 0 if you hit an agent and calls the code
int hitAgent(GameWorld@ world, ObjectID firstEntity, ObjectID secondEntity)
{
    int shouldCollide = 1;


    // Grab the microbe components
    MicrobeComponent@ firstMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(firstEntity));
    MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(secondEntity));
     CellStageWorld@ asCellWorld = cast<CellStageWorld>(world);
    AgentProperties@ firstPropertiesComponent = asCellWorld.GetComponent_AgentProperties(firstEntity);
    AgentProperties@ secondPropertiesComponent = asCellWorld.GetComponent_AgentProperties(secondEntity);

    if (firstPropertiesComponent !is null || secondPropertiesComponent !is null)
    {
        if (firstPropertiesComponent !is null && secondMicrobeComponent !is null)
        {
            if (firstPropertiesComponent.getSpeciesName()==secondMicrobeComponent.speciesName)
                {
                shouldCollide=0;
                return shouldCollide;
                }
        }
        else if (secondPropertiesComponent !is null && firstMicrobeComponent !is null)
        {
            if (secondPropertiesComponent.getSpeciesName()==firstMicrobeComponent.speciesName)
                {
                shouldCollide=0;
                return shouldCollide;
                }
        }
    }
    // Check if one is a microbe, and the other is not
    if ((firstMicrobeComponent !is null || secondMicrobeComponent !is null) && !(firstMicrobeComponent !is null && secondMicrobeComponent !is null))
        {
        //LOG_INFO("called toxin callback");
        cellHitAgent(world,firstEntity,secondEntity);
        shouldCollide=0;
        }

    return shouldCollide;
}

void createAgentCloud(CellStageWorld@ world, CompoundId compoundId, Float3 pos,
    Float3 direction, float amount, float lifetime, string speciesName)
{
    auto normalizedDirection = direction.Normalize();
    auto agentEntity = world.CreateEntity();

    // auto reactionHandler = CollisionComponent()
    // reactionHandler.addCollisionGroup("agent")
    auto position = world.Create_Position(agentEntity, pos + (direction * 1.5),
        Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            Ogre::Vector3(0, 1, 0)));




    auto rigidBody = world.Create_Physics(agentEntity, world, position, null);


   auto agentProperties = world.Create_AgentProperties(agentEntity);
   agentProperties.setSpeciesName(speciesName);
   agentProperties.setAgentType("oxytoxy");

    rigidBody.SetCollision(world.GetPhysicalWorld().CreateSphere(HEX_SIZE));
    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(), world.GetPhysicalMaterial("agentCollision"));
    // Agent

    rigidBody.CreatePlaneConstraint(world.GetPhysicalWorld(), Float3(0, 1, 0));

    rigidBody.SetMass(1.0);

    // TODO: physics property applying here as well
    // rigidBody.properties.friction = 0.4;
    // rigidBody.properties.linearDamping = 0.4;

    rigidBody.SetVelocity(normalizedDirection * AGENT_EMISSION_VELOCITY);

    rigidBody.JumpTo(position);
    auto sceneNode = world.Create_RenderNode(agentEntity);
    auto model = world.Create_Model(agentEntity, sceneNode.Node, "oxytoxy.mesh");

    // Need to set the tint
    model.GraphicalObject.setCustomParameter(1, Ogre::Vector4(1, 1, 1, 1));

    auto timedLifeComponent = world.Create_TimedLifeComponent(agentEntity, int(lifetime));
}

void resetWorld(CellStageWorld@ world)
    {
    // We have to call this so we get a new batch of species and so that everything spawns properly.
    // I worry that not just creating a whole new world will cause tons of problems later aswell ,
    // for example when we want new planets and everything, but eh. We can call it all in this method.

    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).resetAutoEvo();
    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).createNewEcoSystem();
    }



//local void setSpawnablePhysics(ObjectID entity, Float3 pos, mesh, scale, collisionShape){
//                               // Rigid body
//                               auto rigidBody = RigidBodyComponent()
//                               rigidBody.properties.friction = 0.2
//                               rigidBody.properties.linearDamping = 0.8

//                               rigidBody.properties.shape = collisionShape
//                               rigidBody.setDynamicProperties(
//                                   pos,
//                                   Quaternion(Radian(Degree(math.random()*360)), Vector3(0, 0, 1)),
//                                   Vector3(0, 0, 0),
//                                   Vector3(0, 0, 0)
//                               )
//                     rigidBody.properties.touch()
//                     entity.addComponent(rigidBody)
//                     // Scene node
//             auto sceneNode = OgreSceneNodeComponent()
//             sceneNode.meshName = mesh
//             sceneNode.transform.scale = Vector3(scale, scale, scale)
//             entity.addComponent(sceneNode)
//                     return entity
//             }


//             local void addEmitter2Entity(entity, compound){
//             auto compoundEmitter = CompoundEmitterComponent()
//             entity.addComponent(compoundEmitter)
//             compoundEmitter.emissionRadius = 1
//             compoundEmitter.maxInitialSpeed = 10
//             compoundEmitter.minInitialSpeed = 2
//             compoundEmitter.minEmissionAngle = Degree(0)
//             compoundEmitter.maxEmissionAngle = Degree(360)
//             compoundEmitter.particleLifeTime = 5000
//                 auto timedEmitter = TimedCompoundEmitterComponent()
//             timedEmitter.compoundId = CompoundRegistry.getCompoundId(compound)
//             timedEmitter.particlesPerEmission = 1
//             timedEmitter.potencyPerParticle = 2.0
//             timedEmitter.emitInterval = 1000
//             entity.addComponent(timedEmitter)
//             }

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
        true,
        // No individual name (could be good for debugging)
        "");
    }
}


ObjectID createToxin(CellStageWorld@ world, Float3 pos)
{
    // Toxins
    ObjectID toxinEntity = world.CreateEntity();
    //LOG_INFO("toxin spawned at pos x"+ pos.X +"y"+ pos.Y +"z"+ pos.Z);
    auto position = world.Create_Position(toxinEntity, pos,Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),Ogre::Vector3(0,1,0)));
    auto renderNode = world.Create_RenderNode(toxinEntity);
    renderNode.Scale = Float3(1, 1, 1);
    renderNode.Marked = true;
    renderNode.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),Ogre::Vector3(0,1,1)));
    renderNode.Node.setPosition(pos);
    // Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
    //     Ogre::Vector3(0, 1, 0)));

    auto model = world.Create_Model(toxinEntity, renderNode.Node, "oxytoxy.mesh");
    // Need to set the tint
    model.GraphicalObject.setCustomParameter(1, Ogre::Vector4(1, 1, 1, 1));

    auto rigidBody = world.Create_Physics(toxinEntity, world, position, null);
    rigidBody.SetCollision(world.GetPhysicalWorld().CreateSphere(1));
    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
        world.GetPhysicalMaterial("agentCollision"));
    rigidBody.CreatePlaneConstraint(world.GetPhysicalWorld(), Float3(0,1,0));
    rigidBody.SetMass(1.0f);

    rigidBody.JumpTo(position);

    return toxinEntity;
}

ObjectID createChloroplast(CellStageWorld@ world, Float3 pos)
{
    // Chloroplasts
    ObjectID chloroplastEntity = world.CreateEntity();
    //LOG_INFO("chloroplast spawned at pos x"+ pos.X +"y"+ pos.Y +"z"+ pos.Z);
    auto position = world.Create_Position(chloroplastEntity, pos,Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),Ogre::Vector3(0,1,1)));

    auto renderNode = world.Create_RenderNode(chloroplastEntity);
    renderNode.Scale = Float3(1, 1, 1);
    renderNode.Marked = true;
    renderNode.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),Ogre::Vector3(0,1,1)));
    renderNode.Node.setPosition(pos);
    auto model = world.Create_Model(chloroplastEntity, renderNode.Node, "chloroplast.mesh");
    // Need to set the tint
    model.GraphicalObject.setCustomParameter(1, Ogre::Vector4(1, 1, 1, 1));

    auto rigidBody = world.Create_Physics(chloroplastEntity, world, position, null);
    rigidBody.SetCollision(world.GetPhysicalWorld().CreateSphere(1));
    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
        world.GetPhysicalMaterial("floatingOrganelle"));
    rigidBody.CreatePlaneConstraint(world.GetPhysicalWorld(), Float3(0,1,0));
    rigidBody.SetMass(1.0f);
    rigidBody.JumpTo(position);

    return chloroplastEntity;
}

// TODO: the player species handling would be more logically placed if
// it was in SpeciesSystem, so move it there
void setupSpawnSystem(CellStageWorld@ world){
    //spawn code is here, if it isnt obvious by the name
    SpawnSystem@ spawnSystem = world.GetSpawnSystem();

    // Clouds are handled by biome.as

    LOG_INFO("setting up spawn information");

    setupFloatingOrganelles(world);

    LOG_INFO("setting up player species to spawn");
    auto keys = STARTER_MICROBES.getKeys();
    for(uint n = 0; n < keys.length(); n++)
    {
    const string name = keys[n];

        PlayerSpeciesSpawner@ spawner = PlayerSpeciesSpawner(name);

        SpawnFactoryFunc@ factory = SpawnFactoryFunc(spawner.factorySpawn);

        LOG_INFO("adding spawn player species: " + name);

        const auto spawnerId = spawnSystem.addSpawnType(
            factory, DEFAULT_SPAWN_DENSITY, //spawnDensity should depend on population
            MICROBE_SPAWN_RADIUS);
    }
}



//moved this over here fo rnow, its probabbly good to put "free spawning organelles" in their own function
void setupFloatingOrganelles(CellStageWorld@ world){
    LOG_INFO("setting up free floating organelles");
    SpawnSystem@ spawnSystem = world.GetSpawnSystem();

    //spawn toxin and chloroplasts
    const auto chloroId = spawnSystem.addSpawnType(
        @createChloroplast, DEFAULT_SPAWN_DENSITY,
    MICROBE_SPAWN_RADIUS);

    //toxins
    const auto toxinId = spawnSystem.addSpawnType(
        @createToxin, DEFAULT_SPAWN_DENSITY,
    MICROBE_SPAWN_RADIUS);




    //             auto toxinOrganelleSpawnvoid = function(pos){
    //             auto reactionHandler = CollisionComponent()
    //             reactionHandler.addCollisionGroup("powerup")
    //             powerupEntity.addComponent(reactionHandler)
    //               auto powerupComponent = PowerupComponent()
    //             // void name must be in configs.lua{
    //             powerupComponent.setEffect("toxin_number")
    //             powerupEntity.addComponent(powerupComponent)
    //             return powerupEntity
    //             auto reactionHandler = CollisionComponent()
    //                 reactionHandler.addCollisionGroup("powerup")
    //             powerupEntity.addComponent(reactionHandler)

    //             auto powerupComponent = PowerupComponent()
    //             // void name must be in configs.lua{
    //             powerupComponent.setEffect("chloroplast_number")
    //             powerupEntity.addComponent(powerupComponent)
    //             return powerupEntity
    }

void setupSound(CellStageWorld@ world){
    //                               auto ambientEntity = Entity("ambience", gameState.wrapper)
    //                               auto soundSource = SoundSourceComponent()
    //                               soundSource.ambientSoundSource = true
    //                               soundSource.autoLoop = true
    //                               soundSource.volumeMultiplier = 0.3
    //                               ambientEntity.addComponent(soundSource)
    //                               // Gui effects
    //                               auto guiSoundEntity = Entity("gui_sounds", gameState.wrapper)
    //                               soundSource = SoundSourceComponent()
    //                               soundSource.ambientSoundSource = true
    //                               soundSource.autoLoop = false
    //                               soundSource.volumeMultiplier = 1.0
    //                               guiSoundEntity.addComponent(soundSource)
    //                               // Sound
    //                               soundSource.addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
    //                               soundSource.addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    //                               auto listener = Entity("soundListener", gameState.wrapper)
    //                               auto sceneNode = OgreSceneNodeComponent()
    //                               listener.addComponent(sceneNode)
}

//                               setupCompounds()
//                               setupProcesses()

//                               local void createMicrobeStage(name){
//                               return
//                               g_luaEngine.createGameState(
//                                   name,
//                                   {
//                                       MicrobeReplacementSystem(),
//                                           // SwitchGameStateSystem(),
//                                           QuickSaveSystem(),
//                                           // Microbe specific
//                                           MicrobeSystem(),
//                                           MicrobeCameraSystem(),
//                                           MicrobeAISystem(),
//                                           MicrobeControlSystem(),
//                                           HudSystem(),
//                                           TimedLifeSystem(),
//                                           CompoundMovementSystem(),
//                                           CompoundAbsorberSystem(),
//                                           ProcessSystem(),
//                                           //PopulationSystem(),
//                                           PatchSystem(),
//                                           SpeciesSystem(),
//                                           // Physics
//                                           RigidBodyInputSystem(),
//                                           UpdatePhysicsSystem(),
//                                           RigidBodyOutputSystem(),
//                                           BulletToOgreSystem(),
//                                           CollisionSystem(),
//                                           // Microbe Specific again (order sensitive)
//                                           setupSpawnSystem(),
//                                           // Graphics
//                                           OgreAddSceneNodeSystem(),
//                                           OgreUpdateSceneNodeSystem(),
//                                           OgreCameraSystem(),
//                                           OgreLightSystem(),
//                                           SkySystem(),
//                                           OgreWorkspaceSystem(),
//                                           OgreRemoveSceneNodeSystem(),
//                                           RenderSystem(),
//                                           MembraneSystem(),
//                                           CompoundCloudSystem(),
//                                           //AgentCloudSystem(),
//                                           // Other
//                                           SoundSourceSystem(),
//                                           PowerupSystem(),
//                                           CompoundEmitterSystem(), // Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
//                                                                                                                                                                                          },
//                                   true,
//                                   "MicrobeStage",
//                                   function(gameState)
//                                   setupBackground(gameState)
//                                   setupCamera(gameState)
//                                   setupCompoundClouds(gameState)
//                                   setupSpecies(gameState)
//                                   setupPlayer(gameState)
//                                   setupSound(gameState)
//                                   }
//                               )
//                               }


