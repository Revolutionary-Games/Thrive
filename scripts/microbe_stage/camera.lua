-- This system updates the camera position to stay above the player microbe
MicrobeCameraSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)

        -- The offset from player microbe to camera
        self.camera = nil
        self.cameraScenenode = nil
    end
)

function MicrobeCameraSystem:init(gameState)
    LuaSystem.init(self, "MicrobeCameraSystem", gameState)
end

function MicrobeCameraSystem:activate()
    local camera = Entity.new(CAMERA_NAME)
    self.camera = camera:getComponent(OgreCameraComponent.TYPE_ID)
    self.camera.properties.offset = Vector3(0, 0, 30)
    self.camera.properties:touch()
    self.cameraScenenode = camera:getComponent(OgreSceneNodeComponent.TYPE_ID)
end

function MicrobeCameraSystem:update(renderTime, logicTime)    
    local player = Entity.new(PLAYER_NAME)
    local playerNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
	self.cameraScenenode.transform.position = playerNode.transform.position + self.camera.properties.offset
	self.cameraScenenode.transform:touch()
end


