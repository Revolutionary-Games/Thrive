// The biome in which the tutorial is played.
const str TUTORIAL_BIOME = "default"

void setupBackground(CellStageWorld@ world){
	assert(world !is null, "setupBackground requires world");
	
	// Changing the biome makes the biome after the tutorial be default but with a different background
	auto entity = GetThriveGame().m_backgroundPlane;
	
    auto plane = world.GetComponent_Plane(entity);
	
    plane.GraphicalObject.setMaterial(biome.background);
}

/*
void setupCamera(CellStageWorld@ world){
	auto entity = GetThriveGame().m_cameraEntity;
    
	// Camera
    auto camera = OgreCameraComponent.new("camera");
    camera.properties.nearClipDistance = 5;
    camera.properties.offset = Float3(0, 0, 30);
    camera.properties:touch();
    entity:addComponent(camera);
    
	// -- Scene node
     auto sceneNode = OgreSceneNodeComponent.new()
     sceneNode.transform.position.z = 30
     sceneNode.transform:touch()
     entity:addComponent(sceneNode)
    
	// Light
    auto light = OgreLightComponent.new()
    light:setRange(200)
    entity:addComponent(light)
    // Workspace
    auto workspaceEntity = GetThriveGame()
    auto local workspaceComponent = OgreWorkspaceComponent.new("thrive_default")
    workspaceComponent.properties.cameraEntity = entity
    workspaceComponent.properties.position = 0
    workspaceComponent.properties:touch()
    workspaceEntity:addComponent(workspaceComponent)
}
*/