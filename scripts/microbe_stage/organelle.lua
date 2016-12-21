-- Base class for microbe organelles
class 'Organelle'

-- Factory function for organelles
function Organelle.loadOrganelle(storage)
    local organelle = Organelle(0.1)
    organelle:load(storage)
    return organelle
end


-- Constructor
function Organelle:__init(mass, name)
    self.entity = Entity()
    self.entity:setVolatile(true)
    self.sceneNode = self.entity:getOrCreate(OgreSceneNodeComponent)
	self.sceneNode.transform.scale = Vector3(HEX_SIZE,HEX_SIZE,HEX_SIZE)
    self.collisionShape = CompoundShape()
    self.mass = mass
    self.components = {}
    self._hexes = {}
    self.position = {
        q = 0,
        r = 0
    }
    self.rotation = nil

    --Naming the organelle.
    if name == nil then
        self.name = "<nameless>"
    else
        self.name = name
    end
end


-- Adds a hex to this organelle
--
-- @param q, r
--  Axial coordinates of the new hex
--
-- @returns success
--  True if the hex could be added, false if there already is a hex at (q,r)
function Organelle:addHex(q, r)
    assert(not self.microbe, "Cannot change organelle shape while it is in a microbe")
    local s = encodeAxial(q, r)
    if self._hexes[s] then
        return false
    end
    local hex = {
        q = q,
        r = r,
        collisionShape = SphereShape(3.0)
    }
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    
    -- Collision shape
    self.collisionShape:addChildShape(
        translation,
        Quaternion(Radian(0), Vector3(1,0,0)),
        hex.collisionShape
    )
    self._hexes[s] = hex
    return true
end


-- Retrieves a hex
--
-- @param q, r
--  Axial coordinates of the hex
--
-- @returns hex
--  The hex at (q, r) or nil if there's no hex at that position
function Organelle:getHex(q, r)
    local s = encodeAxial(q, r)
    return self._hexes[s]
end


function Organelle:load(storage)
    local hexes = storage:get("hexes", {})
    for i = 1,hexes:size() do
        local hexStorage = hexes:get(i)
        local q = hexStorage:get("q", 0)
        local r = hexStorage:get("r", 0)
        self:addHex(q, r)
    end
    self.position.q = storage:get("q", 0)
    self.position.r = storage:get("r", 0)
    self.mass = storage:get("mass", 0.1)
    self.rotation = storage:get("rotation", 0)
    self.name = storage:get("name", "<nameless>")

    --loading all of the components
    local componentStorage = storage:get("componentStorage", {})
    for i = 1, componentStorage:size() do
        local componentData = componentStorage:get(i)
        local componentName = componentData:get("name", "i dunno")
        local componentType = _G[componentName]
        local newComponent = componentType(nil, nil)
        newComponent:load(componentData)
        self.components[componentName] = newComponent
    end
end


