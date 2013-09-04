
ADD_SYSTEM(MicrobeSystem)
ADD_SYSTEM(MicrobeCameraSystem)
ADD_SYSTEM(MicrobeControlSystem)

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
    local viewport = OgreViewport(0)
    viewport.properties.cameraEntity = entity
    viewport.properties:touch()
    addViewport(viewport)
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
setupPlayer()


