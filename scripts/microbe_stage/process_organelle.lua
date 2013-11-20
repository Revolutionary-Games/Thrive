--------------------------------------------------------------------------------
-- Class for Organelles capable of producing agents
--------------------------------------------------------------------------------
class 'ProcessOrganelle' (Organelle)

-- Constructor
function ProcessOrganelle:__init(processCooldown)
    Organelle.__init(self)
    self.processCooldown = processCooldown
    self.remainingCooldown = processCooldown -- Countdown var until next output batch can be produced
    self.bufferSum = 0         -- Number of agent units summed over all buffers
    self.inputSum = 0          -- Number of agent units summed over all input requirements
    self.originalColour = ColourValue(1,1,1,1)
    self.buffers = {}
    self.inputAgents = {}
    self.outputAgents = {}
end


-- Overridded from Organelle:onAddedToMicrobe
function ProcessOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addProcessOrganelle(self)
end


-- Set the minimum time that has to pass between agents are produced
-- 
-- @param milliseconds
--  The amount of time
function ProcessOrganelle:setprocessCooldown(milliseconds)
    self.processCooldown = milliseconds
end


-- Add input agent to the recipy of the organelle
--
-- @param agentId
--  The agent to be used as input
--
-- @param amount
--  The amount of the agent needed
function ProcessOrganelle:addRecipyInput(agentId, amount)
    self.inputAgents[agentId] = amount
    self.buffers[agentId] = 0
    self.inputSum = self.inputSum + amount;
    self:updateColourDynamic()
end


-- Add output agent to the recipy of the organelle
--
-- @param agentId
--  The agent to be used as output
--
-- @param amount
--  The amount of the agent produced
function ProcessOrganelle:addRecipyOutput(agentId, amount)
    self.outputAgents[agentId] = amount 
end


-- Store agent in buffer of processing organelle. 
-- This will force the organelle to store the agent, even if wantInputAgent is false.
-- It is recommended to check if wantInputAgent is true before calling.
--
-- @param agentId
--  The agent to be stored
--
-- @param amount
--  The amount to be stored
function ProcessOrganelle:storeAgent(agentId, amount)
    self.buffers[agentId] = self.buffers[agentId] + amount
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
                               0.6 + (self.originalColour.b-0.6)*rt, 1) -- Calculate colour relative to how close the organelle is to have enough input agents to produce
end


-- Checks if processing organelle wants to store a given agent.
-- It wants an agent if it has that agent as input and its buffer relatively more full than it's process cooldown has left.
--
-- @param agentId
--  The agent to check for
-- 
-- @returns wantsAgent
--  true if the agent wants the agent, false if it can't use or doesn't want the agent
function ProcessOrganelle:wantsInputAgent(agentId)
    return (self.inputAgents[agentId] ~= nil and 
          self.remainingCooldown / (self.inputAgents[agentId] - self.buffers[agentId]) < (self.processCooldown / self.inputAgents[agentId])) -- calculate if it has enough buffered relative the amount of time left.
end


-- Called by Microbe:update
--
-- Add output agent to the recipy of the organelle
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
        for agentId,amount in pairs(self.inputAgents) do 
            if self.buffers[agentId] < self.inputAgents[agentId] then
                return -- not enough agent material for some agent type. Cannot produce.
            end
        end
        -- Sufficient agent material is available for production
        self.remainingCooldown = self.processCooldown -- Restart cooldown
        
        for agentId,amount in pairs(self.inputAgents) do -- Foreach input agent, take it out of the buffer
            self.buffers[agentId] = self.buffers[agentId] - amount
            self.bufferSum = self.bufferSum - amount
        end
        self._needsColourUpdate = true  -- Update colours for displaying completeness of organelle production
        self:updateColourDynamic()
        for agentId,amount in pairs(self.outputAgents) do 
            microbe:storeAgent(agentId, amount)
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
    local inputAgentsSt = StorageList()
    for agentId, amount in pairs(self.inputAgents) do
        inputStorage = StorageContainer()
        inputStorage:set("agentId", agentId)
        inputStorage:set("amount", amount)
        inputAgentsSt:append(inputStorage)
    end
    storage:set("inputAgents", inputAgentsSt)
    local outputAgentsSt = StorageList()
    for agentId, amount in pairs(self.outputAgents) do
        outputStorage = StorageContainer()
        outputStorage:set("agentId", agentId)
        outputStorage:set("amount", amount)
        outputAgentsSt:append(outputStorage)
    end
    storage:set("outputAgents", outputAgentsSt)
    return storage
end


function ProcessOrganelle:load(storage)
    Organelle.load(self, storage)
    self.originalColour = self._colour
    self.remainingCooldown = storage:get("remainingCooldown", 0)
    local inputAgentsSt = storage:get("inputAgents", {})
    for i = 1,inputAgentsSt:size() do
        local inputStorage = inputAgentsSt:get(i)
        self:addRecipyInput(inputStorage:get("agentId", 0), inputStorage:get("amount", 0))
    end
    local outputAgentsSt = storage:get("outputAgents", {})
    for i = 1,outputAgentsSt:size() do
        local outputStorage = outputAgentsSt:get(i)
        self:addRecipyOutput(outputStorage:get("agentId", 0), outputStorage:get("amount", 0))
    end
end
