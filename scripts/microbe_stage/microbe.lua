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
EXCESS_COMPOUND_COLLECTION_INTERVAL = 1000 -- The amount of time between each loop to maintaining a fill level below STORAGE_EJECTION_THRESHHOLD and eject useless compounds
MICROBE_HITPOINTS_PER_ORGANELLE = 10
MINIMUM_AGENT_EMISSION_AMOUNT = 1
REPRODUCTASE_TO_SPLIT = 5
RELATIVE_VELOCITY_TO_BUMP_SOUND = 6
INITIAL_EMISSION_RADIUS = 2

function MicrobeComponent:__init(isPlayerMicrobe, speciesName)
    Component.__init(self)
    self.speciesName = speciesName
    self.hitpoints = 10
    self.maxHitpoints = 10
    self.dead = false
    self.deathTimer = 0
    self.organelles = {}
    self.processOrganelles = {} -- Organelles responsible for producing compounds from other compounds
    self.specialStorageOrganelles = {} -- Organelles with complete resonsiblity for a specific compound (such as agentvacuoles)
    self.movementDirection = Vector3(0, 0, 0)
    self.facingTargetPoint = Vector3(0, 0, 0)
    self.capacity = 0  -- The amount that can be stored in the microbe. NOTE: This does not include special storage organelles
    self.stored = 0 -- The amount stored in the microbe. NOTE: This does not include special storage organelles
    self.compounds = {}
    self.compoundPriorities = {}
    self.defaultCompoundPriorities = {}
    self.defaultCompoundPriorities[CompoundRegistry.getCompoundId("atp")] = 10
    self.defaultCompoundPriorities[CompoundRegistry.getCompoundId("reproductase")] = 8
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
    for k, v in pairs(self.defaultCompoundPriorities) do
        self.compoundPriorities[k] = v
    end
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

