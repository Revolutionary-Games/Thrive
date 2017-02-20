--Base class for organelle components.
class "OrganelleComponent"

-- Constructor.
--
-- @param arguments
--  The parameters of the constructor, defined in organelle_table.lua.
--
-- @param data
--  The organelle data taken from the make function in organelle.lua.
--
-- The return value should be the organelle component itself (A.K.A. self)
function OrganelleComponent:__init(arguments, data)

    -- TODO: All organelles except the nucleus use the following compound
    -- amounts to reproduce. This should be changed to use getter/setters
    -- as well as unique compound amounts for each component types.


    -- The "Health Bar" of the organelle constrained to [0,2]
    -- 0 means the organelle is dead, 1 means its normal, and 2 means
    -- its ready to divide.
    self.compoundBin = 1.0
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
        
    return self
end

-- Event handler for an organelle added to a microbe.
--
-- @param microbe
--  The microbe this organelle is added to.
--
-- @param q
--  q component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param r
--  r component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param rotation
--  The rotation this organelle has on the microbe.
--  it can be either 0, 60, 120, 180, 240 or 280.
--
-- @param self
--  The organelle object that is made up of these components.
function OrganelleComponent:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    local offset = Vector3(0,0,0)
    local count = 0
    for _, hex in pairs(organelle.microbe:getOrganelleAt(q, r)._hexes) do
        count = count + 1

        local x, y = axialToCartesian(hex.q, hex.r)
       offset = offset + Vector3(x,y,0)
    end
    offset = offset/count
  
    self.sceneNode = OgreSceneNodeComponent()
    self.sceneNode.transform.orientation = Quaternion(Radian(Degree(organelle.rotation)), Vector3(0, 0, 1))
    self.sceneNode.transform.position = offset + organelle.position.cartesian
    self.sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    self.sceneNode.transform:touch()
    self.sceneNode.parent = microbe.entity
    organelle.organelleEntity:addComponent(self.sceneNode)
    
    --Adding a mesh to the organelle.
    local mesh = organelleTable[organelle.name].mesh
    if mesh ~= nil then
        self.sceneNode.meshName = mesh
    end
end

-- Event handler for an organelle removed from a microbe.
--
-- @param microbe
--  The microbe this organelle is removed from.
--
-- @param q
--  q component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param r
--  r component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
function OrganelleComponent:onRemovedFromMicrobe(microbe, q, r)
end

--  Function executed at regular intervals
--
-- @param microbe
--  The microbe this organelle is attached.
--
-- @param organelle
--  The organelle that has this component.
--
-- @param logicTime
--  The time transcurred (in milliseconds) between this call
--  to OrganelleComponent:update() and the previous one.
function OrganelleComponent:update(microbe, organelle, logicTime)
    -- If the organelle is supposed to be another color.
    if organelle._needsColourUpdate == true then
        self:updateColour(organelle)
    end
end

function OrganelleComponent:updateColour(organelle)
    if self.sceneNode.entity ~= nil then
        local entity = self.sceneNode.entity
        entity:tintColour(organelle.name, organelle.colour)
        
        organelle._needsColourUpdate = false
    end
end

-- Function for saving organelle information.
-- If an organelle depends on an atribute, then it should be saved
-- the data gets retrieved later by OrganelleComponent:load().
-- The return value should be a new StorageContainer object
-- filled with the data to save.
function OrganelleComponent:storage()
    return StorageContainer()
end

-- Function for loading organelle information.
-- 
-- @param storage
--  The StorageContainer object that has the organelle information
--  (the one saved in OrganelleComponent:storage()).
function OrganelleComponent:load(storage)
end

-- Damages the organelle component by increasing the amount
-- of compounds it is made out of.
--
-- @param amount
--  The total amount of damage dealt to the compound bin.
function OrganelleComponent:damage(amount)
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


-- Grows each organelle
function OrganelleComponent:grow(compoundBagComponent)
    -- Finds the total number of needed compounds.
    local sum = 0

    -- Finds which compounds the cell currently has.
    if compoundBagComponent:getCompoundAmount(CompoundRegistry.getCompoundId("aminoacids")) >= 1 then
        sum = sum + self.numGlucoseLeft
    end
    if compoundBagComponent:getCompoundAmount(CompoundRegistry.getCompoundId("aminoacids")) >= 1 then
        sum = sum + self.numAminoAcidsLeft
    end
    if compoundBagComponent:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids")) >= 1 then
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


function OrganelleComponent:recalculateBin()
    -- Calculate the new growth growth
    self.compoundBin = 2.0 - (self.numGlucoseLeft + self.numAminoAcidsLeft + self.numFattyAcidsLeft)/self.organelleCost
    -- If the organelle is damaged...
    if self.compoundBin < 1.0 then
        if self.compoundBin <= 0.0 then
            -- If it was split from a primary organelle, destroy it.
            if self.isDuplicate == true then
                self.microbe:removeOrganelle(self.position.q, self.position.r)
                
                -- Notify the organelle the sister organelle it is no longer split.
                self.sisterOrganelle.wasSplit = false
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
        self.sceneNode.transform.scale = Vector3((1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2, (1.0 + self.compoundBin)/2)*HEX_SIZE
        self.sceneNode.transform:touch()
        
        -- Darken the color. Will be updated on next call of update()
        self.colourTint = Vector3((1.0 + self.compoundBin)/2, self.compoundBin, self.compoundBin)
        self._needsColourUpdate = true
    else
        -- Scale the organelle model to reflect the new size.
        self.sceneNode.transform.scale = Vector3(self.compoundBin, self.compoundBin, self.compoundBin)*HEX_SIZE
        self.sceneNode.transform:touch()  
    end
end

function OrganelleComponent:reset()
    -- Return the compound bin to its original state
    self.compoundBin = 1.0
    self.numGlucoseLeft    = self.numGlucose
    self.numAminoAcidsLeft = self.numAminoAcids
    self.numFattyAcidsLeft = self.numFattyAcids
    
    -- Scale the organelle model to reflect the new size.
    self.sceneNode.transform.scale = Vector3(1, 1, 1)*HEX_SIZE
    self.sceneNode.transform:touch()
end
