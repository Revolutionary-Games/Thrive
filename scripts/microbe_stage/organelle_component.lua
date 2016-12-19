--Base class for organelle components.
class "OrganelleComponent"

-- Constructor.
--
-- @param arguments
--  The parameters of the constructor, defined in organelle_table.lua.
--
-- @param data
--  The organelle data taken from the make function in organelle.lua.
--
-- The return value should be the organelle component itself (A.K.A. self)
function OrganelleComponent:__init(arguments, data)
    return self
end

-- Event handler for an organelle added to a microbe.
--
-- @param microbe
--  The microbe this organelle is added to.
--
-- @param q
--  q component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param r
--  r component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param rotation
--  The rotation this organelle has on the microbe.
--  it can be either 0, 60, 120, 180, 240 or 280.
function OrganelleComponent:onAddedToMicrobe(microbe, q, r, rotation)
end

-- Event handler for an organelle removed from a microbe.
--
-- @param microbe
--  The microbe this organelle is removed from.
--
-- @param q
--  q component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
--
-- @param r
--  r component of the organelle relative position in the microbe,
--  in axial coordinates (see hex.lua).
function OrganelleComponent:onRemovedFromMicrobe(microbe, q, r)
end

--  Function executed at regular intervals
--
-- @param microbe
--  The microbe this organelle is attached.
--
-- @param organelle
--  The organelle that has this component.
--
-- @param logicTime
--  The time transcurred (in milliseconds) between this call
--  to OrganelleComponent:update() and the previous one.
function OrganelleComponent:update(microbe, organelle, logicTime)
end

-- Function for saving organelle information.
-- If an organelle depends on an atribute, then it should be saved
-- the data gets retrieved later by OrganelleComponent:load().
-- The return value should be a new StorageContainer object
-- filled with the data to save.
function OrganelleComponent:storage()
    return StorageContainer()
end

-- Function for loading organelle information.
-- 
-- @param storage
--  The StorageContainer object that has the organelle information
--  (the one saved in OrganelleComponent:storage()).
function OrganelleComponent:load(storage)
end