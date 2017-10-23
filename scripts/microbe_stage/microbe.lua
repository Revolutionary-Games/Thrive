--------------------------------------------------------------------------------
-- MicrobeComponent
--
-- Holds data common to all microbes. You probably shouldn't use this directly,
-- use the Microbe class (below) instead.
--------------------------------------------------------------------------------
MicrobeComponent = class(
    function(self, isPlayerMicrobe, speciesName)
        self.speciesName = speciesName
        self.hitpoints = 0
        self.maxHitpoints = 0
        self.dead = false
        self.deathTimer = 0
        self.organelles = {}
        self.processOrganelles = {} -- Organelles responsible for producing compounds from other compounds
        self.specialStorageOrganelles = {} -- Organelles with complete resonsiblity for a specific compound (such as agentvacuoles)
        self.movementDirection = Vector3(0, 0, 0)
        self.facingTargetPoint = Vector3(0, 0, 0)
		self.microbetargetdirection = 0
        self.movementFactor = 1.0 -- Multiplied on the movement speed of the microbe.
        self.capacity = 0  -- The amount that can be stored in the microbe. NOTE: This does not include special storage organelles
        self.stored = 0 -- The amount stored in the microbe. NOTE: This does not include special storage organelles
        self.initialized = false
        self.isPlayerMicrobe = isPlayerMicrobe
        self.maxBandwidth = 10.0 * BANDWIDTH_PER_ORGANELLE -- wtf is a bandwidth anyway?
        self.remainingBandwidth = 0
        self.compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL
        self.isCurrentlyEngulfing = false
        self.isBeingEngulfed = false
        self.wasBeingEngulfed = false
        self.hostileEngulfer = nil
        self.agentEmissionCooldown = 0
        self.flashDuration = nil
        self.flashColour = nil
        self.reproductionStage = 0 -- 1 for G1 complete, 2 for S complete, 3 for G2 complete, and 4 for reproduction finished.
    end
)

MicrobeComponent.TYPE_NAME = "MicrobeComponent"

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
AGENT_EMISSION_COOLDOWN = 1000 -- Cooldown between agent emissions, in milliseconds.

function MicrobeComponent:load(storage)
    
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

    -- local compoundPriorities = storage:get("compoundPriorities", {})
    -- for i = 1,compoundPriorities:size() do
    --     local compound = compoundPriorities:get(i)
    --     self.compoundPriorities[compound:get("compoundId", 0)] = compound:get("priority", 0)
    -- end
end


function MicrobeComponent:storage(storage)
    -- Organelles
    local organelles = StorageList.new()
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

    -- local compoundPriorities = StorageList()
    -- for compoundId, priority in pairs(self.compoundPriorities) do
    --     compound = StorageContainer()
    --     compound:set("compoundId", compoundId)
    --     compound:set("priority", priority)
    --     compoundPriorities:append(compound)
    -- end
    -- storage:set("compoundPriorities", compoundPriorities)
end

REGISTER_COMPONENT("MicrobeComponent", MicrobeComponent)

--------------------------------------------------------------------------------
-- MicrobeSystem
--
-- Updates microbes
--------------------------------------------------------------------------------
-- TODO: This system is HUUUUUUGE! D:
-- We should try to separate it into smaller systems.
-- For example, the agents should be handled in another system
-- (however we're going to redo agents so we should wait until then for that one)
MicrobeSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)

        self.entities = EntityFilter.new(
            {
                CompoundAbsorberComponent,
                MicrobeComponent,
                OgreSceneNodeComponent,
                RigidBodyComponent,
                CollisionComponent
            },
            true
        )
        self.microbeCollisions = CollisionFilter.new(
            "microbe",
            "microbe"
        )
        -- Temporary for 0.3.2, should be moved to separate system.
        self.agentCollisions = CollisionFilter.new(
            "microbe",
            "agent"
        )

        self.bacteriaCollisions = CollisionFilter.new(
            "microbe",
            "bacteria"
        )

        self.microbes = {}
    end
)

-- I don't feel like checking for each component separately, so let's make a
-- loop do it with an assert for good measure (see Microbe.create)
MICROBE_COMPONENTS = {
    compoundAbsorber = CompoundAbsorberComponent,
    microbe = MicrobeComponent,
    rigidBody = RigidBodyComponent,
    sceneNode = OgreSceneNodeComponent,
    collisionHandler = CollisionComponent,
    soundSource = SoundSourceComponent,
    membraneComponent = MembraneComponent,
    compoundBag = CompoundBagComponent
}

function MicrobeSystem:init(gameState)
    LuaSystem.init(self, "MicrobeSystem", gameState)
    self.entities:init(gameState.wrapper)
    self.microbeCollisions:init(gameState.wrapper)
    self.agentCollisions:init(gameState.wrapper)
    self.bacteriaCollisions:init(gameState.wrapper)
end

function MicrobeSystem:shutdown()
    LuaSystem.shutdown(self)
    self.entities:shutdown()
    self.microbeCollisions:shutdown()
    self.agentCollisions:shutdown()
    self.bacteriaCollisions:shutdown()
end

