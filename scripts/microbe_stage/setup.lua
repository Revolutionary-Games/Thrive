
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = "Background"
	skyplane.properties.scale = 200
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

local function setupCamera()
    local entity = Entity(CAMERA_NAME)
    -- Camera
    local camera = OgreCameraComponent("camera")
    camera.properties.nearClipDistance = 5
    camera.offset = Vector3(0, 0, 30)
    camera.properties:touch()
    entity:addComponent(camera)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.transform.position.z = 30
    sceneNode.transform:touch()
    entity:addComponent(sceneNode)
    -- Light
    local light = OgreLightComponent()
    light:setRange(200)
    entity:addComponent(light)
    -- Workspace
    local workspaceEntity = Entity()
    local workspaceComponent = OgreWorkspaceComponent("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupCompounds()

    local ordered_keys = {}

    for k in pairs(compounds) do
        table.insert(ordered_keys, k)
    end

    table.sort(ordered_keys)
    for i = 1, #ordered_keys do
        local k, v = ordered_keys[i], compounds[ ordered_keys[i] ]
        CompoundRegistry.registerCompoundType(k, v["name"], v["mesh"], v["size"], v["weight"])
    end    
    CompoundRegistry.loadFromLua({}, agents)
    --CompoundRegistry.loadFromXML("../scripts/definitions/compounds.xml")
end

local function setupCompoundClouds()
    local compoundId = CompoundRegistry.getCompoundId("glucose")
    local entity = Entity("compound_cloud_glucose")
    local compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 150, 170, 180)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("oxygen")
    entity = Entity("compound_cloud_oxygen")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 60, 160, 180)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("co2")
    entity = Entity("compound_cloud_co2")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 20, 50, 100)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("ammonia")
    entity = Entity("compound_cloud_ammonia")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 255, 220, 50)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("aminoacids")
    entity = Entity("compound_cloud_aminoacids")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 255, 150, 200)
    entity:addComponent(compoundCloud)
end

--  This isn't a finished solution. Optimally the process class would be moved to CPP and loaded there entirely.
global_processMap = {}
local function setupProcesses()
    -- BioProcessRegistry.loadFromXML("../scripts/definitions/processes.xml")
    BioProcessRegistry.loadFromLua(processes)
    for processId in BioProcessRegistry.getList() do
        local inputCompounds = {}
        local outputCompounds = {}
        
        for recipyCompound in BioProcessRegistry.getInputCompounds(processId) do
            inputCompounds[recipyCompound.compoundId] = recipyCompound.amount
        end
        for recipyCompound in BioProcessRegistry.getOutputCompounds(processId) do
            outputCompounds[recipyCompound.compoundId] = recipyCompound.amount
        end
        
        global_processMap[BioProcessRegistry.getInternalName(processId)] = Process(
            BioProcessRegistry.getSpeedFactor(processId),
            inputCompounds,
            outputCompounds
        )
    end
end

