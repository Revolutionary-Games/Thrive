local object = Entity("object")

object.rigidBody = RigidBodyComponent()
object.rigidBody.properties.friction = 0.2
object.rigidBody.properties.linearDamping = 0.8
object.rigidBody.properties.shape = btCylinderShape(Vector3(3.75, 1, 3.75))
object.rigidBody:setDynamicProperties(
    Vector3(10, 0, 0),
    Quaternion(Radian(Degree(90)), Vector3(1, 0, 0)),
    Vector3(0, 0, 0),
    Vector3(0, 0, 0)
)
object.rigidBody.properties:touch()
object:addComponent(object.rigidBody)

object.sceneNode = OgreSceneNodeComponent()
object:addComponent(object.sceneNode)
object:addComponent(OgreEntityComponent("Mesh.mesh"))

object.sceneNode.properties.position = Vector3(0, 0, 0)
object.sceneNode.properties:touch()

