
local function setupBackground()
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = "Background"
	skyplane.properties.scale = 200
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

local function setupCamera()
    local entity = Entity(CAMERA_NAME)
    -- Camera
    local camera = OgreCameraComponent("camera")
    camera.properties.nearClipDistance = 5
    camera.offset = Vector3(0, 0, 30)
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
    -- Workspace
    local workspaceEntity = Entity()
    local workspaceComponent = OgreWorkspaceComponent("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
end

local function setupCompoundClouds()
    local compoundId = CompoundRegistry.getCompoundId("glucose")
    local entity = Entity("compound_cloud_glucose")
    local compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 150, 170, 180)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("oxygen")
    entity = Entity("compound_cloud_oxygen")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 60, 160, 180)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("co2")
    entity = Entity("compound_cloud_co2")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 20, 50, 100)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("ammonia")
    entity = Entity("compound_cloud_ammonia")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 255, 220, 50)
    entity:addComponent(compoundCloud)
    
    compoundId = CompoundRegistry.getCompoundId("aminoacids")
    entity = Entity("compound_cloud_aminoacids")
    compoundCloud = CompoundCloudComponent()
    compoundCloud:initialize(compoundId, 255, 150, 200)
    entity:addComponent(compoundCloud)
end

local function setupPlayer()
    local entity = Entity(PLAYER_NAME)
    local sceneNode = OgreSceneNodeComponent()
    entity:addComponent(sceneNode)
    Engine:playerData():setActiveCreature(entity.id, GameState.MICROBE_TUTORIAL)
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


local function setupSound()
    local ambientEntity = Entity("ambience")
    local soundSource = SoundSourceComponent()
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
    local ambientEntity2 = Entity("ambience2")
    local soundSource = SoundSourceComponent()
    soundSource.volumeMultiplier = 0.3
    soundSource.ambientSoundSource = true
    ambientSound = soundSource:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg")
    soundSource.autoLoop = true
     ambientEntity2:addComponent(soundSource)
    -- Gui effects
    local guiSoundEntity = Entity("gui_sounds")
    soundSource = SoundSourceComponent()
    soundSource.ambientSoundSource = true
    soundSource.autoLoop = false
    soundSource.volumeMultiplier = 1.0
    guiSoundEntity:addComponent(soundSource)
    -- Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg")
    soundSource:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    local listener = Entity("soundListener")
    local sceneNode = OgreSceneNodeComponent()
    listener:addComponent(sceneNode)
end

local function createMicrobeStageTutorial(name)
    return 
        Engine:createGameState(
        name,
        {
            QuickSaveSystem(),
            -- Microbe specific
            MicrobeSystem(),
            MicrobeCameraSystem(),
            MicrobeAISystem(),
            MicrobeControlSystem(),
            MicrobeStageTutorialHudSystem(),
            TimedLifeSystem(),
            CompoundMovementSystem(),
            CompoundAbsorberSystem(),
            --PopulationSystem(),
            PatchSystem(),
            SpeciesSystem(),
            -- Physics
            RigidBodyInputSystem(),
            UpdatePhysicsSystem(),
            RigidBodyOutputSystem(),
            BulletToOgreSystem(),
            CollisionSystem(),
            -- Graphics
            OgreAddSceneNodeSystem(),
            OgreUpdateSceneNodeSystem(),
            OgreCameraSystem(),
            OgreLightSystem(),
            SkySystem(),
            OgreWorkspaceSystem(),
            OgreRemoveSceneNodeSystem(),
            RenderSystem(),
            MembraneSystem(),
            CompoundCloudSystem(),
            -- Other
            SoundSourceSystem(),
            PowerupSystem(),
            CompoundEmitterSystem(), -- Keep this after any logic that might eject compounds such that any entites that are queued for destruction will be destroyed after emitting.
        },
        function()
            setupBackground()
            setupCamera()
            setupCompoundClouds()
            setupSpecies()
            setupPlayer()
            setupSound()
        end,
        "MicrobeStageTutorial"
    )
end

GameState.MICROBE_TUTORIAL = createMicrobeStageTutorial("microbe_tutorial")
--Engine:setCurrentGameState(GameState.MICROBE_TUTORIAL)
