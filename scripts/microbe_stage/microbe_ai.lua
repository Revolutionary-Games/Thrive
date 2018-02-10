--------------------------------------------------------------------------------
-- MicrobeAIControllerComponent
--
-- Component for identifying and determining AI controlled microbes.
--------------------------------------------------------------------------------

OXYGEN_SEARCH_THRESHHOLD = 8
GLUCOSE_SEARCH_THRESHHOLD = 5
AI_MOVEMENT_SPEED = 0.5
microbes_number = {}
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
        self.microbeEntities = EntityFilter.new(
          {
            MicrobeComponent
          },
true		  
		  )
self.microbes = {}
        self.preyCandidates = {}
        self.preyEntityToIndexMap = {} -- Used for removing from preyCandidates
        self.currentPreyIndex = 0
        self.oxygenEmitters = {}
        self.glucoseEmitters = {}
        self.preys = {} --table for preys
		self.p = nil --the final prey the cell should hunt
		self.preyMaxHitpoints = 100000 --i need it to be very big for now it will get changed
		self.preycount = 0 --counting number of frames so the prey get updated the fittest prey
		self.preyEscaped = false --checking if the prey escaped
		self.predators = {} --table for predadtors the cell should run from
		self.predator = nil --the final predator the cell shall run from
    end
)

function MicrobeAISystem:init(gameState)
    LuaSystem.init(self, "MicrobeAISystem", gameState)
    self.entities:init(gameState.wrapper)
    self.emitters:init(gameState.wrapper)
	self.microbeEntities:init(gameState.wrapper)
end


