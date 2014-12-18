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
    self.searchedCompoundId = nil
    self.prey = nil
end

function MicrobeAIControllerComponent:storage()
   
    local storage = Component.storage(self)
    storage:set("movementRadius", self.movementRadius)
    storage:set("reevalutationInterval", self.reevalutationInterval)
    storage:set("intervalRemaining", self.intervalRemaining)
    storage:set("direction", self.direction)
    if self.targetEmitterPosition == nil then
        storage:set("targetEmitterPosition", "nil")
    else
        storage:set("targetEmitterPosition", self.targetEmitterPosition)
    end
    if self.searchedCompoundId == nil then
        storage:set("searchedCompoundId", "nil")
    else
        storage:set("searchedCompoundId", self.searchedCompoundId)
    end
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
    self.searchedCompoundId = storage:get("searchedCompoundId", nil)
    if self.searchedCompoundId == "nil" then
        self.searchedCompoundId = nil
    end
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
            CompoundEmitterComponent
        }, 
        true
    )
    self.microbes = {}
    self.preyCandidates = {}
    self.preyEntityToIndexMap = {} -- Used for removing from preyCandidates
    self.currentPreyIndex = 0
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


function MicrobeAISystem:update(renderTime, logicTime)
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
        if self.preyEntityToIndexMap[entityId] then
            self.preyCandidates[self.preyEntityToIndexMap[entityId]] = nil
            self.preyEntityToIndexMap[entityId] = nil
        end
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
        
        -- This is a hack to remember up to 5 recent microbes as candidates for predators. 
        -- Gives something semi random
        self.preyCandidates[self.currentPreyIndex] = microbe
        self.preyEntityToIndexMap[entityId] = self.currentPreyIndex
        self.currentPreyIndex = (self.currentPreyIndex)%6
        
    end
    
    for entityId in self.emitters:removedEntities() do
        self.oxygenEmitters[entityId] = nil
        self.glucoseEmitters[entityId] = nil
    end
    for entityId in self.emitters:addedEntities() do
        local emitterComponent = Entity(entityId):getComponent(CompoundEmitterComponent.TYPE_ID)
		if emitterComponent ~= nil then -- TODO: Unsure why this is necessary
			if emitterComponent.compoundId == CompoundRegistry.getCompoundId("oxygen") then
				self.oxygenEmitters[entityId] = true
			elseif emitterComponent.compoundId == CompoundRegistry.getCompoundId("glucose") then
				self.glucoseEmitters[entityId] = true
			end
		end
    end
    self.emitters:clearChanges()
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        local aiComponent = microbe:getComponent(MicrobeAIControllerComponent.TYPE_ID)
        aiComponent.intervalRemaining = aiComponent.intervalRemaining + logicTime
        while aiComponent.intervalRemaining > aiComponent.reevalutationInterval do
            aiComponent.intervalRemaining = aiComponent.intervalRemaining - aiComponent.reevalutationInterval
            local targetPosition = nil
            if microbe.microbe.speciesName == "ToxinPredator" or microbe.microbe.speciesName == "Glutony" then
                self.preyCandidates[6] = Microbe(Entity(PLAYER_NAME))
                self.preyEntityToIndexMap[6] = Entity(PLAYER_NAME).id
                local attempts = 0
                while (aiComponent.prey  == nil or not aiComponent.prey:exists() or aiComponent.prey.microbe.dead or
                      (aiComponent.prey.microbe.speciesName ==  microbe.microbe.speciesName) or
                       self.preyEntityToIndexMap[aiComponent.prey.entity.id] == nil) and attempts < 6 do
                    aiComponent.prey = self.preyCandidates[rng:getInt(0, 6)]
                    attempts = attempts + 1       
                end
                if attempts < 6 then
                    local vec = (aiComponent.prey.sceneNode.transform.position - microbe.sceneNode.transform.position)
                    
                    if vec:length() < 10 then 
                        if microbe.microbe.speciesName == "ToxinPredator" then
                            microbe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 1)
                        elseif microbe.microbe.speciesName == "Glutony" and not microbe.microbe.engulfMode then
                            microbe:toggleEngulfMode()
                        end
                    elseif microbe.microbe.speciesName == "Glutony" and microbe.microbe.engulfMode then
                        microbe:toggleEngulfMode()
                    end
                    
                    vec:normalise()
                    aiComponent.direction = vec
                    microbe.microbe.facingTargetPoint = Vector3(aiComponent.prey.sceneNode.transform.position.x,aiComponent.prey.sceneNode.transform.position.y, 0)  
                    microbe.microbe.movementDirection = Vector3(0,AI_MOVEMENT_SPEED,0)
                end
            else
                if microbe:getCompoundAmount(CompoundRegistry.getCompoundId("oxygen")) <= OXYGEN_SEARCH_THRESHHOLD then
                    -- If we are NOT currenty heading towards an emitter
                    if aiComponent.targetEmitterPosition == nil or aiComponent.searchedCompoundId ~= CompoundRegistry.getCompoundId("oxygen") then
                        aiComponent.searchedCompoundId = CompoundRegistry.getCompoundId("oxygen")
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
                elseif microbe:getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) <= GLUCOSE_SEARCH_THRESHHOLD then
                    -- If we are NOT currenty heading towards an emitter
                    if aiComponent.targetEmitterPosition == nil or aiComponent.searchedCompoundId ~= CompoundRegistry.getCompoundId("glucose") then
                    aiComponent.searchedCompoundId = CompoundRegistry.getCompoundId("glucose")
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
end
