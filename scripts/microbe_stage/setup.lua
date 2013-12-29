
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
    CompoundRegistry.registerCompoundType("atp", "ATP", "molecule.mesh")
    CompoundRegistry.registerCompoundType("oxygen", "Oxygen", "molecule.mesh")    
    CompoundRegistry.registerCompoundType("nitrate", "Nitrate", "molecule.mesh")
    CompoundRegistry.registerCompoundType("glucose", "Glucose", "molecule.mesh")
    CompoundRegistry.registerCompoundType("co2", "CO2", "molecule.mesh")
    CompoundRegistry.registerCompoundType("oxytoxy", "OxyToxy NT", "molecule.mesh")
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
    
    --Spawn one emitter on average once in every square of sidelength 10
    -- (square dekaunit?)
    spawnSystem:addSpawnType(testFunction, 1/20^2, 30)
    spawnSystem:addSpawnType(testFunction2, 1/20^2, 30)
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


local function setupHud()
    local ENERGY_WIDTH = 200
    local ENERGY_HEIGHT = 32
    local energyCount = Entity("hud.energyCount")
    local energyText = TextOverlayComponent("hud.energyCount")
    energyCount:addComponent(energyText)
    energyText.properties.horizontalAlignment = TextOverlayComponent.Center
    energyText.properties.verticalAlignment = TextOverlayComponent.Bottom
    energyText.properties.width = ENERGY_WIDTH
    energyText.properties.height = ENERGY_HEIGHT
    energyText.properties.left = -ENERGY_WIDTH / 2
    energyText.properties.top = - ENERGY_HEIGHT
    energyText.properties:touch()
    -- Setting up hud element for displaying all compounds
    local COMPOUNDS_WIDTH = 200
    local COMPOUNDS_HEIGHT = 32    
    local playerCompoundList = Entity("hud.playerCompounds")
    local playerCompoundText = TextOverlayComponent("hud.playerCompounds")
    playerCompoundList:addComponent(playerCompoundText)
    playerCompoundText.properties.horizontalAlignment = TextOverlayComponent.Right
    playerCompoundText.properties.verticalAlignment = TextOverlayComponent.Bottom
    playerCompoundText.properties.width = COMPOUNDS_WIDTH 
    playerCompoundText.properties.height = COMPOUNDS_HEIGHT  -- Note that height and top will change dynamically with the number of compounds displayed
    playerCompoundText.properties.left = -COMPOUNDS_WIDTH
    playerCompoundText.properties.top = -COMPOUNDS_HEIGHT
    playerCompoundText.properties:touch()
    local playerCompoundCounts = Entity("hud.playerCompoundCounts")
    local playerCompoundCountText = TextOverlayComponent("hud.playerCompoundCounts")
    playerCompoundCounts:addComponent(playerCompoundCountText)
    playerCompoundCountText.properties.horizontalAlignment = TextOverlayComponent.Right
    playerCompoundCountText.properties.verticalAlignment = TextOverlayComponent.Bottom
    playerCompoundCountText.properties.width = COMPOUNDS_WIDTH
    playerCompoundCountText.properties.height = COMPOUNDS_HEIGHT  
    playerCompoundCountText.properties.left = -80
    playerCompoundCountText.properties.top = -COMPOUNDS_HEIGHT
    playerCompoundCountText.properties:touch()
end

local function setupPlayer()
    local player = Microbe.createMicrobeEntity(PLAYER_NAME)
    -- Forward
    local forwardOrganelle = MovementOrganelle(
        Vector3(0.0, 50.0, 0.0),
        300
    )
    forwardOrganelle:addHex(0, 0)
    forwardOrganelle:addHex(-1, 0)
    forwardOrganelle:addHex(1, -1)
    forwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
    player:addOrganelle(0, 1, forwardOrganelle)
    -- Backward
    local backwardOrganelle = MovementOrganelle(
        Vector3(0.0, -50.0, 0.0),
        300
    )
    backwardOrganelle:addHex(0, 0)
    backwardOrganelle:addHex(-1, 1)
    backwardOrganelle:addHex(1, 0)
    backwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
    player:addOrganelle(0, -2, backwardOrganelle)
    -- Storage energy
    local storageOrganelle = StorageOrganelle(10, 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, 0, storageOrganelle)
    -- Storage compound 2
    local storageOrganelle2 = StorageOrganelle(10, 100.0)
    storageOrganelle2:addHex(0, 0)
    storageOrganelle2:setColour(ColourValue(0, 1, 0.5, 1))
    player:addOrganelle(0, -1, storageOrganelle2)
    -- Storage compound 3
    local storageOrganelle3 = StorageOrganelle(10, 100.0)
    storageOrganelle3:addHex(0, 0)
    storageOrganelle3:setColour(ColourValue(0.5, 1, 0, 1))
    player:addOrganelle(-1, 0, storageOrganelle3)
	player:storeCompound(CompoundRegistry.getCompoundId("atp"), 20)
    -- Producer making atp from oxygen and glucose
    local processOrganelle1 = ProcessOrganelle(20000) -- 20 second minimum time between producing oxytoxy
    processOrganelle1:addRecipyInput(CompoundRegistry.getCompoundId("glucose"), 1)
    processOrganelle1:addRecipyInput(CompoundRegistry.getCompoundId("oxygen"), 6)
    processOrganelle1:addRecipyOutput(CompoundRegistry.getCompoundId("atp"), 38)
    processOrganelle1:addRecipyOutput(CompoundRegistry.getCompoundId("co2"), 6)
    processOrganelle1:addHex(0, 0)
    processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
    player:addOrganelle(1, -1, processOrganelle1)
end

setupCompounds()

local function createMicrobeStage(name)
    return Engine:createGameState(
        name,
        {
            SwitchGameStateSystem(),
            QuickSaveSystem(),
            -- Microbe specific
            MicrobeSystem(),
            MicrobeCameraSystem(),
            MicrobeControlSystem(),
            HudSystem(),
            CompoundLifetimeSystem(),
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
            setupHud()
            setupPlayer()
        end
    )
end

GameState.MICROBE = createMicrobeStage("microbe")
GameState.MICROBE_ALTERNATE = createMicrobeStage("microbe_alternate")

Engine:setCurrentGameState(GameState.MICROBE)
