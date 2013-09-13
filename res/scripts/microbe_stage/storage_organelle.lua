class 'Vacuole'

function Vacuole:__init(agentId, capacity, amount)
    self.agentId = agentId
    self.capacity = capacity
    self.amount = amount or 0.0
end

function Vacuole.load(storage)
    local agentId = storage:get("agentId", 0)
    local capacity = storage:get("capacity", 0)
    local amount = storage:get("amount", 0)
    return Vacuole(agentId, capacity, amount)
end


function Vacuole:storage()
    local storage = StorageContainer()
    storage:set("agentId", self.agentId)
    storage:set("capacity", self.capacity)
    storage:set("amount", self.amount)
    return storage
end

class 'StorageOrganelle' (Organelle)

function StorageOrganelle:__init(agentId, capacity)
    Organelle.__init(self)
    self._vacuole = Vacuole(agentId, capacity, 0.0)
end


function StorageOrganelle:load(storage)
    Organelle.load(self, storage)
    local vacuoleStorage = storage:get("vacuole", StorageContainer())
    self._vacuole = Vacuole.load(vacuoleStorage)
end


function StorageOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addVacuole(self._vacuole)
end


function StorageOrganelle:onRemovedFromMicrobe(microbe)
    microbe:removeVacuole(self._vacuole)
    Organelle.onRemovedToMicrobe(self, microbe, q, r)
end


function StorageOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("vacuole", self._vacuole:storage())
    return storage
end


