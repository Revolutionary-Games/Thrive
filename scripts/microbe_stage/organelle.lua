-- Base class for microbe organelles
class 'Organelle'

Organelle.mpCosts = {}

-- Factory function for organelles
function Organelle.loadOrganelle(storage)
    local className = storage:get("className", "")
    local cls = _G[className]
    local organelle = cls()
    organelle:load(storage)
    return organelle
end


-- Constructor
function Organelle:__init()
    self.entity = Entity()
    self.entity:setVolatile(true)
    self.sceneNode = self.entity:getOrCreate(OgreSceneNodeComponent)
    self.collisionShape = CompoundShape()
    self._hexes = {}
    self.position = {
        q = 0,
        r = 0
    }
    self.rotation = 0
    self.name = "<nameless>"
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
    self.rotation = storage:get("rotation", 0)
    self.name = storage:get("name", "<nameless>")
end


-- Called by a microbe when this organelle has been added to it
--
-- @param microbe
--  The organelle's new owner
--
-- @param q, r
--  Axial coordinates of the organelle's center
function Organelle:onAddedToMicrobe(microbe, q, r, rotation)
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
	
	self.organelleEntity = Entity()
    local sceneNode = OgreSceneNodeComponent()
    self.sceneNode = sceneNode
    sceneNode.parent = self.entity
	sceneNode.meshName = self.name .. ".mesh"
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
		sceneNode:playAnimation("Float", true)
		sceneNode:setAnimationSpeed(0.25)
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
    storage = StorageContainer()
    storage:set("className", class_info(self).name)
    hexes = StorageList()
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
    --Serializing these causes some minor issues and doesn't serve a purpose anyway
    --storage:set("externalEdgeColour", self._externalEdgeColour)
    return storage
end


-- Called by Microbe:update
--
-- Override this to make your organelle class do something at regular intervals
--
-- @param logicTime
--  The time since the last call to update()
function Organelle:update(microbe, logicTime)
    if self.name == "flagellum" then
		local x, y = axialToCartesian(self.position.q, self.position.r)
		local membraneCoords = microbe.membraneComponent:getExternOrganellePos(x, y)
		local translation = Vector3(membraneCoords[1], membraneCoords[2], 0)
		self.organelleEntity.sceneNode.transform.position = translation - Vector3(x, y, 0)
		self.organelleEntity.sceneNode.transform:touch()
	end
	if self.flashDuration ~= nil then
        self.flashDuration = self.flashDuration - logicTime
		if not self.sceneNode.entity or self.name ~= "nucleus" then
			return
		end
		
		local subEntity = self.sceneNode.entity:getSubEntity(self.name)
		-- How frequent it flashes, would be nice to update the flash function
		if math.fmod(self.flashDuration,600) < 300 then
			subEntity:setColour(self.colour)
		else
			subEntity:setMaterial(self.name)
		end
		
        if self.flashDuration <= 0 then
            self.flashDuration = nil				
			local subEntity = self.sceneNode.entity:getSubEntity(self.name)
			subEntity:setMaterial(self.name)
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
	local subEntity = sceneNode.entity:getSubEntity("center")
	subEntity:setColour(colour)
	for i=1, 6 do
		local sideName = HEX_SIDE_NAME[i]
		subEntity = sceneNode.entity:getSubEntity(sideName)
		subEntity:setColour(colour)
	end
end

function OrganelleFactory.makeOrganelle(data)
    local make_organelle = function()
        return OrganelleFactory["make_"..data.name](data)
    end
    local success, organelle = pcall(make_organelle)
    if success then
        organelle.name = data.name
        return organelle
    else
        if data.name == "" or data.name == nil then data.name = "<nameless>" end
        assert(false, "no organelle by name "..data.name)
    end
end

-- Draws the hexes and uploads the models in the editor
function OrganelleFactory.renderOrganelles(data)
	if data.name == "remove" then
		return {}
	else
		OrganelleFactory["render_"..data.name](data)
	end
end

-- Checks which hexes an organelle occupies
function OrganelleFactory.checkSize(data)
	if data.name == "remove" then
		return {}
	else
		return OrganelleFactory["sizeof_"..data.name](data)
	end
end

-- OrganelleFactory.make_organelle(data) should be defined in the appropriate file
-- each factory function should return an organelle that's ready to be inserted into a microbe
-- check the organelle files for examples on use.
