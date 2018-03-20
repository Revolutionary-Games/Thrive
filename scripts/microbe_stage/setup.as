#include "configs.as"

// For system registering
#include "microbe.as"
#include "microbe_stage_hud.as"
#include "microbe_operations.as"

const auto CLOUD_SPAWN_RADIUS = 75;

const auto POWERUP_SPAWN_RADIUS = 85;
const auto MICROBE_SPAWN_RADIUS = 85;

// This is a helper for calling all the setup functions at the same time
// This is the one called from C++
void setupScriptsForWorld(CellStageWorld@ world){
    setupSpecies(world);
    setupSystemsForWorld(world);
    setupSpawnSystem(world);
    setupSound(world);
}

// This function should be the entry point for all initial-species generation
// For now, it can go through the XML and instantiate all the species, but later this 
// would be all procedural.
// Together with the mutate function, these would be the only ways species are created
// Currently this goes through STARTER_MICROBES (defined in config.as) and makes entities with
// SpeciesComponents with the properties of the species
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

// This function instantiates all script system types for a world
void setupSystemsForWorld(CellStageWorld@ world){

    // Fail if compound registry is empty (hud system caches the compound ids on startup) //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);

    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
    world.RegisterScriptSystem("SpeciesSystem", SpeciesSystem());

    // TODO: add the rest of the systems and component types that are defined in scripts here
}



const auto PLAYER_NAME = "Player";

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




//                               local void setSpawnablePhysics(entity, pos, mesh, scale, collisionShape){
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

	
ObjectID factorySpawn(CellStageWorld@ world, Float3 pos){
        return MicrobeOperations::spawnMicrobe(world, pos, "Default", true,"");
}
	
void setupSpawnSystem(CellStageWorld@ world){
	//spawn code is here, if it isnt obvious by the name
	SpawnSystem@ gSpawnSystem = world.GetSpawnSystem();
	//             compoundSpawnTypes = {}
	//         for(compoundName, compoundInfo in pairs(compoundTable)){
	//                               if(compoundInfo.isCloud){
	//             auto spawnCloud =  function(pos)
	//             return createCompoundCloud(compoundName, pos.x, pos.y)
	//             }

	//             compoundSpawnTypes[compoundName] = gSpawnSystem.addSpawnType(spawnCloud, 1/10000, CLOUD_SPAWN_RADIUS) // Placeholder, the real one is set in biome.lua
	//             }

	//             gSpawnSystem.addSpawnType(toxinOrganelleSpawnFunction, 1/17000, POWERUP_SPAWN_RADIUS)
	//             gSpawnSystem.addSpawnType(ChloroplastOrganelleSpawnFunction, 1/12000, POWERUP_SPAWN_RADIUS)

	//need to spawn microbes from the starter_microbes list, so need to loop through and define spawning for all of them
	

	
	LOG_INFO("setting  up spawn information");
	 auto keys = STARTER_MICROBES.getKeys();
	  for(int n = 0; n < keys.length(); n++)
		{
		const string name = keys[n];
		bool species;
		STARTER_MICROBES.get(name,species);
		LOG_INFO("adding spawn for: "+name);
		//gSpawnSystem.addSpawnType(SpawnFactoryFunc(factorySpawn),DEFAULT_SPAWN_DENSITY, MICROBE_SPAWN_RADIUS);
		}
		
	//for(name, species in pairs(starter_microbes)){
    //
	//                               gSpawnSystem.addSpawnType(
	//                                   function(pos) {
	//                                   return microbeSpawnFunctionGeneric(pos, name, true, null,
	//                                       g_luaEngine.currentGameState) 
	//									   }, species.spawnDensity, MICROBE_SPAWN_RADIUS);
	//                              }
}


//moved this over here fo rnow, its probabbly good to put "free spawning organelles" in their own function

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

    
