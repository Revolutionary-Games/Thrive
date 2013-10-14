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

local function setupEmitter()
    local entity = Entity("object")
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
    -- Emitter
    local agentEmitter = AgentEmitterComponent()
    entity:addComponent(agentEmitter)
    agentEmitter.agentId = 1
    agentEmitter.emitInterval = 1000
    agentEmitter.emissionRadius = 1
    agentEmitter.maxInitialSpeed = 10
    agentEmitter.minInitialSpeed = 2
    agentEmitter.minEmissionAngle = Degree(0)
    agentEmitter.maxEmissionAngle = Degree(360)
    agentEmitter.meshName = "molecule.mesh"
    agentEmitter.particlesPerEmission = 1
    agentEmitter.particleLifeTime = 5000
    agentEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
    agentEmitter.potencyPerParticle = 3.0
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
	
	local AGENTS_WIDTH = 200
    local AGENTS_HEIGHT = 32	
	
	local playerAgentCounts = Entity("hud.playerAgents")
    local playerAgentText = TextOverlayComponent("hud.playerAgents")
    playerAgentCounts:addComponent(playerAgentText)
    playerAgentText.properties.horizontalAlignment = TextOverlayComponent.Right
    playerAgentText.properties.verticalAlignment = TextOverlayComponent.Bottom
    playerAgentText.properties.width = AGENTS_WIDTH
	 -- Note that height and top will change dynamically with the number of agents displayed
    playerAgentText.properties.height = AGENTS_HEIGHT  
    playerAgentText.properties.left = -AGENTS_WIDTH / 2 
    playerAgentText.properties.top = -AGENTS_HEIGHT
    playerAgentText.properties:touch()
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
    -- Storage
    local storageOrganelle = StorageOrganelle(1, 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    player:addOrganelle(0, 0, storageOrganelle)
    player:storeAgent(1, 10)
    -- Backward
    local backwardOrganelle = MovementOrganelle(
        Vector3(0.0, -50.0, 0.0),
        300
    )
    backwardOrganelle:addHex(0, 0)
    backwardOrganelle:addHex(-1, 1)
    backwardOrganelle:addHex(1, 0)
    backwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
    player:addOrganelle(0, -1, backwardOrganelle)
end

setupBackground()
setupCamera()
setupEmitter()
setupHud()
setupPlayer()

