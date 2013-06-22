local playerCam = Entity("playerCam")

-- Camera
playerCam.camera = OgreCameraComponent("playerCam")
playerCam:addComponent(playerCam.camera)
-- Scene node
playerCam.sceneNode = OgreSceneNodeComponent()
playerCam.sceneNode.properties.position.z = 30
playerCam.sceneNode.properties:touch()
playerCam:addComponent(playerCam.sceneNode)

playerCam.camera.properties.nearClipDistance = 5
playerCam.camera.properties:touch()


-- OnUpdate
playerCam.onUpdate = OnUpdateComponent()
playerCam:addComponent(playerCam.onUpdate)
local time = 0
OFFSET = Vector3(0, 0, 30)
playerCam.onUpdate.callback = function(entityId, milliseconds)
    local player = Entity("player")
    local playerSceneNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID())
    playerCam.sceneNode.properties.position = playerSceneNode.properties.position + OFFSET
    playerCam.sceneNode.properties:touch()
end



local viewport = OgreViewport(0)
viewport.properties.cameraEntity = playerCam
viewport.properties:touch()
addViewport(viewport)


-- Picture in Picture
local pipViewport = OgreViewport(1)
pipViewport.properties.cameraEntity = playerCam
pipViewport.properties.width = 0.1
pipViewport.properties.height = 0.1
pipViewport.properties:touch()
addViewport(pipViewport)
