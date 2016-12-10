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
function Organelle:__init(mass)
    self.organelleEntity = Entity()
    self.sceneNode = OgreSceneNodeComponent()
    self.organelleEntity:setVolatile(true)
    self.organelleEntity:addComponent(self.sceneNode)
    
    self.collisionShape = CompoundShape()
    self.mass = mass
    self._hexes = {}
    self.position = {
        q = 0,
        r = 0
    }
    self.rotation = nil
    self.name = "<nameless>"
    
    -- The deviation of the organelle color from the species color
    self.colourTint = Vector3(1.0, 1.0, 1.0)
    self._needsColourUpdate = true
	
	-- The "Health Bar" of the organelle constrained to [0,2]
	-- 0 means the organelle is dead, 1 means its normal, and 2 means
	-- its ready to divide.
	self.compoundBin = 1.0
	-- Whether or not this organelle has already divided.
	self.split = false
    -- If this organelle is a duplicate of another organelle caused by splitting.
    self.isDuplicate = false
    -- The compounds left to divide this organelle.
    -- Decreases every time one compound is absorbed.
    self.numGlucose = 2
    self.numAminoAcids = 3
    self.numFattyAcids = 0
    -- The compounds that make up this organelle.
    self.numGlucoseLeft = self.numGlucose
    self.numAminoAcidsLeft = self.numAminoAcids
    self.numFattyAcidsLeft = self.numFattyAcids
    -- The total number of compounds we need before we can split.
    self.organelleCost = self.numGlucose + self.numAminoAcids + self.numFattyAcids
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
        collisionShape = SphereShape(2)
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
    local x, y = axialToCartesian(q, r)
    self.position.cartesian = Vector3(x,y,0)
    self.rotation = rotation
	
	local offset = Vector3(0,0,0)
	local count = 0
	for _, hex in pairs(self.microbe:getOrganelleAt(q, r)._hexes) do
		count = count + 1
		
		local x, y = axialToCartesian(hex.q, hex.r)
		offset = offset + Vector3(x,y,0)
	end
	offset = offset/count
    
    if self.name ~= "cytoplasm" then
        self.sceneNode.meshName = self.name .. ".mesh"
    end
    
	self.sceneNode.transform.orientation = Quaternion(Radian(Degree(self.rotation)), Vector3(0, 0, 1))
	self.sceneNode.transform.position = offset + self.position.cartesian
    self.sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    self.sceneNode.transform:touch()
        
    -- Change the colour of this species to be tinted by the membrane.
    self.colour = ColourValue(microbe:getSpeciesComponent().colour.x, 
                              microbe:getSpeciesComponent().colour.y,
                              microbe:getSpeciesComponent().colour.z, 1)
    self._needsColourUpdate = true
end

function Organelle:setAnimationSpeed()
    self.sceneNode:setAnimationSpeed(0.25)
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
end

function Organelle:flashOrganelle(duration, colour)
	if self.flashDuration == nil then
        self.flashColour = colour
        self.flashDuration = duration
    end
end

function Organelle:updateColour()
    -- If the material was not cloned, clone it. This is a dumb workaround because
    -- entity doesn't exist when onAddedToMicrobe is called.
    --if self.clonedMaterial == nil and self.sceneNode.entity ~= nil    
    --    and (self.name == "chloroplast" or self.name == "mitochondrion" or self.name == "nucleus" or self.name == "ER" or self.name == "golgi") then
        
    --    self.scenenNode.entity:tintColour(self.name, self.colour)
        
    --    self.clonedMaterial = true
    --end

    if self.sceneNode.entity ~= nil
        and (self.name == "chloroplast" or self.name == "mitochondrion" or self.name == "nucleus" or self.name == "ER" or self.name == "golgi") then
		local entity = self.sceneNode.entity
        entity:tintColour(self.name, self.colour)
        
        self._needsColourUpdate = false
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
    storage:set("mass", self.mass)
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
	if self.flashDuration ~= nil
        and (self.name == "mitochondrion" or self.name == "nucleus" or self.name == "ER" or self.name == "golgi") then
        
        self.flashDuration = self.flashDuration - logicTime
        local speciesColour = ColourValue(microbe:getSpeciesComponent().colour.x, 
                                          microbe:getSpeciesComponent().colour.y,
                                          microbe:getSpeciesComponent().colour.z, 1)
		
		local entity = self.sceneNode.entity
		-- How frequent it flashes, would be nice to update the flash function to have this variable
		if math.fmod(self.flashDuration,600) < 300 then
            self.colour = self.flashColour
		else
			self.colour = speciesColour
		end
		
        if self.flashDuration <= 0 then
            self.flashDuration = nil
			self.colour = speciesColour
        end
        
        self._needsColourUpdate = true
    end
    
    if self._needsColourUpdate == true then
        self:updateColour()
    end
end


