background = Entity("background")
skyplane = SkyPlaneComponent()
skyplane.workingCopy.plane.normal = Vector3(0, 0, 1)
skyplane.workingCopy.plane.d = 1000
skyplane:touch()
background:addComponent(skyplane)

onupdate = OnUpdateComponent()
background:addComponent(onupdate)
onupdate.callback = function(entityId, milliseconds)
    skyplane.workingCopy.plane.d = (skyplane.workingCopy.plane.d + milliseconds) % 10000
    skyplane:touch()
end

player = Entity("player")
playerTransform = TransformComponent()
player:addComponent(playerTransform)
playerMesh = MeshComponent()
player:addComponent(playerMesh)
playerMesh.workingCopy.meshName = "Sinbad.mesh"
playerMesh:touch()

playerTransform.position = Vector3(0, 0, 0)
playerTransform:touch()

playerMovable = MovableComponent()
player:addComponent(playerMovable)

playerInput = OnKeyComponent()
player:addComponent(playerInput)
MOVEMENT_SPEED = 10
playerInput.onPressed = function (entityId, event)
    if event.key == KeyEvent.KC_W then
        playerMovable.velocity.y = playerMovable.velocity.y + MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_S then
        playerMovable.velocity.y = playerMovable.velocity.y - MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_A then
        playerMovable.velocity.x = playerMovable.velocity.x - MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_D then
        playerMovable.velocity.x = playerMovable.velocity.x + MOVEMENT_SPEED
    end
end

playerInput.onReleased = function (entityId, event)
    if event.key == KeyEvent.KC_W then
        playerMovable.velocity.y = playerMovable.velocity.y - MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_S then
        playerMovable.velocity.y = playerMovable.velocity.y + MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_A then
        playerMovable.velocity.x = playerMovable.velocity.x + MOVEMENT_SPEED
    elseif event.key == KeyEvent.KC_D then
        playerMovable.velocity.x = playerMovable.velocity.x - MOVEMENT_SPEED
    end
end



