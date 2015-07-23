
local function setupCamera()
    local entity = Entity(CAMERA_NAME .. "2")
    -- Camera
    local camera = OgreCameraComponent("camera2")
    camera.properties.nearClipDistance = 5
    camera.properties:touch()
    entity:addComponent(camera)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    entity:addComponent(sceneNode)
    -- Workspace
    local workspaceEntity = Entity()
    -- TODO: could create a workspace without shadows
    local workspaceComponent = OgreWorkspaceComponent("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupSound()
    -- Background music
    local ambientEntity = Entity("main_menu_ambience")
    local soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = true
    soundSource.volumeMultiplier = 0.8
    ambientEntity:addComponent(soundSource)
    soundSource:addSound("main-menu-theme-1", "main-menu-theme-1.ogg")
    soundSource:addSound("main-menu-theme-2", "main-menu-theme-2.ogg")
    -- Gui effects
    local guiSoundEntity = Entity("gui_sounds")
    soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
end

local function createMainMenu(name)
    return Engine:createGameState(
        name,
        {   
            -- Graphics
            OgreAddSceneNodeSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            MainMenuHudSystem(),
            OgreWorkspaceSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
            -- Other
            SoundSourceSystem(),
        },
        function()
            setupCamera()
            setupSound()
        end,
        "MainMenu"
    )
end

GameState.MAIN_MENU = createMainMenu("main_menu")

Engine:setCurrentGameState(GameState.MAIN_MENU)