function setupSpecies()
    --[[
    This function should be the entry point for all initial-species generation
    For now, it can go through the XML and instantiate all the species, but later this 
    would be all procedural.
    Together with the mutate function, these would be the only ways species are created
    ]]
    
    for name, data in pairs(starter_microbes) do
        speciesEntity = Entity(name)
        speciesComponent = SpeciesComponent(name)
        speciesEntity:addComponent(speciesComponent)
        for i, organelle in pairs(data.organelles) do
            local org = {}
            org.name = organelle.name
            org.q = organelle.q
            org.r = organelle.r
            org.rotation = organelle.rotation
            speciesComponent.organelles[i] = org
        end
        processorComponent = ProcessorComponent()
        speciesEntity:addComponent(processorComponent)
        speciesComponent.colour = Vector3(data.colour.r, data.colour.g, data.colour.b)

        -- iterates over all compounds, and sets amounts and priorities
        for compoundID in CompoundRegistry.getCompoundList() do
            compound = CompoundRegistry.getCompoundInternalName(compoundID)
            thresholdData = default_thresholds[compound]
             -- we'll need to generate defaults from species template
            processorComponent:setThreshold(compoundID, thresholdData.low, thresholdData.high, thresholdData.vent)
            compoundData = data.compounds[compound]
            if compoundData ~= nil then
                amount = compoundData.amount
                -- priority = compoundData.priority
                speciesComponent.avgCompoundAmounts["" .. compoundID] = amount
                -- speciesComponent.compoundPriorities[compoundID] = priority
            end
        end
        if data[thresholds] ~= nil then
            local thresholds = data[thresholds]
            for compoundID in CompoundRegistry.getCompoundList() do
                compound = CompoundRegistry.getCompoundInternalName(compoundID)
                if thresholds[compound] ~= nil then
                    if thresholds[compound].low ~= nil then
                        processorComponent:setLowThreshold(compoundID, thresholds[compound].low)
                    end
                    if thresholds[compound].low ~= nil then
                        processorComponent:setHighThreshold(compoundID, thresholds[compound].high)
                    end
                    if thresholds[compound].vent ~= nil then
                        processorComponent:setVentThreshold(compoundID, thresholds[compound].vent)
                    end
                end
            end
        end
        local capacities = {}
        for _, organelle in pairs(data.organelles) do
            if organelles[organelle.name] ~= nil then
                if organelles[organelle.name]["processes"] ~= nil then
                    for process, capacity in pairs(organelles[organelle.name]["processes"]) do
                        if capacities[process] == nil then
                            capacities[process] = 0
                        end
                        capacities[process] = capacities[process] + capacity
                    end
                end
            end
        end
        for bioProcessId in BioProcessRegistry.getList() do
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
function microbeSpawnFunctionGeneric(pos, speciesName, aiControlled, individualName)
    local microbe = Microbe.createMicrobeEntity(individualName, aiControlled, speciesName)
    if pos ~= nil then
        microbe.rigidBody:setDynamicProperties(
            pos, -- Position
            Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), -- Orientation
            Vector3(0, 0, 0), -- Linear velocity
            Vector3(0, 0, 0)  -- Angular velocity
        )
    end
    return microbe
end

