--------------------------------------------------------------------------------
-- Class for Organelles capable of producing compounds
--------------------------------------------------------------------------------
class 'ProcessOrganelle' (Organelle)

-- Constructor
function ProcessOrganelle:__init(processCooldown)
    Organelle.__init(self)
    self.processCooldown = processCooldown
    self.remainingCooldown = processCooldown -- Countdown var until next output batch can be produced
    self.bufferSum = 0         -- Number of compound units summed over all buffers
    self.inputSum = 0          -- Number of compound units summed over all input requirements
    self.originalColour = ColourValue(1,1,1,1)
    self.buffers = {}
    self.inputCompounds = {}
    self.outputCompounds = {}
end


-- Overridded from Organelle:onAddedToMicrobe
function ProcessOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addProcessOrganelle(self)
end


-- Set the minimum time that has to pass between compounds are produced
-- 
-- @param milliseconds
--  The amount of time
function ProcessOrganelle:setprocessCooldown(milliseconds)
    self.processCooldown = milliseconds
end


-- Add input compound to the recipy of the organelle
--
-- @param compoundId
--  The compound to be used as input
--
-- @param amount
--  The amount of the compound needed
function ProcessOrganelle:addRecipyInput(compoundId, amount)
    self.inputCompounds[compoundId] = amount
    self.buffers[compoundId] = 0
    self.inputSum = self.inputSum + amount;
    self:updateColourDynamic()
end


-- Add output compound to the recipy of the organelle
--
-- @param compoundId
--  The compound to be used as output
--
-- @param amount
--  The amount of the compound produced
function ProcessOrganelle:addRecipyOutput(compoundId, amount)
    self.outputCompounds[compoundId] = amount 
end


-- Store compound in buffer of processing organelle. 
-- This will force the organelle to store the compound, even if wantInputCompound is false.
-- It is recommended to check if wantInputCompound is true before calling.
--
-- @param compoundId
--  The compound to be stored
--
-- @param amount
--  The amount to be stored
function ProcessOrganelle:storeCompound(compoundId, amount)
    self.buffers[compoundId] = self.buffers[compoundId] + amount
    self.bufferSum = self.bufferSum + amount
    self:updateColourDynamic()
    self._needsColourUpdate = true
end


-- Private function used to update colour of organelle based on how full it is
function ProcessOrganelle:updateColourDynamic()
    local rt = self.bufferSum/self.inputSum -- Ratio: how close to required input
    if rt > 1 then rt = 1 end
    self._colour = ColourValue(0.6 + (self.originalColour.r-0.6)*rt, 
                               0.6 + (self.originalColour.g-0.6)*rt,                              
                               0.6 + (self.originalColour.b-0.6)*rt, 1) -- Calculate colour relative to how close the organelle is to have enough input compounds to produce
end


-- Checks if processing organelle wants to store a given compound.
-- It wants an compound if it has that compound as input and its buffer relatively more full than it's process cooldown has left.
--
-- @param compoundId
--  The compound to check for
-- 
-- @returns wantsCompound
--  true if the compound wants the compound, false if it can't use or doesn't want the compound
function ProcessOrganelle:wantsInputCompound(compoundId)
    return (self.inputCompounds[compoundId] ~= nil and 
          self.remainingCooldown / (self.inputCompounds[compoundId] - self.buffers[compoundId]) < (self.processCooldown / self.inputCompounds[compoundId])) -- calculate if it has enough buffered relative the amount of time left.
end


-- Called by Microbe:update
--
-- Add output compound to the recipy of the organelle
--
-- @param microbe
--  The microbe containing the organelle
--
-- @param milliseconds
--  The time since the last call to update()
function ProcessOrganelle:update(microbe, milliseconds)
    Organelle.update(self, microbe, milliseconds)
    self.remainingCooldown = self.remainingCooldown - milliseconds -- update process cooldown
    if self.remainingCooldown < 0 then self.remainingCooldown = 0 end
    if self.remainingCooldown <= 0 then
        -- Attempt to produce
        for compoundId,amount in pairs(self.inputCompounds) do 
            if self.buffers[compoundId] < self.inputCompounds[compoundId] then
                return -- not enough compound material for some compound type. Cannot produce.
            end
        end
        -- Sufficient compound material is available for production
        self.remainingCooldown = self.processCooldown -- Restart cooldown
        
        for compoundId,amount in pairs(self.inputCompounds) do -- Foreach input compound, take it out of the buffer
            self.buffers[compoundId] = self.buffers[compoundId] - amount
            self.bufferSum = self.bufferSum - amount
        end
        self._needsColourUpdate = true  -- Update colours for displaying completeness of organelle production
        self:updateColourDynamic()
        for compoundId,amount in pairs(self.outputCompounds) do 
            microbe:storeCompound(compoundId, amount)
        end
    end
end


-- Override from Organelle:setColour
function ProcessOrganelle:setColour(colour)
    Organelle.setColour(self, colour)
    self.originalColour = colour
    local rt = self.bufferSum/self.inputSum -- Ratio: how close to required input
    self:updateColourDynamic()   
    self._needsColourUpdate = true
end

-- Buffer amounts aren't stored, could be added fairly easily
function ProcessOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("remainingCooldown", self.remainingCooldown)
    local inputCompoundsSt = StorageList()
    for compoundId, amount in pairs(self.inputCompounds) do
        inputStorage = StorageContainer()
        inputStorage:set("compoundId", compoundId)
        inputStorage:set("amount", amount)
        inputCompoundsSt:append(inputStorage)
    end
    storage:set("inputCompounds", inputCompoundsSt)
    local outputCompoundsSt = StorageList()
    for compoundId, amount in pairs(self.outputCompounds) do
        outputStorage = StorageContainer()
        outputStorage:set("compoundId", compoundId)
        outputStorage:set("amount", amount)
        outputCompoundsSt:append(outputStorage)
    end
    storage:set("outputCompounds", outputCompoundsSt)
    return storage
end


function ProcessOrganelle:load(storage)
    Organelle.load(self, storage)
    self.originalColour = self._colour
    self.remainingCooldown = storage:get("remainingCooldown", 0)
    local inputCompoundsSt = storage:get("inputCompounds", {})
    for i = 1,inputCompoundsSt:size() do
        local inputStorage = inputCompoundsSt:get(i)
        self:addRecipyInput(inputStorage:get("compoundId", 0), inputStorage:get("amount", 0))
    end
    local outputCompoundsSt = storage:get("outputCompounds", {})
    for i = 1,outputCompoundsSt:size() do
        local outputStorage = outputCompoundsSt:get(i)
        self:addRecipyOutput(outputStorage:get("compoundId", 0), outputStorage:get("amount", 0))
    end
end
