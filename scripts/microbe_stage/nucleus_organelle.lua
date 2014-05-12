--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (ProcessOrganelle)

-- Constructor
function NucleusOrganelle:__init()
    ProcessOrganelle.__init(self)
end


-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r)
    ProcessOrganelle.onAddedToMicrobe(self, microbe, q, r)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    ProcessOrganelle.onRemovedFromMicrobe(self, microbe, q, r)
end


function NucleusOrganelle:storage()
    local storage = ProcessOrganelle.storage(self)
    return storage
end


function NucleusOrganelle:load(storage)
    ProcessOrganelle.load(self, storage)
end