function MicrobeSystem:update(renderTime, logicTime)
    for _, entityId in pairs(self.entities:removedEntities()) do
        self.microbes[entityId] = nil
    end
    for _, entityId in pairs(self.entities:addedEntities()) do
        local microbeEntity = Entity.new(entityId, self.gameState.wrapper)
        self.microbes[entityId] = microbeEntity
    end
    self.entities:clearChanges()
    for _, microbeEntity in pairs(self.microbes) do
        MicrobeSystem.updateMicrobe(microbeEntity, logicTime)
    end
    -- Note that this triggers every frame there is a collision
    for _, collision in pairs(self.microbeCollisions:collisions()) do
        local entity1 = Entity.new(collision.entityId1, self.gameState.wrapper)
        local entity2 = Entity.new(collision.entityId2, self.gameState.wrapper)
        if entity1:exists() and entity2:exists() then
            -- Engulf initiation
            MicrobeSystem.checkEngulfment(entity1, entity2)
            MicrobeSystem.checkEngulfment(entity2, entity1)
        end
    end
    self.microbeCollisions:clearCollisions()

    -- TEMP, DELETE FOR 0.3.3!!!!!!!!
    for _, collision in pairs(self.agentCollisions:collisions()) do
        local entity = Entity.new(collision.entityId1, self.gameState.wrapper)
        local agent = Entity.new(collision.entityId2, self.gameState.wrapper)
        
        if entity:exists() and agent:exists() then
            MicrobeSystem.damage(entity, .5, "toxin")
            agent:destroy()
        end
    end
    self.agentCollisions:clearCollisions()

    for _, collision in pairs(self.bacteriaCollisions:collisions()) do
        local microbe_entity = Entity.new(collision.entityId1, self.gameState.wrapper)
        local bacterium_entity = Entity.new(collision.entityId2, self.gameState.wrapper)

        if microbe_entity:exists() and bacterium_entity:exists() then
            if not (getComponent(bacterium_entity, Bacterium.COMPONENTS.bacterium) == nil) then
                local bacterium = Bacterium(bacterium_entity)
                bacterium:damage(4)
            end
        end
    end
    self.bacteriaCollisions:clearCollisions()
end

function MicrobeSystem.checkEngulfment(engulferMicrobeEntity, engulfedMicrobeEntity)
    local body = getComponent(engulferMicrobeEntity, RigidBodyComponent)
    local microbe1Comp = getComponent(engulferMicrobeEntity, MicrobeComponent)
    local microbe2Comp = getComponent(engulfedMicrobeEntity, MicrobeComponent)
    local soundSourceComponent = getComponent(engulferMicrobeEntity, SoundSourceComponent)
    local bodyEngulfed = getComponent(engulfedMicrobeEntity, RigidBodyComponent)

    -- That actually happens sometimes, and i think it shouldn't. :/
    -- Probably related to a collision detection bug.
    -- assert(body ~= nil, "Microbe without a rigidBody tried to engulf.")
    -- assert(bodyEngulfed ~= nil, "Microbe without a rigidBody tried to be engulfed.")
    if body == nil or bodyEngulfed == nil then return end

    if microbe1Comp.engulfMode and 
       microbe1Comp.maxHitpoints > ENGULF_HP_RATIO_REQ * microbe2Comp.maxHitpoints and
       microbe1Comp.dead == false and microbe2Comp.dead == false then

        if not microbe1Comp.isCurrentlyEngulfing then
            --We have just started engulfing
            microbe2Comp.movementFactor = microbe2Comp.movementFactor / ENGULFED_MOVEMENT_DIVISION
            microbe1Comp.isCurrentlyEngulfing = true
            microbe2Comp.wasBeingEngulfed = true
            microbe2Comp.hostileEngulfer = engulferMicrobeEntity
            body:disableCollisionsWith(engulfedMicrobeEntity.id)
            soundSourceComponent:playSound("microbe-engulfment")
        end

       --isBeingEngulfed is set to false every frame
       -- we detect engulfment stopped by isBeingEngulfed being false while wasBeingEngulfed is true
       microbe2Comp.isBeingEngulfed = true
    end
end

-- Attempts to obtain an amount of bandwidth for immediate use.
-- This should be in conjunction with most operations ejecting  or absorbing compounds and agents for microbe.
--
-- @param maicrobeEntity
-- The entity of the microbe to get the bandwidth from.
--
-- @param maxAmount
-- The max amount of units that is requested.
--
-- @param compoundId
-- The compound being requested for volume considerations.
--
-- @return
--  amount in units avaliable for use.
function MicrobeSystem.getBandwidth(microbeEntity, maxAmount, compoundId)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local compoundVolume = CompoundRegistry.getCompoundUnitVolume(compoundId)
    local amount = math.min(maxAmount * compoundVolume, microbeComponent.remainingBandwidth)
    microbeComponent.remainingBandwidth = microbeComponent.remainingBandwidth - amount
    return amount / compoundVolume
end

function MicrobeSystem.regenerateBandwidth(microbeEntity, logicTime)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local addedBandwidth = microbeComponent.remainingBandwidth + logicTime * (microbeComponent.maxBandwidth / BANDWIDTH_REFILL_DURATION)
    microbeComponent.remainingBandwidth = math.min(addedBandwidth, microbeComponent.maxBandwidth)
end

function MicrobeSystem.calculateHealthFromOrganelles(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    microbeComponent.hitpoints = 0
    microbeComponent.maxHitpoints = 0
    for _, organelle in pairs(microbeComponent.organelles) do
        microbeComponent.hitpoints = microbeComponent.hitpoints + (organelle:getCompoundBin() < 1.0 and organelle:getCompoundBin() or 1.0) * MICROBE_HITPOINTS_PER_ORGANELLE
        microbeComponent.maxHitpoints = microbeComponent.maxHitpoints + MICROBE_HITPOINTS_PER_ORGANELLE
    end
end

-- Queries the currently stored amount of an compound
--
-- @param compoundId
-- The id of the compound to query
--
-- @returns amount
-- The amount stored in the microbe's storage oraganelles
function MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)
    return getComponent(microbeEntity, CompoundBagComponent):getCompoundAmount(compoundId)
end

-- Getter for microbe species
-- 
-- returns the species component or nil if it doesn't have a valid species
function MicrobeSystem.getSpeciesComponent(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    return getComponent(microbeComponent.speciesName, g_luaEngine.currentGameState, SpeciesComponent)
end

-- Sets the color of the microbe's membrane.
function MicrobeSystem.setMembraneColour(microbeEntity, colour)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)
    membraneComponent:setColour(colour.x, colour.y, colour.z, 1)
end