-- Abstract method, must be overloaded.
--
-- Override to make each organelle larger
function Organelle:grow(compoundBagComponent)
    -- Finds the total number of needed compounds.
    local sum = 0

    -- Finds which compounds the cell currently has.
    if compoundBagComponent:aboveLowThreshold(CompoundRegistry.getCompoundId("glucose")) >= 1 then
        sum = sum + self.numGlucoseLeft
    end
    if compoundBagComponent:aboveLowThreshold(CompoundRegistry.getCompoundId("aminoacids")) >= 1 then
        sum = sum + self.numAminoAcidsLeft
    end
    if compoundBagComponent:aboveLowThreshold(CompoundRegistry.getCompoundId("fattyacids")) >= 1 then
        sum = sum + self.numFattyAcidsLeft
    end
    
    -- If sum is 0, we either have no compounds, in which case we cannot grow the organelle, or the
    -- organelle is ready to split (i.e. compoundBin = 2), in which case we wait for the microbe to
    -- handle the split.
    if sum == 0 then return end
       
    -- Randomly choose which of the three compounds: glucose, amino acids, and fatty acids
    -- that are used in reproductions.
    local id = math.random()*sum
    
    -- The random number is a glucose, so attempt to take it.
    if id - self.numGlucoseLeft < 0 then
        compoundBagComponent:takeCompound(CompoundRegistry.getCompoundId("glucose"), 1)
        self.numGlucoseLeft = self.numGlucoseLeft - 1
    elseif id - self.numGlucoseLeft - self.numAminoAcidsLeft < 0 then
        compoundBagComponent:takeCompound(CompoundRegistry.getCompoundId("aminoacids"), 1)
        self.numAminoAcidsLeft = self.numAminoAcidsLeft - 1
    else
        compoundBagComponent:takeCompound(CompoundRegistry.getCompoundId("fattyacids"), 1)
        self.numFattyAcidsLeft = self.numFattyAcidsLeft - 1
    end
    
    -- Calculate the new growth value.
    self:recalculateBin()
end

function Organelle:damage(amount)
print(self.name .. " lost " .. amount .. "/2.0 health") 
    -- Flash the organelle that was damaged.
    self:flashOrganelle(3000, ColourValue(1,0.2,0.2,1))
    
    -- Calculate the total number of compounds we need to divide now, so that we can keep this ratio.
    local totalLeft = self.numGlucoseLeft + self.numAminoAcidsLeft + self.numFattyAcidsLeft
    
    -- Calculate how much compounds the organelle needs to have to result in a health equal to compoundBin - amount.
    local damageFactor = (2.0 - self.compoundBin + amount) * self.organelleCost / totalLeft
    self.numGlucoseLeft    = self.numGlucoseLeft * damageFactor
    self.numAminoAcidsLeft = self.numAminoAcidsLeft * damageFactor
    self.numFattyAcidsLeft = self.numFattyAcidsLeft * damageFactor
    
    -- Calculate the new growth value.
    self:recalculateBin()
end

function Organelle:recalculateBin()
    -- Calculate the new growth growth
    self.compoundBin = 2.0 - (self.numGlucoseLeft + self.numAminoAcidsLeft + self.numFattyAcidsLeft)/self.organelleCost
    -- If the organelle is damaged...
    if self.compoundBin < 1.0 then
        if self.compoundBin <= 0.0 then
            -- If it was split from a primary organelle, destroy it.
            if self.isDuplicate == true then
                self.microbe.removeOrganelle(self.position.q, self.position.r)
                return
                
            -- If it is a primary organelle, make sure that it's compound bin is not less than 0.
            else
                self.compoundBin = 0
                self.numGlucoseLeft    = 2 * self.numGlucose
                self.numAminoAcidsLeft = 2 * self.numAminoAcids
                self.numFattyAcidsLeft = 2 * self.numFattyAcids
            end
        end
        -- Scale the model at a slower rate (so that 0.0 is half size).
        print(self.compoundBin)
        self.sceneNode.transform.scale = Vector3((1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2)*HEX_SIZE
        self.sceneNode.transform:touch()
        
        -- Darken the color. Will be updated on next call of update()
        self.colourTint = Vector3((1.0 + self.compoundBin)/2, self.compoundBin, self.compoundBin)
    else
        -- Scale the organelle model to reflect the new size.
        self.sceneNode.transform.scale = Vector3(self.compoundBin, self.compoundBin, self.compoundBin)*HEX_SIZE
        self.sceneNode.transform:touch()  
    end
end

function Organelle:reset()
    self.compoundBin = 1.0
    self.numGlucoseLeft    = self.numGlucose
    self.numAminoAcidsLeft = self.numAminoAcids
    self.numFattyAcidsLeft = self.numFattyAcids
    
    -- Scale the organelle model to reflect the new size.
    self.sceneNode.transform.scale = Vector3(self.compoundBin, self.compoundBin, self.compoundBin)*HEX_SIZE
    self.sceneNode.transform:touch() 
end


function Organelle:removePhysics()
    self.collisionShape:clear()
end

-- The basic organelle maker
class 'OrganelleFactory'

-- Sets the color of the organelle (used in editor for valid/nonvalid placement)
function OrganelleFactory.setColour(sceneNode, colour)
	sceneNode.entity:setColour(colour)
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
