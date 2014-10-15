
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = "background/blue_01"
    skyplane.properties.tiling = 500
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

local function setupSound()
    local ambientEntity = Entity("editor_ambience")
    local soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.volumeMultiplier = 0.4
    ambientEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("microbe-editor-theme-1", "microbe-editor-theme-1.ogg")
    soundSource:addSound("microbe-editor-theme-2", "microbe-editor-theme-2.ogg")
    soundSource:addSound("microbe-editor-theme-3", "microbe-editor-theme-3.ogg")
    soundSource:addSound("microbe-editor-theme-4", "microbe-editor-theme-4.ogg")
    soundSource:addSound("microbe-editor-theme-5", "microbe-editor-theme-5.ogg")   
end

local function createMicrobeEditor(name)
    
    return Engine:createGameState(
        name,
        {   
      --      MicrobeSystem(),
            MicrobeEditorHudSystem(),
            -- Graphics
            OgreAddSceneNodeSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            OgreLightSystem(),
            SkySystem(),
            OgreViewportSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
            -- Other
            SoundSourceSystem(),
        },
        function()
            setupBackground()
            setupCamera()
            setupSound()
        end,
        "MicrobeEditor"
    )
end

GameState.MICROBE_EDITOR = createMicrobeEditor("microbe_editor")

--Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
