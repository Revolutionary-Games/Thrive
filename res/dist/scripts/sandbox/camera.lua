local playerCam = Entity("playerCam")

-- Camera
playerCam.camera = OgreCameraComponent("playerCam")
playerCam:addComponent(playerCam.camera)
-- Scene node
playerCam.sceneNode = OgreSceneNodeComponent()
playerCam.sceneNode.workingCopy.position.z = 30
playerCam.sceneNode:touch()
playerCam:addComponent(playerCam.sceneNode)

playerCam.camera.workingCopy.nearClipDistance = 5
playerCam.camera:touch()


-- OnUpdate
playerCam.onUpdate = OnUpdateComponent()
playerCam:addComponent(playerCam.onUpdate)
local time = 0
OFFSET = Vector3(0, 0, 30)
playerCam.onUpdate.callback = function(entityId, milliseconds)
    local player = Entity("player")
    local playerSceneNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID())
    playerCam.sceneNode.workingCopy.position = playerSceneNode.latest.position + OFFSET
    playerCam.sceneNode:touch()
end



local viewport = OgreViewport(0)
viewport.workingCopy.cameraEntity = playerCam
viewport:touch()
addViewport(viewport)


-- Picture in Picture
local pipViewport = OgreViewport(1)
pipViewport.workingCopy.cameraEntity = playerCam
pipViewport.workingCopy.width = 0.1
pipViewport.workingCopy.height = 0.1
pipViewport:touch()
addViewport(pipViewport)
