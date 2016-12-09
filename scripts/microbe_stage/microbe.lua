--------------------------------------------------------------------------------
-- MicrobeComponent
--
-- Holds data common to all microbes. You probably shouldn't use this directly,
-- use the Microbe class (below) instead.
--------------------------------------------------------------------------------
class 'MicrobeComponent' (Component)

COMPOUND_PROCESS_DISTRIBUTION_INTERVAL = 100 -- quantity of physics time between each loop distributing compounds to organelles. TODO: Modify to reflect microbe size.
BANDWIDTH_PER_ORGANELLE = 1.0 -- amount the microbes maxmimum bandwidth increases with per organelle added. This is a temporary replacement for microbe surface area
BANDWIDTH_REFILL_DURATION = 800 -- The of time it takes for the microbe to regenerate an amount of bandwidth equal to maxBandwidth
STORAGE_EJECTION_THRESHHOLD = 0.8
EXCESS_COMPOUND_COLLECTION_INTERVAL = 1000 -- The amount of time between each loop to maintaining a fill level below STORAGE_EJECTION_THRESHHOLD and eject useless compounds
MICROBE_HITPOINTS_PER_ORGANELLE = 10
MINIMUM_AGENT_EMISSION_AMOUNT = .1
REPRODUCTASE_TO_SPLIT = 5
RELATIVE_VELOCITY_TO_BUMP_SOUND = 6
INITIAL_EMISSION_RADIUS = 0.5
ENGULFING_MOVEMENT_DIVISION = 3
ENGULFED_MOVEMENT_DIVISION = 4
ENGULFING_ATP_COST_SECOND = 1.5
ENGULF_HP_RATIO_REQ = 1.5 

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
    self.movementFactor = 1.0 -- Multiplied on the movement speed of the microbe.
    self.capacity = 0  -- The amount that can be stored in the microbe. NOTE: This does not include special storage organelles
    self.stored = 0 -- The amount stored in the microbe. NOTE: This does not include special storage organelles
    self.compounds = {}
    self.initialized = false
    self.isPlayerMicrobe = isPlayerMicrobe
    self.maxBandwidth = 10.0*BANDWIDTH_PER_ORGANELLE
    self.remainingBandwidth = 0
    self.compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL
    self.isCurrentlyEngulfing = false
    self.isBeingEngulfed = false
    self.wasBeingEngulfed = false
    self.hostileEngulfer = nil
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
    -- local compoundPriorities = storage:get("compoundPriorities", {})
    -- for i = 1,compoundPriorities:size() do
    --     local compound = compoundPriorities:get(i)
    --     self.compoundPriorities[compound:get("compoundId", 0)] = compound:get("priority", 0)
    -- end
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
    for compoundId in CompoundRegistry.getCompoundList() do
        --[[
        if self:getCompoundAmount(compoundId) > 0 then
            compound = StorageContainer()
            compound:set("compoundId", compoundId)
            compound:set("amount", amount)
            storedCompounds:append(compound)
        end
        --]]
    end
    storage:set("storedCompounds", storedCompounds)
    -- local compoundPriorities = StorageList()
    -- for compoundId, priority in pairs(self.compoundPriorities) do
    --     compound = StorageContainer()
    --     compound:set("compoundId", compoundId)
    --     compound:set("priority", priority)
    --     compoundPriorities:append(compound)
    -- end
    -- storage:set("compoundPriorities", compoundPriorities)
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

