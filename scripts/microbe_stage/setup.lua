
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 1)
    skyplane.properties.plane.d = 1000
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

local function setupCamera()
    local entity = Entity(CAMERA_NAME)
    -- Camera
    local camera = OgreCameraComponent("camera")
    camera.properties.nearClipDistance = 5
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
    -- Viewport
    local viewportEntity = Entity()
    local viewportComponent = OgreViewportComponent(0)
    viewportComponent.properties.cameraEntity = entity
    viewportComponent.properties:touch()
    viewportEntity:addComponent(viewportComponent)
end

local function setupCompounds()
    CompoundRegistry.registerCompoundType("atp", "ATP", "atp.mesh", 0.1, 1)
    CompoundRegistry.registerCompoundType("oxygen", "Oxygen", "molecule.mesh", 0.3, 1 )    
    CompoundRegistry.registerCompoundType("nitrate", "Nitrate", "molecule.mesh", 0.3, 1)
    CompoundRegistry.registerCompoundType("glucose", "Glucose", "glucose.mesh", 0.3, 1)
    CompoundRegistry.registerCompoundType("co2", "CO2", "co2.mesh", 0.16, 1)
    CompoundRegistry.registerCompoundType("oxytoxy", "OxyToxy NT", "oxytoxy.mesh", 0.3, 1)
end

local function createSpawnSystem()
    local spawnSystem = SpawnSystem()
    
    local testFunction = function(pos)
        -- Setting up an emitter for oxygen
        local entity = Entity()
        -- Rigid body
        local rigidBody = RigidBodyComponent()
        rigidBody.properties.friction = 0.2
        rigidBody.properties.linearDamping = 0.8
        rigidBody.properties.shape = CylinderShape(
            CollisionShape.AXIS_X, 
            0.4,
            2.0
        )
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
        sceneNode.meshName = "molecule.mesh"
        entity:addComponent(sceneNode)
        -- Emitter oxygen
        local oxygenEmitter = CompoundEmitterComponent()
        entity:addComponent(oxygenEmitter)
        oxygenEmitter.emissionRadius = 1
        oxygenEmitter.maxInitialSpeed = 10
        oxygenEmitter.minInitialSpeed = 2
        oxygenEmitter.minEmissionAngle = Degree(0)
        oxygenEmitter.maxEmissionAngle = Degree(360)
        oxygenEmitter.particleLifeTime = 5000
        local timedEmitter = TimedCompoundEmitterComponent()
        timedEmitter.compoundId = CompoundRegistry.getCompoundId("oxygen")
        timedEmitter.particlesPerEmission = 1
        timedEmitter.potencyPerParticle = 2.0
        timedEmitter.emitInterval = 1000
        entity:addComponent(timedEmitter)
        return entity
    end
    local testFunction2 = function(pos)
        -- Setting up an emitter for glucose
        local entity = Entity()
        -- Rigid body
        local rigidBody = RigidBodyComponent()
        rigidBody.properties.friction = 0.2
        rigidBody.properties.linearDamping = 0.8
        rigidBody.properties.shape = CylinderShape(
            CollisionShape.AXIS_X, 
            0.4,
            2.0
        )
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
        sceneNode.meshName = "molecule.mesh"
        entity:addComponent(sceneNode)
        -- Emitter glucose
        local glucoseEmitter = CompoundEmitterComponent()
        entity:addComponent(glucoseEmitter)
        glucoseEmitter.emissionRadius = 1
        glucoseEmitter.maxInitialSpeed = 10
        glucoseEmitter.minInitialSpeed = 2
        glucoseEmitter.minEmissionAngle = Degree(0)
        glucoseEmitter.maxEmissionAngle = Degree(360)
        glucoseEmitter.particleLifeTime = 5000
        local timedEmitter = TimedCompoundEmitterComponent()
        timedEmitter.compoundId = CompoundRegistry.getCompoundId("glucose")
        timedEmitter.particlesPerEmission = 1
        timedEmitter.potencyPerParticle = 1.0
        timedEmitter.emitInterval = 2000
        entity:addComponent(timedEmitter)
        return entity
    end
    
    local microbeSpawnFunction = function(pos)
        local microbe = Microbe.createMicrobeEntity(nil, true)
        microbe.rigidBody:setDynamicProperties(
            pos, -- Position
            Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), -- Orientation
            Vector3(0, 0, 0), -- Linear velocity
            Vector3(0, 0, 0)  -- Angular velocity
        )
        -- Forward
        local forwardOrganelle = MovementOrganelle(
            Vector3(0.0, 50.0, 0.0),
            300
        )
        forwardOrganelle:addHex(0, 0)
        forwardOrganelle:addHex(-1, 0)
        forwardOrganelle:addHex(1, -1)
        forwardOrganelle:setColour(ColourValue(0, 0.7, 0.7, 1))
        microbe:addOrganelle(0, 1, forwardOrganelle)
        -- Backward
        local backwardOrganelle = MovementOrganelle(
            Vector3(0.0, -50.0, 0.0),
            300
        )
        backwardOrganelle:addHex(0, 0) 
        backwardOrganelle:addHex(-1, 1)
        backwardOrganelle:addHex(1, 0)
        backwardOrganelle:setColour(ColourValue(0, 0.7, 0.7, 1))
        microbe:addOrganelle(0, -2, backwardOrganelle)
        -- Storage energy
        local storageOrganelle = StorageOrganelle(100.0)
        storageOrganelle:addHex(0, 0)
        storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
        microbe:addOrganelle(0, 0, storageOrganelle)
        microbe:storeCompound(CompoundRegistry.getCompoundId("atp"), 40, false)
        -- Storage compound 2
        local storageOrganelle2 = StorageOrganelle(100.0)
        storageOrganelle2:addHex(0, 0)
        storageOrganelle2:setColour(ColourValue(0, 1, 0.5, 1))
        microbe:addOrganelle(0, -1, storageOrganelle2)
        -- Storage compound 3
        local storageOrganelle3 = StorageOrganelle(100.0)
        storageOrganelle3:addHex(0, 0)
        storageOrganelle3:setColour(ColourValue(0.5, 1, 0, 1))
        microbe:addOrganelle(-1, 0, storageOrganelle3)
        -- Producer making atp from oxygen and glucose
        local processOrganelle1 = ProcessOrganelle()
        local inputCompounds = {[CompoundRegistry.getCompoundId("glucose")] = 1,
                                [CompoundRegistry.getCompoundId("oxygen")] = 6}
        local outputCompounds = {[CompoundRegistry.getCompoundId("atp")] = 38,
                                [CompoundRegistry.getCompoundId("co2")] = 6}
        local respiration = Process(0.5, 1.0, inputCompounds, outputCompounds)
        processOrganelle1:addProcess(respiration)
        processOrganelle1:addHex(0, 0)
        processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
        microbe:addOrganelle(1, -1, processOrganelle1)
            
        return microbe
    end
    
    --Spawn one emitter on average once in every square of sidelength 10
    -- (square dekaunit?)
    spawnSystem:addSpawnType(testFunction, 1/20^2, 30)
    spawnSystem:addSpawnType(testFunction2, 1/20^2, 30)
   -- spawnSystem:addSpawnType(microbeSpawnFunction, 1/75^2, 40)
    return spawnSystem
