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
function StorageOrganelle:onAddedToMicrobe(microbe, q, r, rotation)
    Organelle.onAddedToMicrobe(self, microbe, q, r, rotation)
    parentIndex = microbe:addStorageOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbe, q, r)
    Organelle.onRemovedFromMicrobe(self, microbe, q, r)
    microbe:removeStorageOrganelle(self)
end

Organelle.mpCosts["vacuole"] = 15
Organelle.mpCosts["cytoplasm"] = 5

function OrganelleFactory.make_vacuole(data)
    local vacuole = StorageOrganelle(100.0)
    vacuole:addHex(0, 0)
    return vacuole
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

function OrganelleFactory.sizeof_vacuole(data)
    local hexes = {}
	hexes[1] = {["q"]=0, ["r"]=0}
	return hexes
end

-- Should eventually have its own file.
function OrganelleFactory.make_cytoplasm(data)
    local cytoplasm = StorageOrganelle(10.0)
    cytoplasm:addHex(0, 0)
    return cytoplasm
end

function OrganelleFactory.render_cytoplasm(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
    
    data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
end

function OrganelleFactory.sizeof_cytoplasm(data)
    local hexes = {}
	hexes[1] = {["q"]=0, ["r"]=0}
	return hexes
end    
