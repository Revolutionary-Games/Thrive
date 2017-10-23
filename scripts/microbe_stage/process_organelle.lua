--------------------------------------------------------------------------------
-- Class for organelles capable of producing compounds.
-- TODO: Make this handle adding and removing processes from the microbes.
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

-- Adds a process to the processing organelle
-- The organelle will distribute its capacity between processes
--
-- @param process
-- The process to add
function ProcessOrganelle:addProcess(process)
    -- table.insert(self.processes, process)
end

function ProcessOrganelle:storage()
    return StorageContainer.new()
end

function ProcessOrganelle:load(storage)
end
