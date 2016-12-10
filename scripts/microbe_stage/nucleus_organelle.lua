--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (ProcessOrganelle)

-- Constructor
function NucleusOrganelle:__init()
    local mass = 0.7
    ProcessOrganelle.__init(self, mass)
    self.golgi = Entity()
	self.ER = Entity()
end


-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r, rotation)
    local x, y = axialToCartesian(q-1, r-1)
    local sceneNode1 = OgreSceneNodeComponent()
    sceneNode1.meshName = "golgi.mesh"
    sceneNode1.parent = self.organelleEntity
	sceneNode1.transform.position = Vector3(x,y,0)
    sceneNode1.transform.scale = Vector3(1, 1, 1)
    sceneNode1.transform.orientation = Quaternion(Radian(Degree(rotation)), Vector3(0, 0, 1))
    sceneNode1.transform:touch()
    microbe.entity:addChild(self.golgi)
    self.golgi:addComponent(sceneNode1)
	self.golgi.sceneNode = sceneNode1
	self.golgi:setVolatile(true)
	
	local sceneNode2 = OgreSceneNodeComponent()
    sceneNode2.meshName = "ER.mesh"
	sceneNode2.parent = self.organelleEntity
	sceneNode2.transform.position = Vector3(0,0,0)
    sceneNode2.transform.scale = Vector3(1, 1, 1)
    sceneNode2.transform.orientation = Quaternion(Radian(Degree(rotation+5)), Vector3(0, 0, 1))
    sceneNode2.transform:touch()
    microbe.entity:addChild(self.ER)
    self.ER:addComponent(sceneNode2)
	self.ER.sceneNode = sceneNode2
	self.ER:setVolatile(true)
    
    self.numProteinLeft = 2
    self.numNucleicAcidsLeft = 2
    self.nucleusCost = self.numProteinLeft + self.numNucleicAcidsLeft
	
    ProcessOrganelle.onAddedToMicrobe(self, microbe, q, r, rotation)
    
    self.sceneNode.transform.position = Vector3(0,0,0)
    self.sceneNode.transform:touch()
end

-- Overridded from Organelle:onRemovedFromMicrobe
function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    ProcessOrganelle.onRemovedFromMicrobe(self, microbe, q, r)
end


function NucleusOrganelle:storage()
    local storage = ProcessOrganelle.storage(self)
    return storage
end


function NucleusOrganelle:load(storage)
    ProcessOrganelle.load(self, storage)
end

function NucleusOrganelle:updateColour()
    -- Update the colours of the additional organelle models.
    if self.sceneNode.entity ~= nil then
        local entity = self.sceneNode.entity
        local golgiEntity = self.golgi.sceneNode.entity
        local ER_entity = self.ER.sceneNode.entity
        
        entity:tintColour(self.name, self.colour)
        golgiEntity:tintColour("golgi", self.colour)
        ER_entity:tintColour("ER", self.colour)
        
        self._needsColourUpdate = false
    end
end

function NucleusOrganelle:update(microbe, logicTime)
    Organelle.update(self, microbe, logicTime)
end

-- Makes nucleus larger
function NucleusOrganelle:grow(compoundBagComponent)
    -- Finds the total number of needed compounds.
    local sum = 0

    -- Finds which compounds the cell currently has.
    if compoundBagComponent:aboveLowThreshold(CompoundRegistry.getCompoundId("aminoacids")) >= 1 then
        sum = sum + self.numProteinLeft
    end
    if compoundBagComponent:aboveLowThreshold(CompoundRegistry.getCompoundId("aminoacids")) >= 1 then
        sum = sum + self.numNucleicAcidsLeft
    end
    
    -- If sum is 0, we either have no compounds, in which case we cannot grow the organelle, or the
    -- DNA duplication is done (i.e. compoundBin = 2), in which case we wait for the microbe to
    -- handle the split.
    if sum == 0 then return end
       
    -- Randomly choose which of the three compounds: glucose, amino acids, and fatty acids
    -- that are used in reproductions.
    local id = math.random()*sum
    
    -- The random number is a protein, so attempt to take it.
    if id <= self.numProteinLeft then
        compoundBagComponent:takeCompound(CompoundRegistry.getCompoundId("aminoacids"), 1)
        self.numProteinLeft = self.numProteinLeft - 1
    -- The random number is a nucleic acid.
    else
        compoundBagComponent:takeCompound(CompoundRegistry.getCompoundId("aminoacids"), 1)
        self.numNucleicAcidsLeft = self.numNucleicAcidsLeft - 1
    end
    
    -- Calculate the new growth growth
    self:recalculateBin()
