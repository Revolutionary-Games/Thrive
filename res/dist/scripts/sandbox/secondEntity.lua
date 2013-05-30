local object = Entity("object")

object.rigidBody = RigidBodyComponent()
object.rigidBody.workingCopy.friction = 0.2
object.rigidBody.workingCopy.shape = btCylinderShape(Vector3(6.4, 1, 6.4))
object:addComponent(object.rigidBody)

object.sceneNode = OgreSceneNodeComponent()
object:addComponent(object.sceneNode)
object:addComponent(OgreEntityComponent("Mesh.mesh"))

object.sceneNode.workingCopy.position = Vector3(0, 0, 0)
object.sceneNode:touch()

---[[
object.rigidBody:setDynamicProperties(
	Vector3(10,0,0),
	Quaternion(1,0,0,0),
	Vector3(0,0,0),
	Vector3(0,0,0))
--]]
