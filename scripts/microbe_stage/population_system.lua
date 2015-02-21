
POPULATION_SIMULATION_INTERVAL = 1200

--------------------------------------------------------------------------------
-- Population
--
-- Holds information about a specific population (species \intersect patch)
--------------------------------------------------------------------------------
class 'Population'

function Population:__init(species)
    self.species = species
    self.heldCompounds = {} -- compounds that are available for intracellular processes
    self.lockedCompounds = {} -- compounds that aren't, but will be released on deaths
end

--[[
There are probably some functions here that would be useful to package with the population,
possibly useful enough to make this a component and have a system that coordinates.

Getting the effective population number from 


--]]


--[[

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
    --self.starterSpecies()
    self.entities:init(gameState)
end

-- Override from System
function PopulationSystem:shutdown()
    self.entities:shutdown()
    System.shutdown(self)
end


-- Override from System
function PopulationSystem:update(_, milliseconds)
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds

    -- this entire system is probably unnecessary, we should have a PatchSystem instead that does analogous work
    
    --Perform spawn cycle if necessary (Reason for "while" rather than "if" stated below)
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
--]]
