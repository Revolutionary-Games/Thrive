--------------------------------------------------------------------------------
-- MicrobeComponent
--
-- Holds data common to all microbes. You probably shouldn't use this directly,
-- use the Microbe class (below) instead.
--------------------------------------------------------------------------------
class 'MicrobeComponent' (Component)

COMPOUND_PROCESS_DISTRIBUTION_INTERVAL = 100 -- quantity of physics time between each loop distributing compounds to organelles. TODO: Modify to reflect microbe size.

BANDWIDTH_PER_ORGANELLE = 0.5 -- amount the microbes maxmimum bandwidth increases with per organelle added. This is a temporary replacement for microbe surface area

BANDWIDTH_REFILL_DURATION = 1000 -- The amount of time it takes for the microbe to regenerate an amount of bandwidth equal to maxBandwidth

STORAGE_EJECTION_THRESHHOLD = 0.8

EXCESS_COMPOUND_COLLECTION_INTERVAL = 1500 -- The amount of time between each loop to maintaining a fill level below STORAGE_EJECTION_THRESHHOLD and eject useless compounds


MICROBE_HITPOINTS_PER_ORGANELLE = 10

function MicrobeComponent:__init(isPlayerMicrobe)
    Component.__init(self)
    self.hitpoints = 10
    self.maxHitpoints = 10
    self.dead = false
    self.deathTimer = 0
    self.organelles = {}
    self.storageOrganelles = {}
    self.processOrganelles = {}
    self.movementDirection = Vector3(0, 0, 0)
    self.facingTargetPoint = Vector3(0, 0, 0)
    self.capacity = 0
    self.stored = 0
    self.compounds = {}
    self.compoundPriorities = {}
    self:_resetCompoundPriorities()
    self.initialized = false
    self.isPlayerMicrobe = isPlayerMicrobe
    self.maxBandwidth = 0
    self.remainingBandwidth = 0
    self.compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL
end

function MicrobeComponent:_resetCompoundPriorities()
    for compound in CompoundRegistry.getCompoundList() do
        self.compoundPriorities[compound] = 0
    end
    self.compoundPriorities[CompoundRegistry.getCompoundId("atp")] = 10
end

function MicrobeComponent:_updateCompoundPriorities() 
    -- placeholder solution for compound priorities
    self:_resetCompoundPriorities()
    for _,procOrg in pairs(self.processOrganelles) do
        for _,process in ipairs(procOrg.processes) do
            -- Calculate the value of the process' output
            local processProductValue = 0
            for compound,amount in pairs(process.outputCompounds) do
                processProductValue = processProductValue + self.compoundPriorities[compound]*amount 
            end
            -- Calculate new priorities for compounds
            
            for compound,amount in pairs(process.inputCompounds) do
                -- Find the minimum compound concentration in the recipy that isn't this one
                local minOtherConcentrationFactor = 1.0
                for compound2,_ in pairs(process.inputCompounds) do
                    local compoundStored = self.compounds[compound2] 
                    if compoundStored == nil then
                        compoundStored = 0
                    end
                    local otherConcentration = compoundStored/self.capacity
                    if otherConcentration < minOtherConcentrationFactor and compound ~= compound2 then
                        minOtherConcentrationFactor = otherConcentration
                    end
                end
                
                minOtherConcentrationFactor = 1/(1.5+0.5^minOtherConcentrationFactor) - 0.3 -- Modified sigmoid function
                local compoundStored = self.compounds[compound] 
                if compoundStored == nil then
                    compoundStored = 0
                end
                local compoundConcentrationFactor = 1.01-(compoundStored / self.capacity)^0.4
                local newPriority = processProductValue * 
                                    amount/process.inputUnitSum * 
                                    process.costPriorityFactor * 
                                    (compoundConcentrationFactor+0.01)^4 * -- We quadruple the effect of compoundConcentrationFactor
                                    (minOtherConcentrationFactor) -- Reduce priority if not limiting input
                -- We are using max priority as the final priority. However sum and average are also valid options as no aggregation makes perfect sense
                if newPriority > self.compoundPriorities[compound] then
                   self.compoundPriorities[compound] = newPriority
                end
            end
        end
    end
