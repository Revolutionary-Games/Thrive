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
    end
)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Overridded from ProcessOrganelle:onAddedToMicrobe
function AgentVacuole:onAddedToMicrobe(microbeEntity, q, r, rotation, organelle)
    OrganelleComponent.onAddedToMicrobe(self, microbeEntity, q, r, rotation, organelle)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    if microbeComponent.specialStorageOrganelles[self.compoundId] == nil then
        microbeComponent.specialStorageOrganelles[self.compoundId] = 1
    else
        microbeComponent.specialStorageOrganelles[self.compoundId] = microbeComponent.specialStorageOrganelles[self.compoundId] + 1
    end
end

-- Overridded from ProcessOrganelle:onRemovedFromMicrobe
function AgentVacuole:onRemovedFromMicrobe(microbeEntity, q, r)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    microbeComponent.specialStorageOrganelles[self.compoundId] = microbeComponent.specialStorageOrganelles[self.compoundId] - 1
end

function AgentVacuole:storage()
    local storage = StorageContainer.new()
    storage:set("compoundId", self.compoundId)
    storage:set("q", self.position.q)
    storage:set("r", self.position.r)
    return storage
end

function AgentVacuole:load(storage)
    self.compoundId = storage:get("compoundId", 0)
    self.position = {}
    self.position.q = storage:get("q", 0)
    self.position.r = storage:get("r", 0)
end
