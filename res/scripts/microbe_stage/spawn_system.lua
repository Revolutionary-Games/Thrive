-- System for handling the spawning of entities
class 'SpawnSystem' (System)

function SpawnSystem:__init()
    System.__init(self)
    
    self.spawnTypes = {} --Keeps track of factory functions.
    self.spawnedEntities = {} --Keeps track of spawned entities to despawn later.
    
    self.minSpawnDist = 30
    self.maxSpawnDist = 60
    self.minDespawnDist = 30
    self.maxDespawnDist = 60
    
    self.residueTime = 0 --Stores how much time has passed since the last spawn cycle
    self.spawnInterval = 100 --Time between spawn cycles
end

-- Adds a new type of entity to spawn in the SpawnSystem
--
-- @param factoryFunction
--  The function called by the SpawnSystem to create the entity. It should have two
--  parameters, x and y positions, and it should return the new entity.
--
-- @param spawnFrequency
--  On average, the entities of the given type that should attempt to spawn every
--  second.
function SpawnSystem:addSpawnType(factoryFunction, spawnFrequency)
    table.insert(self.spawnTypes, {factoryFunction = factoryFunction, spawnFrequency = spawnFrequency})
end

function SpawnSystem:update(milliseconds)
    self.residueTime = self.residueTime + milliseconds
    
    while self.residueTime > self.spawnInterval do
        self.residueTime = self.residueTime - self.spawnInterval
         
        local player = Entity(PLAYER_NAME)
        local playerNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
        local playerX = playerNode.transform.position.x
        local playerY = playerNode.transform.position.y
        
        --Despawn entities
        for entity,v in pairs(self.spawnedEntities) do
            local entityNode = entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
            local entityX = entityNode.transform.position.x
            local entityY = entityNode.transform.position.y
            local xDist = entityX-playerX
            local yDist = entityY-playerY
            local distSqr = xDist*xDist + yDist*yDist
            
            if distSqr >= self.maxDespawnDist*self.maxDespawnDist or
                    (distSqr >= self.minDespawnDist*self.minDespawnDist and
                    math.random() < self.spawnInterval / 1000 * 1) then
                entity:destroy()
                self.spawnedEntities[entity] = nil
            end
        end
        
        --Spawn entities
        for k,v in pairs(self.spawnTypes) do
            for i = 1, 10 do
                --TODO use RandomManager
                if math.random() < self.spawnInterval / 10 / 1000 * v["spawnFrequency"] then
                    --Attempt to find a suitable location.
                    local xDist = (2*math.random() - 1) * self.maxSpawnDist
                    local yDist = (2*math.random() - 1) * self.maxSpawnDist
                    local distSqr = xDist*xDist + yDist*yDist
                    
                    if distSqr >= self.minSpawnDist*self.minSpawnDist and
                            distSqr <= self.maxSpawnDist*self.maxSpawnDist then
                        local entity = v["factoryFunction"](playerX + xDist, playerY + yDist)
                        self.spawnedEntities[entity] = true
                    end
                end
            end
        end
    end
end
