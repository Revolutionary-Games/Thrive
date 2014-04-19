
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 1)
    skyplane.properties.plane.d = 1000
  --  skyplane.properties.materialName = string.format("background/blue_02.png")
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

local function setupCamera()
    local entity = Entity(CAMERA_NAME .. "3")
    -- Camera
    local camera = OgreCameraComponent("camera3")
    camera.properties.nearClipDistance = 5
    camera.properties.orthographicalMode = true
    camera.properties.fovY = Degree(30.0)
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

local function createMicrobeEditor(name)
    
    return Engine:createGameState(
        name,
        {   
            MicrobeSystem(),
            -- Graphics
            OgreAddSceneNodeSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            OgreLightSystem(),
            MicrobeEditorHudSystem(),
            SkySystem(),
            TextOverlaySystem(),
            OgreViewportSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
        },
        function()
            setupBackground()
            setupCamera()
        end,
        "MicrobeEditor"
    )
end

GameState.MICROBE_EDITOR = createMicrobeEditor("microbe_editor")

Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
