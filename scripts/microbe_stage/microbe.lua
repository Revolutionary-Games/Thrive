--------------------------------------------------------------------------------
-- MicrobeComponent
--
-- Holds data common to all microbes. You probably shouldn't use this directly,
-- use the Microbe class (below) instead.
--------------------------------------------------------------------------------
class 'MicrobeComponent' (Component)

COMPOUND_DISTRIBUTION_INTERVAL = 100 -- quantity of physics time between each loop distributing agents to organelles. TODO: Modify to reflect microbe size.

function MicrobeComponent:__init()
    Component.__init(self)
    self.organelles = {}
    self.vacuoles = {}
    self.processOrganelles = {}
    self.movementDirection = Vector3(0, 0, 0)
    self.facingTargetPoint = Vector3(0, 0, 0)
    self.initialized = false
end


function MicrobeComponent:load(storage)
    Component.load(self, storage)
    local organelles = storage:get("organelles", {})
    for i = 1,organelles:size() do
        local organelleStorage = organelles:get(i)
        local organelle = Organelle.loadOrganelle(organelleStorage)
        local q = organelle.position.q
        local r = organelle.position.r
        local s = encodeAxial(q, r)
        self.organelles[s] = organelle
    end
end


function MicrobeComponent:storage()
    local storage = Component.storage(self)
    -- Organelles
    local organelles = StorageList()
    for _, organelle in pairs(self.organelles) do
        local organelleStorage = organelle:storage()
        organelles:append(organelleStorage)
    end
    storage:set("organelles", organelles)
    return storage
end

REGISTER_COMPONENT("MicrobeComponent", MicrobeComponent)


--------------------------------------------------------------------------------
-- Microbe class
--
-- This class serves mostly as an interface for manipulating microbe entities
--------------------------------------------------------------------------------
class 'Microbe'


-- Creates a new microbe with all required components
--
-- @param name
--  The entity's name. If nil, the entity will be unnamed.
--
-- @returns microbe
--  An object of type Microbe
function Microbe.createMicrobeEntity(name)
    local entity
    if name then
        entity = Entity(name)
    else
        entity = Entity()
    end
    local rigidBody = RigidBodyComponent()
    rigidBody.properties.shape = CompoundShape()
    rigidBody.properties.linearDamping = 0.5
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    rigidBody.properties:touch()

    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("microbe")
    
    local components = {
        AgentAbsorberComponent(),
        OgreSceneNodeComponent(),
        MicrobeComponent(),
        reactionHandler,
        rigidBody
    }
    for _, component in ipairs(components) do
        entity:addComponent(component)
    end
    return Microbe(entity)
end

-- I don't feel like checking for each component separately, so let's make a 
-- loop do it with an assert for good measure (see Microbe.__init)
Microbe.COMPONENTS = {
    agentAbsorber = AgentAbsorberComponent.TYPE_ID,
    microbe = MicrobeComponent.TYPE_ID,
    rigidBody = RigidBodyComponent.TYPE_ID,
    sceneNode = OgreSceneNodeComponent.TYPE_ID,
    collisionHandler = CollisionComponent.TYPE_ID
}


-- Constructor
--
-- Requires all necessary components (see Microbe.COMPONENTS) to be present in 
-- the entity.
--
-- @param entity
--  The entity this microbe wraps
function Microbe:__init(entity)
    self.entity = entity
    self.residuePhysicsTime = 0
    for key, typeId in pairs(Microbe.COMPONENTS) do
        local component = entity:getComponent(typeId)
        assert(component ~= nil, "Can't create microbe from this entity, it's missing " .. key)
        self[key] = entity:getComponent(typeId)
    end
    if not self.microbe.initialized then
        self:_initialize()
    end
end


-- Adds a new organelle
--
-- The space at (q,r) must not be occupied by another organelle already.
--
-- @param q,r
--  Offset of the organelle's center relative to the microbe's center in 
--  axial coordinates.
--
-- @param organelle
--  The organelle to add
function Microbe:addOrganelle(q, r, organelle)
    local s = encodeAxial(q, r)
    if self.microbe.organelles[s] then
        assert(false)
        return false
    end
    self.microbe.organelles[s] = organelle
    organelle.microbe = self
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    -- Collision shape
    self.rigidBody.properties.shape:addChildShape(
        translation,
        Quaternion(Radian(0), Vector3(1,0,0)),
        organelle.collisionShape
    )
    -- Scene node
    organelle.sceneNode.parent = self.entity
    organelle.sceneNode.transform.position = translation
    organelle.sceneNode.transform:touch()
    organelle:onAddedToMicrobe(self, q, r)
    self:_updateAllHexColours()
    return true
