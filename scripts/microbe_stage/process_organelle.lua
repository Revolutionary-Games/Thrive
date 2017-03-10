--------------------------------------------------------------------------------
-- Class for Organelles capable of producing compounds
--------------------------------------------------------------------------------
ProcessOrganelle = class(
    OrganelleComponent,
    -- Constructor
    --
    -- @param arguments.colourChangeFactor
    --  I got absolutely no idea
    --  what this does :P. Also it doesn't seem to be used anymore
    function(self, arguments, data)

        OrganelleComponent.create(self, arguments, data)
        
        --making sure this doesn't run when load() is called
        if arguments == nil and data == nil then
            return
        end

    end
)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

PROCESS_CAPACITY_UPDATE_INTERVAL = 1000

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
    local storage = StorageContainer.new()
    return storage
end

function ProcessOrganelle:load(storage)
end
