--------------------------------------------------------------------------------
-- Class for Organelles capable of producing and storing agents
--------------------------------------------------------------------------------
class 'AgentVacuole' (ProcessOrganelle)

-- Constructor
function AgentVacuole:__init(compoundId, process)
    local mass = 0.3
    ProcessOrganelle.__init(self, mass)
    self.process = process
    self.compoundId = compoundId
end

-- Overwridden from ProcessOrganelle
function AgentVacuole:addProcess(process)
    assert(false, "AgentVacuoles are not supposed to have additional processes")
end

-- Overridded from ProcessOrganelle:onAddedToMicrobe
function AgentVacuole:onAddedToMicrobe(microbe, q, r, rotation)
    ProcessOrganelle.onAddedToMicrobe(self, microbe, q, r, rotation)
    microbe:addSpecialStorageOrganelle(self, self.compoundId)
end

-- Overridded from ProcessOrganelle:onRemovedFromMicrobe
function AgentVacuole:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeSpecialStorageOrganelle(self, self.compoundId)
    ProcessOrganelle.onRemovedFromMicrobe(self, microbe, q, r)
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
function AgentVacuole:update(microbe, logicTime)
    Organelle.update(self, microbe, logicTime)
    
    self.capacityIntervalTimer = self.capacityIntervalTimer + logicTime
    if self.capacityIntervalTimer > PROCESS_CAPACITY_UPDATE_INTERVAL then
        factorProduct = self.process:produce(self.capacityIntervalTimer, 1.0, microbe, self)
        self.capacityIntervalTimer = 0
    end
end


function AgentVacuole:storage()
    local storage = ProcessOrganelle.storage(self)
    storage:set("compoundId", self.compoundId)
    storage:set("process", self.process:storage())
    return storage
end


function AgentVacuole:load(storage)
    ProcessOrganelle.load(self, storage)
    self.compoundId = storage:get("compoundId", 0)
    local process = Process(0, 0, {},{})
    process:load(storage:get("process", 0))
    self.process = process
end

Organelle.mpCosts["oxytoxy"] = 40

-- factory functions
function OrganelleFactory.make_oxytoxy(data)
    local agentVacuole = AgentVacuole(CompoundRegistry.getCompoundId("oxytoxy"), global_processMap["OxyToxySynthesis"])
    agentVacuole:setColour(ColourValue(0, 1, 1, 0))
    agentVacuole.colourChangeFactor = 0.15
    return agentVacuole
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
