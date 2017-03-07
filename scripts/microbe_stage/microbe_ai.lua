--------------------------------------------------------------------------------
-- MicrobeAIControllerComponent
--
-- Component for identifying and determining AI controlled microbes.
--------------------------------------------------------------------------------

OXYGEN_SEARCH_THRESHHOLD = 8
GLUCOSE_SEARCH_THRESHHOLD = 5
AI_MOVEMENT_SPEED = 0.5

MicrobeAIControllerComponent = class(
    function(self)

        self.movementRadius = 20
        self.reevalutationInterval = 1000
        self.intervalRemaining = self.reevalutationInterval
        self.direction = Vector3(0, 0, 0)
        self.targetEmitterPosition = nil
        self.searchedCompoundId = nil
        self.prey = nil
        
    end
)

MicrobeAIControllerComponent.TYPE_NAME = "MicrobeAIControllerComponent"

function MicrobeAIControllerComponent:storage(storage)
    
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
    
end

function MicrobeAIControllerComponent:load(storage)
    
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

MicrobeAISystem = class(
    LuaSystem,
    function(self)
        
        LuaSystem.create(self)

        self.entities = EntityFilter.new(
            {
                MicrobeAIControllerComponent,
                MicrobeComponent
            }, 
            true
        )
        self.emitters = EntityFilter.new(
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
)

function MicrobeAISystem:init(gameState)
    LuaSystem.init(self, "MicrobeAISystem", gameState)
    self.entities:init(gameState.wrapper)
    self.emitters:init(gameState.wrapper)
end


function MicrobeAISystem:shutdown()
    LuaSystem.shutdown(self)
    self.entities:shutdown()
    self.emitters:shutdown()
end

function MicrobeAISystem:update(renderTime, logicTime)
    for _, entityId in pairs(self.entities:removedEntities()) do
        self.microbes[entityId] = nil
        if self.preyEntityToIndexMap[entityId] then
            self.preyCandidates[self.preyEntityToIndexMap[entityId]] = nil
            self.preyEntityToIndexMap[entityId] = nil
        end
    end
    for _, entityId in pairs(self.entities:addedEntities()) do
        local microbe = Microbe(Entity.new(entityId, self.gameState.wrapper))
        self.microbes[entityId] = microbe
        
        -- This is a hack to remember up to 5 recent microbes as candidates for predators. 
        -- Gives something semi random
        self.preyCandidates[self.currentPreyIndex] = microbe
        self.preyEntityToIndexMap[entityId] = self.currentPreyIndex
        self.currentPreyIndex = (self.currentPreyIndex)%6
        
    end
    
    for _, entityId in pairs(self.emitters:removedEntities()) do
        self.oxygenEmitters[entityId] = nil
        self.glucoseEmitters[entityId] = nil
    end
    for _, entityId in pairs(self.emitters:addedEntities()) do
        local emitterComponent = getComponent(entityId, self.gameState, CompoundEmitterComponent)
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
        local aiComponent = getComponent(microbe.entity, MicrobeAIControllerComponent)
        aiComponent.intervalRemaining = aiComponent.intervalRemaining + logicTime
        while aiComponent.intervalRemaining > aiComponent.reevalutationInterval do
            aiComponent.intervalRemaining = aiComponent.intervalRemaining - aiComponent.reevalutationInterval

            local compoundId = CompoundRegistry.getCompoundId("oxytoxy")
            local targetPosition = nil
            local agentVacuole = microbe.microbe.specialStorageOrganelles[compoundId]
            if agentVacuole ~= nil or microbe.microbe.maxHitpoints > 100 then
                self.preyCandidates[6] = Microbe.new(
                    Entity.new(PLAYER_NAME, self.gameState.wrapper))
                self.preyEntityToIndexMap[Entity.new(PLAYER_NAME, self.gameState.wrapper).id] = 6
                local attempts = 0
                while (aiComponent.prey  == nil or not aiComponent.prey:exists() or aiComponent.prey.microbe.dead or
                           (aiComponent.prey.microbe.speciesName ==  microbe.microbe.speciesName) or
                       self.preyEntityToIndexMap[aiComponent.prey.entity.id] == nil) and attempts < 6 do
                    aiComponent.prey = self.preyCandidates[rng:getInt(0, 6)]
                    attempts = attempts + 1       
                end
                if attempts < 6 then
                    local vec = (aiComponent.prey.sceneNode.transform.position - microbe.sceneNode.transform.position)
                    if vec:length() < 25 and microbe:getCompoundAmount(compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT then
                        microbe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 1)
                    elseif vec:length() < 17 and microbe.microbe.maxHitpoints > ENGULF_HP_RATIO_REQ * aiComponent.prey.microbe.maxHitpoints and not microbe.microbe.engulfMode then
                        microbe:toggleEngulfMode()
                    elseif vec:length() > 20 and microbe.microbe.engulfMode then
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
                            local emitterEntity = Entity.new(
                                emitterArrayList[rng:getInt(1, i)], self.gameState.wrapper)
                            
                            aiComponent.targetEmitterPosition = getComponent(
                                emitterEntity, OgreSceneNodeComponent).transform.position     
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

                            local emitterEntity = Entity.new(
                                emitterArrayList[rng:getInt(1, i)], self.gameState.wrapper)
                            
                            aiComponent.targetEmitterPosition = getComponent(
                                emitterEntity, OgreSceneNodeComponent).transform.position     
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
