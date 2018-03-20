#include "configs.as"

// For system registering
#include "microbe.as"
#include "microbe_stage_hud.as"
#include "microbe_operations.as"
#include "microbe_ai.as"


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

    GetThriveGame().playerData().lockedMap().addLock("Toxin");
    GetThriveGame().playerData().lockedMap().addLock("chloroplast");
    
    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false, PLAYER_NAME);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");
    // TODO: powerupable
    //microbe.collisionHandler.addCollisionGroup("powerupable");

    GetThriveGame().playerData().setActiveCreature(microbe);

    // Testing spawning extra cell
    MicrobeOperations::spawnMicrobe(world, Float3(10, 0, 0), "Default",
        false, "extra player");
}


// TODO: move this somewhere
// This is called from c++ system PlayerMicrobeControlSystem
void applyCellMovementControl(GameWorld@ world, ObjectID entity, const Float3 &in movement,
    const Float3 &in lookPosition)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));
    
    if(!microbeComponent.dead){

        microbeComponent.facingTargetPoint = lookPosition;
        microbeComponent.movementDirection = movement;
    }
}


// TODO: This should be moved somewhere else...
void createAgentCloud(CellStageWorld@ world, CompoundId compoundId, Float3 pos,
    Float3 direction, float amount)
{
    auto normalizedDirection = direction.Normalize();
    auto agentEntity = world.CreateEntity();

    // auto reactionHandler = CollisionComponent()
    // reactionHandler.addCollisionGroup("agent")
    auto position = world.Create_Position(agentEntity, pos + (direction * 1.5),
        Ogre::Quaternion(Ogre::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            Ogre::Vector3(0, 1, 0)));

    auto rigidBody = world.Create_Physics(agentEntity, world, position, null);

    rigidBody.SetCollision(world.GetPhysicalWorld().CreateSphere(HEX_SIZE));
    rigidBody.CreatePhysicsBody(world.GetPhysicalWorld());
    rigidBody.CreatePlaneConstraint(world.GetPhysicalWorld(), Float3(0, 1, 0));

    rigidBody.SetMass(0.001);

    // TODO: physics property applying here as well
    // rigidBody.properties.friction = 0.4;
    // rigidBody.properties.linearDamping = 0.4;

    // TODO: impulse or set velocity?
    rigidBody.SetVelocity(normalizedDirection * AGENT_EMISSION_VELOCITY);
        
    auto sceneNode = world.Create_RenderNode(agentEntity);
    auto model = world.Create_Model(agentEntity, sceneNode.Node, "oxytoxy.mesh");
    
    auto timedLifeComponent = world.Create_TimedLifeComponent(agentEntity, 2000);
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
	//spawn toxin and chloroplasts
	//             auto toxinOrganelleSpawnvoid = function(pos){
	//             powerupEntity = Entity(g_luaEngine.currentGameState.wrapper)
	//             setSpawnablePhysics(powerupEntity, pos, "AgentVacuole.mesh", 0.9,
	//                 SphereShape(HEX_SIZE))

	//             auto reactionHandler = CollisionComponent()
	//             reactionHandler.addCollisionGroup("powerup")
	//             powerupEntity.addComponent(reactionHandler)
			
	//                 auto powerupComponent = PowerupComponent()
	//             // void name must be in configs.lua{
	//             powerupComponent.setEffect("toxin_number")
	//             powerupEntity.addComponent(powerupComponent)
	//             return powerupEntity
	//             }
	//             auto ChloroplastOrganelleSpawnvoid = function(pos) {
	//             powerupEntity = Entity(g_luaEngine.currentGameState.wrapper)
	//             setSpawnablePhysics(powerupEntity, pos, "chloroplast.mesh", 0.9,
	//                 SphereShape(HEX_SIZE))

	//             auto reactionHandler = CollisionComponent()
	//                 reactionHandler.addCollisionGroup("powerup")
	//             powerupEntity.addComponent(reactionHandler)
			
	//             auto powerupComponent = PowerupComponent()
	//             // void name must be in configs.lua{
	//             powerupComponent.setEffect("chloroplast_number")
	//             powerupEntity.addComponent(powerupComponent)
	//             return powerupEntity
	//             }
	}

void setupSound(CellStageWorld@ world){
	//                               auto ambientEntity = Entity("ambience", gameState.wrapper)
	//                               auto soundSource = SoundSourceComponent()
	//                               soundSource.ambientSoundSource = true
	//                               soundSource.autoLoop = true
	//                               soundSource.volumeMultiplier = 0.3
	//                               ambientEntity.addComponent(soundSource)
	//                               // Sound
	//                               soundSource.addSound("microbe-theme-1", "microbe-theme-1.ogg")
	//                               soundSource.addSound("microbe-theme-3", "microbe-theme-3.ogg")
	//                               soundSource.addSound("microbe-theme-4", "microbe-theme-4.ogg")
	//                               soundSource.addSound("microbe-theme-5", "microbe-theme-5.ogg")
	//                               soundSource.addSound("microbe-theme-6", "microbe-theme-6.ogg")   
	//                               soundSource.addSound("microbe-theme-7", "microbe-theme-7.ogg")   
	//                               auto ambientEntity2 = Entity("ambience2", gameState.wrapper)
	//                               auto soundSource = SoundSourceComponent()
	//                               soundSource.volumeMultiplier = 0.1
	//                               soundSource.ambientSoundSource = true
	//                               ambientSound = soundSource.addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
	//                               soundSource.autoLoop = true
	//                               ambientEntity2.addComponent(soundSource)
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

    
