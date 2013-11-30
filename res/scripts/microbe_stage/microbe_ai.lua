--------------------------------------------------------------------------------
-- MicrobeAIComponent
--
-- Component for identifying and determining AI controlled microbes.
--------------------------------------------------------------------------------
class 'MicrobeAIComponent' (Component)

function MicrobeAIComponent:__init()
    Component.__init(self)
    self.movementRadius = 8
    self.reevalutationInterval = 500
    self.intervalRemaining = self.reevalutationInterval
    self.organelles = {}
    self.direction = Vector3(0, 0, 0)
    self.initialized = false
end


function MicrobeAIComponent:load(storage)
    Component.load(self, storage)

end


function MicrobeAIComponent:storage()
    local storage = Component.storage(self)

end

REGISTER_COMPONENT("MicrobeAIComponent", MicrobeAIComponent)


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
            MicrobeAIComponent,
            MicrobeComponent
        }, 
        true
    )
    self.microbes = {}      -- Have the same owning entities as corresponding
end


function MicrobeAISystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)
end


function MicrobeAISystem:shutdown()
    self.entities:shutdown()
end


function MicrobeAISystem:update(milliseconds)
    for entityId in self.entities:removedEntities() do
        self.microbes[entityId] = nil
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Microbe(Entity(entityId))
        self.microbes[entityId] = microbe
    end
    self.entities:clearChanges()
    for _, microbe in pairs(self.microbes) do
        local aiComponent = microbe.aiController
        aiComponent.intervalRemaining = aiComponent.intervalRemaining + milliseconds
        while aiComponent.intervalRemaining > aiComponent.reevalutationInterval do
            aiComponent.intervalRemaining = aiComponent.intervalRemaining - aiComponent.reevalutationInterval
            local randAngle = rng:getReal(0, 2*math.pi)
            local randomTarget = Vector3(math.cos(randAngle), 
                                         math.sin(randAngle), 0)
            aiComponent.direction = randomTarget
        end
        
        microbe.microbe.movementDirection = aiComponent.direction
    end
end
