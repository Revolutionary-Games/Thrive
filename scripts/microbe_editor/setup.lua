
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = "background/blue_01"
	skyplane.properties.scale = 4
    skyplane.properties:touch()
    entity:addComponent(skyplane)
    -- Create floating arrow entity
    entity = Entity("directionarrow")
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "arrow.mesh"
    sceneNode.transform.position = Vector3(0,7,-4)
    sceneNode.transform.orientation = Quaternion(Radian(Degree(90)), Vector3(1, 1, 1))
    sceneNode.transform.scale = Vector3(0.5,0.5,0.5)
    sceneNode.transform:touch()
    sceneNode:playAnimation("Stand", true)
    entity:addComponent(sceneNode)
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
    sceneNode.transform.position.y = -3
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

local function setupSound()
    local ambientEntity = Entity("editor_ambience")
    local soundSource = SoundSourceComponent()
    soundSource.autoLoop = true
    soundSource.ambientSoundSource = true
    soundSource.volumeMultiplier = 0.6
    ambientEntity:addComponent(soundSource)
   
    -- Sound
    soundSource:addSound("microbe-editor-theme-1", "microbe-editor-theme-1.ogg")
    soundSource:addSound("microbe-editor-theme-2", "microbe-editor-theme-2.ogg")
    soundSource:addSound("microbe-editor-theme-3", "microbe-editor-theme-3.ogg")
    soundSource:addSound("microbe-editor-theme-4", "microbe-editor-theme-4.ogg")
    soundSource:addSound("microbe-editor-theme-5", "microbe-editor-theme-5.ogg")  
    -- Gui effects
    local guiSoundEntity = Entity("gui_sounds")
    soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
   
    local ambientEntity2 = Entity("editor_ambience2")
    local soundSource2 = SoundSourceComponent()
    soundSource2.volumeMultiplier = 0.1
    soundSource2.ambientSoundSource = true
    soundSource2:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
    soundSource2.autoLoop = true
    ambientEntity2:addComponent(soundSource2)
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
            OgreWorkspaceSystem(),
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
