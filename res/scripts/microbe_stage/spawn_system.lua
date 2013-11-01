-- System for handling the spawning of entities
class 'SpawnSystem' (System)

function SpawnSystem:__init()
    System.__init(self)
    
    self.spawnTypes = {} --Keeps track of factory functions.
    self.entities = {} --Keeps track of spawned entities to despawn later.
    
    self.minSpawnDist = 30
    self.maxSpawnDist = 50
    self.minDespawnDist = 30
    self.maxDespawnDist = 50
    
    local testFunction = function(x, y)
        -- Setting up an emitter for energy
        local entity = Entity()
        -- Rigid body
        local rigidBody = RigidBodyComponent()
        rigidBody.properties.friction = 0.2
        rigidBody.properties.linearDamping = 0.8
        rigidBody.properties.shape = CylinderShape(
            CollisionShape.AXIS_X, 
            0.4,
            2.0
        )
        rigidBody:setDynamicProperties(
            Vector3(x, y, 0),
            Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)),
            Vector3(0, 0, 0),
            Vector3(0, 0, 0)
        )
        rigidBody.properties:touch()
        entity:addComponent(rigidBody)
        -- Scene node
        local sceneNode = OgreSceneNodeComponent()
        sceneNode.meshName = "molecule.mesh"
        entity:addComponent(sceneNode)
        -- Emitter energy
        local energyEmitter = AgentEmitterComponent()
        entity:addComponent(energyEmitter)
        energyEmitter.agentId = AgentRegistry.getAgentId("energy")
        energyEmitter.emitInterval = 1000
        energyEmitter.emissionRadius = 1
        energyEmitter.maxInitialSpeed = 10
        energyEmitter.minInitialSpeed = 2
        energyEmitter.minEmissionAngle = Degree(0)
        energyEmitter.maxEmissionAngle = Degree(360)
        energyEmitter.meshName = "molecule.mesh"
        energyEmitter.particlesPerEmission = 1
        energyEmitter.particleLifeTime = 5000
        energyEmitter.particleScale = Vector3(0.3, 0.3, 0.3)
        energyEmitter.potencyPerParticle = 3.0
        
        return entity
    end
    
    self:addSpawnType(testFunction, 100)
end

--[[Inserts a factory function for spawning an entity into the SpawnSystem's table.
    Spawn frequency is currently number of spawn attempts per second.]]
function SpawnSystem:addSpawnType(factoryFunction, spawnFrequency)
    table.insert(self.spawnTypes, {factoryFunction = factoryFunction, spawnFrequency = spawnFrequency})
end

function SpawnSystem:update(milliseconds)
    local player = Entity(PLAYER_NAME)
    local playerNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local playerX = playerNode.transform.position.x
    local playerY = playerNode.transform.position.y
    
    --Despawn entities
    for entity,v in pairs(self.entities) do
        local entityNode = entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
        local entityX = entityNode.transform.position.x
        local entityY = entityNode.transform.position.y
        local xDisp = entityX-playerX
        local yDisp = entityY-playerY
        local distSqr = xDisp*xDisp + yDisp*yDisp
        
        if distSqr >= self.maxDespawnDist*self.maxDespawnDist or
                (distSqr >= self.minDespawnDist*self.minDespawnDist and
                math.random() < milliseconds / 1000 * 1) then
            entity:destroy()
            self.entities[entity] = nil
        end
    end
    
    --Spawn entities
    for k,v in pairs(self.spawnTypes) do
        --TODO use RandomManager
        if math.random() < milliseconds / 1000 * v["spawnFrequency"] then
            --Attempt to find a suitable location.
            local xDisp = (2*math.random() - 1) * self.maxSpawnDist
            local yDisp = (2*math.random() - 1) * self.maxSpawnDist
            local distSqr = xDisp*xDisp + yDisp*yDisp
            
            if distSqr >= self.minSpawnDist*self.minSpawnDist and distSqr <= self.maxSpawnDist*self.maxSpawnDist then
                local entity = v["factoryFunction"](playerX + xDisp, playerY + yDisp)
                self.entities[entity] = true
            end
        end
    end
end
