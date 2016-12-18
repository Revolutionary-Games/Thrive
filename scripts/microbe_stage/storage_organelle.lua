--------------------------------------------------------------------------------
-- A storage organelle class
--------------------------------------------------------------------------------
class 'StorageOrganelle' (Organelle)

-- Constructor
--
-- @param capacity
-- The maximum stored amount
--
-- @param mass
-- How heavy this organelle is
function StorageOrganelle:__init(capacity)
    self.capacity = capacity
    self.parentIndex = 0
end

function StorageOrganelle:load(storage)
    --Organelle.load(self, storage)
    self.capacity = storage:get("capacity", 100)
end

function StorageOrganelle:storage(storage)
    --local storage = Organelle.storage(self)
    storage:set("capacity", self.capacity)
    return storage
end

-- Overridded from Organelle:onAddedToMicrobe
function StorageOrganelle:onAddedToMicrobe(microbe, q, r, rotation)
    --Organelle.onAddedToMicrobe(self, microbe, q, r, rotation)
    parentIndex = microbe:addStorageOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbe, q, r)
    --Organelle.onRemovedFromMicrobe(self, microbe, q, r)
    microbe:removeStorageOrganelle(self)
end

function OrganelleFactory.make_vacuole(data, baseOrganelle)
    StorageOrganelle.__init(baseOrganelle, 100.0)
end

function OrganelleFactory.render_vacuole(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
	data.sceneNode[1].meshName = "vacuole.mesh"
	data.sceneNode[1].transform.position = translation
	data.sceneNode[1].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
	
	data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
end

-- Should eventually have its own file.
function OrganelleFactory.make_cytoplasm(data, baseOrganelle)
    StorageOrganelle.__init(baseOrganelle, 10.0)
end

function OrganelleFactory.render_cytoplasm(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
    
    data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
end
