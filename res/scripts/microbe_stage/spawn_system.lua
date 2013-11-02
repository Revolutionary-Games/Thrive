-- System for handling the spawning of entities
class 'SpawnSystem' (System)

function SpawnSystem:__init()
    System.__init(self)
    
    self.spawnTypes = {} --Keeps track of factory functions.
    self.spawnedEntities = {} --Keeps track of spawned entities to despawn later.
    
    self.playerPosPrev = nil --A Vector3 that remembers the player's position in the last spawn cycle
    
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
function SpawnSystem:addSpawnType(factoryFunction, spawnFrequency, spawnRadius)
    newSpawnType = {}
    newSpawnType.factoryFunction = factoryFunction
    newSpawnType.spawnFrequency = spawnFrequency
    newSpawnType.spawnRadius = spawnRadius
    table.insert(self.spawnTypes, newSpawnType)
end

function SpawnSystem:_doSpawnCycle()    
    local player = Entity(PLAYER_NAME)
    
    local playerNode = player:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local playerX = playerNode.transform.position.x
    local playerY = playerNode.transform.position.y
    
    --Initialize previous player position if necessary
    if self.playerPosPrev == nil then
        self.playerPosPrev = Vector3(playerNode.transform.position.x,
                playerNode.transform.position.y, playerNode.transform.position.z)
    end
    
    local playerXPrev = self.playerPosPrev.x
    local playerYPrev = self.playerPosPrev.y
    
    --Despawn entities
    for entity,v in pairs(self.spawnedEntities) do
        local entityNode = entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
        local entityX = entityNode.transform.position.x
        local entityY = entityNode.transform.position.y
        local xDist = entityX-playerX
        local yDist = entityY-playerY
        local distSqr = xDist*xDist + yDist*yDist
        
        if distSqr >= v.spawnRadius * v.spawnRadius then
            entity:destroy()
            self.spawnedEntities[entity] = nil
        end
    end
    
    --Spawn entities
    for k,v in pairs(self.spawnTypes) do
        for i = 1, 10 do
            --TODO use RandomManager
            if math.random() < self.spawnInterval / 10 / 1000 * v.spawnFrequency then
                --Attempt to find a suitable location.
                local xDist = (2*math.random() - 1) * v.spawnRadius
                local yDist = (2*math.random() - 1) * v.spawnRadius
                local distSqr = xDist*xDist + yDist*yDist
                
                local xDistPrev = xDist + playerX - playerXPrev
                local yDistPrev = yDist + playerY - playerYPrev
                local distSqrPrev = xDistPrev*xDistPrev + yDistPrev*yDistPrev
                
                if distSqr <= v.spawnRadius * v.spawnRadius and
                        distSqrPrev > v.spawnRadius * v.spawnRadius then
                    local entity = v.factoryFunction(playerX + xDist, playerY + yDist)
                    self.spawnedEntities[entity] = {spawnRadius = v.spawnRadius}
                end
            end
        end
    end
    
    self.playerPosPrev.x = playerNode.transform.position.x
    self.playerPosPrev.y = playerNode.transform.position.y
    self.playerPosPrev.z = playerNode.transform.position.z
end

function SpawnSystem:update(milliseconds)
    self.residueTime = self.residueTime + milliseconds
    
    while self.residueTime > self.spawnInterval do
        self.residueTime = self.residueTime - self.spawnInterval
        
        self:_doSpawnCycle()
    end
end