end

local function setupEmitter()
    -- Setting up an emitter for glucose
    local entity = Entity("glucose-emitter")
    -- Rigid body
    local rigidBody = RigidBodyComponent()
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearDamping = 0.8
    rigidBody.properties.shape = CylinderShape(
        CollisionShape.AXIS_X, 
        0.4,
        2.0
    )
    rigidBody:setDynamicProperties(
        Vector3(10, 0, 0),
        Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)),
        Vector3(0, 0, 0),
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    entity:addComponent(rigidBody)
    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("emitter")
    entity:addComponent(reactionHandler)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "molecule.mesh"
    entity:addComponent(sceneNode)
    -- Emitter glucose
    local glucoseEmitter = CompoundEmitterComponent()
    entity:addComponent(glucoseEmitter)
    glucoseEmitter.emissionRadius = 1
    glucoseEmitter.maxInitialSpeed = 10
    glucoseEmitter.minInitialSpeed = 2
    glucoseEmitter.minEmissionAngle = Degree(0)
    glucoseEmitter.maxEmissionAngle = Degree(360)
    glucoseEmitter.meshName = "molecule.mesh"
    glucoseEmitter.particleLifeTime = 5000
    local timedEmitter = TimedCompoundEmitterComponent()
    timedEmitter.compoundId = CompoundRegistry.getCompoundId("glucose")
    timedEmitter.particlesPerEmission = 1
    timedEmitter.potencyPerParticle = 3.0
    timedEmitter.emitInterval = 1000
    entity:addComponent(timedEmitter)
