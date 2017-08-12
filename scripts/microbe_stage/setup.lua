CLOUD_SPAWN_RADIUS = 50
CLOUD_SPAWN_DENSITY = 1/5000

BACTERIA_SPAWN_RADIUS = 50
BACTERIA_SPAWN_DENSITY = 1/4000

local function setupBackground(gameState)
    setRandomBiome(gameState)
end

local function setupCamera(gameState)
    local entity = Entity.new(CAMERA_NAME, gameState.wrapper)
    -- Camera
    local camera = OgreCameraComponent.new("camera")
    camera.properties.nearClipDistance = 5
    camera.properties.offset = Vector3(0, 0, 30)
    camera.properties:touch()
    entity:addComponent(camera)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.transform.position.z = 30
    sceneNode.transform:touch()
    entity:addComponent(sceneNode)
    -- Light
    local light = OgreLightComponent.new()
    light:setRange(200)
    entity:addComponent(light)
    -- Workspace
    local workspaceEntity = Entity.new(gameState.wrapper)
    local workspaceComponent = OgreWorkspaceComponent.new("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupCompounds()

    local ordered_keys = {}

    for k in pairs(compoundTable) do
        table.insert(ordered_keys, k)
    end

    table.sort(ordered_keys)
    for i = 1, #ordered_keys do
        local k, v = ordered_keys[i], compoundTable[ ordered_keys[i] ]
        CompoundRegistry.registerCompoundType(k, v["name"], "placeholder.mesh", 1.0, v["isUseful"] == true, v["volume"])
    end    
    CompoundRegistry.loadFromLua({}, agents)
end

local function setupCompoundClouds(gameState)
    for compoundName, compoundInfo in pairs(compoundTable) do
        if compoundInfo.isCloud then
            local compoundId = CompoundRegistry.getCompoundId(compoundName)
            local entity = Entity.new("compound_cloud_" .. compoundName, gameState.wrapper)
            local compoundCloud = CompoundCloudComponent.new()
            local colour = compoundInfo.colour
            compoundCloud:initialize(compoundId, colour.r, colour.g, colour.b)
            entity:addComponent(compoundCloud)
        end
    end
end

local function setupProcesses()
    assert(processes)
    BioProcessRegistry.loadFromLua(processes)
end

function setupSpecies(gameState)
    --[[
    This function should be the entry point for all initial-species generation
    For now, it can go through the XML and instantiate all the species, but later this 
    would be all procedural.
    Together with the mutate function, these would be the only ways species are created
    ]]
    
    for name, data in pairs(starter_microbes) do
        speciesEntity = Entity.new(name, gameState.wrapper)
        speciesComponent = SpeciesComponent.new(name)
        speciesEntity:addComponent(speciesComponent)
        for i, organelle in pairs(data.organelles) do
            local org = {}
            org.name = organelle.name
            org.q = organelle.q
            org.r = organelle.r
            org.rotation = organelle.rotation
            speciesComponent.organelles[i] = org
        end
        processorComponent = ProcessorComponent.new()
        speciesEntity:addComponent(processorComponent)
        speciesComponent.colour = Vector3(data.colour.r, data.colour.g, data.colour.b)

        -- iterates over all compounds, and sets amounts and priorities
        for _, compoundID in pairs(CompoundRegistry.getCompoundList()) do
            compound = CompoundRegistry.getCompoundInternalName(compoundID)
            compoundData = data.compounds[compound]
            if compoundData ~= nil then
                amount = compoundData.amount
                -- priority = compoundData.priority
                speciesComponent.avgCompoundAmounts["" .. compoundID] = amount
                -- speciesComponent.compoundPriorities[compoundID] = priority
            end
        end

        local capacities = {}
        for _, organelle in pairs(data.organelles) do
            if organelleTable[organelle.name] ~= nil then
                if organelleTable[organelle.name]["processes"] ~= nil then
                    for process, capacity in pairs(organelleTable[organelle.name]["processes"]) do
                        if capacities[process] == nil then
                            capacities[process] = 0
                        end
                        capacities[process] = capacities[process] + capacity
                    end
                end
            end
        end
        for _, bioProcessId in pairs(BioProcessRegistry.getList()) do
            local name = BioProcessRegistry.getInternalName(bioProcessId)
            if capacities[name] ~= nil then
                processorComponent:setCapacity(bioProcessId, capacities[name])
            -- else
                -- processorComponent:setCapacity(bioProcessId, 0)
            end
        end
    end
end

-- speciesName decides the template to use, while individualName is used for referencing the instance
function microbeSpawnFunctionGeneric(pos, speciesName, aiControlled, individualName, gameState)

    assert(gameState ~= nil)
    assert(isNotEmpty(speciesName))

    -- Workaround. Find a fix for this
    if gameState ~= g_luaEngine.currentGameState then
        print("Warning used different gameState than currentGameState in microbe spawn. " ..
                  "This would have been bad in earlier versions")
    end
    
    local processor = getComponent(speciesName, gameState, ProcessorComponent)

    if processor == nil then

        print("Skipping microbe spawn because species '" .. speciesName ..
                  "' doesn't have a processor component")
        
        return nil
    end
    
    
    local microbe = Microbe.createMicrobeEntity(individualName, aiControlled, speciesName,
                                                false, gameState)
    if pos ~= nil then
        microbe.rigidBody:setDynamicProperties(
            pos, -- Position
            Quaternion.new(Radian.new(Degree(0)), Vector3(1, 0, 0)), -- Orientation
            Vector3(0, 0, 0), -- Linear velocity
            Vector3(0, 0, 0)  -- Angular velocity
        )
    end
    return microbe
end

local function setSpawnablePhysics(entity, pos, mesh, scale, collisionShape)
    -- Rigid body
    local rigidBody = RigidBodyComponent.new()
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearDamping = 0.8

    rigidBody.properties.shape = collisionShape
    rigidBody:setDynamicProperties(
        pos,
        Quaternion.new(Radian.new(Degree(math.random()*360)), Vector3(0, 0, 1)),
        Vector3(0, 0, 0),
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    entity:addComponent(rigidBody)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.meshName = mesh
    sceneNode.transform.scale = Vector3(scale, scale, scale)
    entity:addComponent(sceneNode)
    return entity
end

function createCompoundCloud(compoundName, x, y, amount)
    if amount == nil then amount = currentBiome.compounds[compoundName] end
    if amount == nil then amount = 0 end

    if compoundTable[compoundName] and compoundTable[compoundName].isCloud then
        -- addCloud requires integer arguments
        x = math.floor(x)
        y = math.floor(y)
        getComponent("compound_cloud_" .. compoundName,
                     g_luaEngine.currentGameState, CompoundCloudComponent
        ):addCloud(amount, x, y)
    end
end

function createAgentCloud(compoundId, x, y, direction, amount)    
    
    local agentEntity = Entity.new(g_luaEngine.currentGameState.wrapper)

    local reactionHandler = CollisionComponent.new()
    reactionHandler:addCollisionGroup("agent")
    agentEntity:addComponent(reactionHandler)
        
    local rigidBody = RigidBodyComponent.new()
    rigidBody.properties.mass = 0.001
    rigidBody.properties.friction = 0.4
    rigidBody.properties.linearDamping = 0.4
    rigidBody.properties.shape = SphereShape.new(HEX_SIZE)
    rigidBody:setDynamicProperties(
        Vector3(x,y,0) + direction,
        Quaternion.new(Radian.new(Degree(math.random()*360)), Vector3(0, 0, 1)),
        direction * 3,
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    agentEntity:addComponent(rigidBody)
    
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.meshName = "oxytoxy.mesh"
    agentEntity:addComponent(sceneNode)
    
    local timedLifeComponent = TimedLifeComponent.new()
    timedLifeComponent.timeToLive = 2000
    agentEntity:addComponent(timedLifeComponent)
    
end

local function addEmitter2Entity(entity, compound)
    local compoundEmitter = CompoundEmitterComponent.new()
    entity:addComponent(compoundEmitter)
    compoundEmitter.emissionRadius = 1
    compoundEmitter.maxInitialSpeed = 10
    compoundEmitter.minInitialSpeed = 2
    compoundEmitter.minEmissionAngle = Degree(0)
    compoundEmitter.maxEmissionAngle = Degree(360)
    compoundEmitter.particleLifeTime = 5000
    local timedEmitter = TimedCompoundEmitterComponent.new()
    timedEmitter.compoundId = CompoundRegistry.getCompoundId(compound)
    timedEmitter.particlesPerEmission = 1
    timedEmitter.potencyPerParticle = 2.0
    timedEmitter.emitInterval = 1000
    entity:addComponent(timedEmitter)
end

local function createSpawnSystem()
    local spawnSystem = SpawnSystem.new()

    local toxinOrganelleSpawnFunction = function(pos)
        powerupEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
        setSpawnablePhysics(powerupEntity, pos, "AgentVacuole.mesh", 0.9,
                            SphereShape.new(HEX_SIZE))

        local reactionHandler = CollisionComponent.new()
        reactionHandler:addCollisionGroup("powerup")
        powerupEntity:addComponent(reactionHandler)
        
        local powerupComponent = PowerupComponent.new()
        -- Function name must be in configs.lua
        powerupComponent:setEffect("toxinEffect")
        powerupEntity:addComponent(powerupComponent)
        return powerupEntity
    end
    local ChloroplastOrganelleSpawnFunction = function(pos) 
        powerupEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
        setSpawnablePhysics(powerupEntity, pos, "chloroplast.mesh", 0.9,
                            SphereShape.new(HEX_SIZE))

        local reactionHandler = CollisionComponent.new()
        reactionHandler:addCollisionGroup("powerup")
        powerupEntity:addComponent(reactionHandler)
        
        local powerupComponent = PowerupComponent.new()
        -- Function name must be in configs.lua
        powerupComponent:setEffect("chloroplastEffect")
        powerupEntity:addComponent(powerupComponent)
        return powerupEntity
    end

    for compoundName, compoundInfo in pairs(compoundTable) do
        if compoundInfo.isCloud then
            local spawnCloud =  function(pos)
                createCompoundCloud(compoundName, pos.x, pos.y)
            end

            spawnSystem:addSpawnType(spawnCloud, CLOUD_SPAWN_DENSITY, CLOUD_SPAWN_RADIUS)
        end
    end

    for bacteriaName, _ in pairs(bacteriaTable) do
        local spawnBacteria =  function(pos)
            Bacterium.createBacterium(bacteriaName, pos, g_luaEngine.currentGameState)
        end

        -- TODO: make the density change on biome change.
        spawnSystem:addSpawnType(spawnBacteria, BACTERIA_SPAWN_DENSITY, BACTERIA_SPAWN_RADIUS)
    end

    spawnSystem:addSpawnType(toxinOrganelleSpawnFunction, 1/17000, 50)
    spawnSystem:addSpawnType(ChloroplastOrganelleSpawnFunction, 1/12000, 50)

    for name, species in pairs(starter_microbes) do

        assert(isNotEmpty(name))
        assert(species)
        
        spawnSystem:addSpawnType(
            function(pos) 
                return microbeSpawnFunctionGeneric(pos, name, true, nil,
                                                   g_luaEngine.currentGameState)
            end, 
            species.spawnDensity, 60)
    end
    return spawnSystem
end

local function setupPlayer(gameState)

    assert(GameState.MICROBE == gameState)
    assert(gameState ~= nil)
    
    local microbe = microbeSpawnFunctionGeneric(nil, "Default", false, PLAYER_NAME, gameState)
    microbe.collisionHandler:addCollisionGroup("powerupable")
    Engine:playerData():lockedMap():addLock("Toxin")
    Engine:playerData():lockedMap():addLock("chloroplast")
    Engine:playerData():setActiveCreature(microbe.entity.id, gameState.wrapper)

    -- Give some atp
    microbe:storeCompound(CompoundRegistry.getCompoundId("atp"), 50, false)
end

local function setupSound(gameState)
    local ambientEntity = Entity.new("ambience", gameState.wrapper)
    local soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = true
    soundSource.volumeMultiplier = 0.3
    ambientEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("microbe-theme-1", "microbe-theme-1.ogg")
    soundSource:addSound("microbe-theme-3", "microbe-theme-3.ogg")
    soundSource:addSound("microbe-theme-4", "microbe-theme-4.ogg")
    soundSource:addSound("microbe-theme-5", "microbe-theme-5.ogg")
    soundSource:addSound("microbe-theme-6", "microbe-theme-6.ogg")   
    soundSource:addSound("microbe-theme-7", "microbe-theme-7.ogg")   
    local ambientEntity2 = Entity.new("ambience2", gameState.wrapper)
    local soundSource = SoundSourceComponent.new()
    soundSource.volumeMultiplier = 0.1
    soundSource.ambientSoundSource = true
    ambientSound = soundSource:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
    soundSource.autoLoop = true
     ambientEntity2:addComponent(soundSource)
    -- Gui effects
    local guiSoundEntity = Entity.new("gui_sounds", gameState.wrapper)
    soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
    soundSource:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    local listener = Entity.new("soundListener", gameState.wrapper)
    local sceneNode = OgreSceneNodeComponent.new()
    listener:addComponent(sceneNode)
end

setupCompounds()
setupProcesses()

local function createMicrobeStage(name)
    return 
        g_luaEngine:createGameState(
        name,
        {
            MicrobeReplacementSystem.new(),
            -- SwitchGameStateSystem.new(),
            QuickSaveSystem.new(),
            -- Microbe specific
            MicrobeSystem.new(),
            MicrobeCameraSystem.new(),
            MicrobeAISystem.new(),
            MicrobeControlSystem.new(),
            HudSystem.new(),
            TimedLifeSystem.new(),
            CompoundMovementSystem.new(),
            CompoundAbsorberSystem.new(),
            ProcessSystem.new(),
            --PopulationSystem.new(),
            PatchSystem.new(),
            SpeciesSystem.new(),
            BacteriaSystem.new(),
            -- Physics
            RigidBodyInputSystem.new(),
            UpdatePhysicsSystem.new(),
            RigidBodyOutputSystem.new(),
            BulletToOgreSystem.new(),
            CollisionSystem.new(),
            -- Microbe Specific again (order sensitive)
            createSpawnSystem(),
            -- Graphics
            OgreAddSceneNodeSystem.new(),
            OgreUpdateSceneNodeSystem.new(),
            OgreCameraSystem.new(),
            OgreLightSystem.new(),
            SkySystem.new(),
            OgreWorkspaceSystem.new(),
            OgreRemoveSceneNodeSystem.new(),
            RenderSystem.new(),
            MembraneSystem.new(),
            CompoundCloudSystem.new(),
            --AgentCloudSystem.new(),
            -- Other
            SoundSourceSystem.new(),
            PowerupSystem.new(),
            CompoundEmitterSystem.new(), -- Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
        },
        true,
        "MicrobeStage",
        function(gameState)
            setupBackground(gameState)
            setupCamera(gameState)
            setupCompoundClouds(gameState)
            setupSpecies(gameState)
            initBacterialSpecies(gameState)
            setupPlayer(gameState)
            setupSound(gameState)
        end
    )
end

GameState.MICROBE = createMicrobeStage("microbe")
--GameState.MICROBE_ALTERNATE = createMicrobeStage("microbe_alternate")
--Engine:setCurrentGameState(GameState.MICROBE)
