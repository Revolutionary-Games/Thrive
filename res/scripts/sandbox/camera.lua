local playerCam = Entity("playerCam")

-- Camera
playerCam.camera = OgreCameraComponent("playerCam")
playerCam.camera.properties.nearClipDistance = 5
playerCam.camera.properties:touch()
playerCam:addComponent(playerCam.camera)

-- Scene node
playerCam.sceneNode = OgreSceneNodeComponent()
playerCam.sceneNode.transform.position.z = 30
playerCam.sceneNode.transform:touch()
playerCam:addComponent(playerCam.sceneNode)


-- Light
playerCam.light = OgreLightComponent()
playerCam.light:setRange(200)
playerCam:addComponent(playerCam.light)

-- OnUpdate
playerCam.onUpdate = OnUpdateComponent()
playerCam:addComponent(playerCam.onUpdate)
local time = 0
OFFSET = Vector3(0, 0, 30)
playerCam.onUpdate.callback = function(entityId, milliseconds)
    local player = Entity("player")
    local playerSceneNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
    playerCam.sceneNode.transform.position = playerSceneNode.transform.position + OFFSET
    playerCam.sceneNode.transform:touch()
end



local viewport = OgreViewport(0)
viewport.properties.cameraEntity = playerCam
viewport.properties:touch()
addViewport(viewport)


