
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
    local entity = Entity(CAMERA_NAME .. "2")
    -- Camera
    local camera = OgreCameraComponent("camera2")
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


local function setupHud()
    local ENERGY_WIDTH = 200
    local ENERGY_HEIGHT = 32
    local title = Entity("menu.hud.title")
    local titleText = TextOverlayComponent("menu.hud.title")
    title:addComponent(titleText)
    titleText.properties.horizontalAlignment = TextOverlayComponent.Center
    titleText.properties.text = string.format(GAME_TITLE)
    titleText.properties.verticalAlignment = TextOverlayComponent.Top
    titleText.properties.charHeight = 30
 --   titleText.properties.width = ENERGY_WIDTH
 --   titleText.properties.height = ENERGY_HEIGHT
    titleText.properties.left = -80
 --   titleText.properties.top = - ENERGY_HEIGHT
 --   titleText.properties:touch()
end


local function createMainMenu(name)
    return Engine:createGameState(
        name,
        {   
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
            setupHud()
        end
    )
end

GameState.MAIN_MENU = createMainMenu("main_menu")

--Engine:setCurrentGameState(GameState.MAIN_MENU)
