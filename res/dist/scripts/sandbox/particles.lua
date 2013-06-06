local PARTICLE_INTERVAL = 50 -- Milliseconds
local MIN_PARTICLE_SPEED = 15
local MAX_PARTICLE_SPEED = 50
local PARTICLE_SCALE = 0.2
local PARTICLE_LIFETIME = 5000 -- Milliseconds

function emitParticle(origin)
    local particle = Entity()
    -- Rigid Body
    particle.rigidBody = RigidBodyComponent()
    particle.rigidBody.workingCopy.linearDamping = 0.5
    particle.rigidBody.workingCopy.shape = btCylinderShape(
        Vector3(3.75, 1, 3.75) * PARTICLE_SCALE
    )
    particle.rigidBody.workingCopy.friction = 0.2
    local speed = math.random(MIN_PARTICLE_SPEED, MAX_PARTICLE_SPEED)
    local direction = Vector3(
        math.random(-1, 1),
        math.random(-1, 1),
        0
    )
    direction:normalise()
    particle.rigidBody:setDynamicProperties(
        origin,
        Quaternion(Radian(Degree(90)), Vector3(1, 0, 0)),
        direction * speed,
        Vector3(0, 0, 0)
    )
    particle.rigidBody.workingCopy.mass = 0.01
    particle.rigidBody:touch()
    particle:addComponent(particle.rigidBody)
    -- Scene Node and Mesh
    particle.sceneNode = OgreSceneNodeComponent()
    particle:addComponent(OgreEntityComponent("Mesh.mesh"))
    particle.sceneNode.workingCopy.position = origin
    particle.sceneNode.workingCopy.orientation = Quaternion(Radian(Degree(90)), Vector3(1, 0, 0))
    particle.sceneNode.workingCopy.scale = Vector3(PARTICLE_SCALE, PARTICLE_SCALE, PARTICLE_SCALE)
    particle.sceneNode:touch()
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
emitter.timeSinceLastParticle = 0
emitter.onUpdate.callback = function(entityId, milliseconds)
    emitter.timeSinceLastParticle = emitter.timeSinceLastParticle + milliseconds
    if emitter.timeSinceLastParticle >= PARTICLE_INTERVAL then
        emitter.timeSinceLastParticle = emitter.timeSinceLastParticle - PARTICLE_INTERVAL
        emitParticle(Vector3(-10,0,0))
    end 
end