function MicrobeSystem.flashMembraneColour(microbeEntity, duration, colour)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
	if microbeComponent.flashDuration == nil then
        microbeComponent.flashColour = colour
        microbeComponent.flashDuration = duration
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
function MicrobeSystem.storeCompound(microbeEntity, compoundId, amount, bandwidthLimited)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local storedAmount = amount + 0 -- Why are we adding 0? Is this a type-casting thing?

    if bandwidthLimited then
        storedAmount = MicrobeSystem.getBandwidth(microbeEntity, amount, compoundId)
    end

    storedAmount = math.min(storedAmount , microbeComponent.capacity - microbeComponent.stored)
    getComponent(microbeEntity, CompoundBagComponent):giveCompound(tonumber(compoundId), storedAmount)
    
    microbeComponent.stored = microbeComponent.stored + storedAmount
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
function MicrobeSystem.takeCompound(microbeEntity, compoundId, maxAmount)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local takenAmount = getComponent(microbeEntity, CompoundBagComponent
    ):takeCompound(compoundId, maxAmount)
    
    microbeComponent.stored = microbeComponent.stored - takenAmount
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
function MicrobeSystem.ejectCompound(microbeEntity, compoundId, amount)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)
    local sceneNodeComponent = getComponent(microbeEntity, OgreSceneNodeComponent)

    -- The back of the microbe
    local exitX, exitY = axialToCartesian(0, 1)
    local membraneCoords = membraneComponent:getExternOrganellePos(exitX, exitY)

    --Get the distance to eject the compunds
    local maxR = 0
    for _, organelle in pairs(microbeComponent.organelles) do
        for _, hex in pairs(organelle._hexes) do
            if hex.r + organelle.position.r > maxR then
                maxR = hex.r + organelle.position.r
            end
        end
    end

    --The distance is two hexes away from the back of the microbe.
    --This distance could be precalculated when adding/removing an organelle
    --for more efficient pooping.
    local ejectionDistance = (maxR + 3) * HEX_SIZE

    local angle = 180
    -- Find the direction the microbe is facing
    local yAxis = sceneNodeComponent.transform.orientation:yAxis()
    local microbeAngle = math.atan2(yAxis.x, yAxis.y)
    if (microbeAngle < 0) then
        microbeAngle = microbeAngle + 2 * math.pi
    end
    microbeAngle = microbeAngle * 180 / math.pi
    -- Take the microbe angle into account so we get world relative degrees
    local finalAngle = (angle + microbeAngle) % 360        
    
    local s = math.sin(finalAngle/180*math.pi);
    local c = math.cos(finalAngle/180*math.pi);

    local xnew = -membraneCoords[1] * c + membraneCoords[2] * s;
    local ynew = membraneCoords[1] * s + membraneCoords[2] * c;

    local amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId, amount/10.0)
    createCompoundCloud(CompoundRegistry.getCompoundInternalName(compoundId),
                        sceneNodeComponent.transform.position.x + xnew * ejectionDistance,
                        sceneNodeComponent.transform.position.y + ynew * ejectionDistance,
                        amount * 5000)
end

function MicrobeSystem.respawnPlayer()
    local playerEntity = Entity.new("player", g_luaEngine.currentGameState.wrapper)
    local microbeComponent = getComponent(playerEntity, MicrobeComponent)
    local rigidBodyComponent = getComponent(playerEntity, RigidBodyComponent)
    local sceneNodeComponent = getComponent(playerEntity, OgreSceneNodeComponent)

    microbeComponent.dead = false
    microbeComponent.deathTimer = 0
    
    -- Reset the growth bins of the organelles to full health.
    for _, organelle in pairs(microbeComponent.organelles) do
        organelle:reset()
    end
    MicrobeSystem.calculateHealthFromOrganelles(playerEntity)

    rigidBodyComponent:setDynamicProperties(
        Vector3(0,0,0), -- Position
        Quaternion.new(Radian.new(Degree(0)), Vector3(1, 0, 0)), -- Orientation
        Vector3(0, 0, 0), -- Linear velocity
        Vector3(0, 0, 0)  -- Angular velocity
    )

    sceneNodeComponent.visible = true
    sceneNodeComponent.transform.position = Vector3(0, 0, 0)
    sceneNodeComponent.transform:touch()

    -- TODO: give the microbe the values from some table instead.
    MicrobeSystem.storeCompound(playerEntity, CompoundRegistry.getCompoundId("atp"), 50, false)

    setRandomBiome(g_luaEngine.currentGameState)
	global_activeMicrobeStageHudSystem:suicideButtonreset()
end

-- Retrieves the organelle occupying a hex cell
--
-- @param q, r
-- Axial coordinates, relative to the microbe's center
--
-- @returns organelle
-- The organelle at (q,r) or nil if the hex is unoccupied
function MicrobeSystem.getOrganelleAt(microbeEntity, q, r)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)

    for _, organelle in pairs(microbeComponent.organelles) do
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
function MicrobeSystem.removeOrganelle(microbeEntity, q, r)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)

    local organelle = MicrobeSystem.getOrganelleAt(microbeEntity, q, r)
    if not organelle then
        return false
    end
    
    local s = encodeAxial(organelle.position.q, organelle.position.r)
    microbeComponent.organelles[s] = nil
    
    rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass - organelle.mass
    rigidBodyComponent.properties:touch()
    -- TODO: cache for performance
    local compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape)
    compoundShape:removeChildShape(
        organelle.collisionShape
    )
    
    organelle:onRemovedFromMicrobe()
    
    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity)
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth - BANDWIDTH_PER_ORGANELLE -- Temporary solution for decreasing max bandwidth
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth
    
    return true
end

