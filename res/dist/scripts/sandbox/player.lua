local player = Entity("player")
playerTransform = TransformComponent()
player:addComponent(playerTransform)

playerPhysicsTransform = PhysicsTransformComponent()
player:addComponent(playerPhysicsTransform)
playerRigidBody = RigidBodyComponent()
player:addComponent(playerRigidBody)

playerSceneNode = OgreSceneNodeComponent()
player:addComponent(playerSceneNode)
player:addComponent(OgreEntityComponent("Mesh.mesh"))

playerTransform.workingCopy.position = Vector3(0, 0, 0)
playerTransform:touch()

--[[
playerRigidBody:setDynamicProperties(
	Vector3(0,0,0),
	Quaternion(1,0,0,0),
	Vector3(1,0,0),
	Vector3(0,0,0))
--]]
playerMovable = MovableComponent()
player:addComponent(playerMovable)

playerInput = OnKeyComponent()
player:addComponent(playerInput)
MOVEMENT_SPEED = 20
---[[
playerInput.onPressed = function (entityId, event)
    --playerPhysicsTransform:printPosition()
    playerRigidBody:printPosition()
    playerRigidBody:printVelocity()
    playerRigidBody:printForce()

    if event.key == KeyEvent.KC_W then
        --playerMovable.velocity.y = playerMovable.velocity.y + MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(0, MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_S then
        --playerMovable.velocity.y = playerMovable.velocity.y - MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(0, -MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_A then
        --playerMovable.velocity.x = playerMovable.velocity.x - MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(-MOVEMENT_SPEED, 0, 0));
    elseif event.key == KeyEvent.KC_D then
        --playerMovable.velocity.x = playerMovable.velocity.x + MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(MOVEMENT_SPEED, 0, 0));
    end
    --playerRigidBody:touch()
end

playerInput.onReleased = function (entityId, event)
    if event.key == KeyEvent.KC_W then
        --playerMovable.velocity.y = playerMovable.velocity.y - MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(0, -MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_S then
        --playerMovable.velocity.y = playerMovable.velocity.y + MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(0,MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_A then
        --playerMovable.velocity.x = playerMovable.velocity.x + MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(MOVEMENT_SPEED, 0, 0));
    elseif event.key == KeyEvent.KC_D then
        --playerMovable.velocity.x = playerMovable.velocity.x - MOVEMENT_SPEED
        playerRigidBody:addToForce(Vector3(-MOVEMENT_SPEED, 0, 0));
    end
end
--]]
