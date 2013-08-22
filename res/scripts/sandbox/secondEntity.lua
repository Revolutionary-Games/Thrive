local object = Entity("object")

object.rigidBody = RigidBodyComponent()
object.rigidBody.properties.friction = 0.2
object.rigidBody.properties.linearDamping = 0.8
object.rigidBody.properties.shape = btCylinderShape(Vector3(3.75, 1, 3.75))
object.rigidBody:setDynamicProperties(
    Vector3(10, 0, 0),
    Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)),
    Vector3(0, 0, 0),
    Vector3(0, 0, 0)
)
object.rigidBody.properties:touch()
object:addComponent(object.rigidBody)

object.sceneNode = OgreSceneNodeComponent()
object.sceneNode:attachObject(Engine.sceneManager:createEntity("molecule.mesh"))
object:addComponent(object.sceneNode)

object.agentEmitter = AgentEmitterComponent()
object:addComponent(object.agentEmitter)
object.agentEmitter.agentId = 1
object.agentEmitter.emitInterval = 1000
object.agentEmitter.emissionRadius = 1
object.agentEmitter.maxInitialSpeed = 10
object.agentEmitter.minInitialSpeed = 2
object.agentEmitter.minEmissionAngle = Degree(0)
object.agentEmitter.maxEmissionAngle = Degree(360)
object.agentEmitter.meshName = "molecule.mesh"
object.agentEmitter.particlesPerEmission = 1
object.agentEmitter.particleLifeTime = 5000
object.agentEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
object.agentEmitter.potencyPerParticle = 3.0
object.agentEmitter.effectCallback = function(agentEntity, otherEntity)
    agentEntity:destroy()
end