function MicrobeSystem.purgeCompounds(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local compoundBag = getComponent(microbeEntity, CompoundBagComponent)

    local compoundAmountToDump = microbeComponent.stored - microbeComponent.capacity

    -- Uncomment to print compound economic information to the console.
    --[[
    if microbeComponent.isPlayerMicrobe then
        for compound, _ in pairs(compoundTable) do
            compoundId = CompoundRegistry.getCompoundId(compound)
            print(compound, compoundBag:getPrice(compoundId), compoundBag:getDemand(compoundId))
        end
    end
    print("")
    ]]

    -- Dumping all the useless compounds (with price = 0).
    for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
        local price = compoundBag:getPrice(compoundId)
        if price <= 0 then
            local amountToEject = MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)
            if amount > 0 then amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId, amountToEject) end
            if amount > 0 then MicrobeSystem.ejectCompound(microbeEntity, compoundId, amountToEject) end
        end
    end

    if compoundAmountToDump > 0 then
        --Calculating each compound price to dump proportionally.
        local compoundPrices = {}
        local priceSum = 0
        for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
            local amount = MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)

            if amount > 0 then
                local price = compoundBag:getPrice(compoundId)
                compoundPrices[compoundId] = price
                priceSum = priceSum + amount / price
            end
        end

        --Dumping each compound according to it's price.
        for compoundId, price in pairs(compoundPrices) do
            local amountToEject = compoundAmountToDump * (MicrobeSystem.getCompoundAmount(microbeEntity, compoundId) / price) / priceSum
            if amount > 0 then amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId, amountToEject) end
            if amount > 0 then MicrobeSystem.ejectCompound(microbeEntity, compoundId, amountToEject) end
        end
    end
end

function MicrobeSystem.removeEngulfedEffect(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)

    microbeComponent.movementFactor = microbeComponent.movementFactor * ENGULFED_MOVEMENT_DIVISION
    microbeComponent.wasBeingEngulfed = false

    local hostileMicrobeComponent = getComponent(microbeComponent.hostileEngulfer, MicrobeComponent)
    if hostileMicrobeComponent ~= nil then
        hostileMicrobeComponent.isCurrentlyEngulfing = false
    end

    local hostileRigidBodyComponent = getComponent(microbeComponent.hostileEngulfer, RigidBodyComponent)

    -- The component is nil sometimes, probably due to despawning.
    if hostileRigidBodyComponent ~= nil then
        hostileRigidBodyComponent:reenableAllCollisions()
    end
    -- Causes crash because sound was already stopped.
    --microbeComponent.hostileEngulfer.soundSource:stopSound("microbe-engulfment")
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
function MicrobeSystem.addOrganelle(microbeEntity, q, r, rotation, organelle)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)

    local s = encodeAxial(q, r)
    if microbeComponent.organelles[s] then
        return false
    end
    microbeComponent.organelles[s] = organelle
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    -- Collision shape
    -- TODO: cache for performance
    local compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape)
    compoundShape:addChildShape(
        translation,
        Quaternion.new(Radian(0), Vector3(1,0,0)),
        organelle.collisionShape
    )
    rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass + organelle.mass
    rigidBodyComponent.properties:touch()
    
    organelle:onAddedToMicrobe(microbeEntity, q, r, rotation)
    
    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity)
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth + BANDWIDTH_PER_ORGANELLE -- Temporary solution for increasing max bandwidth
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth
    
    -- Send the organelles to the membraneComponent so that the membrane can "grow"
    local localQ = q - organelle.position.q
    local localR = r - organelle.position.r
    if organelle:getHex(localQ, localR) ~= nil then
        for _, hex in pairs(organelle._hexes) do
            local q = hex.q + organelle.position.q
            local r = hex.r + organelle.position.r
            local x, y = axialToCartesian(q, r)
            membraneComponent:sendOrganelles(x, y)
        end
        return organelle
    end
       
    return true
end

-- TODO: we have a similar method in procedural_microbes.lua and another one in microbe_editor.lua.
-- They probably should all use the same one.
function MicrobeSystem.validPlacement(microbeEntity, organelle, q, r)
    local touching = false;
    for s, hex in pairs(organelle._hexes) do
        
        local organelle = MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q, hex.r + r)
        if organelle then
            if organelle.name ~= "cytoplasm" then
                return false 
            end
        end
        
		if  MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 0, hex.r + r - 1) or
			MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 1, hex.r + r - 1) or
			MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 1, hex.r + r + 0) or
			MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q + 0, hex.r + r + 1) or
			MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q - 1, hex.r + r + 1) or
			MicrobeSystem.getOrganelleAt(microbeEntity, hex.q + q - 1, hex.r + r + 0) then
			touching = true;
		end
    end
    
    return touching
end

function MicrobeSystem.splitOrganelle(microbeEntity, organelle)
    local q = organelle.position.q
    local r = organelle.position.r

    --Spiral search for space for the organelle
    local radius = 1
    while true do
        --Moves into the ring of radius "radius" and center the old organelle
        q = q + HEX_NEIGHBOUR_OFFSET[HEX_SIDE.BOTTOM_LEFT][1]
        r = r + HEX_NEIGHBOUR_OFFSET[HEX_SIDE.BOTTOM_LEFT][2]

        --Iterates in the ring
        for side = 1, 6 do --necesary due to lua not ordering the tables.
            local offset = HEX_NEIGHBOUR_OFFSET[side]
            --Moves "radius" times into each direction
            for i = 1, radius do
                q = q + offset[1]
                r = r + offset[2]

                --Checks every possible rotation value.
                for j = 0, 5 do
                    local rotation = 360 * j / 6
                    local data = {["name"]=organelle.name, ["q"]=q, ["r"]=r, ["rotation"]=i*60}
                    local newOrganelle = OrganelleFactory.makeOrganelle(data)

                    if MicrobeSystem.validPlacement(microbeEntity, newOrganelle, q, r) then
                        print("placed " .. organelle.name .. " at " .. q .. " " .. r)
                        MicrobeSystem.addOrganelle(microbeEntity, q, r, i * 60, newOrganelle)
                        return newOrganelle
                    end
                end
            end
        end

        radius = radius + 1
    end
end

