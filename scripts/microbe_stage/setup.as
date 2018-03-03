#include "configs.as"

// For system registering
#include "microbe.as"
#include "microbe_stage_hud.as"

const auto CLOUD_SPAWN_RADIUS = 75;

const auto POWERUP_SPAWN_RADIUS = 85;
const auto MICROBE_SPAWN_RADIUS = 85;

// Call setRandomBiome instead from wherever this is needed
// void setupBackground(CellStageWorld@ world){
//     setRandomBiome(world);
// }

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

        ObjectID speciesEntity = world.CreateEntity();
        
        SpeciesComponent@ speciesComponent = world.Create_SpeciesComponent(speciesEntity,
            name);
        
        @speciesComponent.avgCompoundAmounts = dictionary();

        @speciesComponent.organelles = array<ref@>();
        speciesComponent.organelles.resize(data.organelles.length());
        for(uint a = 0; a < data.organelles.length(); ++a){

            // Need to assign with handle assignment
            @speciesComponent.organelles[a] = data.organelles[a];
            //@speciesComponent.organelles.push(data.organelles[a]);
        }

        ProcessorComponent@ processorComponent = world.Create_ProcessorComponent(
            speciesEntity);
    
        speciesComponent.colour = data.colour;

        // iterates over all compounds, and sets amounts and priorities
        uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
        for(uint a = 0; a < compoundCount; ++a){

            auto compound = SimulationParameters::compoundRegistry().getTypeData(a);

            int compoundAmount;
            bool valid = data.compounds.get(compound.internalName, compoundAmount);
            
            if(valid){
                
                // priority = compoundData.priority
                speciesComponent.avgCompoundAmounts[formatUInt(compound.id)] = compoundAmount;
                // speciesComponent.compoundPriorities[compoundID] = priority
            }
        }

        dictionary capacities;
        for(uint a = 0; a < data.organelles.length(); ++a){

            string type = data.organelles[a].type;
            
            const Organelle@ organelleDefinition = getOrganelleDefinition(type);
            if(organelleDefinition is null){

                LOG_ERROR("Organelle table has no organelle named '" + type +
                    "', that was used in starter microbe");
                continue;
            }
            
            for(uint processNumber = 0;
                processNumber < organelleDefinition.processes.length(); ++processNumber)
            {
                // This name needs to match the one in bioProcessRegistry
                TweakedProcess@ process = organelleDefinition.processes[processNumber];
                
                if(!capacities.exists(process.process.internalName)){
                    capacities[process.process.internalName] = 0;
                }

                // Here the second capacities[process.name] was initially capacities[process]
                // but the processes are just strings inside the Organelle class
                capacities[process.process.internalName] = int(capacities[
                        process.process.internalName]) +
                    process.capacity;
            }
        }

        uint64 processCount = SimulationParameters::bioProcessRegistry().getSize();
        for(uint bioProcessId = 0; bioProcessId < processCount; ++bioProcessId){
            
            auto processName = SimulationParameters::bioProcessRegistry().getInternalName(
                bioProcessId);
            
            if(capacities.exists(processName)){

                int capacity;
                if(!capacities.get(processName, capacity)){
                    LOG_ERROR("capacities has invalid value");
                    continue;
                }
                
                processorComponent.setCapacity(bioProcessId, capacity);
                // This may be commented out for the reason that the default should be retained
                // } else {
                // processorComponent.setCapacity(bioProcessId, 0)
            }
        }
    }

    LOG_INFO("setupSpecies created " + keys.length() + " species");
}

ScriptComponent@ MicrobeComponentFactory(GameWorld@ world){

    return MicrobeComponent();
}

// This function instantiates all script system types for a world
void setupSystemsForWorld(CellStageWorld@ world){

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);

    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
    
    assert(false, "TODO: add the rest");
}

// This should be moved somewhere else
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



//             void microbeSpawnFunctionGeneric(pos, speciesName, aiControlled, individualName, gameState){
//             return spawnMicrobe(pos, speciesName, aiControlled, individualName, gameState).entity
//             }

//             // speciesName decides the template to use, while individualName is used for referencing the instance
//             void spawnMicrobe(pos, speciesName, aiControlled, individualName, gameState){

//                 assert(gameState !is null)
//                 assert(isNotEmpty(speciesName))

//                 // Workaround. Find a fix for this
//             if(gameState ~= g_luaEngine.currentGameState){
//             print("Warning used different gameState than currentGameState in microbe spawn. " ..
//                 "This would have been bad in earlier versions")
//             }
    
//             auto processor = getComponent(speciesName, gameState, ProcessorComponent)

//             if(processor == null){

//             print("Skipping microbe spawn because species '" .. speciesName ..
//                 "' doesn't have a processor component")
        
//                 return null
//                 }
    
    
//                 auto microbeEntity = MicrobeSystem.createMicrobeEntity(individualName, aiControlled, speciesName, false)
//                 auto microbe = Microbe(microbeEntity, false, gameState)
//                 if(pos !is null){
//             microbe.rigidBody.setDynamicProperties(
//                 pos, // Position
//                 Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), // Orientation
//                 Vector3(0, 0, 0), // Linear velocity
//                 Vector3(0, 0, 0)  // Angular velocity
//             )
//                               }
//                               return microbe
//                               }

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

//             local void setupSpawnSystem(gameState){
//             gSpawnSystem = SpawnSystem()

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

//             compoundSpawnTypes = {}
//         for(compoundName, compoundInfo in pairs(compoundTable)){
//                               if(compoundInfo.isCloud){
//             auto spawnCloud =  function(pos)
//             return createCompoundCloud(compoundName, pos.x, pos.y)
//             }

//             compoundSpawnTypes[compoundName] = gSpawnSystem.addSpawnType(spawnCloud, 1/10000, CLOUD_SPAWN_RADIUS) // Placeholder, the real one is set in biome.lua
//             }
//             }

//             gSpawnSystem.addSpawnType(toxinOrganelleSpawnFunction, 1/17000, POWERUP_SPAWN_RADIUS)
//             gSpawnSystem.addSpawnType(ChloroplastOrganelleSpawnFunction, 1/12000, POWERUP_SPAWN_RADIUS)

//             for(name, species in pairs(starter_microbes)){

//                           assert(isNotEmpty(name))
//                               assert(species)
        
//                               gSpawnSystem.addSpawnType(
//                                   function(pos) 
//                                   return microbeSpawnFunctionGeneric(pos, name, true, null,
//                                       g_luaEngine.currentGameState)
//                                   }, 
//                                   species.spawnDensity, MICROBE_SPAWN_RADIUS)
//                               }

//                               return gSpawnSystem
//                               }

//                               local void setupPlayer(gameState){
//                               assert(GameState.MICROBE == gameState)
//                               assert(gameState !is null)
    
//                               auto microbe = spawnMicrobe(null, "Default", false, PLAYER_NAME, gameState)
//                               microbe.collisionHandler.addCollisionGroup("powerupable")
//                               Engine.playerData():lockedMap():addLock("Toxin")
//                               Engine.playerData():lockedMap():addLock("chloroplast")
//                               Engine.playerData():setActiveCreature(microbe.entity.id, gameState.wrapper)
//                               }

//                               local void setupSound(gameState){
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
//                               }

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

    
