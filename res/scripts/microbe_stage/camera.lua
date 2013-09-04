class 'MicrobeCameraSystem' (System)

local OFFSET = Vector3(0, 0, 30)

function MicrobeCameraSystem:__init()
    System.__init(self)
    self.camera = Entity(CAMERA_NAME)
    self.player = Entity(PLAYER_NAME)
end


function MicrobeCameraSystem:update(milliseconds)
    local playerNode = self.player:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local cameraNode = self.camera:getComponent(OgreSceneNodeComponent.TYPE_ID)
    cameraNode.transform.position = playerNode.transform.position + OFFSET
    cameraNode.transform:touch()
end