-- Disables or enabled engulfmode for a microbe, allowing or disallowed it to absorb other microbes
function MicrobeSystem.toggleEngulfMode(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)
    local soundSourceComponent = getComponent(microbeEntity, SoundSourceComponent)

    if microbeComponent.engulfMode then
        microbeComponent.movementFactor = microbeComponent.movementFactor * ENGULFING_MOVEMENT_DIVISION
        soundSourceComponent:stopSound("microbe-engulfment") -- Possibly comment out. If version > 0.3.2 delete. --> We're way past 0.3.2, do we still need this?
        rigidBodyComponent:reenableAllCollisions()
    else
        microbeComponent.movementFactor = microbeComponent.movementFactor / ENGULFING_MOVEMENT_DIVISION
    end

    microbeComponent.engulfMode = not microbeComponent.engulfMode
end

-- Kills the microbe, releasing stored compounds into the enviroment
function MicrobeSystem.kill(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)
    local soundSourceComponent = getComponent(microbeEntity, SoundSourceComponent)
    local microbeSceneNode = getComponent(microbeEntity, OgreSceneNodeComponent)

    -- Hacky but meh.
    if microbeComponent.dead then return end

    -- Releasing all the agents.
    for compoundId, _ in pairs(microbeComponent.specialStorageOrganelles) do
        local _amount = MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)
        while _amount > 0 do
            ejectedAmount = MicrobeSystem.takeCompound(microbeEntity, compoundId, 3) -- Eject up to 3 units per particle
            local direction = Vector3(math.random() * 2 - 1, math.random() * 2 - 1, 0)
            createAgentCloud(compoundId, microbeSceneNode.transform.position.x, microbeSceneNode.transform.position.y, direction, amountToEject)
            _amount = _amount - ejectedAmount
        end
    end

    local compoundsToRelease = {}
    -- Eject the compounds that was in the microbe
    for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
        local total = MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)
        local ejectedAmount = MicrobeSystem.takeCompound(microbeEntity, compoundId, total)
        compoundsToRelease[compoundId] = ejectedAmount
    end

    for _, organelle in pairs(microbeComponent.organelles) do
        for compoundName, amount in pairs(organelleTable[organelle.name].composition) do
            local compoundId = CompoundRegistry.getCompoundId(compoundName)
            if(compoundsToRelease[compoundId] == nil) then
                compoundsToRelease[compoundId] = amount * COMPOUND_RELEASE_PERCENTAGE
            else
                compoundsToRelease[compoundId] = compoundsToRelease[compoundId] + amount * COMPOUND_RELEASE_PERCENTAGE
            end
        end
    end

    -- TODO: make the compounds be released inside of the microbe and not in the back.
    for compoundId, amount in pairs(compoundsToRelease) do
        MicrobeSystem.ejectCompound(microbeEntity, compoundId, amount)
    end

    local deathAnimationEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
    local lifeTimeComponent = TimedLifeComponent.new()
    lifeTimeComponent.timeToLive = 4000
    deathAnimationEntity:addComponent(lifeTimeComponent)
    local deathAnimSceneNode = OgreSceneNodeComponent.new()
    deathAnimSceneNode.meshName = "MicrobeDeath.mesh"
    deathAnimSceneNode:playAnimation("Death", false)
    deathAnimSceneNode.transform.position = Vector3(microbeSceneNode.transform.position.x, microbeSceneNode.transform.position.y, 0)
    deathAnimSceneNode.transform:touch()
    deathAnimationEntity:addComponent(deathAnimSceneNode)
    soundSourceComponent:playSound("microbe-death")
    microbeComponent.dead = true
    microbeComponent.deathTimer = 5000
    microbeComponent.movementDirection = Vector3(0,0,0)
    rigidBodyComponent:clearForces()
    if not microbeComponent.isPlayerMicrobe then
        for _, organelle in pairs(microbeComponent.organelles) do
           organelle:removePhysics()
        end
    end
    if microbeComponent.wasBeingEngulfed then
        MicrobeSystem.removeEngulfedEffect(microbeEntity)
    end
    microbeSceneNode.visible = false
end

-- Damages the microbe, killing it if its hitpoints drop low enough
--
-- @param amount
--  amount of hitpoints to substract
function MicrobeSystem.damage(microbeEntity, amount, damageType)
    assert(damageType ~= nil, "Damage type is nil")
    assert(amount >= 0, "Can't deal negative damage. Use MicrobeSystem.heal instead")

    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local soundSourceComponent = getComponent(microbeEntity, SoundSourceComponent)
    
    if damageType == "toxin" then
        soundSourceComponent:playSound("microbe-toxin-damage")
    end
    
    -- Choose a random organelle or membrane to damage.
    -- TODO: CHANGE TO USE AGENT CODES FOR DAMAGE.
    local rand = math.random(1, microbeComponent.maxHitpoints/MICROBE_HITPOINTS_PER_ORGANELLE)
    local i = 1
    for _, organelle in pairs(microbeComponent.organelles) do
        -- If this is the organelle we have chosen...
        if i == rand then
            -- Deplete its health/compoundBin.
            organelle:damageOrganelle(amount)
        end
        i = i + 1
    end
    
    -- Find out the amount of health the microbe has.
    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity)
    
    if microbeComponent.hitpoints <= 0 then
        microbeComponent.hitpoints = 0
        MicrobeSystem.kill(microbeEntity)
    end
end

-- Damage the microbe if its too low on ATP.
function MicrobeSystem.atpDamage(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)

    if MicrobeSystem.getCompoundAmount(microbeEntity, CompoundRegistry.getCompoundId("atp")) < 1.0 then
        -- TODO: put this on a GUI notification.
        --[[
        if microbeComponent.isPlayerMicrobe and not self.playerAlreadyShownAtpDamage then
            self.playerAlreadyShownAtpDamage = true
            showMessage("No ATP hurts you!")
        end
        ]]
        MicrobeSystem.damage(microbeEntity, EXCESS_COMPOUND_COLLECTION_INTERVAL * 0.000002  * microbeComponent.maxHitpoints, "atpDamage") -- Microbe takes 2% of max hp per second in damage
    end
end

