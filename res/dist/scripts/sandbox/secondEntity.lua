local player = Entity("secondPlayer")

playerRigidBody = RigidBodyComponent()
player:addComponent(playerRigidBody)

playerSceneNode = OgreSceneNodeComponent()
player:addComponent(playerSceneNode)
player:addComponent(OgreEntityComponent("Mesh.mesh"))

playerSceneNode.workingCopy.position = Vector3(0, 0, 0)
playerSceneNode:touch()

---[[
playerRigidBody:setDynamicProperties(
	Vector3(10,0,0),
	Quaternion(1,0,0,0),
	Vector3(0,0,0),
	Vector3(0,0,0))
--]]
