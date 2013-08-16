class 'PlayerMicrobe' (Microbe)

function PlayerMicrobe:__init()
    Microbe.__init(self, "player")
end

function PlayerMicrobe:update(milliseconds)
    -- Find mouse target point
    local mousePosition = Engine.mouse:normalizedPosition() 
    local playerCam = Entity("playerCam")
    local cameraComponent = playerCam:getComponent(OgreCameraComponent.TYPE_NAME())
    local ray = cameraComponent:getCameraToViewportRay(mousePosition.x, mousePosition.y)
    local plane = Plane(Vector3(0, 0, 1), 0)
    local intersects, t = ray:intersects(plane)
    self.facingTargetPoint = ray:getPoint(t)
    -- Sum up movement commands
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
    Microbe.update(self, milliseconds)
end

local player = PlayerMicrobe()

local movementOrganelle = MovementOrganelle(
    Vector3(10.0, 50.0, 0.0),
    300
)
movementOrganelle:addHex(0, 0)
movementOrganelle:addHex(-1, 0)
movementOrganelle:addHex(1, -1)
player:addOrganelle(0, 0, movementOrganelle)

