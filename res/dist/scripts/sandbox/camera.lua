local playerCam = Entity("playerCam")

-- Camera
playerCam.camera = OgreCameraComponent("playerCam")
playerCam:addComponent(playerCam.camera)
-- Scene node
playerCam.sceneNode = OgreSceneNodeComponent()
playerCam:addComponent(playerCam.sceneNode)
-- Transform
playerCam.transform = TransformComponent()
playerCam:addComponent(playerCam.transform)

playerCam.camera.workingCopy.nearClipDistance = 5
playerCam.camera:touch()

playerCam.transform.workingCopy.position.z = 30
playerCam.transform:touch()

-- OnUpdate
playerCam.onUpdate = OnUpdateComponent()
playerCam:addComponent(playerCam.onUpdate)
local time = 0
playerCam.onUpdate.callback = function(entityId, milliseconds)
    time = time + milliseconds / 1000
    playerCam.transform.workingCopy.position.z = 25 + 5 * math.sin(time)
    playerCam.transform:touch()
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