-- Called by a microbe when this organelle has been added to it
--
-- @param microbe
--  The organelle's new owner
--
-- @param q, r
--  Axial coordinates of the organelle's center
function Organelle:onAddedToMicrobe(microbe, q, r, rotation)
    --iterating on each OrganelleComponent
    for _, component in pairs(self.components) do
        component:onAddedToMicrobe(microbe, q, r, rotation)
    end

    self.microbe = microbe
    self.position.q = q
    self.position.r = r
    self.rotation = rotation
	
	local offset = Vector3(0,0,0)
	local count = 0
	for _, hex in pairs(self.microbe:getOrganelleAt(q, r)._hexes) do
		count = count + 1
		
		local x, y = axialToCartesian(hex.q, hex.r)
		offset = offset + Vector3(x,y,0)
	end
	offset = offset/count
    
    -- Will cause the color of the organelle to update.
    self.flashDuration = 0
    if microbe:getSpeciesComponent() ~= nil then
        local colorAsVec = microbe:getSpeciesComponent().colour
        self.colour = ColourValue(colorAsVec.x, colorAsVec.y, colorAsVec.z, 1.0)
    else
        self.colour = ColourValue(1, 1, 1, 1)
    end
	
	self.organelleEntity = Entity()
    local sceneNode = OgreSceneNodeComponent()
    self.sceneNode = sceneNode
    sceneNode.parent = self.entity

    --Adding a mesh to the organelle.
    local mesh = organelleTable[self.name].mesh
    if mesh ~= nil then
        sceneNode.meshName = mesh
    end

	if self.name == "nucleus"  then
		offset = Vector3(0,0,0)
		-- TODO: Add specific nucleus animation here.
	elseif self.name == "flagellum" then -- Add all movement organelles here.
		sceneNode:playAnimation("Move", true)
		sceneNode:setAnimationSpeed(0.25)
		local organelleX, organelleY = axialToCartesian(q, r)
		local nucleusX, nucleusY = axialToCartesian(0, 0)
		local deltaX = nucleusX - organelleX
		local deltaY = nucleusY - organelleY
		local angle = math.atan2(deltaY, deltaX)
		if (angle < 0) then
			angle = angle + 2*math.pi
		end
		angle = (angle * 180/math.pi + 180) % 360
		self.rotation = angle;
	elseif self.name == "mitochondrion" or self.name == "chloroplast" then -- When all organelles except the above have animations this should just be an else statement
		--sceneNode:playAnimation("Float", true)
		--sceneNode:setAnimationSpeed(0.25)
	end
	sceneNode.transform.orientation = Quaternion(Radian(Degree(self.rotation)), Vector3(0, 0, 1))
	sceneNode.transform.position = offset
    sceneNode.transform.scale = Vector3(1, 1, 1)
    sceneNode.transform:touch()
    self.microbe.entity:addChild(self.organelleEntity)
    self.organelleEntity:addComponent(sceneNode)
	self.organelleEntity.sceneNode = sceneNode
	self.organelleEntity:setVolatile(true)
end

function Organelle:setAnimationSpeed()
    sceneNode:setAnimationSpeed(0.25)
end

-- Called by a microbe when this organelle has been removed from it
--
-- @param microbe
--  The organelle's previous owner
function Organelle:onRemovedFromMicrobe(microbe)
    --iterating on each OrganelleComponent
    for _, component in pairs(self.components) do
        component:onRemovedFromMicrobe(microbe)
    end

    self:destroy()
	self.microbe = nil
    self.position.q = 0
    self.position.r = 0
    self.rotation = 0
end


-- Removes a hex from this organelle
--
-- @param q,r
--  Axial coordinates of the hex to remove
--
-- @returns success
--  True if the hex could be removed, false if there's no hex at (q,r)
function Organelle:removeHex(q, r)
    assert(not self.microbe, "Cannot change organelle shape while it is in a microbe")
    local s = encodeAxial(q, r)
    local hex = table.remove(self._hexes, s)
    if hex then
        self.collisionShape:removeChildShape(hex.collisionShape)
        return true
    else
        return false
    end
end

function Organelle:destroy()
	self.organelleEntity:destroy()
    self.entity:destroy()
end

function Organelle:flashColour(duration, colour)
	if self.flashDuration == nil then
        self.colour = colour
        self.flashDuration = duration
    end
end

function Organelle:storage()
    local storage = StorageContainer()
    local hexes = StorageList()
    for _, hex in pairs(self._hexes) do
        hexStorage = StorageContainer()
        hexStorage:set("q", hex.q)
        hexStorage:set("r", hex.r)
        hexes:append(hexStorage)
    end
    storage:set("hexes", hexes)
    storage:set("name", self.name)
    storage:set("q", self.position.q)
    storage:set("r", self.position.r)
    storage:set("rotation", self.rotation)
    storage:set("mass", self.mass)
    --Serializing these causes some minor issues and doesn't serve a purpose anyway
    --storage:set("externalEdgeColour", self._externalEdgeColour)

    --iterating on each OrganelleComponent
    local componentStorage = StorageList()
    for componentName, component in pairs(self.components) do
        local componentData = component:storage(storage)
        componentData:set("name", componentName)
        componentStorage:append(componentData)
    end
    storage:set("componentStorage", componentStorage)

    return storage
end


