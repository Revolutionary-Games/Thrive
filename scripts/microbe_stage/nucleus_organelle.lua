--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Constructor
function NucleusOrganelle:__init(arguments, data)
    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end

    self.golgi = Entity()
	self.ER = Entity()
    return self
end


-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    local x, y = axialToCartesian(q-1, r-1)
    local sceneNode1 = OgreSceneNodeComponent()
    sceneNode1.meshName = "golgi.mesh"
	sceneNode1.transform.position = Vector3(x,y,0)
    sceneNode1.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    sceneNode1.transform.orientation = Quaternion(Radian(Degree(rotation)), Vector3(0, 0, 1))
    sceneNode1.transform:touch()
    sceneNode1.parent = microbe.entity
    microbe.entity:addChild(self.golgi)
    self.golgi:addComponent(sceneNode1)
	self.golgi.sceneNode = sceneNode1
	self.golgi:setVolatile(true)
	
	local sceneNode2 = OgreSceneNodeComponent()
    sceneNode2.meshName = "ER.mesh"
	sceneNode2.transform.position = Vector3(0,0,0)
    sceneNode2.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    sceneNode2.transform.orientation = Quaternion(Radian(Degree(rotation+10)), Vector3(0, 0, 1))
    sceneNode2.transform:touch()
	sceneNode2.parent = microbe.entity
    microbe.entity:addChild(self.ER)
    self.ER:addComponent(sceneNode2) 
	self.ER.sceneNode = sceneNode2
	self.ER:setVolatile(true)
    
    -- Center the nucleus model.
    organelle.sceneNode.transform.position = Vector3(0,0,0)
    organelle.sceneNode.transform:touch()
    
    self.numProteinLeft = 2
    self.numNucleicAcidsLeft = 2
    self.nucleusCost = self.numProteinLeft + self.numNucleicAcidsLeft
    
    self._needsColourUpdate = true
end

function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    self.golgi:destroy()
    self.ER:destroy()
end

function NucleusOrganelle:load(storage)
    self.golgi = Entity()
	self.ER = Entity()
end

function NucleusOrganelle:updateColour(organelle)
    -- Update the colours of the additional organelle models.
    if organelle.sceneNode.entity ~= nil and self.golgi.sceneNode.entity ~= nil then
		local entity = organelle.sceneNode.entity
        local golgiEntity = self.golgi.sceneNode.entity
        local ER_entity = self.ER.sceneNode.entity
        
        entity:tintColour(organelle.name, organelle.colour)
        golgiEntity:tintColour("golgi", organelle.colour)
        ER_entity:tintColour("ER", organelle.colour)
        
        self._needsColourUpdate = false
    end
end

function NucleusOrganelle:update(microbe, organelle, logicTime)
    if organelle.flashDuration ~= nil and organelle.sceneNode.entity ~= nil then
        organelle.flashDuration = organelle.flashDuration - logicTime
		
		local entity = organelle.sceneNode.entity
        local golgiEntity = self.golgi.sceneNode.entity
        local ER_entity = self.ER.sceneNode.entity

		if math.fmod(organelle.flashDuration,600) < 300 then      
            organelle.colour = organelle.flashColour          
		else
			organelle.colour = ColourValue(microbe:getSpeciesComponent().colour.x, 
                                           microbe:getSpeciesComponent().colour.y,
                                           microbe:getSpeciesComponent().colour.z, 1)
		end
		
        if organelle.flashDuration <= 0 then
            organelle.flashDuration = nil				
			organelle.colour = ColourValue(microbe:getSpeciesComponent().colour.x, 
                                           microbe:getSpeciesComponent().colour.y,
                                           microbe:getSpeciesComponent().colour.z, 1)
        end
    end
    
    if self._needsColourUpdate then
        self:updateColour(organelle)
    end
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
--print("Nucleus damaged: " .. amount)

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
    self.compoundBin = 2.0 - (self.numProteinLeft + self.numNucleicAcidsLeft)/self.nucleusCost
    
    local speciesColour = self.microbe:getSpeciesComponent().colour
    local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
    
    -- If the organelle is damaged...
    if self.compoundBin < 1.0 then
        -- Make the nucleus smaller.
        self.sceneNode.transform.scale = Vector3((1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2)*HEX_SIZE
        self.sceneNode.transform:touch()
        
        if self.sceneNode.entity ~= nil then
            -- Darken the color.
            local speciesColour = self.microbe:getSpeciesComponent().colour
            local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
            self.sceneNode.entity:tintColour(self.name .. colorSuffix, ColourValue((1.0 + self.compoundBin)/2, self.compoundBin, self.compoundBin, 1.0))
        end
    else
        -- Darken the nucleus as more DNA is made.
        local speciesColour = self.microbe:getSpeciesComponent().colour
        local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
        self.sceneNode.entity:tintColour(self.name .. colorSuffix, ColourValue(2-self.compoundBin, 2-self.compoundBin, 2-self.compoundBin, 1.0))
    end
end

