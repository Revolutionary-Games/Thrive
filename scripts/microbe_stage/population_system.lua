
SIMULATION_INTERVAL = 1200

--------------------------------------------------------------------------------
-- SpeciesComponent
--
-- Holds information about an entity spawned by spawnComponent
--------------------------------------------------------------------------------
class 'SpeciesComponent' (Component)

SpeciesComponent.nameCounter = 1

function SpeciesComponent:__init()
    Component.__init(self)
    self.spawnRadiusSqr = 1000
    self.speciesName = ""
    self.scoreChange = 0
    -- Temporary naming solution
    self.speciesName = "" .. SpeciesComponent.nameCounter
    SpeciesComponent.nameCounter = SpeciesComponent.nameCounter + 1
end

function SpeciesComponent:changeSpeciesScore(amount)
    self.scoreChange = self.scoreChange + amount
end

function SpeciesComponent:load(storage)
    Component.load(self, storage)
    self.spawnRadiusSqr = storage:get("spawnRadius", 1000)
end


function SpeciesComponent:storage()
    local storage = Component.storage(self)
    storage:set("spawnRadius", self.spawnRadiusSqr)
    return storage
end

REGISTER_COMPONENT("SpeciesComponent", SpeciesComponent)


--------------------------------------------------------------------------------
-- PopulationSystem
--
-- System for estimating and simulating population count for various species
--------------------------------------------------------------------------------
class 'PopulationSystem' (System)

function PopulationSystem:__init()
    System.__init(self)
    
    self.entities = EntityFilter(
        {
            SpeciesComponent
        },
        true
    )
    
    self.speciesScores = {}
    self.speciesPopulation = {}
    
    self.timeSinceLastCycle = 0 --Stores how much time has passed since the last spawn cycle
end

-- Override from System
function PopulationSystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)
end

-- Override from System
function PopulationSystem:shutdown()
    self.entities:shutdown()
    System.shutdown(self)
end


-- Override from System
function PopulationSystem:update(milliseconds)
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds
    
    --Perform spawn cycle if necessary (Reason for "if" rather than "while" stated below)
    while self.timeSinceLastCycle > SIMULATION_INTERVAL do
        for entityId in self.entities:addedEntities() do
            species = Entity(entityId):getComponent(SpeciesComponent.TYPE_ID)
            if self.speciesScores[species.speciesName] == nil then
                self.speciesScores[species.speciesName] = 0
            end
            if self.speciesPopulation[species.speciesName] == nil then
                self.speciesPopulation[species.speciesName] = 1
            else
                self.speciesPopulation[species.speciesName] = self.speciesPopulation[species.speciesName] + 1
            end
        end
        -- Species will never be removed atm as we can't access a component after it has been removed, this will need a workaround
       -- for entityId in self.entities:removedEntities() do
       --     species = Entity(entityId):getComponent(SpeciesComponent.TYPE_ID)
       --     self.speciesPopulation[species.speciesName] = nil
       --     self.speciesScores[species.speciesName] = nil
       -- end
        self.entities:clearChanges()
        for entityId in self.entities:entities() do
            species = Entity(entityId):getComponent(SpeciesComponent.TYPE_ID)
            
            self.speciesScores[species.speciesName] = self.speciesScores[species.speciesName] + species.scoreChange

            -- Temporary simple calculation model
            self.speciesPopulation[species.speciesName] =  self.speciesPopulation[species.speciesName] + species.scoreChange
            print(self.speciesPopulation[species.speciesName])
            species.scoreChange = 0
        end
        
         self.timeSinceLastCycle = self.timeSinceLastCycle - SIMULATION_INTERVAL
    end
end
