-- System for processing player input in the microbe stage
class 'MicrobeControlSystem' (System)

function MicrobeControlSystem:__init()
    System.__init(self)
end


-- Computes the point the mouse cursor is at
local function getTargetPoint()
    local mousePosition = Engine.mouse:normalizedPosition() 
    local playerCam = Entity(CAMERA_NAME)
    local cameraComponent = playerCam:getComponent(OgreCameraComponent.TYPE_ID)
    local ray = cameraComponent:getCameraToViewportRay(mousePosition.x, mousePosition.y)
    local plane = Plane(Vector3(0, 0, 1), 0)
    local intersects, t = ray:intersects(plane)
    return ray:getPoint(t)
end


-- Sums up the directional input from the keyboard
local function getMovementDirection()
    local direction = Vector3(0, 0, 0)
    if (Engine.keyboard:isKeyDown(Keyboard.KC_W)) then
        direction = direction + Vector3(0, 1, 0)
    end
    if (Engine.keyboard:isKeyDown(Keyboard.KC_S)) then
        direction = direction + Vector3(0, -1, 0)
    end
    if (Engine.keyboard:isKeyDown(Keyboard.KC_A)) then
        direction = direction + Vector3(-1, 0, 0)
    end
    if (Engine.keyboard:isKeyDown(Keyboard.KC_D)) then
        direction = direction + Vector3(1, 0, 0)
    end
    direction:normalise()
    return direction
end


function MicrobeControlSystem:update(milliseconds)
    local player = Entity("player")
    local microbe = player:getComponent(MicrobeComponent.TYPE_ID)
    local targetPoint = getTargetPoint()
    local movementDirection = getMovementDirection()
    microbe.facingTargetPoint = getTargetPoint()
    microbe.movementDirection = getMovementDirection()
end
