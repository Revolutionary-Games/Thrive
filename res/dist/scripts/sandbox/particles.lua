local PARTICLE_INTERVAL = 30 -- Milliseconds
local PARTICLE_MASS = 0.02
local MIN_PARTICLE_SPEED = 1
local MAX_PARTICLE_SPEED = 10
local PARTICLE_SCALE = 0.1
local PARTICLE_LIFETIME = PARTICLE_INTERVAL * 500 -- Milliseconds

function emitParticle(origin)
    local particle = Entity()
    -- Rigid Body
    particle.rigidBody = RigidBodyComponent()
    particle.rigidBody.properties.linearDamping = 0.5
    particle.rigidBody.properties.shape = btCylinderShape(
        Vector3(3.75, 1, 3.75) * PARTICLE_SCALE
    )
    particle.rigidBody.properties.friction = 0.2
    local speed = math.random(MIN_PARTICLE_SPEED, MAX_PARTICLE_SPEED) * PARTICLE_MASS
    local direction = Vector3(
        math.random(-100, 100),
        math.random(-100, 100),
        0
    )
    direction:normalise()
    particle.rigidBody:setDynamicProperties(
        origin,
        Quaternion(Radian(Degree(90)), Vector3(1, 0, 0)),
        Vector3(0,0,0),
        Vector3(0, 0, 0)
    )
    particle.rigidBody:applyCentralImpulse(direction * speed)
    particle.rigidBody.properties.mass = PARTICLE_MASS
    particle.rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    particle.rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    particle.rigidBody.properties:touch()
    particle:addComponent(particle.rigidBody)
    -- Scene Node and Mesh
    particle.sceneNode = OgreSceneNodeComponent()
    particle:addComponent(OgreEntityComponent("Mesh.mesh"))
    particle.sceneNode.properties.position = origin
    --particle.sceneNode.orientation = Quaternion(Radian(Degree(90)), Vector3(1, 0, 0))
    particle.sceneNode.properties.scale = Vector3(PARTICLE_SCALE, PARTICLE_SCALE, PARTICLE_SCALE)
    particle.sceneNode.properties:touch()
    particle:addComponent(particle.sceneNode)
    -- Handle despawn
    particle.onUpdate = OnUpdateComponent()
    particle:addComponent(particle.onUpdate)
    particle.lifeTime = 0
    particle.onUpdate.callback = function(entityId, milliseconds)
        particle.lifeTime = particle.lifeTime + milliseconds
        if particle.lifeTime >= PARTICLE_LIFETIME then
            particle:destroy()
        end
    end
end


local emitter = Entity()

emitter.onUpdate = OnUpdateComponent()
emitter:addComponent(emitter.onUpdate)
emitter.timeSinceLastParticle = PARTICLE_INTERVAL
emitter.onUpdate.callback = function(entityId, milliseconds)
    emitter.timeSinceLastParticle = emitter.timeSinceLastParticle + milliseconds
    while emitter.timeSinceLastParticle >= PARTICLE_INTERVAL do
        emitter.timeSinceLastParticle = emitter.timeSinceLastParticle - PARTICLE_INTERVAL
        emitParticle(Vector3(-10,0,0))
    end 
end
