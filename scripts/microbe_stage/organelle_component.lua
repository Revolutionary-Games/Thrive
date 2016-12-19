--Base class for organelle components
class "OrganelleComponent"

-- Constructor
function OrganelleComponent:__init(arguments, data)
end

function OrganelleComponent:onAddedToMicrobe(microbe, q, r, rotation)
end

function OrganelleComponent:onRemovedFromMicrobe(microbe, q, r)
end

function OrganelleComponent:update(microbe, organelle, logicTime)
end

function OrganelleComponent:storage()
    return StorageContainer()
end

function OrganelleComponent:load(storage)
end