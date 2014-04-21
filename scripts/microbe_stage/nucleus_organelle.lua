--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (Organelle)

-- Constructor
function NucleusOrganelle:__init()
    Organelle.__init(self)
end


-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    Organelle.onRemovedFromMicrobe(self, microbe, q, r)
end


function NucleusOrganelle:storage()
    local storage = Organelle.storage(self)
    return storage
end


function NucleusOrganelle:load(storage)
    Organelle.load(self, storage)
end
