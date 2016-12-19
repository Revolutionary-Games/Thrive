--------------------------------------------------------------------------------
-- Class for Organelles capable of producing and storing agents
--------------------------------------------------------------------------------
class 'AgentVacuole' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Constructor
--
-- @param arguments.process
--  The process that creates the agent this organelle produces.
--
-- @param arguments.compound
--  The agent this organelle produces.
function AgentVacuole:__init(arguments, data)
    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end

    self.position = {}
    self.position.q = data.q
    self.position.r = data.r
    self.process = global_processMap[arguments.process]
    self.compoundId = CompoundRegistry.getCompoundId(arguments.compound)
    self.capacityIntervalTimer = PROCESS_CAPACITY_UPDATE_INTERVAL
    return self
end

-- Overridded from ProcessOrganelle:onAddedToMicrobe
function AgentVacuole:onAddedToMicrobe(microbe, q, r, rotation)
    microbe:addSpecialStorageOrganelle(self, self.compoundId)
end

-- Overridded from ProcessOrganelle:onRemovedFromMicrobe
function AgentVacuole:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeSpecialStorageOrganelle(self, self.compoundId)
end

-- Called by Microbe:update
--
-- Produces the agent for the process at intervals
--
-- @param microbe
-- The microbe containing the organelle
--
-- @param logicTime
-- The time since the last call to update()
function AgentVacuole:update(microbe, organelle, logicTime)
    self.capacityIntervalTimer = self.capacityIntervalTimer + logicTime
    if self.capacityIntervalTimer > PROCESS_CAPACITY_UPDATE_INTERVAL then
        factorProduct = self.process:produce(self.capacityIntervalTimer, 1.0, microbe, self)
        self.capacityIntervalTimer = 0
    end
end


function AgentVacuole:storage()
    local storage = StorageContainer()
    storage:set("compoundId", self.compoundId)
    storage:set("q", self.position.q)
    storage:set("r", self.position.r)
    storage:set("process", self.process:storage())
    storage:set("capacityIntervalTimer", self.capacityIntervalTimer)
    return storage
end


function AgentVacuole:load(storage)
    self.compoundId = storage:get("compoundId", 0)
    self.position = {}
    self.position.q = storage:get("q", 0)
    self.position.r = storage:get("r", 0)
    self.capacityIntervalTimer = storage:get("capacityIntervalTimer", 0)
    local process = Process(0, 0, {},{})
    process:load(storage:get("process", 0))
    self.process = process
end

function OrganelleFactory.render_oxytoxy(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
	data.sceneNode[1].meshName = "AgentVacuole.mesh"
	data.sceneNode[1].transform.position = translation
	data.sceneNode[1].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
	
    data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
end
