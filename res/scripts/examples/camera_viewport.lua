local cameraEntity = Entity("camera")
-- Camera
local cameraComponent = OgreCameraComponent("camera")
cameraComponent.properties.nearClipDistance = 5
cameraComponent.properties:touch()
cameraEntity:addComponent(cameraComponent)
-- Scene node
local sceneNodeComponent = OgreSceneNodeComponent()
sceneNodeComponent.transform.position.z = 30
sceneNodeComponent.transform:touch()
cameraEntity:addComponent(sceneNodeComponent)

-- Viewport
local viewportEntity = Entity()
local viewportComponent = OgreViewportComponent(0)
viewportComponent.properties.cameraEntity = cameraEntity
viewportComponent.properties:touch()
viewportEntity:addComponent(viewportComponent)

