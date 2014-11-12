
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
    self.starterSpecies()
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

function PopulationSystem:starterSpecies()
    --[[
    This function should be the entry point for all initial-species generation
    For now, it can go through the XML and instantiate all the species, but later this 
    would be all procedural.

    Together with the mutate function, these would be the only ways species are created
    ]]
    SpeciesRegistry.loadFromXML("../definitions/microbes.xml")
    for _, name in ipairs(SpeciesRegistry.getSpeciesNames()) do
        speciesEntity = Entity(name)
        speciesComponent = SpeciesComponent(name)
        speciesEntity:addComponent(speciesComponent)
        -- print("made entity and component")
        local organelles = {}
        assert(pcall(function () SpeciesRegistry.getSize(name) end), "could not load species", name, "from XML")
        -- In this case the species is a default one loaded from xml
        -- print("loaded", name)
        local numOrganelles = SpeciesRegistry.getSize(name)
        -- print("# organelles = "..numOrganelles)
        for i = 0,(numOrganelles-1) do
            -- returns a property table
            local organelleData = SpeciesRegistry.getOrganelle(name, i)
            organelles[#organelles+1] = organelleData
        end
        -- for _, org in pairs(organelles) do print(org.name, org.q, org.r) end

        -- for key, value in pairs(organelles) do print(key, value) end
        speciesComponent.organelles = organelles
    end
end