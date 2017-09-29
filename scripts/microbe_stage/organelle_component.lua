--Base class for organelle components.
OrganelleComponent = class(
    -- Constructor.
    --
    -- @param arguments
    --  The parameters of the constructor, defined in organelle_table.lua.
    --
    -- @param data
    --  The organelle data taken from the make function in organelle.lua.
    --
    -- The return value should be the organelle component itself (A.K.A. self)
    function(self, arguments, data)

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
        
    end
)

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
function OrganelleComponent:onAddedToMicrobe(microbeEntity, q, r, rotation, organelle)
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
function OrganelleComponent:onRemovedFromMicrobe(microbeEntity, q, r)
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
function OrganelleComponent:update(microbeEntity, organelle, logicTime)
end

function OrganelleComponent:updateColour(organelle)
end

-- Function for saving organelle information.
-- If an organelle depends on an atribute, then it should be saved
-- the data gets retrieved later by OrganelleComponent:load().
-- The return value should be a new StorageContainer object
-- filled with the data to save.
function OrganelleComponent:storage()
    return StorageContainer.new()
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
end

-- Grows each organelle.
function OrganelleComponent:grow(compoundBagComponent)
end

-- Resets each organelle.
function OrganelleComponent:reset()
end