-- Drains an agent from the microbes special storage and emits it
--
-- @param compoundId
-- The compound id of the agent to emit
--
-- @param maxAmount
-- The maximum amount to try to emit
function MicrobeSystem.emitAgent(microbeEntity, compoundId, maxAmount)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local sceneNodeComponent = getComponent(microbeEntity, OgreSceneNodeComponent)
    local soundSourceComponent = getComponent(microbeEntity, SoundSourceComponent)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)

    -- Cooldown code
    if microbeComponent.agentEmissionCooldown > 0 then return end
    local numberOfAgentVacuoles = microbeComponent.specialStorageOrganelles[compoundId]
    
    -- Only shoot if you have agent vacuoles.
    if numberOfAgentVacuoles == nil or numberOfAgentVacuoles == 0 then return end

    -- The cooldown time is inversely proportional to the amount of agent vacuoles.
    microbeComponent.agentEmissionCooldown = AGENT_EMISSION_COOLDOWN / numberOfAgentVacuoles

    if MicrobeSystem.getCompoundAmount(microbeEntity, compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT then
        soundSourceComponent:playSound("microbe-release-toxin")

        -- Calculate the emission angle of the agent emitter
        local organelleX, organelleY = axialToCartesian(0, -1) -- The front of the microbe
        local membraneCoords = membraneComponent:getExternOrganellePos(organelleX, organelleY)

        local angle =  math.atan2(organelleY, organelleX)
        if (angle < 0) then
            angle = angle + 2*math.pi
        end
        angle = -(angle * 180/math.pi -90 ) % 360
        --angle = angle * 180/math.pi

        -- Find the direction the microbe is facing
        local yAxis = sceneNodeComponent.transform.orientation:yAxis()
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
        local amountToEject = MicrobeSystem.takeCompound(microbeEntity, compoundId, maxAmount/10.0)
        createAgentCloud(compoundId, sceneNodeComponent.transform.position.x + xnew, sceneNodeComponent.transform.position.y + ynew, direction, amountToEject * 10)
    end
end

function MicrobeSystem.transferCompounds(fromEntity, toEntity)
    for _, compoundID in pairs(CompoundRegistry.getCompoundList()) do
        local amount = MicrobeSystem.getCompoundAmount(fromEntity, compoundID)
    
        if amount ~= 0 then
            MicrobeSystem.takeCompound(fromEntity, compoundID, amount, false)
            MicrobeSystem.storeCompound(toEntity, compoundID, amount, false)
        end
    end
end

-- Creates a new microbe with all required components
--
-- @param name
-- The entity's name. If nil, the entity will be unnamed.
--
-- @returns microbe
-- An object of type Microbe

function MicrobeSystem.createMicrobeEntity(name, aiControlled, speciesName, in_editor)
    assert(isNotEmpty(speciesName))

    local entity
    if name then
        entity = Entity.new(name, g_luaEngine.currentGameState.wrapper)
    else
        entity = Entity.new(g_luaEngine.currentGameState.wrapper)
    end

    local rigidBody = RigidBodyComponent.new()
    rigidBody.properties.shape = CompoundShape.new()
    rigidBody.properties.linearDamping = 0.5
    rigidBody.properties.friction = 0.2
    rigidBody.properties.mass = 0.0
    rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    rigidBody.properties:touch()

    local reactionHandler = CollisionComponent.new()
    reactionHandler:addCollisionGroup("microbe")

    local membraneComponent = MembraneComponent.new()
    
    local soundComponent = SoundSourceComponent.new()
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
    s1.properties.volume = 0.1
    s1.properties:touch()
    s1 = soundComponent:addSound("microbe-movement-2", "soundeffects/microbe-movement-3.ogg")
    s1.properties.volume = 0.4
    s1.properties:touch()

    local components = {
        CompoundAbsorberComponent.new(),
        OgreSceneNodeComponent.new(),
        CompoundBagComponent.new(),
        MicrobeComponent.new(not aiControlled, speciesName),
        reactionHandler,
        rigidBody,
        soundComponent,
        membraneComponent
    }

    if aiControlled then
        local aiController = MicrobeAIControllerComponent.new()
        table.insert(components, aiController)
    end

    for _, component in ipairs(components) do
        entity:addComponent(component)
    end
    
    MicrobeSystem.initializeMicrobe(entity, in_editor, g_luaEngine.currentGameState)

    return entity
end

function MicrobeSystem.calculateStorageSpace(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)

    microbeComponent.stored = 0
    for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
        microbeComponent.stored = microbeComponent.stored + MicrobeSystem.getCompoundAmount(microbeEntity, compoundId)
    end
end

-- Private function for updating the compound absorber
--
-- Toggles the absorber on and off depending on the remaining storage
-- capacity of the storage organelles.
function MicrobeSystem.updateCompoundAbsorber(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local compoundAbsorberComponent = getComponent(microbeEntity, CompoundAbsorberComponent)

    if --microbeComponent.stored >= microbeComponent.capacity or 
               microbeComponent.remainingBandwidth < 1 or 
               microbeComponent.dead then
        compoundAbsorberComponent:disable()
    else
        compoundAbsorberComponent:enable()
    end
end

function MicrobeSystem.divide(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local soundSourceComponent = getComponent(microbeEntity, SoundSourceComponent)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)

    -- Create the two daughter cells.
    local copyEntity = MicrobeSystem.createMicrobeEntity(nil, true, microbeComponent.speciesName, false)
    local microbeComponentCopy = getComponent(copyEntity, MicrobeComponent)
    local rigidBodyComponentCopy = getComponent(copyEntity, RigidBodyComponent)

    --Separate the two cells.
    rigidBodyComponentCopy.dynamicProperties.position = Vector3(rigidBodyComponent.dynamicProperties.position.x - membraneComponent.dimensions/2, rigidBodyComponent.dynamicProperties.position.y, 0)
    rigidBodyComponent.dynamicProperties.position = Vector3(rigidBodyComponent.dynamicProperties.position.x + membraneComponent.dimensions/2, rigidBodyComponent.dynamicProperties.position.y, 0)
    
    -- Split the compounds evenly between the two cells.
    for _, compoundID in pairs(CompoundRegistry.getCompoundList()) do
        local amount = MicrobeSystem.getCompoundAmount(microbeEntity, compoundID)
    
        if amount ~= 0 then
            MicrobeSystem.takeCompound(microbeEntity, compoundID, amount / 2, false)
            MicrobeSystem.storeCompound(copyEntity, compoundID, amount / 2, false)
        end
    end
    
    microbeComponent.reproductionStage = 0
    microbeComponentCopy.reproductionStage = 0

    local spawnedComponent = SpawnedComponent.new()
    spawnedComponent:setSpawnRadius(MICROBE_SPAWN_RADIUS)
    copyEntity:addComponent(spawnedComponent)
    soundSourceComponent:playSound("microbe-reproduction")
end

-- Copies this microbe. The new microbe will not have the stored compounds of this one.
function MicrobeSystem.readyToReproduce(microbeEntity)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)

    if microbeComponent.isPlayerMicrobe then
        showReproductionDialog()
        microbeComponent.reproductionStage = 0
    else
        -- Return the first cell to its normal, non duplicated cell arangement.
        SpeciesSystem.template(microbeEntity, MicrobeSystem.getSpeciesComponent(microbeEntity))
        MicrobeSystem.divide(microbeEntity)
    end
end

-- Updates the microbe's state
function MicrobeSystem.updateMicrobe(microbeEntity, logicTime)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local membraneComponent = getComponent(microbeEntity, MembraneComponent)
    local sceneNodeComponent = getComponent(microbeEntity, OgreSceneNodeComponent)
    local compoundAbsorberComponent = getComponent(microbeEntity, CompoundAbsorberComponent)
    local compoundBag = getComponent(microbeEntity, CompoundBagComponent)

    if not microbeComponent.dead then
        -- Recalculating agent cooldown time.
        microbeComponent.agentEmissionCooldown = math.max(microbeComponent.agentEmissionCooldown - logicTime, 0)

        --calculate storage.
        MicrobeSystem.calculateStorageSpace(microbeEntity)

        compoundBag.storageSpace = microbeComponent.capacity

        -- StorageOrganelles
        MicrobeSystem.updateCompoundAbsorber(microbeEntity)
        -- Regenerate bandwidth
        MicrobeSystem.regenerateBandwidth(microbeEntity, logicTime)
        -- Attempt to absorb queued compounds
        for _, compound in pairs(compoundAbsorberComponent:getAbsorbedCompounds()) do 
            local amount = compoundAbsorberComponent:absorbedCompoundAmount(compound)
            if amount > 0.0 then
                MicrobeSystem.storeCompound(microbeEntity, compound, amount, true)
            end
        end
        -- Flash membrane if something happens.
        if microbeComponent.flashDuration ~= nil and microbeComponent.flashColour ~= nil then
            microbeComponent.flashDuration = microbeComponent.flashDuration - logicTime
            
            local entity = membraneComponent.entity
            -- How frequent it flashes, would be nice to update the flash function to have this variable
            if math.fmod(microbeComponent.flashDuration, 600) < 300 then
                entity:tintColour("Membrane", microbeComponent.flashColour)
            else
                entity:setMaterial(sceneNodeComponent.meshName)
            end
            
            if microbeComponent.flashDuration <= 0 then
                microbeComponent.flashDuration = nil				
                entity:setMaterial(sceneNodeComponent.meshName)
            end
        end
        
        microbeComponent.compoundCollectionTimer = microbeComponent.compoundCollectionTimer + logicTime
        while microbeComponent.compoundCollectionTimer > EXCESS_COMPOUND_COLLECTION_INTERVAL do
            -- For every COMPOUND_DISTRIBUTION_INTERVAL passed

            microbeComponent.compoundCollectionTimer = microbeComponent.compoundCollectionTimer - EXCESS_COMPOUND_COLLECTION_INTERVAL

            MicrobeSystem.purgeCompounds(microbeEntity)

            MicrobeSystem.atpDamage(microbeEntity)
        end
        
        -- First organelle run: updates all the organelles and heals the broken ones.
        if microbeComponent.hitpoints < microbeComponent.maxHitpoints then
            for _, organelle in pairs(microbeComponent.organelles) do
                -- Update the organelle.
                organelle:update(logicTime)
                
                -- If the organelle is hurt.
                if organelle:getCompoundBin() < 1.0 then
                    -- Give the organelle access to the compound bag to take some compound.
                    organelle:growOrganelle(getComponent(microbeEntity, CompoundBagComponent), logicTime)
                    -- An organelle was damaged and we tried to heal it, so out health might be different.
                    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity)
                end
            end
        else

            local reproductionStageComplete = true
            local organellesToAdd = {}

            -- Grow all the large organelles.
            for _, organelle in pairs(microbeComponent.organelles) do
                -- Update the organelle.
                organelle:update(logicTime)
                
                -- We are in G1 phase of the cell cycle, duplicate all organelles.
                if organelle.name ~= "nucleus" and microbeComponent.reproductionStage == 0 then
                    
                    -- If the organelle is not split, give it some compounds to make it larger.
                    if organelle:getCompoundBin() < 2.0 and not organelle.wasSplit then
                        -- Give the organelle access to the compound bag to take some compound.
                        organelle:growOrganelle(getComponent(microbeEntity, CompoundBagComponent), logicTime)
                        reproductionStageComplete = false
                    -- If the organelle was split and has a bin less then 1, it must have been damaged.
                    elseif organelle:getCompoundBin() < 1.0 and organelle.wasSplit then
                        -- Give the organelle access to the compound bag to take some compound.
                        organelle:growOrganelle(getComponent(microbeEntity, CompoundBagComponent), logicTime)
                    -- If the organelle is twice its size...
                    elseif organelle:getCompoundBin() >= 2.0 then
                        --Queue this organelle for splitting after the loop.
                        --(To avoid "cutting down the branch we're sitting on").
                        table.insert(organellesToAdd, organelle)
                    end
                   
                -- In the S phase, the nucleus grows as chromatin is duplicated.
                elseif organelle.name == "nucleus" and microbeComponent.reproductionStage == 1 then
                    -- If the nucleus hasn't finished replicating its DNA, give it some compounds.
                    if organelle:getCompoundBin() < 2.0 then
                        -- Give the organelle access to the compound back to take some compound.
                        organelle:growOrganelle(getComponent(microbeEntity, CompoundBagComponent), logicTime)
                        reproductionStageComplete = false
                    end
                end

            end

            --Splitting the queued organelles.
            for _, organelle in pairs(organellesToAdd) do
                print("ready to split " .. organelle.name)
                -- Mark this organelle as done and return to its normal size.
                organelle:reset()
                organelle.wasSplit = true
                -- Create a second organelle.
                local organelle2 = MicrobeSystem.splitOrganelle(microbeEntity, organelle)
                organelle2.wasSplit = true
                organelle2.isDuplicate = true
                organelle2.sisterOrganelle = organelle

                -- Redo the cell membrane.
                membraneComponent:clear()
            end

            if reproductionStageComplete and microbeComponent.reproductionStage < 2 then
                microbeComponent.reproductionStage = microbeComponent.reproductionStage + 1
            end
            
            -- To finish the G2 phase we just need more than a threshold of compounds.
            if microbeComponent.reproductionStage == 2 or microbeComponent.reproductionStage == 3 then
                MicrobeSystem.readyToReproduce(microbeEntity)
            end
        end

        if microbeComponent.engulfMode then
            -- Drain atp and if we run out then disable engulfmode
            local cost = ENGULFING_ATP_COST_SECOND/1000*logicTime
            
            if MicrobeSystem.takeCompound(microbeEntity, CompoundRegistry.getCompoundId("atp"), cost) < cost - 0.001 then
                print ("too little atp, disabling - 749")
                MicrobeSystem.toggleEngulfMode(microbeEntity)
            end
            -- Flash the membrane blue.
            MicrobeSystem.flashMembraneColour(microbeEntity, 3000, ColourValue(0.2,0.5,1.0,0.5))
        end
        if microbeComponent.isBeingEngulfed and microbeComponent.wasBeingEngulfed then
            MicrobeSystem.damage(microbeEntity, logicTime * 0.000025  * microbeComponent.maxHitpoints, "isBeingEngulfed - Microbe:update()s")
        -- Else If we were but are no longer, being engulfed
        elseif microbeComponent.wasBeingEngulfed then
            MicrobeSystem.removeEngulfedEffect(microbeEntity)
        end
        -- Used to detect when engulfing stops
        microbeComponent.isBeingEngulfed = false;
        compoundAbsorberComponent:setAbsorbtionCapacity(math.min(microbeComponent.capacity - microbeComponent.stored + 10, microbeComponent.remainingBandwidth))
    else
        microbeComponent.deathTimer = microbeComponent.deathTimer - logicTime
        microbeComponent.flashDuration = 0
        if microbeComponent.deathTimer <= 0 then
            if microbeComponent.isPlayerMicrobe == true then
                MicrobeSystem.respawnPlayer()
            else
                for _, organelle in pairs(microbeComponent.organelles) do
                    organelle:onRemovedFromMicrobe()
                end
                microbeEntity:destroy()
            end
        end
    end
