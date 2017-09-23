
local function setupCamera(gameState)
    local entity = Entity.new(CAMERA_NAME .. "2", gameState.wrapper)
    -- Camera
    local camera = OgreCameraComponent.new("camera2")
    camera.properties.nearClipDistance = 5
    camera.properties:touch()
    entity:addComponent(camera)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent.new()
    entity:addComponent(sceneNode)
    -- Workspace
    local workspaceEntity = Entity.new(gameState.wrapper)
    -- TODO: could create a workspace without shadows
    local workspaceComponent = OgreWorkspaceComponent.new("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupSound(gameState)
    -- Background music
    local ambientEntity = Entity.new("main_menu_ambience", gameState.wrapper)
    local soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    ambientEntity:addComponent(soundSource)
    soundSource:addSound("main-menu-theme-1", "main-menu-theme-1.ogg")
    soundSource:addSound("main-menu-theme-2", "main-menu-theme-2.ogg")
    -- Gui effects
    local guiSoundEntity = Entity.new("gui_sounds", gameState.wrapper)
    soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
end

local function createMainMenu(name)

   return g_luaEngine:createGameState(
      name,
      {   
         -- Graphics
         OgreAddSceneNodeSystem.new(),
         OgreUpdateSceneNodeSystem.new(),
         OgreCameraSystem.new(),
         MainMenuHudSystem.new(),
         OgreWorkspaceSystem.new(),
         OgreRemoveSceneNodeSystem.new(),
         RenderSystem.new(),
         -- Other
         SoundSourceSystem.new(),
      },
      -- No physics
      false,
      "MainMenu",
      function(gameState)
         setupCamera(gameState)
         setupSound(gameState)
      end
   )
end

GameState.MAIN_MENU = createMainMenu("main_menu")

g_luaEngine:setCurrentGameState(GameState.MAIN_MENU)
