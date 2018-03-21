
void setupBackground(auto CellStageWorld@){
    auto entity = Entity.new("background", CellStageWorld@)
    auto skyplane = SkyPlaneComponent.new();
    skyplane.properties.plane.normal = Vector3(0, 0, 2000);
    skyplane.properties.materialName = "background/blue_01";
	skyplane.properties.scale = 4;
    skyplane.properties:touch();
    entity:addComponent(skyplane);
    //Create floating arrow entity
    entity = Entity.new("directionarrow", CellStageWorld@);
    auto sceneNode = OgreSceneNodeComponent.new();
    sceneNode.meshName = "arrow.mesh";
    sceneNode.transform.position = Vector3(0,7,-4);
    sceneNode.transform.orientation = Quaternion.new(Radian.new(Degree(90)), Vector3(1, 1, 1));
    sceneNode.transform.scale = Vector3(0.5,0.5,0.5);
    sceneNode.transform:touch();
    sceneNode:playAnimation("Stand", true);
    entity:addComponent(sceneNode);
}

void setupCamera(auto CellStageWorld@){
    auto entity = Entity.new(CAMERA_NAME .. "3", CellStageWorld@);
    //Camera
    auto camera = OgreCameraComponent.new("camera3");
    camera.properties.nearClipDistance = 5;
    camera.properties.orthographicalMode = true;
    camera.properties.fovY = Degree(30.0);
    camera.properties:touch();
    entity:addComponent(camera);
    //Scene node
    auto sceneNode = OgreSceneNodeComponent.new();
    sceneNode.transform.position.z = 30;
    sceneNode.transform.position.y = -3;
    sceneNode.transform:touch();
    entity:addComponent(sceneNode);
    //Light
    local light = OgreLightComponent.new();
    light:setRange(200);
    entity:addComponent(light);
    //Workspace
    local workspaceEntity = Entity.new(gameState.wrapper);
    local workspaceComponent = OgreWorkspaceComponent.new("thrive_default");
    workspaceComponent.properties.cameraEntity = entity;
    workspaceComponent.properties.position = 0;
    workspaceComponent.properties:touch();
    workspaceEntity:addComponent(workspaceComponent);
}

void setupSound(auto CellStageWorld@){
    auto ambientEntity = Entity.new("editor_ambience", CellStageWorld@);
    auto soundSource = SoundSourceComponent.new();
    soundSource.autoLoop = true;
    soundSource.ambientSoundSource = true;
    soundSource.volumeMultiplier = 0.6;
    ambientEntity:addComponent(soundSource);
   
    //Sound
    soundSource:addSound("microbe-editor-theme-1", "microbe-editor-theme-1.ogg");
    soundSource:addSound("microbe-editor-theme-2", "microbe-editor-theme-2.ogg");
    soundSource:addSound("microbe-editor-theme-3", "microbe-editor-theme-3.ogg");
    soundSource:addSound("microbe-editor-theme-4", "microbe-editor-theme-4.ogg");
    soundSource:addSound("microbe-editor-theme-5", "microbe-editor-theme-5.ogg"); 
    //Gui effects
    local guiSoundEntity = Entity.new("gui_sounds", CellStageWorld@);
    soundSource = SoundSourceComponent.new();
    soundSource.ambientSoundSource = true;
    soundSource.autoLoop = false;
    soundSource.volumeMultiplier = 1.0;
    guiSoundEntity:addComponent(soundSource);
    //Sound
    soundSource:addSound("button-hover-click", "soundeffects/gui/button-hover-click.ogg");
   
    local ambientEntity2 = Entity.new("editor_ambience2", CellStageWorld@);
    local soundSource2 = SoundSourceComponent.new();
    soundSource2.volumeMultiplier = 0.1;
    soundSource2.ambientSoundSource = true;
    soundSource2:addSound("microbe-ambient", "soundeffects/microbe-ambience.ogg");
    soundSource2.autoLoop = true;
    ambientEntity2:addComponent(soundSource2);
}

auto createMicrobeEditor(string name){
    
    return g_luaEngine:createGameState(
        name,
        {   
            MicrobeSystem.new(),
            MicrobeEditorHudSystem.new(),
            //Graphics
            OgreAddSceneNodeSystem.new(),
            OgreUpdateSceneNodeSystem.new(),
            OgreCameraSystem.new(),
            OgreLightSystem.new(),
            SkySystem.new(),
            OgreWorkspaceSystem.new(),
            OgreRemoveSceneNodeSystem.new(),
            RenderSystem.new(),
            //Other
            SoundSourceSystem.new(),
        },
        //TODO: check whether physics is required in the editor
        true,
        "MicrobeEditor",
        function(CellStageWorld@){
            setupBackground(CellStageWorld@)
            setupCamera(CellStageWorld@)
            setupSound(CellStageWorld@)
        }
    )
}

CellStageWorld@.MICROBE_EDITOR = createMicrobeEditor("microbe_editor");

//Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
