
--------------------------------------------------------------------------------
-- SpawnedComponent
--
-- Holds information about an entity spawned by spawnComponent
--------------------------------------------------------------------------------
SpawnedComponent = class(
    function(self)

        self.spawnRadiusSqr = 1000
    end
)

SpawnedComponent.TYPE_NAME = "SpawnedComponent"

SPAWN_INTERVAL = 100 --Time between spawn cycles

function SpawnedComponent:load(storage)

    self.spawnRadiusSqr = storage:get("spawnRadius", 1000)
end


function SpawnedComponent:storage(storage)

    storage:set("spawnRadius", self.spawnRadiusSqr)

end

REGISTER_COMPONENT("SpawnedComponent", SpawnedComponent)


--------------------------------------------------------------------------------
-- SpawnSystem
--
-- System for spawning and despawning entities
--------------------------------------------------------------------------------
SpawnSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)
        
        self.entities = EntityFilter.new(
            {
                SpawnedComponent
            }
        )
        self.nextId = 1
        self.spawnTypes = {} --Keeps track of factory functions.
        
        self.playerPosPrev = nil --A Vector3 that remembers the player's position in the last spawn cycle
        
        self.timeSinceLastCycle = 0 --Stores how much time has passed since the last spawn cycle
        
        currentSpawnSystem = self
        
    end
)

currentSpawnSystem = nil

-- Override from System
function SpawnSystem:init(gameState)
    LuaSystem.init(self, "SpawnSystem", gameState)
    self.entities:init(gameState.wrapper)
end

-- Override from System
function SpawnSystem:shutdown()
    self.entities:shutdown()
    LuaSystem.shutdown(self)
end

-- Adds a new type of entity to spawn in the SpawnSystem
-- Also returns an unique id to reference it.
-- Note that SpawnedComponent will be added automatically and should not be done by the factory function
--
-- @param factoryFunction
--  The function called by the SpawnSystem to create the entity. It should have two
--  parameters, x and y positions, and it should return the new entity.
--
-- @param spawnDensity
--  On average, the number of entities of the given type per square unit.
--
-- @param spawnRadius
--  The distance from the player that the entity can spawn or despawn.
function SpawnSystem:addSpawnType(factoryFunction, spawnDensity, spawnRadius)
    newSpawnType = {}
    newSpawnType.factoryFunction = factoryFunction
    newSpawnType.spawnRadius = spawnRadius
    newSpawnType.spawnRadiusSqr = spawnRadius * spawnRadius
    
    --spawnFrequency is on average how many entities should pass the first condition
    --in each spawn cycle (See _doSpawnCycle). spawnRadius^2 * 4 is used because
    --that is the area of the square region where entities attempt to spawn.
    newSpawnType.spawnFrequency = spawnDensity * spawnRadius * spawnRadius * 4
    
    id = "spawnType" .. self.nextId
    self.nextId = self.nextId + 1
    self.spawnTypes[id] = newSpawnType
    return id
end

--removes an element
--
-- @param id
--  The id of the element to be removed
function SpawnSystem:removeSpawnType(id)
    self.spawnTypes[id] = nil
end

-- For each entity type, spawns the appropriate number of entities within the spawn
-- radius of the player at its current location but outside of the spawn radius of the player
-- at its location during the previous spawn cycle.
function SpawnSystem:_doSpawnCycle()    
    local player = Entity.new(PLAYER_NAME, self.gameState.wrapper)
    
    local playerNode = getComponent(player, OgreSceneNodeComponent)
    local playerPos = playerNode.transform.position
    
    --Initialize previous player position if necessary
    if self.playerPosPrev == nil then
        self.playerPosPrev = Vector3(playerPos.x, playerPos.y, playerPos.z)
    end
    
    for _, entity in pairs(self.entities:removedEntities()) do
        self.microbes[entityId] = nil
    end
    for _, entityId in pairs(self.entities:addedEntities()) do
        local microbe = Microbe.new(Entity.new(entityId, self.gameState.wrapper))
        self.microbes[entityId] = microbe
    end
    self.entities:clearChanges()
    
    --Despawn entities    
    for _, entityId in pairs(self.entities:entities()) do
        entity = Entity.new(entityId, self.gameState.wrapper)
        local spawnComponent = getComponent(entity, SpawnedComponent)
        local sceneNode = getComponent(entity, OgreSceneNodeComponent)
        local entityPos = sceneNode.transform.position
        local distSqr = playerPos:squaredDistance(entityPos)
        
        --Destroy and forget entities outside the spawn radius.
        if distSqr >= spawnComponent.spawnRadiusSqr then
            entity:destroy()
        elseif entityPos.z ~= 0 then
            local rigidBody = getComponent(entity, RigidBodyComponent)
            rigidBody.dynamicProperties.position.z = 0
            rigidBody.dynamicProperties:touch()
        end
    end

    
    --Spawn entities
    for id,spawnType in pairs(self.spawnTypes) do
        --To actually spawn a given entity for a given attempt, two conditions should be met.
        --The first condition is a random chance that adjusts the spawn frequency to the approprate
        --amount. The second condition is whether the entity will spawn in a valid position.
        --It is checked when the first condition is met and a position
        --for the entity has been decided.
        
        --To allow more than one entity of each type to spawn per spawn cycle, the SpawnSystem
        --attempts to spawn each given entity multiple times depending on the spawnFrequency.
        --numAttempts stores how many times the SpawnSystem attempts to spawn the given entity.
        local numAttempts = math.max(1, math.ceil(spawnType.spawnFrequency * 2))
        for i = 1, numAttempts do
            if rng:getReal(0,1) < spawnType.spawnFrequency / numAttempts then
                --First condition passed. Choose a location for the entity.
                
                --A random location in the square of sidelength 2*spawnRadius
                --centered on the player is chosen. The corners
                --of the square are outside the spawning region, but they
                --will fail the second condition, so entities still only
                --spawn within the spawning region.
                local xDist = rng:getReal(-1,1) * spawnType.spawnRadius
                local yDist = rng:getReal(-1,1) * spawnType.spawnRadius
                local zDist = 0
                
                --Distance from the player.
                local displacement = Vector3(xDist, yDist, zDist)
                local distSqr = displacement:squaredLength()
                
                --Distance from the location of the player in the previous spawn cycle.
                local displacementPrev = displacement + playerPos - self.playerPosPrev
                local distSqrPrev = displacementPrev:squaredLength()
                
                if distSqr <= spawnType.spawnRadiusSqr and distSqrPrev > spawnType.spawnRadiusSqr then
                    --Second condition passed. Spawn the entity.
                    local entity = spawnType.factoryFunction(playerPos + displacement)
                    if entity then
                        local spawnComponent = SpawnedComponent.new()
                        spawnComponent.spawnRadiusSqr = spawnType.spawnRadiusSqr
                        entity:addComponent(spawnComponent)
                    end
                end
            end
        end
    end
    
    --Update previous player location.
    self.playerPosPrev.x = playerNode.transform.position.x
    self.playerPosPrev.y = playerNode.transform.position.y
    self.playerPosPrev.z = playerNode.transform.position.z
end

-- Override from System
function SpawnSystem:update(renderTime, logicTime)
    self.timeSinceLastCycle = self.timeSinceLastCycle + logicTime
    
    --Perform spawn cycle if necessary (Reason for "if" rather than "while" stated below)
    if self.timeSinceLastCycle > SPAWN_INTERVAL then        
        self:_doSpawnCycle()
        
        --Spawn interval does not affect spawning logic. Therefore, at most one spawn
        --cycle will be done per frame.
         self.timeSinceLastCycle = 0
    end
end
