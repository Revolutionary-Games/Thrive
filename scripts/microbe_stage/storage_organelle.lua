--------------------------------------------------------------------------------
-- A storage organelle class
--------------------------------------------------------------------------------
class 'StorageOrganelle' (Organelle)

-- Constructor
--
-- @param capacity
-- The maximum stored amount
function StorageOrganelle:__init(capacity)
    Organelle.__init(self)
    self.capacity = capacity
    self.parentIndex = 0
end

function StorageOrganelle:load(storage)
    Organelle.load(self, storage)
    self.capacity = storage:get("capacity", 100)
end

function StorageOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("capacity", self.capacity)
    return storage
end

-- Overridded from Organelle:onAddedToMicrobe
function StorageOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    parentIndex = microbe:addStorageOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbe, q, r)
    Organelle.onRemovedFromMicrobe(self, microbe, q, r)
    microbe:removeStorageOrganelle(self)
end

Organelle.mpCosts["vacuole"] = 15

function OrganelleFactory.make_vacuole(data)
    local vacuole = StorageOrganelle(100.0)
    vacuole:addHex(0, 0)
    vacuole:setColour(ColourValue(1, 1, 0, 1))
    return vacuole
end