end

-- Attempts to obtain an amount of bandwidth for immediate use
-- This should be in conjunction with most operations ejecting  or absorbing compounds and agents for microbe
--
-- @param maxAmount
-- The max amount of units that is requested
--
-- @param compoundId
-- The compound being requested for volume considerations
--
-- @return
--  amount in units avaliable for use
function MicrobeComponent:getBandwidth(maxAmount, compoundId)
    local compoundVolume = CompoundRegistry.getCompoundUnitVolume(compoundId)
    local amount = math.min(maxAmount * compoundVolume, self.remainingBandwidth)
    self.remainingBandwidth = self.remainingBandwidth - amount
    return amount / compoundVolume
end

function MicrobeComponent:regenerateBandwidth(milliseconds)
    local addedBandwidth = self.remainingBandwidth + milliseconds * (self.maxBandwidth / BANDWIDTH_REFILL_DURATION)
    self.remainingBandwidth = math.min(addedBandwidth, self.maxBandwidth)
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
    self.hitpoints = storage:get("hitpoints", 0)
    local storedCompound = storage:get("storedCompounds", {})
    for i = 1,storedCompound:size() do
        local compound = storedCompound:get(i)
        
        local amount = compound:get("amount", 0)
        self.compounds[compound:get("compoundId", 0)] = amount
        self.stored = self.stored + amount
    end
    local compoundPriorities = storage:get("compoundPriorities", {})
    for i = 1,compoundPriorities:size() do
        local compound = compoundPriorities:get(i)
        self.compoundPriorities[compound:get("compoundId", 0)] = compound:get("priority", 0)
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
    storage:set("hitpoints", self.hitpoints)
    local storedCompounds = StorageList()
    for compoundId, amount in pairs(self.compounds) do
        compound = StorageContainer()
        compound:set("compoundId", compoundId)
        compound:set("amount", amount)
        storedCompounds:append(compound)
    end
    storage:set("storedCompounds", storedCompounds)
    local compoundPriorities = StorageList()
    for compoundId, priority in pairs(self.compoundPriorities) do
        compound = StorageContainer()
        compound:set("compoundId", compoundId)
        compound:set("priority", priority)
        compoundPriorities:append(compound)
    end
    storage:set("compoundPriorities", compoundPriorities)
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
-- The entity's name. If nil, the entity will be unnamed.
--
-- @returns microbe
-- An object of type Microbe
function Microbe.createMicrobeEntity(name, aiControlled)
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
    local compoundEmitter = CompoundEmitterComponent() -- Emitter for excess compounds
    compoundEmitter.minInitialSpeed = 1
    compoundEmitter.maxInitialSpeed = 3
    compoundEmitter.particleLifetime = 5000
    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("microbe")
    local components = {
        CompoundAbsorberComponent(),
        OgreSceneNodeComponent(),
        MicrobeComponent(not aiControlled),
        reactionHandler,
        rigidBody,
        compoundEmitter
    }
    if aiControlled then
        local aiController = MicrobeAIControllerComponent()
        table.insert(components, aiController)
    end
    for _, component in ipairs(components) do
        entity:addComponent(component)
    end
    return Microbe(entity)
end

-- I don't feel like checking for each component separately, so let's make a
-- loop do it with an assert for good measure (see Microbe.__init)
Microbe.COMPONENTS = {
    compoundAbsorber = CompoundAbsorberComponent.TYPE_ID,
    microbe = MicrobeComponent.TYPE_ID,
    rigidBody = RigidBodyComponent.TYPE_ID,
    sceneNode = OgreSceneNodeComponent.TYPE_ID,
    compoundEmitter = CompoundEmitterComponent.TYPE_ID,
    collisionHandler = CollisionComponent.TYPE_ID
}


