
POPULATION_SIMULATION_INTERVAL = 1200

--------------------------------------------------------------------------------
-- SpeciesComponent
--
-- Holds information about an entity representing a species
--------------------------------------------------------------------------------
class 'SpeciesComponent' (Component)

SPECIES_NUM = 0

function SpeciesComponent:__init(name)
    Component.__init(self)
    self.num = SPECIES_NUM
    SPECIES_NUM = SPECIES_NUM + 1
    if name == nil then
        self.name = "noname"..self.num
    else
        self.name = name
    end
    --self.name="Default" -- TODO TODODO TODODODO get names properly somehow
    self.populationBonusFactor = 1.0
    self.populationPenaltyFactor = 1.0
    self.deathsPerTime = 1.0
    self.birthsPerTime = 1.0
    self.currentPopulation = 1
    -- todo make these actually do stuff
    self.organelles = {} -- stores a table of organelle {q,r,name} tables

    self.avgCompoundAmounts = {} -- maps each compound name to the amount a new spawn should get. Nonentries are zero.
                                 -- we could also add some measure of variability to make things more ...variable.
    self.compoundPriorities = {} -- maps compound name to priority.
end

--is this still todo?
--todo
function SpeciesComponent:load(storage)
    Component.load(self, storage)
    self.name = storage:get("name", "")
    print(self.name,""..self.num)
    self.populationBonusFactor = storage:get("populationBonusFactor", 0)
    self.populationPenaltyFactor = storage:get("populationPenaltyFactor", 0)
    self.deathsPerTime = storage:get("deathsPerTime", 0)
    self.deathsPerTime = storage:get("deathsPerTime", 0)
    self.birthsPerTime = storage:get("birthsPerTime", 0)
    self.currentPopulation = storage:get("currentPopulation", 0)
    self.compoundPriorities = {}
    priorityData = storage:get("compoundPriorities", nil)
    --if priorityData ~= nil then
     --   print("atp: "..priorityData:get("atp", -1))
      --  print("glucose: "..priorityData:get("glucose", -1))
    --end
    organelleData = storage:get("organelleData", nil)
    self.organelles = {}
    if organelleData ~= nil then
        i = 1
        while organelleData:contains(""..i) do
            organelle = {}
            orgData = organelleData:get(""..i, nil)
            if orgData ~= nil then
                organelle.name = orgData:get("name", "")
                organelle.q = orgData:get("q", 0)
                organelle.r = orgData:get("r", 0)
            end
            self.organelles[i] = organelle
            i = i + 1
        end
    end
end

--todo
function SpeciesComponent:storage()
    local storage = Component.storage(self)
    storage:set("name", self.name)
    storage:set("populationBonusFactor", self.populationBonusFactor)
    storage:set("populationPenaltyFactor", self.populationPenaltyFactor)
    storage:set("deathsPerTime", self.deathsPerTime)
    storage:set("birthsPerTime", self.birthsPerTime)
    storage:set("currentPopulation", self.currentPopulation)
    compoundPriorities = StorageContainer()
    for k,v in pairs(self.compoundPriorities) do
        compoundPriorities:set(k,v)
    end
    storage:set("compoundPriorities", compoundPriorities)
    organelles = StorageContainer()
    for i, org in ipairs(self.organelles) do
        orgData = StorageContainer()
        orgData:set("name", org.name)
        orgData:set("q", org.q)
        orgData:set("r", org.r)
        organelles:set(""..i, orgData)
    end
    storage:set("organelleData", organelles)
    return storage
end

function SpeciesComponent:mutate(aiControlled)
    --[[
    SpeciesComponent:mutate should call the correct evolution-handler
    (Ie, trigger an entry into microbe editor for player; and auto-evo 
    for AI), and then create and add the new species to the system
    

    ]]
end


-- Given a newly-created microbe, this sets the organelles and all other species-specific microbe data
--  like agent codes, for example.
function SpeciesComponent:template(microbe)

    microbe.microbe.speciesName = self.name
    -- give it organelles
    for i, orgdata in pairs(self.organelles) do
        organelle = OrganelleFactory.makeOrganelle(orgdata)
        microbe:addOrganelle(orgdata.q, orgdata.r, organelle)
    end

    for compoundID, amount in pairs(self.avgCompoundAmounts) do
        if amount ~= 0 then
            microbe:storeCompound(compoundID, amount, false)
        end
    end
    for compoundID, priority in pairs(self.compoundPriorities) do
        if priority ~= 0 then
            microbe:setDefaultCompoundPriority(compoundID, priority)
        end
    end
    return microbe
end

REGISTER_COMPONENT("SpeciesComponent", SpeciesComponent)

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
