--------------------------------------------------------------------------------
-- Class for Organelles capable of producing compounds
--------------------------------------------------------------------------------
class 'ProcessOrganelle' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

PROCESS_CAPACITY_UPDATE_INTERVAL = 1000

-- Constructor
--
-- @param arguments.colourChangeFactor
--  I got absolutely no idea what this does :P.
function ProcessOrganelle:__init(arguments, data)
    OrganelleComponent.__init(self, arguments, data)
    
    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end

    return self
end

-- Adds a process to the processing organelle
-- The organelle will distribute its capacity between processes
--
-- @param process
-- The process to add
function ProcessOrganelle:addProcess(process)
    -- table.insert(self.processes, process)
end


-- Overridded from Organelle:onAddedToMicrobe
function ProcessOrganelle:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    OrganelleComponent.onAddedToMicrobe(self, microbe, q, r, rotation, organelle)
end

function ProcessOrganelle:storage(storage)
    local storage = StorageContainer()
    return storage
end

function ProcessOrganelle:load(storage)
end