local function setSpawnablePhysics(entity, pos, mesh, scale, collisionShape)
    -- Rigid body
    local rigidBody = RigidBodyComponent()
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearDamping = 0.8

    rigidBody.properties.shape = collisionShape
    rigidBody:setDynamicProperties(
        pos,
        Quaternion(Radian(Degree(math.random()*360)), Vector3(0, 0, 1)),
        Vector3(0, 0, 0),
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    entity:addComponent(rigidBody)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = mesh
    sceneNode.transform.scale = Vector3(scale, scale, scale)
    entity:addComponent(sceneNode)
    return entity
end

function createCompoundCloud(compoundName, x, y, amount)
    if compoundName == "aminoacids" or compoundName == "glucose" or compoundName == "co2" or compoundName == "oxygen" or compoundName == "ammonia" then
        Entity("compound_cloud_" .. compoundName):getComponent(CompoundCloudComponent.TYPE_ID):addCloud(amount, x, y)
    end
end

function createAgentCloud(compoundId, x, y, direction, amount)
    -- local entity = Entity()
    -- local sceneNode = OgreSceneNodeComponent()
    -- sceneNode.meshName = "oxytoxy.mesh"
    -- sceneNode.transform.position = Vector3(x + direction.x/2, y + direction.y/2, 0)
    -- sceneNode.transform:touch()
    -- local agent = AgentCloudComponent()
    -- agent:initialize(compoundId, 255, 0, 255)
    -- agent.direction = direction*2
    -- agent.potency = amount
    -- entity:addComponent(sceneNode)
    -- entity:addComponent(agent)
    
    
    local agentEntity = Entity()

    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("agent")
    agentEntity:addComponent(reactionHandler)
        
    local rigidBody = RigidBodyComponent()
    rigidBody.properties.mass = 0.001
    rigidBody.properties.friction = 0.4
    rigidBody.properties.linearDamping = 0.4
    rigidBody.properties.shape = SphereShape(HEX_SIZE)
    rigidBody:setDynamicProperties(
        Vector3(x,y,0) + direction,
        Quaternion(Radian(Degree(math.random()*360)), Vector3(0, 0, 1)),
        direction * 3,
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    agentEntity:addComponent(rigidBody)
    
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "oxytoxy.mesh"
    agentEntity:addComponent(sceneNode)
    
    local timedLifeComponent = TimedLifeComponent()
    timedLifeComponent.timeToLive = 2000
    agentEntity:addComponent(timedLifeComponent)
    
end

local function addEmitter2Entity(entity, compound)
    local compoundEmitter = CompoundEmitterComponent()
    entity:addComponent(compoundEmitter)
    compoundEmitter.emissionRadius = 1
    compoundEmitter.maxInitialSpeed = 10
    compoundEmitter.minInitialSpeed = 2
    compoundEmitter.minEmissionAngle = Degree(0)
    compoundEmitter.maxEmissionAngle = Degree(360)
    compoundEmitter.particleLifeTime = 5000
    local timedEmitter = TimedCompoundEmitterComponent()
    timedEmitter.compoundId = CompoundRegistry.getCompoundId(compound)
    timedEmitter.particlesPerEmission = 1
    timedEmitter.potencyPerParticle = 2.0
    timedEmitter.emitInterval = 1000
    entity:addComponent(timedEmitter)
end

local function createSpawnSystem()
    local spawnSystem = SpawnSystem()

    local toxinOrganelleSpawnFunction = function(pos)
        powerupEntity = Entity()
        setSpawnablePhysics(powerupEntity, pos, "AgentVacuole.mesh", 0.9, SphereShape(HEX_SIZE))

        local reactionHandler = CollisionComponent()
        reactionHandler:addCollisionGroup("powerup")
        powerupEntity:addComponent(reactionHandler)
        
        local powerupComponent = PowerupComponent()
        -- Function name must be in configs.lua
        powerupComponent:setEffect("toxinEffect")
        powerupEntity:addComponent(powerupComponent)
        return powerupEntity
    end
    local ChloroplastOrganelleSpawnFunction = function(pos) 
        powerupEntity = Entity()
        setSpawnablePhysics(powerupEntity, pos, "chloroplast.mesh", 0.9, SphereShape(HEX_SIZE))

        local reactionHandler = CollisionComponent()
        reactionHandler:addCollisionGroup("powerup")
        powerupEntity:addComponent(reactionHandler)
        
        local powerupComponent = PowerupComponent()
        -- Function name must be in configs.lua
        powerupComponent:setEffect("chloroplastEffect")
        powerupEntity:addComponent(powerupComponent)
        return powerupEntity
    end
        
    local spawnGlucoseCloud =  function(pos)
        createCompoundCloud("glucose", pos.x, pos.y, 75000)
    end
    local spawnOxygenCloud =  function(pos)
        createCompoundCloud("oxygen", pos.x, pos.y, 75000)
    end
    local spawnCO2Cloud =  function(pos)
        createCompoundCloud("co2", pos.x, pos.y, 75000)
    end
    local spawnAmmoniaCloud =  function(pos)
        createCompoundCloud("ammonia", pos.x, pos.y, 75000)
    end

    --Spawn one emitter on average once in every square of sidelength 10
    -- (square dekaunit?)
	-- Spawn radius should depend on view rectangle
    --spawnSystem:addSpawnType(spawnOxygenEmitter, 1/1600, 50)
    --spawnSystem:addSpawnType(spawnCO2Emitter, 1/1700, 50)
    --spawnSystem:addSpawnType(spawnGlucoseEmitter, 1/1600, 50)
    --spawnSystem:addSpawnType(spawnAmmoniaEmitter, 1/2250, 50)
    
    spawnSystem:addSpawnType(toxinOrganelleSpawnFunction, 1/17000, 50)
    spawnSystem:addSpawnType(ChloroplastOrganelleSpawnFunction, 1/12000, 50)
    
    spawnSystem:addSpawnType(spawnGlucoseCloud, 1/5000, 50)
    spawnSystem:addSpawnType(spawnCO2Cloud, 1/5000, 50)
    spawnSystem:addSpawnType(spawnAmmoniaCloud, 1/5000, 50)
    spawnSystem:addSpawnType(spawnOxygenCloud, 1/5000, 50)

    for name, species in pairs(starter_microbes) do
        spawnSystem:addSpawnType(
            function(pos) 
                return microbeSpawnFunctionGeneric(pos, name, true, nil)
            end, 
            species.spawnDensity, 60)
    end
    return spawnSystem
end

local function setupEmitter()
    -- -- Setting up a test emitter
    -- local entity = Entity("glucose-emitter")
    -- -- Rigid body
    -- local rigidBody = RigidBodyComponent()
    -- rigidBody.properties.friction = 0.2
    -- rigidBody.properties.linearDamping = 0.8
    -- rigidBody.properties.shape = CylinderShape(
        -- CollisionShape.AXIS_X, 
        -- 0.4,
        -- 2.0
    -- )
    -- rigidBody:setDynamicProperties(
        -- Vector3(10, 0, 0),
        -- Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)),
        -- Vector3(0, 0, 0),
        -- Vector3(0, 0, 0)
    -- )
    -- rigidBody.properties:touch()
    -- entity:addComponent(rigidBody)
    -- local reactionHandler = CollisionComponent()
    -- reactionHandler:addCollisionGroup("emitter")
    -- entity:addComponent(reactionHandler)
    -- -- Scene node
    -- local sceneNode = OgreSceneNodeComponent()
    -- sceneNode.meshName = "molecule.mesh"
    -- entity:addComponent(sceneNode)
    -- -- Emitter test
    -- addEmitter2Entity(entity, "glucose")