function Microbe.createMicrobeEntity(name, aiControlled, speciesName, in_editor)
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
    rigidBody.properties.mass = 0.0
    rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    rigidBody.properties:touch()
    local compoundEmitter = CompoundEmitterComponent() -- Emitter for excess compounds
    compoundEmitter.minInitialSpeed = 3
    compoundEmitter.maxInitialSpeed = 4
    compoundEmitter.particleLifetime = 5000
    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("microbe")
    local membraneComponent = MembraneComponent()
    
    local soundComponent = SoundSourceComponent()
    local s1 = nil
    soundComponent:addSound("microbe-release-toxin", "soundeffects/microbe-release-toxin.ogg")
    soundComponent:addSound("microbe-toxin-damage", "soundeffects/microbe-toxin-damage.ogg")
    soundComponent:addSound("microbe-death", "soundeffects/microbe-death.ogg")
    soundComponent:addSound("microbe-pickup-organelle", "soundeffects/microbe-pickup-organelle.ogg")
    soundComponent:addSound("microbe-engulfment", "soundeffects/engulfment.ogg")
    soundComponent:addSound("microbe-reproduction", "soundeffects/reproduction.ogg")
    
    s1 = soundComponent:addSound("microbe-movement-1", "soundeffects/microbe-movement-1.ogg")
    s1.properties.volume = 0.4
    s1.properties:touch()
    s1 = soundComponent:addSound("microbe-movement-turn", "soundeffects/microbe-movement-2.ogg")
    s1.properties.volume = 0
    s1.properties:touch()
    s1 = soundComponent:addSound("microbe-movement-2", "soundeffects/microbe-movement-3.ogg")
    s1.properties.volume = 0.4
    s1.properties:touch()

    local components = {
        CompoundAbsorberComponent(),
        OgreSceneNodeComponent(),
        CompoundBagComponent(),
        MicrobeComponent(not aiControlled, speciesName),
        reactionHandler,
        rigidBody,
        compoundEmitter,
        soundComponent,
        membraneComponent
    }
    if aiControlled then
        local aiController = MicrobeAIControllerComponent()
        table.insert(components, aiController)
    end
    for _, component in ipairs(components) do
        entity:addComponent(component)
    end
    return Microbe(entity, in_editor)
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
    membraneComponent = MembraneComponent.TYPE_ID,
    compoundBag = CompoundBagComponent.TYPE_ID,
}


