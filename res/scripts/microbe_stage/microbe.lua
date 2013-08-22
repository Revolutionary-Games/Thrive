class 'Microbe'

function Microbe:__init(name)
    self._organelles = {}
    self._vacuoles = {}
    self.movementDirection = Vector3(0, 0, 0)
    self.facingTargetPoint = Vector3(0, 0, 0)
    if name then
        self.entity = Entity(name)
    else
        self.entity = Entity()
    end
    -- Rigid body
    self.rigidBody = RigidBodyComponent()
    self.rigidBody.properties.shape = btCompoundShape()
    self.rigidBody.properties.linearDamping = 0.5
    self.rigidBody.properties.friction = 0.2
    self.rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    self.rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    self.rigidBody.properties:touch()
    self.entity:addComponent(self.rigidBody)
    -- Scene node
    self.sceneNode = OgreSceneNodeComponent()
    self.entity:addComponent(self.sceneNode)
    -- OnUpdate
    self.onUpdate = OnUpdateComponent()
    self.onUpdate.callback = function(entityId, milliseconds)
        self:update(milliseconds)
    end
    self.entity:addComponent(self.onUpdate)
    -- Absorber
    self.agentAbsorber = AgentAbsorberComponent()
    self.entity:addComponent(self.agentAbsorber)
end


function Microbe:addOrganelle(q, r, organelle)
    local s = encodeAxial(q, r)
    if self._organelles[s] then
        assert(false)
        return false
    end
    self._organelles[s] = organelle
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    -- Collision shape
    self.rigidBody.properties.shape:addChildShape(
        Quaternion(Radian(0), Vector3(1,0,0)),
        translation,
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
    if not self._vacuoles[agentId] then
        self._vacuoles[agentId] = {}
    end
    local vacuoleList = self._vacuoles[agentId]
    table.insert(vacuoleList, vacuole)
    self:_updateAgentAbsorber(vacuole.agentId)
end


function Microbe:getAgentAmount(agentId)
    local vacuoleList = self._vacuoles[agentId]
    local totalAmount = 0.0
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            totalAmount = totalAmount + vacuole.amount
        end
    end
    return totalAmount
end


function Microbe:getOrganelleAt(q, r)
    for _, organelle in pairs(self._organelles) do
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
    local organelle = table.remove(self._organelles, index)
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
    local vacuoleList = self._vacuoles[vacuole.agentId]
    local indexToRemove = 0
    for i, v in ipairs(self._vacuoles) do
        if v == vacuole then
            indexToRemove = i
            break
        end
    end
    assert(indexToRemove > 0, "Vacuole not found")
    table.remove(self._vacuoles, indexToRemove)
    self:_updateAgentAbsorber(vacuole.agentId)
end


function Microbe:storeAgent(agentId, amount)
    local vacuoleList = self._vacuoles[agentId]
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
    local vacuoleList = self._vacuoles[agentId]
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
    for agentId, vacuoleList in pairs(self._vacuoles) do
        local amount = self.agentAbsorber:absorbedAgentAmount(agentId)
        if amount > 0.0 then
            self:storeAgent(agentId, amount)
        end
    end
    -- Other organelles
    for _, organelle in pairs(self._organelles) do
        organelle:update(self, milliseconds)
    end
end


function Microbe:_updateAgentAbsorber(agentId)
    local vacuoleList = self._vacuoles[agentId]
    local canAbsorb = false
    if vacuoleList then
        for _, vacuole in ipairs(vacuoleList) do
            canAbsorb = canAbsorb or vacuole.amount < vacuole.capacity
        end
    end
    self.agentAbsorber:setCanAbsorbAgent(agentId, canAbsorb)
end


function Microbe:updateAllHexColours()
    for s, organelle in pairs(self._organelles) do
        organelle:updateHexColours()
    end
end
