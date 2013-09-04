--------------------------------------------------------------------------------
-- MicrobeComponent
--
-- Holds data common to all microbes. You probably shouldn't use this directly,
-- use the Microbe class (below) instead.
--------------------------------------------------------------------------------
class 'MicrobeComponent' (Component)

function MicrobeComponent:__init()
    Component.__init(self)
    self.organelles = {}
    self.vacuoles = {}
    self.movementDirection = Vector3(0, 0, 0)
    self.facingTargetPoint = Vector3(0, 0, 0)
end

REGISTER_COMPONENT("MicrobeComponent", MicrobeComponent)


--------------------------------------------------------------------------------
-- Microbe class
--
-- This class serves mostly as an interface for manipulating microbe entities
--------------------------------------------------------------------------------
class 'Microbe'


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
    local components = {
        AgentAbsorberComponent(),
        OgreSceneNodeComponent(),
        MicrobeComponent(),
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
}


function Microbe:__init(entity)
    self.entity = entity
    for key, typeId in pairs(Microbe.COMPONENTS) do
        local component = entity:getComponent(typeId)
        assert(component ~= nil, "Can't create microbe from this entity, it's missing " .. key)
        self[key] = entity:getComponent(typeId)
    end
end


function Microbe:addOrganelle(q, r, organelle)
    local s = encodeAxial(q, r)
    if self.microbe.organelles[s] then
        assert(false)
        return false
    end
    self.microbe.organelles[s] = organelle
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
    self:updateAllHexColours()
    return true
end


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
    self:updateAllHexColours()
    return true
end


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
    if remainingAmount > 0.0 then
        -- TODO: Release agent back into environment?
        return
    end
end


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


function Microbe:update(milliseconds)
    -- Vacuoles
    for agentId, vacuoleList in pairs(self.microbe.vacuoles) do
        local amount = self.agentAbsorber:absorbedAgentAmount(agentId)
        if amount > 0.0 then
            self:storeAgent(agentId, amount)
        end
    end
    -- Other organelles
    for _, organelle in pairs(self.microbe.organelles) do
        organelle:update(self, milliseconds)
    end
end


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


function Microbe:updateAllHexColours()
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
            RigidBodyComponent 
        }, 
        true
    )
    self.microbes = {}
end


function MicrobeSystem:init(engine)
    self.entities:init()
end


function MicrobeSystem:shutdown()
    self.entities:shutdown()
end


function MicrobeSystem:update(milliseconds)
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
    end
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
    end
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        microbe:update(milliseconds)
    end
end