-- Constructor
--
-- Requires all necessary components (see Microbe.COMPONENTS) to be present in
-- the entity.
--
-- @param entity
-- The entity this microbe wraps
function Microbe:__init(entity, in_editor)
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
        if in_editor == nil then
            self.compoundBag:setProcessor(Entity(self.microbe.speciesName):getComponent(ProcessorComponent.TYPE_ID), self.microbe.speciesName)
            SpeciesSystem.template(self, self:getSpeciesComponent())
        end
    end
    self:_updateCompoundAbsorber()
    self.playerAlreadyShownAtpDamage = false
    self.membraneHealth = 1.0
    self.reproductionStage = 0 -- 1 for G1 complete, 2 for S complete, 3 for G2 complete, and 4 for reproduction finished.
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
function Microbe:addOrganelle(q, r, rotation, organelle)
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
    self.rigidBody.properties.mass = self.rigidBody.properties.mass + organelle.mass
    self.rigidBody.properties:touch()
    
    organelle.sceneNode.parent = self.entity
    self.entity:addChild(organelle.organelleEntity)
    organelle:onAddedToMicrobe(self, q, r, rotation)
    
    self.microbe.hitpoints = (self.microbe.hitpoints/self.microbe.maxHitpoints) * (self.microbe.maxHitpoints + MICROBE_HITPOINTS_PER_ORGANELLE)
    self.microbe.maxHitpoints = self.microbe.maxHitpoints + MICROBE_HITPOINTS_PER_ORGANELLE
    self.microbe.maxBandwidth = self.microbe.maxBandwidth + BANDWIDTH_PER_ORGANELLE -- Temporary solution for increasing max bandwidth
    self.microbe.remainingBandwidth = self.microbe.maxBandwidth
    
    -- Send the organelles to the membraneComponent so that the membrane can "grow"
    local localQ = q - organelle.position.q
    local localR = r - organelle.position.r
    if organelle:getHex(localQ, localR) ~= nil then
        for _, hex in pairs(organelle._hexes) do
            local q = hex.q + organelle.position.q
            local r = hex.r + organelle.position.r
            local x, y = axialToCartesian(q, r)
            self.membraneComponent:sendOrganelles(x, y)
        end
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
    local organelle = self:getOrganelleAt(q,r)
    if not organelle then
        return false
    end
    local s = encodeAxial(organelle.position.q, organelle.position.r)
    self.microbe.organelles[s] = nil
    self.rigidBody.properties.mass = self.rigidBody.properties.mass - organelle.mass
    self.rigidBody.properties:touch()
    organelle.position.q = 0
    organelle.position.r = 0
    organelle:onRemovedFromMicrobe(self)
    organelle:destroy()
    self.rigidBody.properties.shape:removeChildShape(
        organelle.collisionShape
    )
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
    return self.entity:getComponent(CompoundBagComponent.TYPE_ID):getCompoundAmount(compoundId)
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
    
    -- Choose a random organelle or membrane to damage.
    -- TODO: CHANGE TO USE AGENT CODES FOR DAMAGE.
    local rand = math.random(1,self.microbe.maxHitpoints/MICROBE_HITPOINTS_PER_ORGANELLE)
    local i = 1
    for _, organelle in pairs(self.microbe.organelles) do
        -- If this is the organelle we have chosen...
        if i == rand then
            -- Deplete its health/compoundBin.
            organelle:damage(amount)
        end
        i = i + 1
    end
    if i == rand then
        -- Damage the membrane.
        self.membraneHealth = self.membraneHealth - amount
        self:flashMembraneColour(3000, ColourValue(1,0.2,0.2,1))
    end
    
    -- Find out the amount of health the microbe has.
    self:calculateHealthFromOrganelles()
    
    if self.microbe.hitpoints <= 0 then
        self.microbe.hitpoints = 0
        self:kill()
    end
    --print ("end damage")
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
    if agentVacuole ~= nil and self:getCompoundAmount(compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT then
        self.soundSource:playSound("microbe-release-toxin")
        -- Calculate the emission angle of the agent emitter
        local organelleX, organelleY = axialToCartesian(agentVacuole.position.q, agentVacuole.position.r)
        local membraneCoords = self.membraneComponent:getExternOrganellePos(organelleX, organelleY)
        
        local angle =  math.atan2(organelleY, organelleX)
        if (angle < 0) then
            angle = angle + 2*math.pi
        end
        angle = -(angle * 180/math.pi -90 ) % 360
        --angle = angle * 180/math.pi
        
        -- Find the direction the microbe is facing
        local yAxis = self.sceneNode.transform.orientation:yAxis()
        local microbeAngle = math.atan2(yAxis.x, yAxis.y)
        if (microbeAngle < 0) then
            microbeAngle = microbeAngle + 2*math.pi
        end
        microbeAngle = microbeAngle * 180/math.pi
        -- Take the microbe angle into account so we get world relative degrees
        local finalAngle = (angle + microbeAngle) % 360        
        
        local s = math.sin(finalAngle/180*math.pi);
        local c = math.cos(finalAngle/180*math.pi);

        local xnew = -membraneCoords[1] * c + membraneCoords[2] * s;
        local ynew = membraneCoords[1] * s + membraneCoords[2] * c;
        
        local direction = Vector3(xnew, ynew, 0)
        direction:normalise()
        local amountToEject = self:takeCompound(compoundId, maxAmount/10.0)
        createAgentCloud(compoundId, self.sceneNode.transform.position.x + xnew, self.sceneNode.transform.position.y + ynew, direction * 5, amountToEject * 10)
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
-- 
-- @returns leftover
-- The amount of compound not stored, due to bandwidth or being full
function Microbe:storeCompound(compoundId, amount, bandwidthLimited)
    local storedAmount = amount + 0
    if bandwidthLimited then
        storedAmount = self.microbe:getBandwidth(amount, compoundId)
    end
    storedAmount = math.min(storedAmount , self.microbe.capacity - self.microbe.stored)
    self.entity:getComponent(CompoundBagComponent.TYPE_ID):giveCompound(tonumber(compoundId), storedAmount)
    self.microbe.stored = self.microbe.stored + storedAmount
    return amount - storedAmount
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
    --if self.microbe.specialStorageOrganelles[compoundId] == nil then
    
    local takenAmount = self.entity:getComponent(CompoundBagComponent.TYPE_ID):takeCompound(compoundId, maxAmount)
    self.microbe.stored = self.microbe.stored - takenAmount
    return takenAmount
end

-- Ejects compounds from the microbes behind position, into the enviroment
-- Note that the compounds ejected are created in this function and not taken from the microbe
--
-- @param compoundId
-- The compound type to create and eject
--
-- @param amount
-- The amount to eject
function Microbe:ejectCompound(compoundId, amount)
    -- The back of the microbe
    local exitX, exitY = axialToCartesian(0, 1)
    local membraneCoords = self.membraneComponent:getExternOrganellePos(exitX, exitY)
    
    local angle = 180
    -- Find the direction the microbe is facing
    local yAxis = self.sceneNode.transform.orientation:yAxis()
    local microbeAngle = math.atan2(yAxis.x, yAxis.y)
    if (microbeAngle < 0) then
        microbeAngle = microbeAngle + 2*math.pi
    end
    microbeAngle = microbeAngle * 180/math.pi
    -- Take the microbe angle into account so we get world relative degrees
    local finalAngle = (angle + microbeAngle) % 360        
    
    local s = math.sin(finalAngle/180*math.pi);
    local c = math.cos(finalAngle/180*math.pi);

    local xnew = -membraneCoords[1] * c + membraneCoords[2] * s;
    local ynew = membraneCoords[1] * s + membraneCoords[2] * c;
    
    local direction = Vector3(xnew, ynew, 0)
    direction:normalise()
    local amountToEject = self:takeCompound(compoundId, amount/10.0)
    createCompoundCloud(CompoundRegistry.getCompoundInternalName(compoundId), self.sceneNode.transform.position.x + xnew*1.1, self.sceneNode.transform.position.y + ynew*1.1, amount*5000)
end




-- Kills the microbe, releasing stored compounds into the enviroment
function Microbe:kill()
    -- Eject the compounds that was in the microbe
    for compoundId in CompoundRegistry.getCompoundList() do
        local total = self:getCompoundAmount(compoundId)
        ejectedAmount = self:takeCompound(compoundId, total)
        self:ejectCompound(compoundId, ejectedAmount)
    end    
    for compoundId, specialStorageOrg in pairs(self.microbe.specialStorageOrganelles) do
        local _amount = self:getCompoundAmount(compoundId)
        while _amount > 0 do
            ejectedAmount = self:takeCompound(compoundId, 3) -- Eject up to 3 units per particle
            local direction = Vector3(math.random(), math.random(), math.random())
            createAgentCloud(compoundId, self.sceneNode.transform.position.x, self.sceneNode.transform.position.y, direction, amountToEject)
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
    if not self.microbe.isPlayerMicrobe then
        for _, organelle in pairs(self.microbe.organelles) do
           organelle:removePhysics()
        end
    end
    if self.microbe.wasBeingEngulfed then
        self:removeEngulfedEffect()
    end
    microbeSceneNode.visible = false
end

-- Copies this microbe. The new microbe will not have the stored compounds of this one.
function Microbe:readyToReproduce()
    if self.microbe.isPlayerMicrobe then
        showReproductionDialog()
    else
        -- Return the first cell to its normal, non duplicated cell arangement.
        SpeciesSystem.template(self, self:getSpeciesComponent())
        self:divide()
    end
end

function Microbe:divide()
    print("divide")
    -- Create the two daughter cells.
    local copy = Microbe.createMicrobeEntity(nil, true, self.microbe.speciesName)
    print(self.microbe.speciesName.." <> "..copy.microbe.speciesName)
    
    --Separate the two cells.
    copy.rigidBody.dynamicProperties.position = Vector3(self.rigidBody.dynamicProperties.position.x - self.membraneComponent.dimensions/2, self.rigidBody.dynamicProperties.position.y, 0)
    self.rigidBody.dynamicProperties.position = Vector3(self.rigidBody.dynamicProperties.position.x + self.membraneComponent.dimensions/2, self.rigidBody.dynamicProperties.position.y, 0)
    
    if self.microbe.isPlayerMicrobe == true then
        --print("before " .. self:getCompoundAmount(CompoundRegistry.getCompoundId("glucose")))
    end
    
    -- Split the compounds evenly between the two cells.
    for compoundID in CompoundRegistry.getCompoundList() do
        local amount = self:getCompoundAmount(compoundID)
    
        if amount ~= 0 then
            self:takeCompound(compoundID, amount/2, false)
            copy:storeCompound(compoundID, amount/2, false)
        end
    end
    
    if self.microbe.isPlayerMicrobe == true then
        --print("divided " .. self:getCompoundAmount(CompoundRegistry.getCompoundId("glucose")))
    end
    
    copy.entity:addComponent(SpawnedComponent())
    self.soundSource:playSound("microbe-reproduction")
end

function Microbe.transferCompounds(from, to)
    for compoundID in CompoundRegistry.getCompoundList() do
        local amount = from:getCompoundAmount(compoundID)
    
        if amount ~= 0 then
            from:takeCompound(compoundID, amount, false)
            to:storeCompound(compoundID, amount, false)
        end
    end
end

-- Disables or enabled engulfmode for a microbe, allowing or disallowed it to absorb other microbes
function Microbe:toggleEngulfMode()
    if self.microbe.engulfMode then
        self.microbe.movementFactor = self.microbe.movementFactor * ENGULFING_MOVEMENT_DIVISION
        self.soundSource:stopSound("microbe-engulfment") -- Possibly comment out. If version > 0.3.2 delete.
        self.rigidBody:reenableAllCollisions()
    else
        self.microbe.movementFactor = self.microbe.movementFactor / ENGULFING_MOVEMENT_DIVISION
    end
    self.microbe.engulfMode = not self.microbe.engulfMode
end

function Microbe:removeEngulfedEffect()
    self.microbe.movementFactor = self.microbe.movementFactor * ENGULFED_MOVEMENT_DIVISION
    self.microbe.wasBeingEngulfed = false
    self.microbe.hostileEngulfer.microbe.isCurrentlyEngulfing = false;
    self.microbe.hostileEngulfer.rigidBody:reenableAllCollisions()
    -- Causes crash because sound was already stopped.
    --self.microbe.hostileEngulfer.soundSource:stopSound("microbe-engulfment")
end

-- Sets the color of the microbe's membrane.
function Microbe:setMembraneColour(colour)
    self.membraneComponent:setColour(colour.x, colour.y, colour.z, 1)
end

function Microbe:flashMembraneColour(duration, colour)
	if self.flashDuration == nil then
        self.flashColour = colour
        self.flashDuration = duration
    end
end


-- Updates the microbe's state
function Microbe:update(logicTime)
    if self.microbe.isPlayerMicrobe == true then
        --print("update " .. self:getCompoundAmount(CompoundRegistry.getCompoundId("glucose")))
    end
    
    
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
        -- Flash membrane if something happens.
        if self.flashDuration ~= nil and self.flashColour ~= nil then
            self.flashDuration = self.flashDuration - logicTime
            
            local entity = self.membraneComponent.entity
            -- How frequent it flashes, would be nice to update the flash function to have this variable
            if math.fmod(self.flashDuration,600) < 300 then
                entity:tintColour("Membrane", self.flashColour)
            else
                entity:setMaterial(self.sceneNode.meshName)
            end
            
            if self.flashDuration <= 0 then
                self.flashDuration = nil				
                entity:setMaterial(self.sceneNode.meshName)
            end
        end
        
        self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer + logicTime
        while self.microbe.compoundCollectionTimer > EXCESS_COMPOUND_COLLECTION_INTERVAL do
            -- For every COMPOUND_DISTRIBUTION_INTERVAL passed

            self.microbe.compoundCollectionTimer = self.microbe.compoundCollectionTimer - EXCESS_COMPOUND_COLLECTION_INTERVAL

            self:purgeCompounds()

            self:atpDamage()
        end

        local reproductionStageComplete = true
        for _, organelle in pairs(self.microbe.organelles) do
            -- Update the organelle.
            organelle:update(self, logicTime)
            
            -- We are in G1 phase of the cell cycle, duplicate all organelles.
            if organelle.name ~= "nucleus" and self.reproductionStage == 0 then
                
                -- If the organelle is not split, give it some compounds to make it larger.
                if organelle.compoundBin < 2.0 and not organelle.wasSplit then
                    -- Give the organelle access to the compound back to take some compound.
                    organelle:grow(self.entity:getComponent(CompoundBagComponent.TYPE_ID))
                    reproductionStageComplete = false
                -- If the organelle is twice its size...
                elseif organelle.compoundBin == 2.0 then
                    print("ready to split " .. organelle.name)
                    -- Mark this organelle as done and return to its normal size.
                    organelle:reset()
                    organelle.wasSplit = true
                    -- Create a second organelle.
                    local organelle2 = self:splitOrganelle(organelle)
                    organelle2.wasSplit = true
                    organelle2.isDuplicate = true

                    -- Redo the cell membrane.
                    self.membraneComponent:clear()
                end
            -- In the S phase, the nucleus grows as chromatin is duplicated.
            elseif organelle.name == "nucleus" and self.reproductionStage == 1 then
                -- If the nucleus hasn't finished replicating its DNA, give it some compounds.
                if organelle.compoundBin < 2.0 then
                    -- Give the organelle access to the compound back to take some compound.
                    organelle:grow(self.entity:getComponent(CompoundBagComponent.TYPE_ID))
                    reproductionStageComplete = false
                end
            end
        end
        if reproductionStageComplete and self.reproductionStage < 2 then
            self.reproductionStage = self.reproductionStage + 1
        end
        -- To finish the G2 phase we just need more than a threshold of compounds.
        if self.reproductionStage == 2 or self.reproductionStage == 3 then
            -- If we have more than the threshold,
            if self:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids")) > 1 and 
                self:getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) > 5 and
                self:getCompoundAmount(CompoundRegistry.getCompoundId("aminoacids")) > 1 and
                self:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) > 10 then
               
                showReproductionDialog()
            else
                hideReproductionDialog()
            end
        end

        if self.microbe.engulfMode then
            -- Drain atp and if we run out then disable engulfmode
            local cost = ENGULFING_ATP_COST_SECOND/1000*logicTime
            
            if self:takeCompound(CompoundRegistry.getCompoundId("atp"), cost) < cost - 0.001 then
                print ("too little atp, disabling - 749")
                self:toggleEngulfMode()
            end
            -- Flash the membrane blue.
            self:flashMembraneColour(3000, ColourValue(0.2,0.5,1.0,0.5))
        end
        if self.microbe.isBeingEngulfed and self.microbe.wasBeingEngulfed then
            self:damage(logicTime * 0.00025  * self.microbe.maxHitpoints, "isBeingEngulfed - 764") -- Engulfment damages 25% per second
        -- Else If we were but are no longer, being engulfed
        elseif self.microbe.wasBeingEngulfed then
            self:removeEngulfedEffect()
        end
        -- Used to detect when engulfing stops
        self.microbe.isBeingEngulfed = false;
        self.compoundAbsorber:setAbsorbtionCapacity(math.min(self.microbe.capacity - self.microbe.stored + 10, self.microbe.remainingBandwidth))
    else
        self.microbe.deathTimer = self.microbe.deathTimer - logicTime
        self.flashDuration = 0
        if self.microbe.deathTimer <= 0 then
            if self.microbe.isPlayerMicrobe == true then
                self:respawn()
            else
                self:destroy()
            end
        end
    end
    -- print("finished update")
