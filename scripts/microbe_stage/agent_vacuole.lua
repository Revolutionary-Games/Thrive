--------------------------------------------------------------------------------
-- Class for Organelles capable of producing and storing agents
--------------------------------------------------------------------------------
AgentVacuole = class(
    OrganelleComponent,
    -- Constructor
    --
    -- @param arguments.process
    --  The process that creates the agent this organelle produces.
    --
    -- @param arguments.compound
    --  The agent this organelle produces.
    function(self, arguments, data)

        OrganelleComponent.create(self, arguments, data)
        
        --making sure this doesn't run when load() is called
        if arguments == nil and data == nil then
            return
        end

        self.position = {}
        self.position.q = data.q
        self.position.r = data.r
        self.compoundId = CompoundRegistry.getCompoundId(arguments.compound)
        self.capacityIntervalTimer = PROCESS_CAPACITY_UPDATE_INTERVAL
        
    end
)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Overridded from ProcessOrganelle:onAddedToMicrobe
function AgentVacuole:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    OrganelleComponent.onAddedToMicrobe(self, microbe, q, r, rotation, organelle)
    microbe:addSpecialStorageOrganelle(self, self.compoundId)
end

-- Overridded from ProcessOrganelle:onRemovedFromMicrobe
function AgentVacuole:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeSpecialStorageOrganelle(self, self.compoundId)
end

-- Called by Microbe:update
--
-- Produces the agent for the process at intervals
--
-- @param microbe
-- The microbe containing the organelle
--
-- @param logicTime
-- The time since the last call to update()
function AgentVacuole:update(microbe, organelle, logicTime)
end

-- Empty override function to prevent mesh from being altered.
function AgentVacuole:updateColour(organelle)
end


function AgentVacuole:storage()
    local storage = StorageContainer()
    storage:set("compoundId", self.compoundId)
    storage:set("q", self.position.q)
    storage:set("r", self.position.r)
    storage:set("capacityIntervalTimer", self.capacityIntervalTimer)
    return storage
end


function AgentVacuole:load(storage)
    self.compoundId = storage:get("compoundId", 0)
    self.position = {}
    self.position.q = storage:get("q", 0)
    self.position.r = storage:get("r", 0)
    self.capacityIntervalTimer = storage:get("capacityIntervalTimer", 0)
end
