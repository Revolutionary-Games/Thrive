Engine:setPhysicsDebugDrawingEnabled(true)

ADD_SYSTEM(MicrobeSystem)
ADD_SYSTEM(MicrobeCameraSystem)
ADD_SYSTEM(MicrobeControlSystem)
ADD_SYSTEM(HudSystem)

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
    AgentRegistry.registerAgentType("energy", "Energy")
    AgentRegistry.registerAgentType("oxygen", "Oxygen")    
    AgentRegistry.registerAgentType("nitrate", "Nitrate")
    AgentRegistry.registerAgentType("faxekondium", "Faxekondium")
end

local function setupEmitter()
    -- Setting up an emitter for energy
    local entity = Entity("energy-emitter")
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
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "molecule.mesh"
    entity:addComponent(sceneNode)
    -- Emitter energy
    local energyEmitter = AgentEmitterComponent()
    entity:addComponent(energyEmitter)
    energyEmitter.agentId = AgentRegistry.getAgentId("energy")
    energyEmitter.emitInterval = 1000
    energyEmitter.emissionRadius = 1
    energyEmitter.maxInitialSpeed = 10
    energyEmitter.minInitialSpeed = 2
    energyEmitter.minEmissionAngle = Degree(0)
    energyEmitter.maxEmissionAngle = Degree(360)
    energyEmitter.meshName = "molecule.mesh"
    energyEmitter.particlesPerEmission = 1
    energyEmitter.particleLifeTime = 5000
    energyEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
    energyEmitter.potencyPerParticle = 3.0
    -- Setting up an emitter for agent 2
    local entity2 = Entity("oxygen-emitter")
    -- Rigid body
    rigidBody = RigidBodyComponent()
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearDamping = 0.8
    rigidBody.properties.shape = CylinderShape(
        CollisionShape.AXIS_X, 
        0.4,
        2.0
    )
    rigidBody:setDynamicProperties(
        Vector3(20, -10, 0),
        Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)),
        Vector3(0, 0, 0),
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    entity2:addComponent(rigidBody)
    -- Scene node
    sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "molecule.mesh"
    entity2:addComponent(sceneNode)
    -- Emitter Agent 2
    local agent2Emitter = AgentEmitterComponent()
    entity2:addComponent(agent2Emitter)
    agent2Emitter.agentId = AgentRegistry.getAgentId("oxygen")
    agent2Emitter.emitInterval = 1000
    agent2Emitter.emissionRadius = 1
    agent2Emitter.maxInitialSpeed = 10
    agent2Emitter.minInitialSpeed = 2
    agent2Emitter.minEmissionAngle = Degree(0)
    agent2Emitter.maxEmissionAngle = Degree(360)
    agent2Emitter.meshName = "molecule.mesh"
    agent2Emitter.particlesPerEmission = 1
    agent2Emitter.particleLifeTime = 5000
    agent2Emitter.particleScale = Vector3(0.3, 0.3, 0.3)
    agent2Emitter.potencyPerParticle = 1.0
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
    local storageOrganelle = StorageOrganelle(AgentRegistry.getAgentId("energy"), 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, 0, storageOrganelle)
    player:storeAgent(AgentRegistry.getAgentId("energy"), 10)
    -- Storage agent 2
    local storageOrganelle2 = StorageOrganelle(AgentRegistry.getAgentId("oxygen"), 100.0)
    storageOrganelle2:addHex(0, 0)
    storageOrganelle2:setColour(ColourValue(0, 1, 1, 1))
    player:addOrganelle(0, -1, storageOrganelle2)
    -- Storage agent 3
    local storageOrganelle3 = StorageOrganelle(AgentRegistry.getAgentId("faxekondium"), 100.0)
    storageOrganelle3:addHex(0, 0)
    storageOrganelle3:setColour(ColourValue(1, 1, 0, 1))
    player:addOrganelle(-1, 0, storageOrganelle3)
    -- Storage agent 4
    local storageOrganelle4 = StorageOrganelle(AgentRegistry.getAgentId("nitrate"), 100.0)
    storageOrganelle4:addHex(0, 0)
    storageOrganelle4:setColour(ColourValue(1, 0, 1, 0))
    player:addOrganelle(1, -1, storageOrganelle4)
end

setupBackground()
setupCamera()
setupAgents()
setupEmitter()
setupHud()
setupPlayer()
