#include "configs.as"

const auto CLOUD_SPAWN_RADIUS = 75;

const auto POWERUP_SPAWN_RADIUS = 85;
const auto MICROBE_SPAWN_RADIUS = 85;

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

            auto organelle = data.organelles[a];
            
            if(!ORGANELLE_TABLE.exists(organelle.name))
                continue;

            const Organelle@ organelleDefinition;
            if(!ORGANELLE_TABLE.get(organelle.name, organelleDefinition)){

                LOG_ERROR("Organelle table has no organelle named '" + organelle.name +
                    "', that was used in starter microbe");
                continue;
            }
                
            for(uint processNumber = 0;
                processNumber < organelleDefinition.processes.length(); ++processNumber)
            {
                // This name needs to match the one in bioProcessRegistry
                auto process = organelleDefinition.processes[processNumber];
                
                if(!capacities.exists(process.name)){
                    capacities[process.name] = 0;
                }

                capacities[process.name] = cast<int>(capacities[process]) + process.capacity;
            }
        }

        uint64 processCount = SimulationParameters::bioProcessRegistry().getSize();
        for(uint bioProcessId = 0; bioProcessId < processCount; ++bioProcessId){
            
            auto name = SimulationParameters::bioProcessRegistry().getInternalName(
                bioProcessId);
            
            if(capacities.exists(name)){

                int capacity;
                if(!capacities.get(name, capacity)){
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


//             function microbeSpawnFunctionGeneric(pos, speciesName, aiControlled, individualName, gameState)
//             return spawnMicrobe(pos, speciesName, aiControlled, individualName, gameState).entity
//             }

//             -- speciesName decides the template to use, while individualName is used for referencing the instance
//             function spawnMicrobe(pos, speciesName, aiControlled, individualName, gameState)

//                 assert(gameState ~= nil)
//                 assert(isNotEmpty(speciesName))

//                 -- Workaround. Find a fix for this
//             if gameState ~= g_luaEngine.currentGameState then
//             print("Warning used different gameState than currentGameState in microbe spawn. " ..
//                 "This would have been bad in earlier versions")
//             }
    
//             local processor = getComponent(speciesName, gameState, ProcessorComponent)

//             if processor == nil then

//             print("Skipping microbe spawn because species '" .. speciesName ..
//                 "' doesn't have a processor component")
        
//                 return nil
//                 }
    
    
//                 local microbeEntity = MicrobeSystem.createMicrobeEntity(individualName, aiControlled, speciesName, false)
//                 local microbe = Microbe(microbeEntity, false, gameState)
//                 if pos ~= nil then
//             microbe.rigidBody:setDynamicProperties(
//                 pos, -- Position
//                 Quaternion.new(Radian.new(Degree(0)), Vector3(1, 0, 0)), -- Orientation
//                 Vector3(0, 0, 0), -- Linear velocity
//                 Vector3(0, 0, 0)  -- Angular velocity
//             )
//                               }
//                               return microbe
//                               }

//                               local function setSpawnablePhysics(entity, pos, mesh, scale, collisionShape)
//                               -- Rigid body
//                               local rigidBody = RigidBodyComponent.new()
//                               rigidBody.properties.friction = 0.2
//                               rigidBody.properties.linearDamping = 0.8

//                               rigidBody.properties.shape = collisionShape
//                               rigidBody:setDynamicProperties(
//                                   pos,
//                                   Quaternion.new(Radian.new(Degree(math.random()*360)), Vector3(0, 0, 1)),
//                                   Vector3(0, 0, 0),
//                                   Vector3(0, 0, 0)
//                               )
//                     rigidBody.properties:touch()
//                     entity:addComponent(rigidBody)
//                     -- Scene node
//             local sceneNode = OgreSceneNodeComponent.new()
//             sceneNode.meshName = mesh
//             sceneNode.transform.scale = Vector3(scale, scale, scale)
//             entity:addComponent(sceneNode)
//                     return entity
//             }

//             function createCompoundCloud(compoundName, x, y, amount)
//             if amount == nil then amount = currentBiome.compounds[compoundName] }
//             if amount == nil then amount = 0 }

//             if compoundTable[compoundName] and compoundTable[compoundName].isCloud then
//             -- addCloud requires integer arguments
//             x = math.floor(x)
//             y = math.floor(y)
//             getComponent("compound_cloud_" .. compoundName,
//                 g_luaEngine.currentGameState, CompoundCloudComponent
//             ):addCloud(amount, x, y)
//             }

//             -- The spawn system expects an entity.
//             return Entity.new(g_luaEngine.currentGameState.wrapper)
//             }

//             function createAgentCloud(compoundId, x, y, direction, amount)    
//             local normalizedDirection = direction
//             normalizedDirection:normalise()
//             local agentEntity = Entity.new(g_luaEngine.currentGameState.wrapper)

//             local reactionHandler = CollisionComponent.new()
//             reactionHandler:addCollisionGroup("agent")
//             agentEntity:addComponent(reactionHandler)

//             local rigidBody = RigidBodyComponent.new()
//                 rigidBody.properties.mass = 0.001
//             rigidBody.properties.friction = 0.4
//             rigidBody.properties.linearDamping = 0.4
//             rigidBody.properties.shape = SphereShape.new(HEX_SIZE)
//             rigidBody:setDynamicProperties(
//                 Vector3(x, y, 0) + direction * 1.5,
//                 Quaternion.new(Radian.new(Degree(math.random()*360)), Vector3(0, 0, 1)),
//                 normalizedDirection * AGENT_EMISSION_VELOCITY,
//                 Vector3(0, 0, 0)
//             )
//             rigidBody.properties:touch()
//             agentEntity:addComponent(rigidBody)
    
//             local sceneNode = OgreSceneNodeComponent.new()
//             sceneNode.meshName = "oxytoxy.mesh"
//             agentEntity:addComponent(sceneNode)
    
//             local timedLifeComponent = TimedLifeComponent.new()
//             timedLifeComponent.timeToLive = 2000
//             agentEntity:addComponent(timedLifeComponent)
//             }

//             local function addEmitter2Entity(entity, compound)
//             local compoundEmitter = CompoundEmitterComponent.new()
//             entity:addComponent(compoundEmitter)
//             compoundEmitter.emissionRadius = 1
//             compoundEmitter.maxInitialSpeed = 10
//             compoundEmitter.minInitialSpeed = 2
//             compoundEmitter.minEmissionAngle = Degree(0)
//             compoundEmitter.maxEmissionAngle = Degree(360)
//             compoundEmitter.particleLifeTime = 5000
//                 local timedEmitter = TimedCompoundEmitterComponent.new()
//             timedEmitter.compoundId = CompoundRegistry.getCompoundId(compound)
//             timedEmitter.particlesPerEmission = 1
//             timedEmitter.potencyPerParticle = 2.0
//             timedEmitter.emitInterval = 1000
//             entity:addComponent(timedEmitter)
//             }

//             local function setupSpawnSystem(gameState)
//             gSpawnSystem = SpawnSystem.new()

//             local toxinOrganelleSpawnFunction = function(pos)
//             powerupEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
//             setSpawnablePhysics(powerupEntity, pos, "AgentVacuole.mesh", 0.9,
//                 SphereShape.new(HEX_SIZE))

//             local reactionHandler = CollisionComponent.new()
//             reactionHandler:addCollisionGroup("powerup")
//             powerupEntity:addComponent(reactionHandler)
        
//                 local powerupComponent = PowerupComponent.new()
//             -- Function name must be in configs.lua
//             powerupComponent:setEffect("toxin_number")
//             powerupEntity:addComponent(powerupComponent)
//             return powerupEntity
//             }
//             local ChloroplastOrganelleSpawnFunction = function(pos) 
//             powerupEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
//             setSpawnablePhysics(powerupEntity, pos, "chloroplast.mesh", 0.9,
//                 SphereShape.new(HEX_SIZE))

//             local reactionHandler = CollisionComponent.new()
//                 reactionHandler:addCollisionGroup("powerup")
//             powerupEntity:addComponent(reactionHandler)
        
//             local powerupComponent = PowerupComponent.new()
//             -- Function name must be in configs.lua
//             powerupComponent:setEffect("chloroplast_number")
//             powerupEntity:addComponent(powerupComponent)
//             return powerupEntity
//             }

//             compoundSpawnTypes = {}
//         for compoundName, compoundInfo in pairs(compoundTable) do
//                               if compoundInfo.isCloud then
//             local spawnCloud =  function(pos)
//             return createCompoundCloud(compoundName, pos.x, pos.y)
//             }

//             compoundSpawnTypes[compoundName] = gSpawnSystem:addSpawnType(spawnCloud, 1/10000, CLOUD_SPAWN_RADIUS) -- Placeholder, the real one is set in biome.lua
//             }
//             }

//             gSpawnSystem:addSpawnType(toxinOrganelleSpawnFunction, 1/17000, POWERUP_SPAWN_RADIUS)
//             gSpawnSystem:addSpawnType(ChloroplastOrganelleSpawnFunction, 1/12000, POWERUP_SPAWN_RADIUS)

//             for name, species in pairs(starter_microbes) do

//                           assert(isNotEmpty(name))
//                               assert(species)
        
//                               gSpawnSystem:addSpawnType(
//                                   function(pos) 
//                                   return microbeSpawnFunctionGeneric(pos, name, true, nil,
//                                       g_luaEngine.currentGameState)
//                                   }, 
//                                   species.spawnDensity, MICROBE_SPAWN_RADIUS)
//                               }

//                               return gSpawnSystem
//                               }

//                               local function setupPlayer(gameState)
//                               assert(GameState.MICROBE == gameState)
//                               assert(gameState ~= nil)
    
//                               local microbe = spawnMicrobe(nil, "Default", false, PLAYER_NAME, gameState)
//                               microbe.collisionHandler:addCollisionGroup("powerupable")
//                               Engine:playerData():lockedMap():addLock("Toxin")
//                               Engine:playerData():lockedMap():addLock("chloroplast")
//                               Engine:playerData():setActiveCreature(microbe.entity.id, gameState.wrapper)
//                               }

//                               local function setupSound(gameState)
//                               local ambientEntity = Entity.new("ambience", gameState.wrapper)
//                               local soundSource = SoundSourceComponent.new()
//                               soundSource.ambientSoundSource = true
//                               soundSource.autoLoop = true
//                               soundSource.volumeMultiplier = 0.3
//                               ambientEntity:addComponent(soundSource)
//                               -- Sound
//                               soundSource:addSound("microbe-theme-1", "microbe-theme-1.ogg")
//                               soundSource:addSound("microbe-theme-3", "microbe-theme-3.ogg")
//                               soundSource:addSound("microbe-theme-4", "microbe-theme-4.ogg")
//                               soundSource:addSound("microbe-theme-5", "microbe-theme-5.ogg")
//                               soundSource:addSound("microbe-theme-6", "microbe-theme-6.ogg")   
//                               soundSource:addSound("microbe-theme-7", "microbe-theme-7.ogg")   
//                               local ambientEntity2 = Entity.new("ambience2", gameState.wrapper)
//                               local soundSource = SoundSourceComponent.new()
//                               soundSource.volumeMultiplier = 0.1
//                               soundSource.ambientSoundSource = true
//                               ambientSound = soundSource:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
//                               soundSource.autoLoop = true
//                               ambientEntity2:addComponent(soundSource)
//                               -- Gui effects
//                               local guiSoundEntity = Entity.new("gui_sounds", gameState.wrapper)
//                               soundSource = SoundSourceComponent.new()
//                               soundSource.ambientSoundSource = true
//                               soundSource.autoLoop = false
//                               soundSource.volumeMultiplier = 1.0
//                               guiSoundEntity:addComponent(soundSource)
//                               -- Sound
//                               soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
//                               soundSource:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
//                               local listener = Entity.new("soundListener", gameState.wrapper)
//                               local sceneNode = OgreSceneNodeComponent.new()
//                               listener:addComponent(sceneNode)
//                               }

//                               setupCompounds()
//                               setupProcesses()

//                               local function createMicrobeStage(name)
//                               return 
//                               g_luaEngine:createGameState(
//                                   name,
//                                   {
//                                       MicrobeReplacementSystem.new(),
//                                           -- SwitchGameStateSystem.new(),
//                                           QuickSaveSystem.new(),
//                                           -- Microbe specific
//                                           MicrobeSystem.new(),
//                                           MicrobeCameraSystem.new(),
//                                           MicrobeAISystem.new(),
//                                           MicrobeControlSystem.new(),
//                                           HudSystem.new(),
//                                           TimedLifeSystem.new(),
//                                           CompoundMovementSystem.new(),
//                                           CompoundAbsorberSystem.new(),
//                                           ProcessSystem.new(),
//                                           --PopulationSystem.new(),
//                                           PatchSystem.new(),
//                                           SpeciesSystem.new(),
//                                           -- Physics
//                                           RigidBodyInputSystem.new(),
//                                           UpdatePhysicsSystem.new(),
//                                           RigidBodyOutputSystem.new(),
//                                           BulletToOgreSystem.new(),
//                                           CollisionSystem.new(),
//                                           -- Microbe Specific again (order sensitive)
//                                           setupSpawnSystem(),
//                                           -- Graphics
//                                           OgreAddSceneNodeSystem.new(),
//                                           OgreUpdateSceneNodeSystem.new(),
//                                           OgreCameraSystem.new(),
//                                           OgreLightSystem.new(),
//                                           SkySystem.new(),
//                                           OgreWorkspaceSystem.new(),
//                                           OgreRemoveSceneNodeSystem.new(),
//                                           RenderSystem.new(),
//                                           MembraneSystem.new(),
//                                           CompoundCloudSystem.new(),
//                                           --AgentCloudSystem.new(),
//                                           -- Other
//                                           SoundSourceSystem.new(),
//                                           PowerupSystem.new(),
//                                           CompoundEmitterSystem.new(), -- Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
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

    
