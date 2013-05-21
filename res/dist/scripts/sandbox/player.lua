local player = Entity("player")

playerRigidBody = RigidBodyComponent()
playerRigidBody.workingCopy.friction = 0.2
playerRigidBody:touch()
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

ACCELERATION = 0.01
player.onUpdate = OnUpdateComponent()
player:addComponent(player.onUpdate)
player.onUpdate.callback = function(entityId, milliseconds)
    impulse = Vector3(0, 0, 0)
    if (Keyboard:isKeyDown(KeyboardSystem.KC_W)) then
        impulse = impulse + Vector3(0, 1, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_S)) then
        impulse = impulse + Vector3(0, -1, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_A)) then
        impulse = impulse + Vector3(-1, 0, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_D)) then
        impulse = impulse + Vector3(1, 0, 0)
    end
    impulse = impulse * ACCELERATION * milliseconds
    playerRigidBody:applyCentralImpulse(impulse);
end