end

-- Microbe entity initializer
--
-- Requires all necessary components (see MICROBE_COMPONENTS) to be present in
-- the entity.
--
-- @param entity
-- The entity this microbe wraps
function MicrobeSystem.initializeMicrobe(microbeEntity, in_editor)
    -- Checking if the entity exists.
    assert(microbeEntity ~= nil)

    -- Checking if all the components are there.
    for key, ctype in pairs(MICROBE_COMPONENTS) do
        local component = getComponent(microbeEntity, ctype)
        assert(component ~= nil, "Can't create microbe from this entity, it's missing " .. key)
    end

    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    local compoundAbsorberComponent = getComponent(microbeEntity, CompoundAbsorberComponent)
    local compoundBag = getComponent(microbeEntity, CompoundBagComponent)
    local rigidBodyComponent = getComponent(microbeEntity, RigidBodyComponent)
    local sceneNodeComponent = getComponent(microbeEntity, OgreSceneNodeComponent)

    -- Allowing the microbe to absorb all the compounds.
    for _, compound in pairs(CompoundRegistry.getCompoundList()) do
        compoundAbsorberComponent:setCanAbsorbCompound(compound, true)
    end

    if not microbeComponent.initialized then
        -- TODO: cache for performance
        local compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape)
        assert(compoundShape ~= nil)
        compoundShape:clear()
        rigidBodyComponent.properties.mass = 0.0

        -- Organelles
        for s, organelle in pairs(microbeComponent.organelles) do
            organelle:onAddedToMicrobe(microbeEntity, organelle.position.q, organelle.position.r, organelle.rotation)   
            organelle:reset()
            rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass + organelle.mass
        end

        -- Membrane
        sceneNodeComponent.meshName = "membrane_" .. microbeComponent.speciesName
        rigidBodyComponent.properties:touch()
        microbeComponent.initialized = true
        
        if in_editor ~= true then
            assert(microbeComponent.speciesName)
            
            local processor = getComponent(microbeComponent.speciesName,
                                           g_luaEngine.currentGameState,
                                           ProcessorComponent)
            
            if processor == nil then
                print("Microbe species '" .. microbeComponent.speciesName .. "' doesn't exist")
                assert(processor)
            end

            assert(isNotEmpty(microbeComponent.speciesName))
            compoundBag:setProcessor(processor, microbeComponent.speciesName)
            
            SpeciesSystem.template(microbeEntity, MicrobeSystem.getSpeciesComponent(microbeEntity))
        end
    end
    MicrobeSystem.updateCompoundAbsorber(microbeEntity)
end
