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
function StorageOrganelle:onAddedToMicrobe(microbeEntity, q, r, rotation, organelle)
    OrganelleComponent.onAddedToMicrobe(self, microbe, q, r, rotation, organelle)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    microbeComponent.capacity = microbeComponent.capacity + self.capacity
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbeEntity, q, r)
    local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
    microbeComponent.capacity = microbeComponent.capacity - self.capacity
end
