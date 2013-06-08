local player = Entity("player")

player.rigidBody = RigidBodyComponent()
player.rigidBody.workingCopy.linearDamping = 0.5
player.rigidBody.workingCopy.shape = btCylinderShape(Vector3(3.75, 1, 3.75))
player.rigidBody.workingCopy.friction = 0.2
player.rigidBody.workingCopy.linearFactor = Vector3(1, 1, 0)
player.rigidBody.workingCopy.angularFactor = Vector3(0, 0, 1)
player.rigidBody:setDynamicProperties(
    Vector3(0, 0, 0),
    Quaternion(Radian(Degree(90)), Vector3(1, 0, 0)),
    Vector3(0, 0, 0),
    Vector3(0, 0, 0)
)
player.rigidBody:touch()
player:addComponent(player.rigidBody)

player.sceneNode = OgreSceneNodeComponent()
player:addComponent(player.sceneNode)
player:addComponent(OgreEntityComponent("Mesh.mesh"))

player.sceneNode.workingCopy.position = Vector3(0, 0, 0)
player.sceneNode:touch()

ACCELERATION = 0.05
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
    if not impulse:isZeroLength() then
        impulse = impulse:normalisedCopy() * ACCELERATION * milliseconds
        player.rigidBody:applyCentralImpulse(impulse);
    end
end