end

function Microbe:calculateHealthFromOrganelles()
    self.microbe.hitpoints = 0
    for _, organelle in pairs(self.microbe.organelles) do
        self.microbe.hitpoints = self.microbe.hitpoints + (organelle.compoundBin < 1.0 and organelle.compoundBin or 1.0) * MICROBE_HITPOINTS_PER_ORGANELLE
    end
    self.microbe.hitpoints = self.microbe.hitpoints + self.membraneHealth * MICROBE_HITPOINTS_PER_ORGANELLE
end

function Microbe:splitOrganelle(organelle)
    -- Find an empty place where the organelle can be placed.
    for _, q, r in iterateNeighbours(organelle.position.q, organelle.position.r) do
        if self:getOrganelleAt(q,r) == nil then
            for i=0, 6 do
                local data = {["name"]=organelle.name, ["q"]=q, ["r"]=r, ["rotation"]=i*60}
                newOrganelle = OrganelleFactory.makeOrganelle(data)
                
                if self:validPlacement(newOrganelle, q, r) then
                    print("placed " .. organelle.name .. " at " .. q .. " " .. r)
                    self:addOrganelle(q, r, i*60, newOrganelle)
                    return newOrganelle
                end
            end
        end
    end
    

end

function Microbe:validPlacement(organelle, q, r)
    local empty = true
    local touching = false;
    for s, hex in pairs(organelle._hexes) do
        
        local organelle = self:getOrganelleAt(hex.q + q, hex.r + r)
        if organelle then
            if organelle.name ~= "cytoplasm" then
                empty = false 
            end
        end
        
		if  self:getOrganelleAt(hex.q + q + 0, hex.r + r - 1) or
			self:getOrganelleAt(hex.q + q + 1, hex.r + r - 1) or
			self:getOrganelleAt(hex.q + q + 1, hex.r + r + 0) or
			self:getOrganelleAt(hex.q + q + 0, hex.r + r + 1) or
			self:getOrganelleAt(hex.q + q - 1, hex.r + r + 1) or
			self:getOrganelleAt(hex.q + q - 1, hex.r + r + 0) then
			touching = true;
		end
    end
    
    if empty and touching then
        return true
    else
        return false
    end
