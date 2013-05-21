local player = Entity("secondPlayer")
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

---[[
playerRigidBody:setDynamicProperties(
	Vector3(10,0,0),
	Quaternion(1,0,0,0),
	Vector3(0,0,0),
	Vector3(0,0,0))
--]]