-- Constructor
--
-- Requires all necessary components (see Microbe.COMPONENTS) to be present in
-- the entity.
--
-- @param entity
-- The entity this microbe wraps
function Microbe:__init(entity)
    self.entity = entity
    self.gatheredDistributionTime = 0
    for key, typeId in pairs(Microbe.COMPONENTS) do
        local component = entity:getComponent(typeId)
        assert(component ~= nil, "Can't create microbe from this entity, it's missing " .. key)
        self[key] = entity:getComponent(typeId)
    end
    for compound in CompoundRegistry.getCompoundList() do
        self.compoundAbsorber:setCanAbsorbCompound(compound, true)
    end
    if not self.microbe.initialized then
        self:_initialize()
    end
    self:_updateCompoundAbsorber()
end


-- Adds a new organelle
--
-- The space at (q,r) must not be occupied by another organelle already.
--
-- @param q,r
-- Offset of the organelle's center relative to the microbe's center in
-- axial coordinates.
--
-- @param organelle
-- The organelle to add
--
-- @return
--  returns whether the organelle was added
function Microbe:addOrganelle(q, r, organelle)
    local s = encodeAxial(q, r)
    if self.microbe.organelles[s] then
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
    self.microbe.hitpoints = (self.microbe.hitpoints/self.microbe.maxHitpoints) * (self.microbe.maxHitpoints + MICROBE_HITPOINTS_PER_ORGANELLE)
    self.microbe.maxHitpoints = self.microbe.maxHitpoints + MICROBE_HITPOINTS_PER_ORGANELLE
    self.microbe.maxBandwidth = self.microbe.maxBandwidth + BANDWIDTH_PER_ORGANELLE -- Temporary solution for increasing max bandwidth
    self.microbe.remainingBandwidth = self.microbe.maxBandwidth
    return true
end


-- Adds a storage organelle
-- This will be called automatically by storage organelles added with addOrganelle(...)
--
-- @param organelle
--   An object of type StorageOrganelle
function Microbe:addStorageOrganelle(storageOrganelle)
    assert(storageOrganelle.capacity ~= nil)
    
    self.microbe.capacity = self.microbe.capacity + storageOrganelle.capacity
    table.insert(self.microbe.storageOrganelles, storageOrganelle)
    return #self.microbe.storageOrganelles
end

-- Removes a storage organelle
--
-- @param organelle
--   An object of type StorageOrganelle
function Microbe:removeStorageOrganelle(storageOrganelle)
    self.microbe.capacity = self.microbe.capacity - storageOrganelle.capacity
    table.remove(self.microbe.storageOrganelles, storageOrganelle.parentId)
end

-- Removes a process organelle
-- This will be called automatically by process organelles removed with with removeOrganelle(...)
--
-- @param processOrganelle
--   An object of type ProcessOrganelle
function Microbe:removeProcessOrganelle(processOrganelle)
    self.microbe.processOrganelles[processOrganelle] = nil
end

-- Adds a process organelle
-- This will be called automatically by process organelles added with addOrganelle(...)
--
-- @param processOrganelle
--   An object of type ProcessOrganelle
function Microbe:addProcessOrganelle(processOrganelle)
    self.microbe.processOrganelles[processOrganelle] = processOrganelle
end



-- Retrieves the organelle occupying a hex cell
--
-- @param q, r
-- Axial coordinates, relative to the microbe's center
--
-- @returns organelle
-- The organelle at (q,r) or nil if the hex is unoccupied
function Microbe:getOrganelleAt(q, r)
    local s = encodeAxial(q, r)
    local organelle = self.microbe.organelles[s]
    return organelle
end


-- Removes the organelle at a hex cell
-- Note that this renders the organelle unusable as we destroy its underlying entity
--
-- @param q, r
-- Axial coordinates of the organelle's center
--
-- @returns success
-- True if an organelle has been removed, false if there was no organelle
-- at (q,r)
function Microbe:removeOrganelle(q, r)
    local s = encodeAxial(q, r)
    local organelle = self.microbe.organelles[s]
    if not organelle then
        return false
    end
    self.microbe.organelles[s] = nil
    organelle.position.q = 0
    organelle.position.r = 0
    organelle:onRemovedFromMicrobe(self)
    organelle.entity:destroy()
    self.rigidBody.properties.shape:removeChildShape(
        organelle.collisionShape
    )
    self:_updateAllHexColours()
    self.microbe.hitpoints = (self.microbe.hitpoints/self.microbe.maxHitpoints) * (self.microbe.maxHitpoints - MICROBE_HITPOINTS_PER_ORGANELLE)
    self.microbe.maxHitpoints = self.microbe.maxHitpoints - MICROBE_HITPOINTS_PER_ORGANELLE
    self.microbe.maxBandwidth = self.microbe.maxBandwidth - BANDWIDTH_PER_ORGANELLE -- Temporary solution for decreasing max bandwidth
    self.microbe.remainingBandwidth = self.microbe.maxBandwidth
    return true