end

PURGE_SCALE = 0.4

function Microbe:purgeCompounds()
    -- Eject a fraction of all compounds over vent thresholds
    -- TODO: only eject compounds when microbe is full, and eject excess compounds proportionally to the amount each is in excess

    for compoundId in CompoundRegistry.getCompoundList() do
        local amount = self.entity:getComponent(CompoundBagComponent.TYPE_ID):excessAmount(compoundId) * PURGE_SCALE
        if amount > 0 then amount = self:takeCompound(compoundId, amount) end
        if amount > 0 then self:ejectCompound(compoundId, amount) end
    end
end

function Microbe:atpDamage()
    -- Damage microbe if its too low on ATP
    if self:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) < 1.0 then
        if self.microbe.isPlayerMicrobe and not self.playerAlreadyShownAtpDamage then
            self.playerAlreadyShownAtpDamage = true
            showMessage("No ATP hurts you!")
        end
        self:damage(EXCESS_COMPOUND_COLLECTION_INTERVAL * 0.000002  * self.microbe.maxHitpoints, "atpDamage") -- Microbe takes 2% of max hp per second in damage
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
    sceneNode.transform.position = Vector3(0, 0, 0)
    sceneNode.transform:touch()
    
    self:storeCompound(CompoundRegistry.getCompoundId("atp"), 50, false)
    
    local rand = math.random(0,3)
    local backgroundEntity = Entity("background")
    local skyplane = backgroundEntity:getComponent(SkyPlaneComponent.TYPE_ID)
    if rand == 0 then
        skyplane.properties.materialName = "Background"
    elseif rand == 1 then
        skyplane.properties.materialName = "Background_Vent"
    elseif rand == 2 then
        skyplane.properties.materialName = "Background_Abyss"
    else 
        skyplane.properties.materialName = "Background_Shallow"
    end
    skyplane.properties:touch()
