local player = Entity("player")
playerTransform = TransformComponent()
player:addComponent(playerTransform)
playerSceneNode = OgreSceneNodeComponent()
player:addComponent(playerSceneNode)
player:addComponent(OgreEntityComponent("Sinbad.mesh"))

playerTransform.workingCopy.position = Vector3(0, 0, 0)
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




