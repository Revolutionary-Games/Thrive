-- BacteriaComponent: bacterial analogue to MicrobeComponent

BacteriaComponent = class(
	function(self, speciesName, health)
		self.speciesName = speciesName
		self.invincibility_timer = 0
		self.health = health
	end
)

BacteriaComponent.TYPE_NAME = "BacteriaComponent"

function BacteriaComponent:load(storage)
	self.speciesName = storage:get("speciesName", "Rickettsia")
	self.invincibility_timer = storage:get("invincibility_timer", 0)
	self.health = storage:get("health", 1)
end

function BacteriaComponent:storage()
	storage = StorageContainer.new()
	storage:set("speciesName", self.speciesName)
	storage:set("invincibility_timer", self.invincibility_timer)
	storage:set("health", self.health)
	return storage
end

REGISTER_COMPONENT("BacteriaComponent", BacteriaComponent)


function initBacterialSpecies(gameState)
	for name, data in pairs(bacteriaTable) do
		local speciesEntity = Entity.new(name, gameState.wrapper)
		local processorComponent = ProcessorComponent.new()
		for process, capacity in pairs(data.processes) do
			processorComponent:setCapacity(BioProcessRegistry.getId(process), capacity)
		end
		speciesEntity:addComponent(processorComponent)
	end
end

-- Bacterium: Entity wrapper for a bacterium

Bacterium = class(
	function(self, entity)
		self.entity = entity
		local all_components_available = true
		for key, cType in pairs(Bacterium.COMPONENTS) do
			local component = getComponent(entity, cType)
			if component == nil then
                print("entity is missing component: " .. key)
                all_components_available = false
            end
			self[key] = component
		end
        assert(all_components_available, "Can't create bacterium from this entity")
	end
)

Bacterium.COMPONENTS = {
	compoundBag = CompoundBagComponent,
	bacterium = BacteriaComponent,
	rigidBody = RigidBodyComponent,
	sceneNode = OgreSceneNodeComponent,
	collisionHandler = CollisionComponent,
}

function Bacterium.createBacterium(speciesName, pos, gameState)
	-- print("BS:"..speciesName)
	local entity = Entity.new(gameState.wrapper)
	local species_data = bacteriaTable[speciesName]
	local bacteriaComponent = BacteriaComponent.new(speciesName,
							bacteriaTable[speciesName].health)

	local rigidBody = RigidBodyComponent.new()
	rigidBody.properties.shape = SphereShape.new(HEX_SIZE)
	rigidBody.properties.mass = bacteriaTable[speciesName].mass
	rigidBody.properties.friction = 0.2
	rigidBody.properties.linearDamping = 0.8
	rigidBody:setDynamicProperties(
		pos,
		Quaternion.new(Radian.new(Degree(math.random()*360)), Vector3(0, 0, 1)),
		Vector3(0, 0, 0),
		Vector3(0, 0, 0)
	)
	rigidBody.properties:touch()

	local sceneNode = OgreSceneNodeComponent.new()
	sceneNode.meshName = species_data.mesh
	sceneNode.visible = true
	sceneNode.transform.scale = Vector3(1, 1, 1)
	sceneNode.transform.position = pos
	sceneNode.transform:touch()

	local compoundBag = CompoundBagComponent.new()
	compoundBag.storageSpace = species_data.capacity
	for compound, amount in pairs(species_data.compounds) do
		compoundBag:giveCompound(CompoundRegistry.getCompoundId(compound), amount)
	end
	compoundBag:setProcessor(getComponent(speciesName, gameState, ProcessorComponent))

	local reactionHandler = CollisionComponent.new()
	reactionHandler:addCollisionGroup("bacteria")

	local components = {
		bacteriaComponent,
		rigidBody,
		sceneNode,
		compoundBag,
		reactionHandler,
	}

	for _, component in ipairs(components) do
		entity:addComponent(component)
	end

	local bacterium = Bacterium(entity)
	return entity
end

