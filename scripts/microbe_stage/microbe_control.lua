-- System for processing player input in the microbe stage
MicrobeControlSystem = class(
    LuaSystem,
    function(self)
        
        LuaSystem.create(self)
        
    end
)

function MicrobeControlSystem:init(gameState)
    LuaSystem.init(self, "MicrobeControlSystem", gameState)
end

-- Computes the point the mouse cursor is at
local function getTargetPoint()
    local mousePosition = Engine.mouse:normalizedPosition() 
    local playerCam = Entity.new(CAMERA_NAME, g_luaEngine.currentGameState.wrapper)
    local cameraComponent = getComponent(playerCam, OgreCameraComponent)
    local ray = cameraComponent:getCameraToViewportRay(mousePosition.x, mousePosition.y)
    local plane = Plane.new(Vector3(0, 0, 1), 0)
    local intersects, t = ray:intersects(plane)
    return ray:getPoint(t)
end


-- Sums up the directional input from the keyboard
local function getMovementDirection()
    local direction = Vector3(0, 0, 0)
    if (Engine.keyboard:isKeyDown(KEYCODE.KC_W)) then
        direction = direction + Vector3(0, 1, 0)
    end
    if (Engine.keyboard:isKeyDown(KEYCODE.KC_S)) then
        direction = direction + Vector3(0, -1, 0)
    end
    if (Engine.keyboard:isKeyDown(KEYCODE.KC_A)) then
        direction = direction + Vector3(-1, 0, 0)
    end
    if (Engine.keyboard:isKeyDown(KEYCODE.KC_D)) then
        direction = direction + Vector3(1, 0, 0)
    end
    direction:normalise()
    return direction
end


function MicrobeControlSystem:update(renderTime, logicTime)
    local player = Entity.new("player", self.gameState.wrapper)
    local microbe = getComponent(player, MicrobeComponent)
    if microbe and not microbe.dead then
        local targetPoint = getTargetPoint()
        local movementDirection = getMovementDirection()
        microbe.facingTargetPoint = getTargetPoint()
        microbe.movementDirection = movementDirection
    end
end
