local player = Entity("player")

player.rigidBody = RigidBodyComponent()
player.rigidBody.properties.linearDamping = 0.5
player.rigidBody.properties.shape = btCylinderShape(Vector3(3.75, 1, 3.75))
player.rigidBody.properties.friction = 0.2
player.rigidBody.properties.linearFactor = Vector3(1, 1, 0)
player.rigidBody.properties.angularFactor = Vector3(0, 0, 1)
player.rigidBody:setDynamicProperties(
    Vector3(0, 0, 0),
    Quaternion(Radian(Degree(90)), Vector3(1, 0, 0)),
    Vector3(0, 0, 0),
    Vector3(0, 0, 0)
)
player.rigidBody.properties:touch()
player:addComponent(player.rigidBody)

player.sceneNode = OgreSceneNodeComponent()
player:addComponent(player.sceneNode)
player:addComponent(OgreEntityComponent("Mesh.mesh"))

player.sceneNode.properties.position = Vector3(0, 0, 0)
player.sceneNode.properties:touch()

player.microbeMovement = MicrobeMovementComponent()
player:addComponent(player.microbeMovement)
player.microbeMovement.force = 0.05

player.onUpdate = OnUpdateComponent()
player:addComponent(player.onUpdate)
player.onUpdate.callback = function(entityId, milliseconds)
    direction = Vector3(0, 0, 0)
    if (Keyboard:isKeyDown(KeyboardSystem.KC_W)) then
        direction = direction + Vector3(0, 1, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_S)) then
        direction = direction + Vector3(0, -1, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_A)) then
        direction = direction + Vector3(-1, 0, 0)
    end
    if (Keyboard:isKeyDown(KeyboardSystem.KC_D)) then
        direction = direction + Vector3(1, 0, 0)
    end
    direction:normalise()
    player.microbeMovement.direction = direction;
end