end

local function setupPlayer()
    player = Microbe.createMicrobeEntity(PLAYER_NAME, false)
    -- Forward
    local forwardOrganelle = MovementOrganelle(
        Vector3(0.0, 50.0, 0.0),
        300
    )
    forwardOrganelle:addHex(0, 0)
    forwardOrganelle:addHex(-1, 0)
    forwardOrganelle:addHex(1, -1)
    forwardOrganelle:setColour(ColourValue(0.8, 0.3, 0.3, 1))
    player:addOrganelle(0, 1, forwardOrganelle)
    -- Backward
    local backwardOrganelle = MovementOrganelle(
        Vector3(0.0, -50.0, 0.0),
        300
    )
    backwardOrganelle:addHex(0, 0)
    backwardOrganelle:addHex(-1, 1)
    backwardOrganelle:addHex(1, 0)
    backwardOrganelle:setColour(ColourValue(0.8, 0.3, 0.3, 1))
    player:addOrganelle(0, -2, backwardOrganelle)
    -- Storage organelle 1
    local storageOrganelle = StorageOrganelle(100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, 0, storageOrganelle)
    -- Storage organelle 2
    local storageOrganelle2 = StorageOrganelle(100.0)
    storageOrganelle2:addHex(0, 0)
    storageOrganelle2:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, -1, storageOrganelle2)
    -- Storage organelle 3
    local storageOrganelle3 = StorageOrganelle(100.0)
    storageOrganelle3:addHex(0, 0)
    storageOrganelle3:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(-1, 0, storageOrganelle3)
	player:storeCompound(CompoundRegistry.getCompoundId("atp"), 20, false)
    -- Producer making atp from oxygen and glucose
    local processOrganelle1 = ProcessOrganelle()
    local inputCompounds = {[CompoundRegistry.getCompoundId("glucose")] = 1,
                            [CompoundRegistry.getCompoundId("oxygen")] = 6}
    local outputCompounds = {[CompoundRegistry.getCompoundId("atp")] = 38,
                            [CompoundRegistry.getCompoundId("co2")] = 6}
    local respiration = Process(0.5, 1.0, inputCompounds, outputCompounds)
    processOrganelle1:addProcess(respiration)
    processOrganelle1:addHex(0, 0)
    processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
    player:addOrganelle(1, -1, processOrganelle1)
end

local function setupSound()
    local ambientEntity = Entity("ambience")
    local soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.volumeMultiplier = 0.3
    ambientEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("microbe-theme-1", "microbe-theme-1.ogg")
    soundSource:addSound("microbe-theme-3", "microbe-theme-3.ogg")
    soundSource:addSound("microbe-theme-4", "microbe-theme-4.ogg")
    soundSource:addSound("microbe-theme-5", "microbe-theme-5.ogg")
    soundSource:addSound("microbe-theme-6", "microbe-theme-6.ogg")   
    soundSource:addSound("microbe-theme-7", "microbe-theme-7.ogg")   
end

setupCompounds()

local function createMicrobeStage(name)
    return 
        Engine:createGameState(
        name,
        {
            MicrobeReplacementSystem(),
            SwitchGameStateSystem(),
            QuickSaveSystem(),
            -- Microbe specific
            MicrobeSystem(),
            MicrobeCameraSystem(),
            MicrobeAISystem(),
            MicrobeControlSystem(),
            HudSystem(),
            TimedLifeSystem(),
            CompoundMovementSystem(),
            CompoundEmitterSystem(),
            CompoundAbsorberSystem(),
            createSpawnSystem(),
            -- Physics
            RigidBodyInputSystem(),
            UpdatePhysicsSystem(),
            RigidBodyOutputSystem(),
            BulletToOgreSystem(),
            CollisionSystem(),
            -- Graphics
            OgreAddSceneNodeSystem(),
            SoundSourceSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            OgreLightSystem(),
            SkySystem(),
            TextOverlaySystem(),
            OgreViewportSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
        },
        function()
            setupBackground()
            setupCamera()
            setupEmitter()
            setupPlayer()
            setupSound()
        end,
        "MicrobeStage"
    )
end

player = nil
newPlayerAvaliable = nil -- if this is non-nil the microbe object contained therein replaces the current player microbe



GameState.MICROBE = createMicrobeStage("microbe")
GameState.MICROBE_ALTERNATE = createMicrobeStage("microbe_alternate")

--Engine:setCurrentGameState(GameState.MICROBE)
