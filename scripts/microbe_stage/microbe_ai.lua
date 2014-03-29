--------------------------------------------------------------------------------
-- MicrobeAIControllerComponent
--
-- Component for identifying and determining AI controlled microbes.
--------------------------------------------------------------------------------
class 'MicrobeAIControllerComponent' (Component)

OXYGEN_SEARCH_THRESHHOLD = 8
GLUCOSE_SEARCH_THRESHHOLD = 5
AI_MOVEMENT_SPEED = 0.5


function MicrobeAIControllerComponent:__init()
    Component.__init(self)
    self.movementRadius = 20
    self.reevalutationInterval = 1000
    self.intervalRemaining = self.reevalutationInterval
    self.direction = Vector3(0, 0, 0)
    self.targetEmitterPosition = nil
    self.searchedAgentId = nil
end

function MicrobeAIControllerComponent:storage()
   
    local storage = Component.storage(self)
    storage:set("movementRadius", self.movementRadius)
    storage:set("reevalutationInterval", self.reevalutationInterval)
    storage:set("intervalRemaining", self.intervalRemaining)
    storage:set("direction", self.direction)
    if targetEmitterPosition == nil then
        storage:set("targetEmitterPosition", "nil")
    else
        storage:set("targetEmitterPosition", self.targetEmitterPosition)
    end
    storage:set("searchedAgentId", self.searchedAgentId)
    
    return storage
end

function MicrobeAIControllerComponent:load(storage)
    Component.load(self, storage)
    self.movementRadius = storage:get("movementRadius", 20)
    self.reevalutationInterval = storage:get("reevalutationInterval", 1000)
    self.intervalRemaining = storage:get("intervalRemaining", self.reevalutationInterval)
    self.direction = storage:get("direction", Vector3(0, 0, 0))
    local emitterPosition = storage:get("targetEmitterPosition", nil)
    if emitterPosition == "nil" then
        self.targetEmitterPosition = nil
    else
        self.targetEmitterPosition = emitterPosition
    end
    self.searchedAgentId = storage:get("searchedAgentId", nil)
end

REGISTER_COMPONENT("MicrobeAIControllerComponent", MicrobeAIControllerComponent)


--------------------------------------------------------------------------------
-- MicrobeAISystem
--
-- Updates AI controlled microbes
--------------------------------------------------------------------------------

class 'MicrobeAISystem' (System)

function MicrobeAISystem:__init()
    System.__init(self)
    self.entities = EntityFilter(
        {
            MicrobeAIControllerComponent,
            MicrobeComponent
        }, 
        true
    )
    self.emitters = EntityFilter(
        {
            AgentEmitterComponent
        }, 
        true
    )
    self.microbes = {}
    self.oxygenEmitters = {}
    self.glucoseEmitters = {}
end


function MicrobeAISystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)
    self.emitters:init(gameState)
end


function MicrobeAISystem:shutdown()
    System.shutdown(self)
    self.entities:shutdown()
    self.emitters:shutdown()
end


function MicrobeAISystem:update(milliseconds)
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
    end
    
    for entityId in self.emitters:removedEntities() do
        self.oxygenEmitters[entityId] = nil
        self.glucoseEmitters[entityId] = nil
    end
    for entityId in self.emitters:addedEntities() do
        local emitterComponent = Entity(entityId):getComponent(AgentEmitterComponent.TYPE_ID)
        if emitterComponent ~= nil then
            if emitterComponent.agentId == AgentRegistry.getAgentId("oxygen") then
                self.oxygenEmitters[entityId] = true
            elseif emitterComponent.agentId == AgentRegistry.getAgentId("glucose") then
                self.glucoseEmitters[entityId] = true
            end
        end
    end
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        local aiComponent = microbe:getComponent(MicrobeAIControllerComponent.TYPE_ID)
        aiComponent.intervalRemaining = aiComponent.intervalRemaining + milliseconds
        while aiComponent.intervalRemaining > aiComponent.reevalutationInterval do
            aiComponent.intervalRemaining = aiComponent.intervalRemaining - aiComponent.reevalutationInterval
            
            local targetPosition = nil
            if microbe:getAgentAmount(AgentRegistry.getAgentId("oxygen")) <= OXYGEN_SEARCH_THRESHHOLD then
                -- If we are NOT currenty heading towards an emitter
                if aiComponent.targetEmitterPosition == nil or aiComponent.searchedAgentId ~= AgentRegistry.getAgentId("oxygen") then
                    aiComponent.searchedAgentId = AgentRegistry.getAgentId("oxygen")
                    local emitterArrayList = {}
                    local i = 0
                    for emitterId, _ in pairs(self.oxygenEmitters) do
                        i = i + 1
                        emitterArrayList[i] = emitterId
                    end     
                    if i ~= 0 then
                        local emitterEntity = Entity(emitterArrayList[rng:getInt(1, i)])
                        aiComponent.targetEmitterPosition = emitterEntity:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position     
                    end  
                end
                targetPosition = aiComponent.targetEmitterPosition           
                if aiComponent.targetEmitterPosition ~= nil and aiComponent.targetEmitterPosition.z ~= 0 then
                    aiComponent.targetEmitterPosition = nil
                end             
            elseif microbe:getAgentAmount(AgentRegistry.getAgentId("glucose")) <= GLUCOSE_SEARCH_THRESHHOLD then
                -- If we are NOT currenty heading towards an emitter
                if aiComponent.targetEmitterPosition == nil or aiComponent.searchedAgentId ~= AgentRegistry.getAgentId("glucose") then
                aiComponent.searchedAgentId = AgentRegistry.getAgentId("glucose")
                    local emitterArrayList = {}
                    local i = 0
                    for emitterId, _ in pairs(self.glucoseEmitters) do
                        i = i + 1
                        emitterArrayList[i] = emitterId
                    end     
                    if i ~= 0 then
                        local emitterEntity = Entity(emitterArrayList[rng:getInt(1, i)])
                        aiComponent.targetEmitterPosition = emitterEntity:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position         
                    end
                end
                targetPosition = aiComponent.targetEmitterPosition
                if aiComponent.targetEmitterPosition ~= nil and aiComponent.targetEmitterPosition.z ~= 0 then
                    aiComponent.targetEmitterPosition = nil
                end    
            else
                aiComponent.targetEmitterPosition = nil
            end
            if aiComponent.targetEmitterPosition == nil then
                local randAngle = rng:getReal(0, 2*math.pi)
                local randDist = rng:getInt(10, aiComponent.movementRadius)
                targetPosition = Vector3(math.cos(randAngle)* randDist, 
                                         math.sin(randAngle)* randDist, 0)
            end
            local vec = (targetPosition - microbe.sceneNode.transform.position)
            vec:normalise()
            aiComponent.direction = vec
            microbe.microbe.facingTargetPoint = targetPosition 
            microbe.microbe.movementDirection = Vector3(0,AI_MOVEMENT_SPEED,0)
            
        end
    end
end
