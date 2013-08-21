class 'StorageOrganelle' (Organelle)

function StorageOrganelle:__init(agentId, capacity)
    Organelle.__init(self)
    self._vacuole = {
        agentId = agentId,
        capacity = capacity,
        amount = 0.0
    }
end


function StorageOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addVacuole(self._vacuole)
end


function StorageOrganelle:onRemovedFromMicrobe(microbe)
    microbe:removeVacuole(self._vacuole)
    Organelle.onRemovedToMicrobe(self, microbe, q, r)
end