-- Called by Microbe:update
--
-- Override this to make your organelle class do something at regular intervals
--
-- @param logicTime
--  The time since the last call to update()
function Organelle:update(microbe, logicTime)
    --iterating on each OrganelleComponent
    for _, component in pairs(self.components) do
        component:update(microbe, self, logicTime)
    end

	if self.flashDuration ~= nil and self.sceneNode.entity ~= nil 
        and (self.name == "mitochondrion" or self.name == "nucleus" or self.name == "ER" or self.name == "golgi") then
        
        self.flashDuration = self.flashDuration - logicTime
        local speciesColour = microbe:getSpeciesComponent().colour
		
		local entity = self.sceneNode.entity
		-- How frequent it flashes, would be nice to update the flash function to have this variable
		if math.fmod(self.flashDuration,600) < 300 then
            entity:tintColour(self.name, self.colour)
		else
			entity:setMaterial(self.name .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256))
		end
		
        if self.flashDuration <= 0 then
            self.flashDuration = nil
			entity:setMaterial(self.name .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256))
        end
    end
end


function Organelle:removePhysics()
    self.collisionShape:clear()
end

-- The basic organelle maker
class 'OrganelleFactory'

-- Sets the color of the organelle
function OrganelleFactory.setColour(sceneNode, colour)
	sceneNode.entity:setColour(colour)
end

function OrganelleFactory.makeOrganelle(data)
    if not (data.name == "" or data.name == nil) then
        --retrieveing the organelle info from the table
        local organelleInfo = organelleTable[data.name]

        --creating an empty organelle
        local organelle = Organelle(organelleInfo.mass, data.name)

        --adding all of the components.
        for componentName, arguments in pairs(organelleInfo.components) do
            local componentType = _G[componentName]
            organelle.components[componentName] = componentType:__init(arguments, data)
        end

        --getting the hex table of the organelle rotated by the angle
        local hexes = OrganelleFactory.checkSize(data)

        --adding the hexes to the organelle
        for _, hex in pairs(hexes) do
            organelle:addHex(hex.q, hex.r)
        end

        --setting the organelle colour

        return organelle
    end
end

-- Draws the hexes and uploads the models in the editor
function OrganelleFactory.renderOrganelles(data)
	if data.name == "remove" then
		return {}
	else
        --Getting the list hexes occupied by this organelle.
        occupiedHexList = OrganelleFactory.checkSize(data)

        --Used to get the average x and y values.
        local xSum = 0
        local ySum = 0

        --Rendering a cytoplasm in each of those hexes.
        --Note: each scenenode after the first one is considered a cytoplasm by the engine automatically.
        local i = 2
        for _, hex in pairs(occupiedHexList) do
            local organelleX, organelleY = axialToCartesian(data.q, data.r)
            local hexX, hexY = axialToCartesian(hex.q, hex.r)
            local x = organelleX + hexX
            local y = organelleY + hexY
            local translation = Vector3(-x, -y, 0)
            data.sceneNode[i].transform.position = translation
            data.sceneNode[i].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
            xSum = xSum + x
            ySum = ySum + y
            i = i + 1
        end

        --Getting the average x and y values to render the organelle mesh in the middle.
        local xAverage = xSum / (i - 2) -- Number of occupied hexes = (i - 2).
        local yAverage = ySum / (i - 2)

        --Rendering the organelle mesh (if it has one).
        local mesh = organelleTable[data.name].mesh
        if(mesh ~= nil) then
            data.sceneNode[1].meshName = mesh
            data.sceneNode[1].transform.position = Vector3(-xAverage, -yAverage, 0)
            data.sceneNode[1].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
        end
	end
end

-- Checks which hexes an organelle occupies
function OrganelleFactory.checkSize(data)
    if data.name == "remove" then
        return {}
    else
        --getting the angle the organelle has
        --(and setting one if it doesn't have one).
        if data.rotation == nil then
            data.rotation = 0
        end
        local angle = data.rotation / 60

        --getting the hex table of the organelle rotated by the angle
        local hexes = rotateHexListNTimes(organelleTable[data.name].hexes, angle)
        return hexes
    end
end

-- OrganelleFactory.make_organelle(data) should be defined in the appropriate file
-- each factory function should return an organelle that's ready to be inserted into a microbe
-- check the organelle files for examples on use.