end


-- Queries the currently stored amount of an compound
--
-- @param compoundId
-- The id of the compound to query
--
-- @returns amount
-- The amount stored in the microbe's storage oraganelles
function Microbe:getCompoundAmount(compoundId)
    if self.microbe.compounds[compoundId] == nil then
        return 0
    else
        return self.microbe.compounds[compoundId]
    end
end


-- Damages the microbe, killing it if its hitpoints drop low enough
--
-- @param amount
--  amount of hitpoints to substract
function Microbe:damage(amount)
    assert(amount >= 0, "Can't deal negative damage. Use Microbe:heal instead")
    self.microbe.hitpoints = self.microbe.hitpoints - amount
    for _, organelle in pairs(self.microbe.organelles) do
        organelle:flashColour(300, ColourValue(1,0.2,0.2,1))
    end
    self:_updateAllHexColours()
    if self.microbe.hitpoints <= 0 then
        self.microbe.hitpoints = 0
        self:kill()
    end
end

-- Heals the microbe, restoring hitpoints, cannot exceede the microbes max hitpoints
--
-- @param amount
--  amount of hitpoints heal
function Microbe:heal(amount)
    assert(amount >= 0, "Can't heal for negative amount of hitpoints. Use Microbe:damage instead")
    self.microbe.hitpoints = (self.microbe.hitpoints + amount) % self.microbe.maxHitpoints
end


-- Stores an compound in the microbe's storage organelles
--
-- @param compoundId
-- The compound to store
--
-- @param amount
-- The amount to store
--
-- @param bandwidthLimited
-- Determines if the storage operation is to be limited by the bandwidth of the microbe
function Microbe:storeCompound(compoundId, amount, bandwidthLimited)
    local storedAmount = 0
    if bandwidthLimited then
        storedAmount = self.microbe:getBandwidth(amount, compoundId)
    else
        storedAmount = amount
    end
    if self.microbe.compounds[compoundId] == nil then
        self.microbe.compounds[compoundId] = 0
    end
    self.microbe.compounds[compoundId] = self.microbe.compounds[compoundId] + storedAmount
    self.microbe.stored = self.microbe.stored + storedAmount
    local remainingAmount = amount - storedAmount
    -- If there is excess compounds, we will eject them
    -- This is necessary as bandwidth doesnt prevent or reduce absorbtion of large compound particles
    if remainingAmount > 0 then
        local particleCount = 1
        if remainingAmount >= 3 then
            particleCount = 3
        end
        local i
        for i = 1, particleCount do
            self:ejectCompound(compoundId, remainingAmount/particleCount, true)
        end
    end
    self.microbe:_updateCompoundPriorities()
end


-- Removes an compound from the microbe's storage organelles
--
-- @param compoundId
-- The compound to remove
--
-- @param maxAmount
-- The maximum amount to take
--
-- @returns amount
-- The amount that was actually taken, between 0.0 and maxAmount.
function Microbe:takeCompound(compoundId, maxAmount)
    if self.microbe.compounds[compoundId] == nil then
        return 0
    else
        local takenAmount = math.min(maxAmount, self.microbe.compounds[compoundId])
        self.microbe.compounds[compoundId] = self.microbe.compounds[compoundId] - takenAmount    
        self.microbe.stored = self.microbe.stored - takenAmount
        return takenAmount
    end
    self.microbe:_updateCompoundPriorities()
end