end

local function setupPlayer()
    local microbe = microbeSpawnFunctionGeneric(nil, "Default", false, PLAYER_NAME)
    microbe.collisionHandler:addCollisionGroup("powerupable")
    Engine:playerData():lockedMap():addLock("Toxin")
    Engine:playerData():lockedMap():addLock("chloroplast")
    Engine:playerData():setActiveCreature(microbe.entity.id, GameState.MICROBE)
end

local function setupSound()
    local ambientEntity = Entity("ambience")
    local soundSource = SoundSourceComponent()
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
    local ambientEntity2 = Entity("ambience2")
    local soundSource = SoundSourceComponent()
    soundSource.volumeMultiplier = 0.1
    soundSource.ambientSoundSource = true
    ambientSound = soundSource:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
    soundSource.autoLoop = true
     ambientEntity2:addComponent(soundSource)
    -- Gui effects
    local guiSoundEntity = Entity("gui_sounds")
    soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
    soundSource:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    local listener = Entity("soundListener")
    local sceneNode = OgreSceneNodeComponent()
    listener:addComponent(sceneNode)
end

setupCompounds()
setupProcesses()

local function createMicrobeStage(name)
    return 
        Engine:createGameState(
        name,
        {
            MicrobeReplacementSystem(),
            -- SwitchGameStateSystem(),
            QuickSaveSystem(),
            -- Microbe specific
            MicrobeSystem(),
            MicrobeCameraSystem(),
            MicrobeAISystem(),
            MicrobeControlSystem(),
            HudSystem(),
            TimedLifeSystem(),
            CompoundMovementSystem(),
            CompoundAbsorberSystem(),
            ProcessSystem(),
            --PopulationSystem(),
            PatchSystem(),
            SpeciesSystem(),
            -- Physics
            RigidBodyInputSystem(),
            UpdatePhysicsSystem(),
            RigidBodyOutputSystem(),
            BulletToOgreSystem(),
            CollisionSystem(),
            -- Microbe Specific again (order sensitive)
            createSpawnSystem(),
            -- Graphics
            OgreAddSceneNodeSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            OgreLightSystem(),
            SkySystem(),
            OgreWorkspaceSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
            MembraneSystem(),
            CompoundCloudSystem(),
            --AgentCloudSystem(),
            -- Other
            SoundSourceSystem(),
            PowerupSystem(),
            CompoundEmitterSystem(), -- Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
        },
        function()
            setupBackground()
            setupCamera()
            setupCompoundClouds()
            setupEmitter()
            setupSpecies()
            setupPlayer()
            setupSound()
        end,
        "MicrobeStage"
    )
end

GameState.MICROBE = createMicrobeStage("microbe")
--GameState.MICROBE_ALTERNATE = createMicrobeStage("microbe_alternate")
--Engine:setCurrentGameState(GameState.MICROBE)