end


-- Adds a storage vacuole
--
-- @param vacuole
--  An object of type Vacuole
function Microbe:addVacuole(vacuole)
    assert(vacuole.agentId ~= nil)
    assert(vacuole.capacity ~= nil)
    assert(vacuole.amount ~= nil)
    local agentId = vacuole.agentId
    if not self.microbe.vacuoles[agentId] then
        self.microbe.vacuoles[agentId] = {}
    end
    local vacuoleList = self.microbe.vacuoles[agentId]
    table.insert(vacuoleList, vacuole)
    self:_updateAgentAbsorber(vacuole.agentId)
end


-- Adds a process organelle
--
-- @param processOrganelle
--  An object of type ProcessOrganelle
function Microbe:addProcessOrganelle(processOrganelle)
    table.insert(self.microbe.processOrganelles, processOrganelle)
end


-- Queries the currently stored amount of an agent
--
-- @param agentId
--  The id of the agent to query
--
-- @returns amount
--  The amount stored in the microbe's vacuoles
function Microbe:getAgentAmount(agentId)
    local vacuoleList = self.microbe.vacuoles[agentId]
    local totalAmount = 0.0
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            totalAmount = totalAmount + vacuole.amount
        end
    end
    return totalAmount
end


-- Retrieves the organelle occupying a hex cell
--
-- @param q, r
--  Axial coordinates, relative to the microbe's center
--
-- @returns organelle
--  The organelle at (q,r) or nil if the hex is unoccupied
function Microbe:getOrganelleAt(q, r)
    for _, organelle in pairs(self.microbe.organelles) do
        local localQ = q - organelle.position.q
        local localR = r - organelle.position.r
        if organelle:getHex(localQ, localR) ~= nil then
            return organelle
        end
    end
    return nil
end


-- Removes the organelle at a hex cell
--
-- @param q, r
--  Axial coordinates of the organelle's center
--
-- @returns success
--  True if an organelle has been removed, false if there was no organelle
--  at (q,r)
function Microbe:removeOrganelle(q, r)
    local index = nil
    local s = encodeAxial(q, r)
    local organelle = table.remove(self.microbe.organelles, index)
    if not organelle then
        return false
    end
    organelle.position.q = 0
    organelle.position.r = 0
    organelle:onRemovedFromMicrobe(self)
    self:_updateAllHexColours()
    return true
end


-- Removes a vacuole from the microbe
--
-- @param vacuole
--  The vacuole to remove
function Microbe:removeVacuole(vacuole)
    local vacuoleList = self.microbe.vacuoles[vacuole.agentId]
    local indexToRemove = 0
    for i, v in ipairs(self.microbe.vacuoles) do
        if v == vacuole then
            indexToRemove = i
            break
        end
    end
    assert(indexToRemove > 0, "Vacuole not found")
    table.remove(self.microbe.vacuoles, indexToRemove)
    self:_updateAgentAbsorber(vacuole.agentId)
end


-- Stores an agent in the microbe's vacuoles
--
-- @param agentId
--  The agent to store
--
-- @param amount
--  The amount to store
--
-- @returns remainingAmount
--  The surplus that could not be stored because the microbe's vacuoles for
--  this agent are full.
function Microbe:storeAgent(agentId, amount)
    local vacuoleList = self.microbe.vacuoles[agentId]
    local remainingAmount = amount
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            local storedAmount = math.min(remainingAmount, vacuole.capacity - vacuole.amount)
            vacuole.amount = vacuole.amount + storedAmount
            remainingAmount = remainingAmount - storedAmount
            if remainingAmount <= 0.0 then
                break
            end
        end
    end
    self:_updateAgentAbsorber(agentId)
    return remainingAmount
end


-- Removes an agent from the microbe's vacuoles
--
-- @param agentId
--  The agent to remove
--
-- @param maxAmount
--  The maximum amount to take
--
-- @returns amount
--  The amount that was actually taken, between 0.0 and maxAmount.
function Microbe:takeAgent(agentId, maxAmount)
    local vacuoleList = self.microbe.vacuoles[agentId]
    local totalTaken = 0.0
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            local amountTaken = math.min(maxAmount - totalTaken, vacuole.amount)
            vacuole.amount = math.max(vacuole.amount - amountTaken, 0.0)
            totalTaken = totalTaken + amountTaken
            if totalTaken >= maxAmount then
                break
            end
        end
    end
    self:_updateAgentAbsorber(agentId)
    return totalTaken
