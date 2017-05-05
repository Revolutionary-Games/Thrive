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
    local camera = Entity.new(CAMERA_NAME, self.gameState.wrapper)
    self.camera = getComponent(camera, OgreCameraComponent)
    self.camera.properties.offset = Vector3(0, 0, 30)
    self.camera.properties:touch()
    self.cameraScenenode = getComponent(camera, OgreSceneNodeComponent)
end

function MicrobeCameraSystem:update(renderTime, logicTime)    
    local player = Entity.new(PLAYER_NAME, self.gameState.wrapper)
    local playerNode = getComponent(player, OgreSceneNodeComponent)
	self.cameraScenenode.transform.position = playerNode.transform.position + self.camera.properties.offset
	self.cameraScenenode.transform:touch()
end