function MicrobeAISystem:shutdown()
    LuaSystem.shutdown(self)
    self.entities:shutdown()
    self.emitters:shutdown()
	self.microbeEntities:shutdown()
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
        local microbeEntity = Entity.new(entityId, self.gameState.wrapper)
        self.microbes[entityId] = microbeEntity
        
        -- This is a hack to remember up to 5 recent microbes as candidates for predators. 
        -- Gives something semi random
        self.preyCandidates[self.currentPreyIndex] = microbeEntity
        self.preyEntityToIndexMap[entityId] = self.currentPreyIndex
        self.currentPreyIndex = (self.currentPreyIndex)%6
        
    end

	--for removing cell from table when it is removed from the world
	for _, entityId in pairs(self.microbeEntities:removedEntities()) do
        microbes_number[entityId] = nil
	end

	--for counting all the cells in the world and get it's entity
    for _, entityId in pairs(self.microbeEntities:addedEntities()) do
        local microbeEntity = Entity.new(entityId, self.gameState.wrapper)
        microbes_number[entityId] = microbeEntity
	end

    -- Does this actually do something?
    for _, entityId in pairs(self.emitters:removedEntities()) do
        self.oxygenEmitters[entityId] = nil
        self.glucoseEmitters[entityId] = nil
    end
	
    for _, entityId in pairs(self.emitters:addedEntities()) do
        local emitterComponent = getComponent(entityId, self.gameState, CompoundEmitterComponent)
		if emitterComponent ~= nil then --for making sure the emmitterComponent get set before
			if emitterComponent.compoundId == CompoundRegistry.getCompoundId("oxygen") then
				self.oxygenEmitters[entityId] = true
			elseif emitterComponent.compoundId == CompoundRegistry.getCompoundId("glucose") then
				self.glucoseEmitters[entityId] = true
			end
		end
    end
    self.emitters:clearChanges()
    self.entities:clearChanges()
	self.microbeEntities:clearChanges()
    for _, microbeEntity in pairs(self.microbes) do
        local aiComponent = getComponent(microbeEntity, MicrobeAIControllerComponent)
        local microbeComponent = getComponent(microbeEntity, MicrobeComponent)
        local sceneNodeComponent = getComponent(microbeEntity, OgreSceneNodeComponent)

        aiComponent.intervalRemaining = aiComponent.intervalRemaining + logicTime
        while aiComponent.intervalRemaining > aiComponent.reevalutationInterval do
            aiComponent.intervalRemaining = aiComponent.intervalRemaining - aiComponent.reevalutationInterval

            local compoundId = CompoundRegistry.getCompoundId("oxytoxy")
            local targetPosition = nil
            local numberOfAgentVacuoless = microbeComponent.specialStorageOrganelles[compoundId]

            --for getting the prey
            for m_microbeEntityId, m_microbeEntity in pairs (microbes_number) do
                local m_microbeComponent = getComponent(m_microbeEntity, MicrobeComponent)
                local m_sceneNodeComponent = getComponent(m_microbeEntity, OgreSceneNodeComponent)

                if self.preys ~= nil then
                    local v = (m_sceneNodeComponent.transform.position - sceneNodeComponent.transform.position)
                    if v:length() < 25 and  v:length() ~= 0  then
                        if microbeComponent.maxHitpoints > 1.5 * m_microbeComponent.maxHitpoints then
                            self.preys[m_microbeEntityId] = m_microbeEntity
                        end
                        if numberOfAgentVacuoles ~= nil and numberOfAgentVacuoles ~= 0
                            and (m_microbeComponent.specialStorageOrganelles[compoundId] == nil
                            or m_microbeComponent.specialStorageOrganelles[compoundId] == 0)
                            and self.preys[m_microbeEntityId] == nil then

                            self.preys[m_microbeEntityId] = m_microbeEntity
                        end
                    elseif v:length() > 25 or v:length() == 0 then
                        self.preys[m_microbeEntityId] = nil
                    end
                    if self.preys[m_microbeEntityId] ~= nil then
                        preyMicrobeComponent = getComponent(self.preys[m_microbeEntityId], MicrobeComponent)
                        if preyMicrobeComponent.maxHitpoints <= self.preyMaxHitpoints then
                            self.preyMaxHitpoints = preyMicrobeComponent.maxHitpoints
                            self.p = self.preys[m_microbeEntityId]
                        end
                        self.preycount = self.preycount + 1
                    end
                end
            end

            --for getting the predator
            for predatorEntityId, predatorEntity in pairs (microbes_number) do
                local predatorMicrobeComponent = getComponent(predatorEntity, MicrobeComponent)
                local predatorSceneNodeComponent = getComponent(predatorEntity, OgreSceneNodeComponent)

                local vec = (predatorSceneNodeComponent.transform.position - sceneNodeComponent.transform.position)
                if predatorMicrobeComponent.maxHitpoints > microbeComponent.maxHitpoints * 1.5 and vec:length() < 25 then
                    self.predators[predatorEntityId] = predatorEntity
                end
                if (predatorMicrobeComponent.specialStorageOrganelles[compoundId] ~= nil and predatorMicrobeComponent.specialStorageOrganelles[compoundId] ~= 0)
                    and (numberOfAgentVacuoles == nil or numberOfAgentVacuoles == 0) and vec:length() < 25 then
                    self.predators[predatorEntityId] = predatorEntity
                end
                if vec:length() > 25 then
                    self.predators[predatorEntityId] = nil
                end
                self.predator = self.predators[predatorEntityId]
            end
	
            if (numberOfAgentVacuoles ~= nil and numberOfAgentVacuoles ~= 0) or microbeComponent.maxHitpoints > 100 then
                self.preyCandidates[6] = Entity.new(PLAYER_NAME, self.gameState.wrapper)
                self.preyEntityToIndexMap[Entity.new(PLAYER_NAME, self.gameState.wrapper).id] = 6
                local attempts = 0
                while (aiComponent.prey == nil or not aiComponent.prey:exists()
                    or getComponent( aiComponent.prey, MicrobeComponent) == nil
                    or getComponent( aiComponent.prey, MicrobeComponent).dead
                    or (getComponent(aiComponent.prey, MicrobeComponent).speciesName == microbeComponent.speciesName)
                    or self.preyEntityToIndexMap[aiComponent.prey.id] == nil or self.preyEscaped == true)
                    and attempts < 6 and self.preycount > 10 do

                    aiComponent.prey = self.p --setting the prey
                    attempts = attempts + 1  
                    self.preyEscaped = false					
                end

				if self.predator ~= nil then -- for running away from the predadtor
                    local predatorSceneNodeComponent = getComponent(self.predator, OgreSceneNodeComponent)
                    microbeComponent.facingTargetPoint = Vector3(-predatorSceneNodeComponent.transform.position.x, -predatorSceneNodeComponent.transform.position.y, 0)
                    microbeComponent.movementDirection = Vector3(0, AI_MOVEMENT_SPEED, 0)
				end

                if attempts < 6 and aiComponent.prey ~= nil and self.predator == nil then --making sure it is not a prey for someone before start hunting
                    local preyMicrobeComponent = getComponent(aiComponent.prey, MicrobeComponent)
                    local preySceneNodeComponent = getComponent(aiComponent.prey, OgreSceneNodeComponent)

                    vec = (preySceneNodeComponent.transform.position - sceneNodeComponent.transform.position)
                    if vec:length() > 25 then
                        self.preyEscaped = true
                    end
                    if vec:length() < 25 and vec:length() > 10
                        and MicrobeSystem.getCompoundAmount(microbeEntity, compoundId) > MINIMUM_AGENT_EMISSION_AMOUNT
                        and microbeComponent.microbetargetdirection < 10 then
						MicrobeSystem.emitAgent(microbeEntity, CompoundRegistry.getCompoundId("oxytoxy"), 1)
                    elseif vec:length() < 10
                        and microbeComponent.maxHitpoints > ENGULF_HP_RATIO_REQ * preyMicrobeComponent.maxHitpoints
                        and not microbeComponent.engulfMode then
                        MicrobeSystem.toggleEngulfMode(microbeEntity)
                    elseif vec:length() > 15  and microbeComponent.engulfMode then
                        MicrobeSystem.toggleEngulfMode(microbeEntity)
                    end
                    
                    vec:normalise()
                    aiComponent.direction = vec
                    microbeComponent.facingTargetPoint = Vector3(preySceneNodeComponent.transform.position.x, preySceneNodeComponent.transform.position.y, 0)  
                    microbeComponent.movementDirection = Vector3(0, AI_MOVEMENT_SPEED, 0)
				end
            else
                if MicrobeSystem.getCompoundAmount(microbeEntity, CompoundRegistry.getCompoundId("oxygen")) <= OXYGEN_SEARCH_THRESHHOLD then
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
                elseif MicrobeSystem.getCompoundAmount(microbeEntity, CompoundRegistry.getCompoundId("glucose")) <= GLUCOSE_SEARCH_THRESHHOLD then
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
                local vec = (targetPosition - sceneNodeComponent.transform.position)
                vec:normalise()
                aiComponent.direction = vec
                microbeComponent.facingTargetPoint = targetPosition 
                microbeComponent.movementDirection = Vector3(0,AI_MOVEMENT_SPEED,0)
            end
        end
    end
end
