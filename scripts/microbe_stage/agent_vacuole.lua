--------------------------------------------------------------------------------
-- Class for Organelles capable of producing and storing agents
--------------------------------------------------------------------------------
class 'AgentVacuole' (ProcessOrganelle)

-- Constructor
function AgentVacuole:__init(compoundId, process)
    ProcessOrganelle.__init(self)
    self.storedAmount = 0.0
    self.process = process
    self.compoundId = compoundId
end

-- Overwridden from ProcessOrganelle
function AgentVacuole:addProcess(process)
    assert(false, "AgentVacuoles are not supposed to have additional processes")
end

-- Overridded from ProcessOrganelle:onAddedToMicrobe
function AgentVacuole:onAddedToMicrobe(microbe, q, r)
    ProcessOrganelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addSpecialStorageOrganelle(self, self.compoundId)
end

-- Overridded from ProcessOrganelle:onRemovedFromMicrobe
function AgentVacuole:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeSpecialStorageOrganelle(self, self.compoundId)
    ProcessOrganelle.onRemovedFromMicrobe(self, microbe, q, r)
end

function AgentVacuole:takeCompound(compoundId, maxAmount)
    assert(compoundId == self.compoundId, "Tried to take wrong compound/agent from AgentVacuole")
    takenAmount = math.min(maxAmount, self.storedAmount)
    self.storedAmount = self.storedAmount - takenAmount
    return takenAmount
end

function AgentVacuole:storeCompound(compoundId, amount)
    assert(compoundId == self.compoundId, "Tried to store wrong compound/agent from AgentVacuole")
    self.storedAmount = self.storedAmount + amount
end

-- Called by Microbe:update
--
-- Produces the agent for the process at intervals
--
-- @param microbe
-- The microbe containing the organelle
--
-- @param milliseconds
-- The time since the last call to update()
function AgentVacuole:update(microbe, milliseconds)
    Organelle.update(self, microbe, milliseconds)
    
    self.capacityIntervalTimer = self.capacityIntervalTimer + milliseconds
    if self.capacityIntervalTimer > PROCESS_CAPACITY_UPDATE_INTERVAL then
        factorProduct = self.process:produce(self.capacityIntervalTimer, 1.0, microbe, self)
        self.capacityIntervalTimer = 0
        self._needsColourUpdate = true -- Update colours for displaying speed of production
        self:_updateColourDynamic(factorProduct)
    end
end


function AgentVacuole:storage()
    local storage = ProcessOrganelle.storage(self)
    storage:set("compoundId", self.compoundId)
    storage:set("storedAmount", self.storedAmount)
    storage:set("process", self.process:storage())
    return storage
end


function AgentVacuole:load(storage)
    ProcessOrganelle.load(self, storage)
    self.compoundId = storage:get("compoundId", 0)
    self.storedAmount = storage:get("storedAmount", 0)
    local process = Process(0, 0, {},{})
    process:load(storage:get("process", 0))
    self.process = process
end