function Bacterium:purgeCompounds()
    local compoundAmountToDump = self.compoundBag:getStorageSpaceUsed() - self.compoundBag.storageSpace
    
    -- if compoundAmountToDump > 0 then print("BD:"..compoundAmountToDump) end

    -- Dumping all the useless compounds (with price = 0).
    for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
        local price = self.compoundBag:getPrice(compoundId)
        if price <= 0 then
            local amount = self.compoundBag:getCompoundAmount(compoundId)
            if amount > 0 then
            	local amountToEject = self.compoundBag:takeCompound(compoundId, amount)
            	self:ejectCompound(compoundId, amountToEject)
            end
        end
    end

    if compoundAmountToDump > 0 then
        --Calculating each compound price to dump proportionally.
        local compoundPrices = {}
        local priceSum = 0
        for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
            local amount = getComponent(self.entity, CompoundBagComponent)
                :getCompoundAmount(compoundId)

            if amount > 0 then
                local price = self.compoundBag:getPrice(compoundId)
                compoundPrices[compoundId] = price
                priceSum = priceSum + price
            end
        end

        --Dumping each compound according to it's price.
        for compoundId, price in pairs(compoundPrices) do
            local amountToEject = compoundAmountToDump * price / priceSum
            if amount > 0 then amountToEject = self.compoundBag:takeCompound(compoundId, amountToEject) end
            if amount > 0 then self:ejectCompound(compoundId, amountToEject) end
        end
    end
end

function Bacterium:ejectCompound(compoundId, amount)
    createCompoundCloud(CompoundRegistry.getCompoundInternalName(compoundId),
                        self.sceneNode.transform.position.x,
                        self.sceneNode.transform.position.y,
                        amount * 5000)
end

Bacterium.INVINCIBILITY_TIME = 250

function Bacterium:damage(amount)
	if self.bacterium.invincibility_timer > 0 then return end
	self.bacterium.health = self.bacterium.health - amount
	if self.bacterium.health < 0 then
		self:kill()
	end
	self.bacterium.invincibility_timer = Bacterium.INVINCIBILITY_TIME
end

function Bacterium:kill()
    local compoundsToRelease = {}

    for _, compoundId in pairs(CompoundRegistry.getCompoundList()) do
        local total = self.compoundBag:getCompoundAmount(compoundId)
        local ejectedAmount = self.compoundBag:takeCompound(compoundId, total)
        compoundsToRelease[compoundId] = ejectedAmount
    end

    -- todo: add compounds locked in bacterium

    for compoundId, amount in pairs(compoundsToRelease) do
        self:ejectCompound(compoundId, amount)
    end

    -- Releasing the organelle if needed.
    local speciesName = self.bacterium.speciesName
    if bacteriaTable[speciesName].organelle ~= nil then
        local organelleName = bacteriaTable[speciesName].organelle
        
        -- Creating a powerup entity
        local powerupEntity = Entity.new(g_luaEngine.currentGameState.wrapper)
        setSpawnablePhysics(powerupEntity, self.sceneNode.transform.position,
                            organelleTable[organelleName].mesh, 0.9, SphereShape.new(HEX_SIZE))

        local reactionHandler = CollisionComponent.new()
        reactionHandler:addCollisionGroup("powerup")
        powerupEntity:addComponent(reactionHandler)
        
        local powerupComponent = PowerupComponent.new()
        -- Function name must be in configs.lua
        powerupComponent:setEffect(organelleName .. "Effect")
        powerupEntity:addComponent(powerupComponent)
    end

    self.entity:destroy()
end

function Bacterium:update(logicTime)
	self:purgeCompounds()
	if self.bacterium.invincibility_timer > 0 then
		self.bacterium.invincibility_timer = self.bacterium.invincibility_timer - logicTime
	end
end

-- BacteriaSystem

BacteriaSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)

        self.entities = EntityFilter.new(
            {
                BacteriaComponent,
                OgreSceneNodeComponent,
                RigidBodyComponent,
                CollisionComponent
            },
            true
        )
        
        self.bacteria = {}
    end
)

function BacteriaSystem:init(gameState)
    LuaSystem.init(self, "BacteriaSystem", gameState)
    self.entities:init(gameState.wrapper)
end


function BacteriaSystem:shutdown()
    LuaSystem.shutdown(self)
    self.entities:shutdown()
end


function BacteriaSystem:update(renderTime, logicTime)
    for _, entityId in pairs(self.entities:removedEntities()) do
        self.bacteria[entityId] = nil
    end
    for _, entityId in pairs(self.entities:addedEntities()) do
        local bacterium = Bacterium(Entity.new(entityId, self.gameState.wrapper))
        self.bacteria[entityId] = bacterium
    end
    self.entities:clearChanges()
    for _, bacterium in pairs(self.bacteria) do
        bacterium:update(logicTime)
    end
end



