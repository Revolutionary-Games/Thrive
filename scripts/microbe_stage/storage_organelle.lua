--------------------------------------------------------------------------------
-- A storage organelle class
--------------------------------------------------------------------------------
StorageOrganelle = class(
    OrganelleComponent,
    -- Constructor
    --
    -- @param arguments.capacity
    -- The maximum stored amount
    function(self, arguments, data)
        
        OrganelleComponent.create(self, arguments, data)
        
        --making sure this doesn't run when load() is called
        if arguments == nil and data == nil then
            return
        end

        self.capacity = arguments.capacity
        self.parentIndex = 0
        return self
    end
)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.


function StorageOrganelle:load(storage)
    self.capacity = storage:get("capacity", 100)
end

function StorageOrganelle:storage()
    local storage = StorageContainer.new()
    storage:set("capacity", self.capacity)
    return storage
end

-- Overridded from Organelle:onAddedToMicrobe
function StorageOrganelle:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    OrganelleComponent.onAddedToMicrobe(self, microbe, q, r, rotation, organelle)
    self.parentIndex = microbe:addStorageOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeStorageOrganelle(self)
end

-- Empty override function to prevent mesh from being altered.
function StorageOrganelle:updateColour(organelle)
end