function MicrobeComponent:regenerateBandwidth(logicTime)
    local addedBandwidth = self.remainingBandwidth + logicTime * (self.maxBandwidth / BANDWIDTH_REFILL_DURATION)
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
    self.speciesName = storage:get("speciesName", "Default")
    self.maxHitpoints = storage:get("maxHitpoints", 0)
    self.maxBandwidth = storage:get("maxBandwidth", 0)
    self.remainingBandwidth = storage:get("remainingBandwidth", 0)
    self.isPlayerMicrobe = storage:get("isPlayerMicrobe", false)
    self.speciesName = storage:get("speciesName", "")
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
    storage:set("speciesName", self.speciesName)
    storage:set("maxHitpoints", self.maxHitpoints)
    storage:set("remainingBandwidth", self.remainingBandwidth)
    storage:set("maxBandwidth", self.maxBandwidth)
    storage:set("isPlayerMicrobe", self.isPlayerMicrobe)
    storage:set("speciesName", self.speciesName)
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
function Microbe.createMicrobeEntity(name, aiControlled, speciesName)
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
    compoundEmitter.minInitialSpeed = 3
    compoundEmitter.maxInitialSpeed = 4
    compoundEmitter.particleLifetime = 5000
    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("microbe")
    local soundComponent = SoundSourceComponent()
    local s1 = nil
    soundComponent:addSound("microbe-release-toxin", "soundeffects/microbe-release-toxin.ogg")
    soundComponent:addSound("microbe-toxin-damage", "soundeffects/microbe-toxin-damage.ogg")
    soundComponent:addSound("microbe-death", "soundeffects/microbe-death.ogg")
    soundComponent:addSound("microbe-collision", "soundeffects/microbe-collision.ogg")
    soundComponent:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    s1 = soundComponent:addSound("microbe-movement-1", "soundeffects/microbe-movement-1.ogg")
    s1.properties.volume = 1
    s1.properties:touch()
    s1 = soundComponent:addSound("microbe-movement-turn", "soundeffects/microbe-movement-2.ogg")
    s1.properties.volume = 0.2
    s1.properties:touch()
    s1 = soundComponent:addSound("microbe-movement-2", "soundeffects/microbe-movement-3.ogg")
    s1.properties.volume = 1
    s1.properties:touch()
    local components = {
        CompoundAbsorberComponent(),
        OgreSceneNodeComponent(),
        MicrobeComponent(not aiControlled, speciesName),
        reactionHandler,
        rigidBody,
        compoundEmitter,
        soundComponent
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
    collisionHandler = CollisionComponent.TYPE_ID,
    soundSource = SoundSourceComponent.TYPE_ID,
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
    self.playerAlreadyShownAtpDamage = false
    self.playerAlreadyShownVictory = false
end

-- Getter for microbe species
-- 
-- returns the species component or nil if it doesn't have a valid species
function Microbe:getSpeciesComponent()
    return Entity(self.microbe.speciesName):getComponent(SpeciesComponent.TYPE_ID)
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
    local localQ = q - organelle.position.q
    local localR = r - organelle.position.r
    if organelle:getHex(localQ, localR) ~= nil then
        return organelle
    end
       
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

end

-- Removes a storage organelle
--
-- @param organelle
--   An object of type StorageOrganelle
function Microbe:removeStorageOrganelle(storageOrganelle)
    self.microbe.capacity = self.microbe.capacity - storageOrganelle.capacity
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

-- Removes a special storage organelle
-- This will be called automatically by process organelles removed with with removeOrganelle(...)
--
-- @param organelle
--   An object of type ProcessOrganelle
function Microbe:removeSpecialStorageOrganelle(organelle, compoundId)
    self.microbe.specialStorageOrganelles[compoundId] = nil
end

-- Adds a special storage organelle that holds complete responsibility for some compound
-- This will be called automatically by process organelles added with addOrganelle(...)
--
-- @param processOrganelle
--   An object of type ProcessOrganelle
function Microbe:addSpecialStorageOrganelle(organelle, compoundId)
    self.microbe.specialStorageOrganelles[compoundId] = organelle
end

-- Retrieves the organelle occupying a hex cell
--
-- @param q, r
-- Axial coordinates, relative to the microbe's center
--
-- @returns organelle
-- The organelle at (q,r) or nil if the hex is unoccupied
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
    organelle:destroy()
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
    if self.microbe.specialStorageOrganelles[compoundId] == nil then
        if self.microbe.compounds[compoundId] == nil then
            return 0
        else
            return self.microbe.compounds[compoundId]
        end
    else
        return self.microbe.specialStorageOrganelles[compoundId].storedAmount
    end
end

-- Sets the default compound priorities
--
-- @param compoundId
-- @param priority
function Microbe:setDefaultCompoundPriority(compoundId, priority)
    self.microbe.defaultCompoundPriorities[compoundId] = priority
end

-- Damages the microbe, killing it if its hitpoints drop low enough
--
-- @param amount
--  amount of hitpoints to substract
function Microbe:damage(amount, damageType)
    assert(amount >= 0, "Can't deal negative damage. Use Microbe:heal instead")
    if damageType ~= nil and damageType == "toxin" then
        self.soundSource:playSound("microbe-toxin-damage")
    end
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

-- Heals the microbe, restoring hitpoints, cannot exceed the microbes max hitpoints
--
-- @param amount
--  amount of hitpoints heal
function Microbe:heal(amount)
    assert(amount >= 0, "Can't heal for negative amount of hitpoints. Use Microbe:damage instead")
    self.microbe.hitpoints = (self.microbe.hitpoints + amount)
    if self.microbe.hitpoints > self.microbe.maxHitpoints then
        self.microbe.hitpoints = self.microbe.maxHitpoints
    end
end

-- Drains an agent from the microbes special storage and emits it
--
-- @param compoundId
-- The compound id of the agent to emit
--
-- @param maxAmount
-- The maximum amount to try to emit
function Microbe:emitAgent(compoundId, maxAmount)
    local agentVacuole = self.microbe.specialStorageOrganelles[compoundId]
    if agentVacuole ~= nil and agentVacuole.storedAmount > MINIMUM_AGENT_EMISSION_AMOUNT then
        self.soundSource:playSound("microbe-release-toxin")
        -- Calculate the emission angle of the agent emitter
        local organelleX, organelleY = axialToCartesian(agentVacuole.position.q, agentVacuole.position.r)
        local nucleusX, nucleusY = axialToCartesian(0, 0)
        local deltaX = nucleusX - organelleX
        local deltaY = nucleusY - organelleY
        local angle =  math.atan2(-deltaY, -deltaX)
        if (angle < 0) then
            angle = angle + 2*math.pi
        end
        angle = -(angle * 180/math.pi -90 ) % 360
        local amountToEject = math.min(maxAmount, agentVacuole.storedAmount)
        local particleCount = 1
        if amountToEject >= 3 then
            particleCount = 3
        end
        agentVacuole:takeCompound(compoundId, amountToEject)
        local i
        for i = 1, particleCount do
            self:ejectCompound(compoundId, amountToEject/particleCount, angle,angle)
        end
    end
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
    storedAmount = math.min(storedAmount , self.microbe.capacity - self.microbe.stored)
    if self.microbe.specialStorageOrganelles[compoundId] == nil then
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
                self:ejectCompound(compoundId, remainingAmount/particleCount, 160, 200)
            end
        end
    else
        self.microbe.specialStorageOrganelles[compoundId]:storeCompound(compoundId, storedAmount)
    end
    self.microbe:_updateCompoundPriorities()
end


-- Removes compounds from the microbe's storage organelles
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
    if self.microbe.specialStorageOrganelles[compoundId] == nil then
        if self.microbe.compounds[compoundId] == nil then
            return 0
        else
            local takenAmount = math.min(maxAmount, self.microbe.compounds[compoundId])
            self.microbe.compounds[compoundId] = self.microbe.compounds[compoundId] - takenAmount    
            self.microbe.stored = self.microbe.stored - takenAmount
            return takenAmount
        end
    else
        return self.microbe.specialStorageOrganelles:takeCompound(compoundId, maxAmount)
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
-- @param minAngle
-- Relative angle to the microbe. 0 = microbes front. Should be between 0 and 359 and lower or equal than maxAngle
--
-- @param maxAngle
-- Relative angle to the microbe. 0 = microbes front. Should be between 0 and 359 and higher or equal than minAngle
function Microbe:ejectCompound(compoundId, amount, minAngle, maxAngle)
    local chosenAngle = rng:getReal(minAngle, maxAngle)
    -- Find the direction the microbe is facing
    local yAxis = self.sceneNode.transform.orientation:yAxis()
    local microbeAngle = math.atan2(yAxis.x, yAxis.y)
    if (microbeAngle < 0) then
        microbeAngle = microbeAngle + 2*math.pi
    end
    microbeAngle = microbeAngle * 180/math.pi
    -- Take the mirobe angle into account so we get world relative degrees
    local finalAngle = (chosenAngle + microbeAngle) % 360
    -- Find how far away we should spawn the particle so it doesn't collide with microbe.
    local radius = INITIAL_EMISSION_RADIUS
    self.compoundEmitter:emitCompound(compoundId, amount, finalAngle, radius)
    self.microbe:_updateCompoundPriorities()
end




-- Kills the microbe, releasing stored compounds into the enviroment
function Microbe:kill()
    -- Eject the compounds that was in the microbe
    for compoundId,amount in pairs(self.microbe.compounds) do
        local _amount = amount
        while _amount > 0 do
            ejectedAmount = self:takeCompound(compoundId, 2.5) -- Eject up to 3 units per particle
            self:ejectCompound(compoundId, ejectedAmount, 0, 359)
            _amount = _amount - ejectedAmount
        end
    end    
    for compoundId, specialStorageOrg in pairs(self.microbe.specialStorageOrganelles) do
        local _amount = specialStorageOrg.storedAmount
        while _amount > 0 do
            ejectedAmount = specialStorageOrg:takeCompound(compoundId, 3) -- Eject up to 3 units per particle
            self:ejectCompound(compoundId, ejectedAmount, 0, 359)
            _amount = _amount - ejectedAmount
        end
    end    
    local microbeSceneNode = self.entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
    local deathAnimationEntity = Entity()
    local lifeTimeComponent = TimedLifeComponent()
    lifeTimeComponent.timeToLive = 4000
    deathAnimationEntity:addComponent(lifeTimeComponent)
    local deathAnimSceneNode = OgreSceneNodeComponent()
    deathAnimSceneNode.meshName = "MicrobeDeath.mesh"
    deathAnimSceneNode:playAnimation("Death", false)
    deathAnimSceneNode.transform.position = Vector3(microbeSceneNode.transform.position.x, microbeSceneNode.transform.position.y, 0)
    deathAnimSceneNode.transform:touch()
    deathAnimationEntity:addComponent(deathAnimSceneNode)
    self.soundSource:playSound("microbe-death")
    self.microbe.dead = true
    self.microbe.deathTimer = 5000
    self.microbe.movementDirection = Vector3(0,0,0)
    self.rigidBody:clearForces()
    microbeSceneNode.visible = false
    --[[ since other microbes can kll each other now, this is deprecated
    if self.microbe.isPlayerMicrobe  ~= true then
        if not self.playerAlreadyShownVictory then
            self.playerAlreadyShownVictory = true
            showMessage("VICTORY!!!")
        end
    end
    --]]
end

-- Copies this microbe. The new microbe will not have the stored compounds of this one.
function Microbe:reproduce()
    copy = Microbe.createMicrobeEntity(nil, true)
    self:getSpeciesComponent():template(copy)
    copy.rigidBody.dynamicProperties.position = Vector3(self.rigidBody.dynamicProperties.position.x, self.rigidBody.dynamicProperties.position.y, 0)
    copy:storeCompound(CompoundRegistry.getCompoundId("atp"), 20, false)
    copy.microbe:_resetCompoundPriorities()  
    copy.entity:addComponent(SpawnedComponent())
    if self.microbe.isPlayerMicrobe then
        showReproductionDialog()
    end
end

-- Updates the microbe's state
function Microbe:update(logicTime)
    if not self.microbe.dead then
        -- StorageOrganelles
        self:_updateCompoundAbsorber()
        -- Regenerate bandwidth
        self.microbe:regenerateBandwidth(logicTime)
        -- Attempt to absorb queued compounds
        for compound in self.compoundAbsorber:getAbsorbedCompounds() do 
            local amount = self.compoundAbsorber:absorbedCompoundAmount(compound)
            if amount > 0.0 then
                self:storeCompound(compound, amount, true)
            end
        end
        -- Distribute compounds to Process Organelles
        for _, processOrg in pairs(self.microbe.processOrganelles) do
            processOrg:update(self, logicTime)
        end
        
        self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer + logicTime
        while self.microbe.compoundCollectionTimer > EXCESS_COMPOUND_COLLECTION_INTERVAL do
            -- For every COMPOUND_DISTRIBUTION_INTERVAL passed

            self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer - EXCESS_COMPOUND_COLLECTION_INTERVAL

            self:purgeCompounds()

            self:atpDamage()

            self:attemptReproduce()
        end

        -- Other organelles
        for _, organelle in pairs(self.microbe.organelles) do
            organelle:update(self, logicTime)
        end

        self.compoundAbsorber:setAbsorbtionCapacity(self.microbe.remainingBandwidth)
    else
        self.microbe.deathTimer = self.microbe.deathTimer - logicTime
        if self.microbe.deathTimer <= 0 then
            if self.microbe.isPlayerMicrobe  == true then
                self:respawn()
            else
                self:destroy()
            end
        end
    end
end

function Microbe:purgeCompounds()
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

    -- Expel compounds of priority 0 periodically
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
            self:ejectCompound(compoundId, amount, 160, 200, true)
        end
    end
end

function Microbe:atpDamage()
    -- Damage microbe if its too low on ATP
    if self.microbe.compounds[CompoundRegistry.getCompoundId("atp")] ~= nil and self.microbe.compounds[CompoundRegistry.getCompoundId("atp")] < 1.0 then
        if self.microbe.isPlayerMicrobe and not self.playerAlreadyShownAtpDamage then
            self.playerAlreadyShownAtpDamage = true
            showMessage("No ATP hurts you!")
        end
        self:damage(EXCESS_COMPOUND_COLLECTION_INTERVAL * 0.00002  * self.microbe.maxHitpoints) -- Microbe takes 2% of max hp per second in damage
    end
end

function Microbe:attemptReproduce()
    -- Split microbe if it has enough reproductase
    if self.microbe.compounds[CompoundRegistry.getCompoundId("reproductase")] ~= nil and self.microbe.compounds[CompoundRegistry.getCompoundId("reproductase")] > REPRODUCTASE_TO_SPLIT then
        self:takeCompound(CompoundRegistry.getCompoundId("reproductase"), 5)
        self:reproduce()
    end
end

function Microbe:respawn()
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
    self.microbeCollisions = CollisionFilter(
        "microbe",
        "microbe"
    );
    self.microbes = {}
end


function MicrobeSystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)
    self.microbeCollisions:init(gameState)
end


function MicrobeSystem:shutdown()
    self.entities:shutdown()
    self.microbeCollisions:shutdown()
end


function MicrobeSystem:update(renderTime, logicTime)
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
    end
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        microbe:update(logicTime)
    end
    -- Note that this triggers every frame there is a collision, but the sound system ensures that the sound doesn't overlap itself. Could potentially be optimised
    for collision in self.microbeCollisions:collisions() do
        local entity1 = Entity(collision.entityId1)
        local entity2 = Entity(collision.entityId2)
        if entity1:exists() and entity2:exists() then
            microbe.rigidBody.dynamicProperties.linearVelocity:length()
            local body1 = entity1:getComponent(RigidBodyComponent.TYPE_ID)
            local body2 = entity2:getComponent(RigidBodyComponent.TYPE_ID)
            if body1~=nil and body2~=nil then
                if ((body1.dynamicProperties.linearVelocity - body2.dynamicProperties.linearVelocity):length()) > RELATIVE_VELOCITY_TO_BUMP_SOUND then
                    local soundComponent = entity1:getComponent(SoundSourceComponent.TYPE_ID)
                    soundComponent:playSound("microbe-collision")
                end
            end
        end
    end
    self.microbeCollisions:clearCollisions()
end
