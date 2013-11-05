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
    local playerPos = playerNode.transform.position
    
    --Initialize previous player position if necessary
    if self.playerPosPrev == nil then
        self.playerPosPrev = Vector3(playerPos.x, playerPos.y, playerPos.z)
    end
    
    --Despawn entities
    for entity,info in pairs(self.spawnedEntities) do
        local entityNode = entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
        local entityPos = entityNode.transform.position
        local distSqr = playerPos:squaredDistance(entityPos)
        
        if distSqr >= info.spawnRadius * info.spawnRadius then
            entity:destroy()
            self.spawnedEntities[entity] = nil
        end
    end
    
    --Spawn entities
    for _,spawnType in pairs(self.spawnTypes) do
        for i = 1, 10 do
            --TODO use RandomManager
            if math.random() < self.spawnInterval / 10 / 1000 * spawnType.spawnFrequency then
                --Attempt to find a suitable location.
                local xDist = (2*math.random() - 1) * spawnType.spawnRadius
                local yDist = (2*math.random() - 1) * spawnType.spawnRadius
                local zDist = 0
                local displacement = Vector3(xDist, yDist, zDist)
                local distSqr = displacement:squaredLength()
                
                local displacementPrev = displacement + playerPos - self.playerPosPrev
                local distSqrPrev = displacementPrev:squaredLength()
                
                if distSqr <= spawnType.spawnRadius * spawnType.spawnRadius and
                        distSqrPrev > spawnType.spawnRadius * spawnType.spawnRadius then
                    local entity = spawnType.factoryFunction(playerPos + displacement)
                    self.spawnedEntities[entity] = {spawnRadius = spawnType.spawnRadius}
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
