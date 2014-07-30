
POPULATION_SIMULATION_INTERVAL = 1200

--------------------------------------------------------------------------------
-- SpeciesComponent
--
-- Holds information about an entity representing a species
--------------------------------------------------------------------------------
class 'SpeciesComponent' (Component)

function SpeciesComponent:__init(name)
    Component.__init(self)
    self.name = name
    self.populationBonusFactor = 1.0
    self.populationPenaltyFactor = 1.0
    self.deathsPerTime = 1.0
    self.birthsPerTime = 1.0
    self.currentPopulation = 1
end


--todo
function SpeciesComponent:load(storage)
    Component.load(self, storage)
    self.spawnRadiusSqr = storage:get("spawnRadius", 1000)
end

--todo
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
    while self.timeSinceLastCycle > POPULATION_SIMULATION_INTERVAL do
        for entityId in self.entities:addedEntities() do
        end
       -- for entityId in self.entities:removedEntities() do
       --     species = Entity(entityId):getComponent(SpeciesComponent.TYPE_ID)
       -- end
        self.entities:clearChanges()
        for entityId in self.entities:entities() do
            
            species = Entity(entityId):getComponent(SpeciesComponent.TYPE_ID)
            
            species.currentPopulation = species.currentPopulation + species.birthsPerTime * species.populationBonusFactor - species.deathsPerTime * species.populationPenaltyFactor
            -- Decay bonuses
            species.populationBonusFactor = species.populationBonusFactor + (1.0 - species.populationBonusFactor)/4 *  POPULATION_SIMULATION_INTERVAL/1000
     
            species.populationPenaltyFactor = species.populationPenaltyFactor + (1.0 - species.populationPenaltyFactor)/8 *  POPULATION_SIMULATION_INTERVAL/1000
            species.birthsPerTime = species.birthsPerTime
        end
        
        
        self.timeSinceLastCycle = self.timeSinceLastCycle - POPULATION_SIMULATION_INTERVAL
    end
end
