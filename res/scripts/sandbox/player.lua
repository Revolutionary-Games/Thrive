class 'PlayerMicrobe' (Microbe)

function PlayerMicrobe:__init()
    Microbe.__init(self, "player")
end

function PlayerMicrobe:update(milliseconds)
    self:updateFacingTargetPoint()
    self:updateMovementDirection()
    Microbe.update(self, milliseconds)
    setPlayerEnergyCount(self:getAgentAmount(1))
end

function PlayerMicrobe:updateFacingTargetPoint()
    local mousePosition = Engine.mouse:normalizedPosition() 
    local playerCam = Entity("playerCam")
    local cameraComponent = playerCam:getComponent(OgreCameraComponent.TYPE_ID())
    local ray = cameraComponent:getCameraToViewportRay(mousePosition.x, mousePosition.y)
    local plane = Plane(Vector3(0, 0, 1), 0)
    local intersects, t = ray:intersects(plane)
    self.facingTargetPoint = ray:getPoint(t)
end

function PlayerMicrobe:updateMovementDirection()
    local direction = Vector3(0, 0, 0)
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_W)) then
        direction = direction + Vector3(0, 1, 0)
    end
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_S)) then
        direction = direction + Vector3(0, -1, 0)
    end
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_A)) then
        direction = direction + Vector3(-1, 0, 0)
    end
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_D)) then
        direction = direction + Vector3(1, 0, 0)
    end
    direction:normalise()
    self.movementDirection = direction;
end

local player = PlayerMicrobe()

local forwardOrganelle = MovementOrganelle(
    Vector3(0.0, 50.0, 0.0),
    300
)
forwardOrganelle:addHex(0, 0)
forwardOrganelle:addHex(-1, 0)
forwardOrganelle:addHex(1, -1)
forwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
player:addOrganelle(0, 1, forwardOrganelle)


local storageOrganelle = StorageOrganelle(1, 100.0)
storageOrganelle:addHex(0, 0)
storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
player:addOrganelle(0, 0, storageOrganelle)

local backwardOrganelle = MovementOrganelle(
    Vector3(0.0, -50.0, 0.0),
    300
)
backwardOrganelle:addHex(0, 0)
backwardOrganelle:addHex(-1, 1)
backwardOrganelle:addHex(1, 0)
backwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
player:addOrganelle(0, -1, backwardOrganelle)



player:storeAgent(1, 10)