end

-- Private function for initializing a microbe's components
function Microbe:_initialize()
    self.rigidBody.properties.shape:clear()
    self.rigidBody.properties.mass = 0.0
    -- Organelles
    for s, organelle in pairs(self.microbe.organelles) do
        organelle.microbe = self
        local q = organelle.position.q
        local r = organelle.position.r
        local x, y = axialToCartesian(q, r)
        local rotation = organelle.rotation
        local translation = Vector3(x, y, 0)
        -- Collision shape
        self.rigidBody.properties.shape:addChildShape(
            translation,
            Quaternion(Radian(0), Vector3(1,0,0)),
            organelle.collisionShape
        )
        self.rigidBody.properties.mass = self.rigidBody.properties.mass + organelle.mass
        -- Scene node
        organelle.sceneNode.parent = self.entity
        organelle.sceneNode.transform.position = translation
        organelle.sceneNode.transform:touch()
        organelle:onAddedToMicrobe(self, q, r, rotation)

    end
    -- Membrane
    self.sceneNode.meshName = "membrane_" .. self.microbe.speciesName
    self.rigidBody.properties:touch()
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

-- Must exists for current spawningSystem to function
function Microbe:exists()
    return self.entity:exists()
end

-- Must exists for current spawningSystem to function, also used by microbe:kill
function Microbe:destroy()
    for _, organelle in pairs(self.microbe.organelles) do
        organelle:destroy()
    end
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
    -- Temporary for 0.3.2, should be moved to separate system.
    self.agentCollisions = CollisionFilter(
        "microbe",
        "agent"
    );
    self.microbes = {}