end


-- Updates the microbe's state
function Microbe:update(milliseconds)
    -- Vacuoles
    
    for agentId, vacuoleList in pairs(self.microbe.vacuoles) do
        -- Check for agents to store
        local amount = self.agentAbsorber:absorbedAgentAmount(agentId)
        if amount > 0.0 then
            self:storeAgent(agentId, amount)
        end
    end
    -- Distribute agents to StorageOrganelles
    self.residuePhysicsTime = self.residuePhysicsTime + milliseconds
    while self.residuePhysicsTime > COMPOUND_DISTRIBUTION_INTERVAL do -- For every COMPOUND_DISTRIBUTION_INTERVAL passed
        for agentId, vacuoleList in pairs(self.microbe.vacuoles) do -- Foreach agent type.
            if self:getAgentAmount(agentId) > 0 then -- If microbe contains the compound
                local candidateIndices = {} -- Indices of organelles that want the agent
                for i, processOrg in ipairs(self.microbe.processOrganelles) do  
                    if processOrg:wantsInputAgent(agentId) then   
                        table.insert(candidateIndices, i) -- Organelle has determined that it is interrested in obtaining the agnet
                    end
                end
                if #candidateIndices > 0 then -- If there were any candidates, pick a random winner.
                    local chosenProcessOrg = self.microbe.processOrganelles[candidateIndices[rng:getInt(1,#candidateIndices)]]
                    chosenProcessOrg:storeAgent(agentId, self:takeAgent(agentId, 1))
                end
            end
        end
        self.residuePhysicsTime = self.residuePhysicsTime - COMPOUND_DISTRIBUTION_INTERVAL
    end
    -- Other organelles
    for _, organelle in pairs(self.microbe.organelles) do
        organelle:update(self, milliseconds)
    end
end


-- Private function for initializing a microbe's components
function Microbe:_initialize()
    self.rigidBody.properties.shape:clear()
    -- Organelles
    for s, organelle in pairs(self.microbe.organelles) do
        organelle.microbe = self
        local q = organelle.position.q
        local r = organelle.position.r
        local x, y = axialToCartesian(q, r)
        local translation = Vector3(x, y, 0)
        -- Collision shape
        self.rigidBody.properties.shape:addChildShape(
            translation,
            Quaternion(Radian(0), Vector3(1,0,0)),
            organelle.collisionShape
        )
        -- Scene node
        organelle.sceneNode.parent = self.entity
        organelle.sceneNode.transform.position = translation
        organelle.sceneNode.transform:touch()
        organelle:onAddedToMicrobe(self, q, r)
    end
    self:_updateAllHexColours()
    self.microbe.initialized = true
end


-- Private function for updating the agent absorber
--
-- Toggles the absorber on and off depending on the remaining storage
-- capacity of the vacuoles.
function Microbe:_updateAgentAbsorber(agentId)
    local vacuoleList = self.microbe.vacuoles[agentId]
    local canAbsorb = false
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            canAbsorb = canAbsorb or vacuole.amount < vacuole.capacity
        end
    end
    self.agentAbsorber:setCanAbsorbAgent(agentId, canAbsorb)
end


-- Private function for updating the colours of the organelles
--
-- The simple coloured hexes are a placeholder for proper models.
function Microbe:_updateAllHexColours()
    for s, organelle in pairs(self.microbe.organelles) do
        organelle:updateHexColours()
    end
end


--------------------------------------------------------------------------------
-- MicrobeSystem
--
-- Updates microbes
--------------------------------------------------------------------------------

class 'MicrobeSystem' (System)

function MicrobeSystem:__init()
    System.__init(self)
    self.entities = EntityFilter(
        {
            AgentAbsorberComponent,
            MicrobeComponent,
            OgreSceneNodeComponent,
            RigidBodyComponent, 
            CollisionComponent
        }, 
        true
    )
    self.microbes = {}
end


function MicrobeSystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)    
end


function MicrobeSystem:shutdown()
    self.entities:shutdown()
end


function MicrobeSystem:update(milliseconds)
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
    end
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        microbe:update(milliseconds)
    end
end

