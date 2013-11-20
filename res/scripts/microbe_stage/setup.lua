
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

local function setupAgents()
    AgentRegistry.registerAgentType("atp", "ATP")
    AgentRegistry.registerAgentType("oxygen", "Oxygen")    
    AgentRegistry.registerAgentType("nitrate", "Nitrate")
    AgentRegistry.registerAgentType("glucose", "Glucose")
    AgentRegistry.registerAgentType("co2", "CO2")
    AgentRegistry.registerAgentType("oxytoxy", "OxyToxy NT")
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
        local oxygenEmitter = AgentEmitterComponent()
        entity:addComponent(oxygenEmitter)
        oxygenEmitter.agentId = AgentRegistry.getAgentId("oxygen")
        oxygenEmitter.emitInterval = 1000
        oxygenEmitter.emissionRadius = 1
        oxygenEmitter.maxInitialSpeed = 10
        oxygenEmitter.minInitialSpeed = 2
        oxygenEmitter.minEmissionAngle = Degree(0)
        oxygenEmitter.maxEmissionAngle = Degree(360)
        oxygenEmitter.meshName = "molecule.mesh"
        oxygenEmitter.particlesPerEmission = 1
        oxygenEmitter.particleLifeTime = 5000
        oxygenEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
        oxygenEmitter.potencyPerParticle = 2.0
        
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
        local glucoseEmitter = AgentEmitterComponent()
        entity:addComponent(glucoseEmitter)
        glucoseEmitter.agentId = AgentRegistry.getAgentId("glucose")
        glucoseEmitter.emitInterval = 2000
        glucoseEmitter.emissionRadius = 1
        glucoseEmitter.maxInitialSpeed = 10
        glucoseEmitter.minInitialSpeed = 2
        glucoseEmitter.minEmissionAngle = Degree(0)
        glucoseEmitter.maxEmissionAngle = Degree(360)
        glucoseEmitter.meshName = "molecule.mesh"
        glucoseEmitter.particlesPerEmission = 1
        glucoseEmitter.particleLifeTime = 5000
        glucoseEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
        glucoseEmitter.potencyPerParticle = 1.0
        
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
    local glucoseEmitter = AgentEmitterComponent()
    entity:addComponent(glucoseEmitter)
    glucoseEmitter.agentId = AgentRegistry.getAgentId("glucose")
    glucoseEmitter.emitInterval = 1000
    glucoseEmitter.emissionRadius = 1
    glucoseEmitter.maxInitialSpeed = 10
    glucoseEmitter.minInitialSpeed = 2
    glucoseEmitter.minEmissionAngle = Degree(0)
    glucoseEmitter.maxEmissionAngle = Degree(360)
    glucoseEmitter.meshName = "molecule.mesh"
    glucoseEmitter.particlesPerEmission = 1
    glucoseEmitter.particleLifeTime = 5000
    glucoseEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
    glucoseEmitter.potencyPerParticle = 3.0
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
    -- Setting up hud element for displaying all agents
    local AGENTS_WIDTH = 200
    local AGENTS_HEIGHT = 32    
    local playerAgentList = Entity("hud.playerAgents")
    local playerAgentText = TextOverlayComponent("hud.playerAgents")
    playerAgentList:addComponent(playerAgentText)
    playerAgentText.properties.horizontalAlignment = TextOverlayComponent.Right
    playerAgentText.properties.verticalAlignment = TextOverlayComponent.Bottom
    playerAgentText.properties.width = AGENTS_WIDTH 
    playerAgentText.properties.height = AGENTS_HEIGHT  -- Note that height and top will change dynamically with the number of agents displayed
    playerAgentText.properties.left = -AGENTS_WIDTH
    playerAgentText.properties.top = -AGENTS_HEIGHT
    playerAgentText.properties:touch()
    local playerAgentCounts = Entity("hud.playerAgentCounts")
    local playerAgentCountText = TextOverlayComponent("hud.playerAgentCounts")
    playerAgentCounts:addComponent(playerAgentCountText)
    playerAgentCountText.properties.horizontalAlignment = TextOverlayComponent.Right
    playerAgentCountText.properties.verticalAlignment = TextOverlayComponent.Bottom
    playerAgentCountText.properties.width = AGENTS_WIDTH
    playerAgentCountText.properties.height = AGENTS_HEIGHT  
    playerAgentCountText.properties.left = -80
    playerAgentCountText.properties.top = -AGENTS_HEIGHT
    playerAgentCountText.properties:touch()
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
    local storageOrganelle = StorageOrganelle(AgentRegistry.getAgentId("atp"), 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, 0, storageOrganelle)
    player:storeAgent(AgentRegistry.getAgentId("atp"), 20)
    -- Storage agent 2
    local storageOrganelle2 = StorageOrganelle(AgentRegistry.getAgentId("oxygen"), 100.0)
    storageOrganelle2:addHex(0, 0)
    storageOrganelle2:setColour(ColourValue(0, 1, 0.5, 1))
    player:addOrganelle(0, -1, storageOrganelle2)
    -- Storage agent 3
    local storageOrganelle3 = StorageOrganelle(AgentRegistry.getAgentId("glucose"), 100.0)
    storageOrganelle3:addHex(0, 0)
    storageOrganelle3:setColour(ColourValue(0.5, 1, 0, 1))
    player:addOrganelle(-1, 0, storageOrganelle3)
    -- Producer making atp from oxygen and glucose
    local processOrganelle1 = ProcessOrganelle(20000) -- 20 second minimum time between producing oxytoxy
    processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("glucose"), 1)
    processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("oxygen"), 6)
    processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("atp"), 38)
    processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("co2"), 6)
    processOrganelle1:addHex(0, 0)
    processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
    player:addOrganelle(1, -1, processOrganelle1)
end

setupAgents()

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
            AgentLifetimeSystem(),
            AgentMovementSystem(),
            AgentEmitterSystem(),
            AgentAbsorberSystem(),
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