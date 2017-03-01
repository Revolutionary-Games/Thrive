--The biome in which the tutorial is played.
TUTORIAL_BIOME = "default"

local function setupBackground(gameState)
    --Actually changing the biome makes the biome after the tutorial be default
    --but with a different background.
    local entity = Entity.new("background", gameState.wrapper)
    local skyplane = SkyPlaneComponent.new()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = biomeTable[TUTORIAL_BIOME].background
	skyplane.properties.scale = 200
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

local function setupCamera(gameState)
    local entity = Entity.new(CAMERA_NAME, gameState.wrapper)
    -- Camera
    local camera = OgreCameraComponent.new("camera")
    camera.properties.nearClipDistance = 5
    camera.properties.offset = Vector3(0, 0, 30)
    camera.properties:touch()
    entity:addComponent(camera)
    -- Scene node
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.transform.position.z = 30
    sceneNode.transform:touch()
    entity:addComponent(sceneNode)
    -- Light
    local light = OgreLightComponent.new()
    light:setRange(200)
    entity:addComponent(light)
    -- Workspace
    local workspaceEntity = Entity.new(gameState.wrapper)
    local workspaceComponent = OgreWorkspaceComponent.new("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupCompoundClouds(gameState)
    for compoundName, compoundInfo in pairs(compoundTable) do
        if compoundInfo.isCloud then
            local compoundId = CompoundRegistry.getCompoundId(compoundName)
            local entity = Entity.new("compound_cloud_" .. compoundName, gameState.wrapper)
            local compoundCloud = CompoundCloudComponent.new()
            local colour = compoundInfo.colour
            compoundCloud:initialize(compoundId, colour.r, colour.g, colour.b)
            entity:addComponent(compoundCloud)
        end
    end
end

local function setupPlayer(gameState)
    local entity = Entity.new(PLAYER_NAME, gameState.wrapper)
    local sceneNode = OgreSceneNodeComponent.new()
    entity:addComponent(sceneNode)
    Engine:playerData():setActiveCreature(entity.id, GameState.MICROBE_TUTORIAL.wrapper)
    -- local microbe = Microbe.createMicrobeEntity(PLAYER_NAME, false)
    -- Entity("Default"):getComponent(SpeciesComponent.TYPE_ID):template(microbe)
    -- microbe.collisionHandler:addCollisionGroup("powerupable")
    -- Engine:playerData():lockedMap():addLock("Toxin")
    -- Engine:playerData():lockedMap():addLock("chloroplast")
    -- Engine:playerData():setActiveCreature(microbe.entity.id, GameState.MICROBE_TUTORIAL)
    -- speciesEntity = Entity("defaultMicrobeSpecies")
    -- species = SpeciesComponent("Default")
    -- species:fromMicrobe(microbe)
    -- speciesEntity:addComponent(species)
    -- microbe.microbe.speciesName = "Default"
end


local function setupSound(gameState)
    local ambientEntity = Entity.new("ambience", gameState.wrapper)
    local soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = true
    soundSource.volumeMultiplier = 0.5
    ambientEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("microbe-theme-1", "microbe-theme-1.ogg")
    soundSource:addSound("microbe-theme-3", "microbe-theme-3.ogg")
    soundSource:addSound("microbe-theme-4", "microbe-theme-4.ogg")
    soundSource:addSound("microbe-theme-5", "microbe-theme-5.ogg")
    soundSource:addSound("microbe-theme-6", "microbe-theme-6.ogg")   
    soundSource:addSound("microbe-theme-7", "microbe-theme-7.ogg")   
    local ambientEntity2 = Entity.new("ambience2", gameState.wrapper)
    local soundSource = SoundSourceComponent.new()
    soundSource.volumeMultiplier = 0.3
    soundSource.ambientSoundSource = true
    ambientSound = soundSource:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
    soundSource.autoLoop = true
    ambientEntity2:addComponent(soundSource)
    -- Gui effects
    local guiSoundEntity = Entity.new("gui_sounds", gameState.wrapper)
    soundSource = SoundSourceComponent.new()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
    soundSource:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    local listener = Entity.new("soundListener", gameState.wrapper)
    local sceneNode = OgreSceneNodeComponent.new()
    listener:addComponent(sceneNode)
end

local function createMicrobeStageTutorial(name)
    return 
        g_luaEngine:createGameState(
        name,
        {
            QuickSaveSystem.new(),
            -- Microbe specific
            MicrobeSystem.new(),
            MicrobeCameraSystem.new(),
            MicrobeAISystem.new(),
            MicrobeControlSystem.new(),
            MicrobeStageTutorialHudSystem.new(),
            TimedLifeSystem.new(),
            CompoundMovementSystem.new(),
            CompoundAbsorberSystem.new(),
            --PopulationSystem.new(),
            PatchSystem.new(),
            SpeciesSystem.new(),
            -- Physics
            RigidBodyInputSystem.new(),
            UpdatePhysicsSystem.new(),
            RigidBodyOutputSystem.new(),
            BulletToOgreSystem.new(),
            CollisionSystem.new(),
            -- Graphics
            OgreAddSceneNodeSystem.new(),
            OgreUpdateSceneNodeSystem.new(),
            OgreCameraSystem.new(),
            OgreLightSystem.new(),
            SkySystem.new(),
            OgreWorkspaceSystem.new(),
            OgreRemoveSceneNodeSystem.new(),
            RenderSystem.new(),
            MembraneSystem.new(),
            CompoundCloudSystem.new(),
            -- Other
            SoundSourceSystem.new(),
            PowerupSystem.new(),
            CompoundEmitterSystem.new(), -- Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
        },
        true,
        "MicrobeStageTutorial",
        function(gameState)
            setupBackground(gameState)
            setupCamera(gameState)
            setupCompoundClouds(gameState)
            setupSpecies(gameState)
            setupPlayer(gameState)
            setupSound(gameState)
        end
    )
end

GameState.MICROBE_TUTORIAL = createMicrobeStageTutorial("microbe_tutorial")
--Engine:setCurrentGameState(GameState.MICROBE_TUTORIAL)