-- Ejects compounds from the microbes behind position, into the enviroment
-- Note that the compounds ejected are created in this function and not taken from the microbe
--
-- @param compoundId
-- The compound type to create and eject
--
-- @param amount
-- The amount to eject
--
-- @param ejectBehind
-- If true eject behind microbe otherwise anywhere
function Microbe:ejectCompound(compoundId, amount, ejectBehind)
    local minAngle
    local maxAngle
    if ejectBehind then
        local yAxis = self.sceneNode.transform.orientation:yAxis()
        local angle = math.atan2(-yAxis.x, -yAxis.y)
        if (angle < 0) then
            angle = angle + 2*math.pi
        end
        angle = angle * 180/math.pi
        minAngle = angle - 30 -- over and underflow of angles are handled automatically
        maxAngle = angle + 30
        self.compoundEmitter.emissionRadius = 5
    else
        minAngle = 0
        maxAngle = 359
        self.compoundEmitter.emissionRadius = 1
    end
    self.compoundEmitter.minEmissionAngle = Degree(minAngle)
    self.compoundEmitter.maxEmissionAngle = Degree(maxAngle)
    self.compoundEmitter:emitCompound(compoundId, amount)
    self.microbe:_updateCompoundPriorities()
end



-- Kills the microbe, releasing stored compounds into the enviroment
function Microbe:kill()
    -- Eject the compounds that was in the microbe
    for compoundId,_ in pairs(self.microbe.compounds) do
        local amount = self.microbe.compounds[compoundId]
        while amount > 0 do
            
            ejectedAmount = self:takeCompound(compoundId, 3) -- Eject up to 3 units per particle
            self:ejectCompound(compoundId, ejectedAmount, false)
            amount = amount - ejectedAmount
        end
    end    
    local microbeSceneNode = self.entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local deathAnimationEntity = Entity()
    local lifeTimeComponent = TimedLifeComponent()
    lifeTimeComponent.timeToLive = 4000
    deathAnimationEntity:addComponent(lifeTimeComponent)
    local deathAnimSceneNode = OgreSceneNodeComponent()
    deathAnimSceneNode.meshName = "MeshMicrobeDeath.mesh"
    deathAnimSceneNode:playAnimation("Death", false)
    deathAnimSceneNode.transform.position = Vector3(microbeSceneNode.transform.position.x, microbeSceneNode.transform.position.y, 0)
    deathAnimSceneNode.transform:touch()
    deathAnimationEntity:addComponent(deathAnimSceneNode)
    
    if self.microbe.isPlayerMicrobe then
        self.microbe.dead = true
        self.microbe.deathTimer = 5000
        self.microbe.movementDirection = Vector3(0,0,0)
        self.rigidBody:clearForces()
        microbeSceneNode.visible = false
    else
        self:destroy()
    end
   
end

