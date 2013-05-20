local player = Entity("player")

playerPhysicsTransform = PhysicsTransformComponent()
player:addComponent(playerPhysicsTransform)
playerRigidBody = RigidBodyComponent()
player:addComponent(playerRigidBody)

playerSceneNode = OgreSceneNodeComponent()
player:addComponent(playerSceneNode)
player:addComponent(OgreEntityComponent("Mesh.mesh"))

playerSceneNode.workingCopy.position = Vector3(0, 0, 0)
playerSceneNode:touch()

--[[
playerRigidBody:setDynamicProperties(
	Vector3(0,0,0),
	Quaternion(1,0,0,0),
	Vector3(1,0,0),
	Vector3(0,0,0))
--]]

playerInput = OnKeyComponent()
player:addComponent(playerInput)
MOVEMENT_SPEED = 20
---[[
playerInput.onPressed = function (entityId, event)

    if event.key == KeyEvent.KC_W then
        playerRigidBody:addToForce(Vector3(0, MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_S then
        playerRigidBody:addToForce(Vector3(0, -MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_A then
        playerRigidBody:addToForce(Vector3(-MOVEMENT_SPEED, 0, 0));
    elseif event.key == KeyEvent.KC_D then
        playerRigidBody:addToForce(Vector3(MOVEMENT_SPEED, 0, 0));
    end
    --playerRigidBody:touch()
end

playerInput.onReleased = function (entityId, event)
    if event.key == KeyEvent.KC_W then
        playerRigidBody:addToForce(Vector3(0, -MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_S then
        playerRigidBody:addToForce(Vector3(0,MOVEMENT_SPEED, 0));
    elseif event.key == KeyEvent.KC_A then
        playerRigidBody:addToForce(Vector3(MOVEMENT_SPEED, 0, 0));
    elseif event.key == KeyEvent.KC_D then
        playerRigidBody:addToForce(Vector3(-MOVEMENT_SPEED, 0, 0));
    end
end
--]]
