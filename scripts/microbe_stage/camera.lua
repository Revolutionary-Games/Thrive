-- This system updates the camera position to stay above the player microbe
class 'MicrobeCameraSystem' (System)

-- The offset from player microbe to camera
local OFFSET = Vector3(0, 0, 30)

function MicrobeCameraSystem:__init()
    System.__init(self)
end


function MicrobeCameraSystem:update(milliseconds)
    local camera = Entity(CAMERA_NAME)
    local player = Entity(PLAYER_NAME)
    local playerNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local cameraNode = camera:getComponent(OgreSceneNodeComponent.TYPE_ID)
    cameraNode.transform.position = playerNode.transform.position + OFFSET
    cameraNode.transform:touch()
end