-- Updates the microbe's state
function Microbe:update(milliseconds)
    if not self.microbe.dead then
        -- StorageOrganelles
        self:_updateCompoundAbsorber()
        -- Regenerate bandwidth
        self.microbe:regenerateBandwidth(milliseconds)
        -- Attempt to absorb queued compounds
        for compound in CompoundRegistry.getCompoundList() do
            -- Check for compounds to store
            local amount = self.compoundAbsorber:absorbedCompoundAmount(compound)
            if amount > 0.0 then
                self:storeCompound(compound, amount, true)
            end
        end
        
        -- Distribute compounds to Process Organelles
        for _, processOrg in pairs(self.microbe.processOrganelles) do
            processOrg:update(self, milliseconds)
        end
        
        self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer + milliseconds
        while self.microbe.compoundCollectionTimer > EXCESS_COMPOUND_COLLECTION_INTERVAL do -- For every COMPOUND_DISTRIBUTION_INTERVAL passed
            -- Gather excess compounds that are the compounds that the storage organelles automatically emit to stay less than full
            local excessCompounds = {}
            while self.microbe.stored/self.microbe.capacity > STORAGE_EJECTION_THRESHHOLD+0.01 do
                -- Find lowest priority compound type contained in the microbe
                local lowestPriorityId = nil
                local lowestPriority = math.huge
                for compoundId,_ in pairs(self.microbe.compounds) do
                    assert(self.microbe.compoundPriorities[compoundId] ~= nil, "Compound priority table was missing compound")
                    if self.microbe.compounds[compoundId] > 0  and self.microbe.compoundPriorities[compoundId] < lowestPriority then
                        lowestPriority = self.microbe.compoundPriorities[compoundId]
                        lowestPriorityId = compoundId
                    end
                end
                assert(lowestPriorityId ~= nil, "The microbe didn't seem to contain any compounds but was over the threshold")
                assert(self.microbe.compounds[lowestPriorityId] ~= nil, "Microbe storage was over threshold but didn't have any valid compounds to expell")
                -- Return an amount that either is how much the microbe contains of the compound or until it goes to the threshhold
                local amountInExcess
                
                amountInExcess = math.min(self.microbe.compounds[lowestPriorityId],self.microbe.stored - self.microbe.capacity * STORAGE_EJECTION_THRESHHOLD)
                excessCompounds[lowestPriorityId] = self:takeCompound(lowestPriorityId, amountInExcess)
            end
            -- Expell compounds of priority 0 periodically
            for compoundId,_ in pairs(self.microbe.compounds) do
                if self.microbe.compoundPriorities[compoundId] == 0 and self.microbe.compounds[compoundId] > 1 then
                    local uselessCompoundAmount
                    
                    uselessCompoundAmount = self.microbe:getBandwidth(self.microbe.compounds[compoundId], compoundId)
                    if excessCompounds[compoundId] ~= nil then
                        excessCompounds[compoundId] = excessCompounds[compoundId] + self:takeCompound(compoundId, uselessCompoundAmount)
                    else
                        excessCompounds[compoundId] = self:takeCompound(compoundId, uselessCompoundAmount)
                    end
                end
            end 
            for compoundId, amount in pairs(excessCompounds) do
                if amount > 0 then
                    self:ejectCompound(compoundId, amount, true)
                end
            end
            self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer - EXCESS_COMPOUND_COLLECTION_INTERVAL
        end
        -- Other organelles
        for _, organelle in pairs(self.microbe.organelles) do
            organelle:update(self, milliseconds)
        end
    else
        self.microbe.deathTimer = self.microbe.deathTimer - milliseconds
        if self.microbe.deathTimer <= 0 then
            self.microbe.dead = false
            self.microbe.deathTimer = 0
            self.residuePhysicsTime = 0
            self.microbe.hitpoints = self.microbe.maxHitpoints
            
            self.rigidBody:setDynamicProperties(
                Vector3(0,0,0), -- Position
                Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), -- Orientation
                Vector3(0, 0, 0), -- Linear velocity
                Vector3(0, 0, 0)  -- Angular velocity
            )
            local sceneNode = self.entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
            sceneNode.visible = true
            self:storeCompound(CompoundRegistry.getCompoundId("atp"), 20, false)
        end
    end
    self.compoundAbsorber:setAbsorbtionCapacity(self.microbe.remainingBandwidth)
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


-- Private function for updating the compound absorber
--
-- Toggles the absorber on and off depending on the remaining storage
-- capacity of the storage organelles.
function Microbe:_updateCompoundAbsorber()
    if self.microbe.stored >= self.microbe.capacity or 
               self.microbe.remainingBandwidth < 1 or 
               self.microbe.dead then
        self.compoundAbsorber:disable()
    else
        self.compoundAbsorber:enable()
    end
    
end


-- Private function for updating the colours of the organelles
--
-- The simple coloured hexes are a placeholder for proper models.
function Microbe:_updateAllHexColours()
    for s, organelle in pairs(self.microbe.organelles) do
        organelle:updateHexColours()
    end
end



-- Must exists for current spawningSystem to function
function Microbe:exists()
    return self.entity:exists()
end

-- Must exists for current spawningSystem to function, also used by microbe:kill
function Microbe:destroy()
    self.entity:destroy()
end

-- The last two functions are only present since the spawn system expects an entity interface.

function Microbe:addComponent(component)
    self.entity:addComponent(component)
end

function Microbe:getComponent(typeid)
    return self.entity:getComponent(typeid)
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
            CompoundAbsorberComponent,
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