end


function MicrobeSystem:init(gameState)
    System.init(self, "MicrobeSystem", gameState)
    self.entities:init(gameState)
    self.microbeCollisions:init(gameState)
    
    self.agentCollisions:init(gameState)
end


function MicrobeSystem:shutdown()
    self.entities:shutdown()
    self.microbeCollisions:shutdown()
    
    self.agentCollisions:shutdown()
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
    -- Note that this triggers every frame there is a collision
    for collision in self.microbeCollisions:collisions() do
        local entity1 = Entity(collision.entityId1)
        local entity2 = Entity(collision.entityId2)
        if entity1:exists() and entity2:exists() then
            local body1 = entity1:getComponent(RigidBodyComponent.TYPE_ID)
            local body2 = entity2:getComponent(RigidBodyComponent.TYPE_ID)
            local microbe1Comp = entity1:getComponent(MicrobeComponent.TYPE_ID)
            local microbe2Comp = entity2:getComponent(MicrobeComponent.TYPE_ID)
            if body1~=nil and body2~=nil then
                -- Engulf initiation
                checkEngulfment(microbe1Comp, microbe2Comp, body1, entity1, entity2)
                checkEngulfment(microbe2Comp, microbe1Comp, body2, entity2, entity1)
            end
        end
    end
    self.microbeCollisions:clearCollisions()
    
    
    
    -- TEMP, DELETE FOR 0.3.3!!!!!!!!
    for collision in self.agentCollisions:collisions() do
        local entity = Entity(collision.entityId1)
        local agent = Entity(collision.entityId2)
        
        if entity:exists() and agent:exists() then
            Microbe(entity):damage(5, "toxin")
            agent:destroy()
        end
    end
    self.agentCollisions:clearCollisions()
end

function checkEngulfment(microbe1Comp, microbe2Comp, body, entity1, entity2)
    
    if microbe1Comp.engulfMode and 
       microbe1Comp.maxHitpoints > ENGULF_HP_RATIO_REQ*microbe2Comp.maxHitpoints and
       microbe1Comp.dead == false and microbe2Comp.dead == false then

        if not microbe1Comp.isCurrentlyEngulfing then
            --We have just started engulfing
            microbe2Comp.movementFactor = microbe2Comp.movementFactor / ENGULFED_MOVEMENT_DIVISION
            microbe1Comp.isCurrentlyEngulfing = true
            microbe2Comp.wasBeingEngulfed = true
            microbeObj = Microbe(entity1)
            microbe2Comp.hostileEngulfer = microbeObj
            body:disableCollisionsWith(entity2.id)     
            microbeObj.soundSource:playSound("microbe-engulfment")
        end

       --isBeingEngulfed is set to false every frame
       -- we detect engulfment stopped by isBeingEngulfed being false while wasBeingEngulfed is true
       microbe2Comp.isBeingEngulfed = true

    end
end