end

function NucleusOrganelle:damage(amount)
print("Nucleus damaged: " .. amount)

    -- Flash the nucleus.
    self:flashOrganelle(3000, ColourValue(1,0.2,0.2,1))
    
    -- Calculate the total number of compounds we need to divide now, so that we can keep this ratio.
    local totalLeft = self.numProteinLeft + self.numNucleicAcidsLeft
    
    -- Calculate how much compounds the organelle needs to have to result in a health equal to compoundBin - amount.
    local damageFactor = (2.0 - self.compoundBin + amount) * self.nucleusCost / totalLeft
    self.numProteinLeft = self.numProteinLeft * damageFactor
    self.numNucleicAcidsLeft = self.numNucleicAcidsLeft * damageFactor
    
    -- Calculate the new growth value.
    self:recalculateBin()
end

function NucleusOrganelle:recalculateBin()
    -- Calculate the new growth growth
    self.compoundBin = 2.0 - (self.numProteinLeft + self.numNucleicAcidsLeft)/self.organelleCost
    
    -- If the organelle is damaged...
    if self.compoundBin < 1.0 then
        -- Make the nucleus smaller.
        self.sceneNode.transform.scale = Vector3((1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2)*HEX_SIZE
        self.sceneNode.transform:touch()
        
        -- Darken the color.
        local speciesColour = self.microbe:getSpeciesComponent().colour
        local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
        --self.sceneNode.entity:tintColour(self.name .. colorSuffix, ColourValue((1.0 + self.compoundBin)/2, self.compoundBin, self.compoundBin, 1.0))      
    else
        -- Darken the nucleus as more DNA is made.
        local speciesColour = self.microbe:getSpeciesComponent().colour
        local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
        --self.sceneNode.entity:tintColour(self.name .. colorSuffix, ColourValue(2-self.compoundBin, 2-self.compoundBin, 2-self.compoundBin, 1.0))
    end
end

function OrganelleFactory.make_nucleus(data)
    local nucleus = NucleusOrganelle()
    -- nucleus:addProcess(global_processMap["ReproductaseSynthesis"])
    -- nucleus:addProcess(global_processMap["AminoAcidSynthesis"])
        
    if data.rotation == nil then
		data.rotation = 0
	end
	local angle = (data.rotation / 60)
	
    nucleus:addHex(0, 0)
	local q = 1
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = 0
	r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = 0
	r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = 1
	r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = -1
	r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = -1
	r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = -1
	r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = -2
	r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	q = -2
	r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	nucleus:addHex(q, r)
	
	return nucleus
end

function OrganelleFactory.render_nucleus(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
	
	data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
	
	local angle = (data.rotation / 60)
	local q = 1
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	x, y = axialToCartesian(q + data.q, r + data.r)
	translation = Vector3(-x, -y, 0)
	data.sceneNode[3].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[3], data.colour)
	
	q = 0
	r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	x, y = axialToCartesian(q + data.q, r + data.r)
	translation = Vector3(-x, -y, 0)
	data.sceneNode[4].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[4], data.colour)
	
	data.sceneNode[1].meshName = "nucleus.mesh"
	data.sceneNode[1].transform.position = Vector3(0,0,0)
	data.sceneNode[1].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
end

function OrganelleFactory.sizeof_nucleus(data)
	local hexes = {}
	
	if data.rotation == nil then
		data.rotation = 0
	end
	local angle = (data.rotation / 60)
	
	hexes[1] = {["q"]=0, ["r"]=0}
	
	local q = 1
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[2] = {["q"]=q, ["r"]=r}
	
	local q = 0
	local r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[3] = {["q"]=q, ["r"]=r}
	
	local q = 0
	local r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[4] = {["q"]=q, ["r"]=r}
	
	local q = 1
	local r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[5] = {["q"]=q, ["r"]=r}
	
	local q = -1
	local r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[6] = {["q"]=q, ["r"]=r}
	
	local q = -1
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[7] = {["q"]=q, ["r"]=r}
	
	local q = -1
	local r = -1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[8] = {["q"]=q, ["r"]=r}
	
	local q = -2
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[9] = {["q"]=q, ["r"]=r}
	
	local q = -2
	local r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	hexes[10] = {["q"]=q, ["r"]=r}
	
    return hexes
end
