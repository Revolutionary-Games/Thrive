--------------------------------------------------------------------------------
-- Species class
--
-- Class for representing an individual species
--------------------------------------------------------------------------------
class 'Species'

--How big is a newly created species's population.
INITIAL_POPULATION = 1337

--limits the size of the initial stringCodes
local MIN_INITIAL_LENGTH = 5
local MAX_INITIAL_LENGTH = 15

local DEFAULT_SPAWN_DENSITY = 1/9000 --same as Teeny

local DEFAULT_SPAWN_RADIUS = 60

local MIN_COLOR = 0.3
local MAX_COLOR = 1.0

--sets up the spawn of the species
function Species:setupSpawn()
    local name = self.name --this line is important, otherwise the game only spawns the las microbe generated
    self.id = currentSpawnSystem:addSpawnType
    (
        function(pos)
            return microbeSpawnFunctionGeneric(pos, name, true, nil)
        end, 
        DEFAULT_SPAWN_DENSITY, --spawnDensity should depend on population
        DEFAULT_SPAWN_RADIUS
    )
end

--copy-pasted from setupSpecies in setup.lua
function createSpeciesTemplate(name, organelles, colour, compounds, speciesThresholds)
    speciesEntity = Entity(name)
    speciesComponent = SpeciesComponent(name)
    speciesEntity:addComponent(speciesComponent)
    for i, organelle in pairs(organelles) do
        local org = {}
            org.name = organelle.name
            org.q = organelle.q
            org.r = organelle.r
            org.rotation = organelle.rotation
            speciesComponent.organelles[i] = org
    end
    processorComponent = ProcessorComponent()
    speciesEntity:addComponent(processorComponent)
    speciesComponent.colour = Vector3(colour.r, colour.g, colour.b)
     -- iterates over all compounds, and sets amounts and priorities
    for compoundID in CompoundRegistry.getCompoundList() do
        compound = CompoundRegistry.getCompoundInternalName(compoundID)
        thresholdData = default_thresholds[compound]
         -- we'll need to generate defaults from species template
        processorComponent:setThreshold(compoundID, thresholdData.low, thresholdData.high, thresholdData.vent)
        compoundData = compounds[compound]
        if compoundData ~= nil then
            amount = compoundData.amount
            -- priority = compoundData.priority
            speciesComponent.avgCompoundAmounts["" .. compoundID] = amount
             -- speciesComponent.compoundPriorities[compoundID] = priority
        end
    end
    if speciesThresholds ~= nil then
        local thresholds = speciesThresholds
        for compoundID in CompoundRegistry.getCompoundList() do
            compound = CompoundRegistry.getCompoundInternalName(compoundID)
            if thresholds[compound] ~= nil then
                if thresholds[compound].low ~= nil then
                    processorComponent:setLowThreshold(compoundID, thresholds[compound].low)
                end
                if thresholds[compound].low ~= nil then
                    processorComponent:setHighThreshold(compoundID, thresholds[compound].high)
                end
                if thresholds[compound].vent ~= nil then
                    processorComponent:setVentThreshold(compoundID, thresholds[compound].vent)
                end
            end
        end
    end
    local capacities = {}
    for _, organelle in pairs(organelles) do
        if organelles[organelle.name] ~= nil then
            if organelles[organelle.name]["processes"] ~= nil then
                for process, capacity in pairs(organelles[organelle.name]["processes"]) do
                    if capacities[process] == nil then
                        capacities[process] = 0
                    end
                    capacities[process] = capacities[process] + capacity
                end
            end
        end
    end
    for bioProcessId in BioProcessRegistry.getList() do
        local name = BioProcessRegistry.getInternalName(bioProcessId)
        if capacities[name] ~= nil then
            processorComponent:setCapacity(bioProcessId, capacities[name])
        -- else
            -- processorComponent:setCapacity(bioProcessId, 0)
        end
    end
    return speciesEntity
end

function Species:init()
    self.population = INITIAL_POPULATION
    self.name = "Species_" .. tostring(math.random()) --gotta use the latin names
    
    local stringSize = math.random(MIN_INITIAL_LENGTH, MAX_INITIAL_LENGTH)
    self.stringCode = "NY" --it should always have a nucleus and a cytoplasm.
    for i = 1, stringSize do
        local newLetterIndex = math.random(#VALID_LETTERS)
        self.stringCode = self.stringCode .. VALID_LETTERS[newLetterIndex]
    end
    print(self.stringCode)
    
    local organelles = positionOrganelles(self.stringCode)

    local initial_compounds = {
            atp = {priority=10,amount=40},
            glucose = {amount = 5},
            reproductase = {priority = 8},
        }

    self.colour = {
        r = math.random() * (MAX_COLOR - MIN_COLOR) + MIN_COLOR,
        g = math.random() * (MAX_COLOR - MIN_COLOR) + MIN_COLOR,
        b = math.random() * (MAX_COLOR - MIN_COLOR) + MIN_COLOR,
    }

    self.template = createSpeciesTemplate(self.name, organelles, self.colour, initial_compounds, nil)
    self:setupSpawn()
    return self
end

--updates the population count of the species
function Species:updatePopulation()
    --TODO:
    --fill me
    --with code
    self.population = INITIAL_POPULATION + math.random(-400, 400)
end

--delete a species
function Species.extinguish()
    self.template:destroy()
    currentSpawnSystem:removeSpawnType(self.id)
end

--returns a mutated version of the species and reduces the species population by half
function Species.getChild()
    --TODO: implement this
    self.population = math.floor(self.population / 2)
    return Species:init()
end
--------------------------------------------------------------------------------
-- SpeciesSystem
--
-- System for estimating and simulating population count for various species
--------------------------------------------------------------------------------

--how much time does it take for the simulation to update.
SPECIES_SIM_INTERVAL = 20000

--if a specie's population goes below this it goes extinct.
MIN_POP_SIZE = 500

--if a specie's population goes above this it gets split in half and a new mutated specie apears.
MAX_POP_SIZE = 10000

--the amount of species at the start of the microbe stage (not counting Default/Player)
INITIAL_SPECIES = 10

--if there are more species than this the ones with less population
--should go extinct and the rest have their population reduced.
MAX_SPECIES = 30

--if there are less species than this create new ones.
MIN_SPECIES = 3

class 'SpeciesSystem' (System)

function SpeciesSystem:__init(spawnSystem)
    gSpawnSystem = spawnSystem
    System.__init(self)
    
    self.entities = EntityFilter(
        {
            SpeciesComponent,
            ProcessorComponent,
        },
        true
    )
    self.timeSinceLastCycle = 0
end

-- Override from System
function SpeciesSystem:init(gameState)
    System.init(self, "SpeciesSystem", gameState)
    self.entities:init(gameState)

    self.species = {}
    self.number_of_species = 0

    --doing this crashes the game
    --probably because init gets called twice
    --running this in only one of those calls crashes the game
    --(somehow)

    --for i = 1, INITIAL_SPECIES do
    --    newSpecies = Species:init()
    --    table.insert(self.species, newSpecies)
    --    self.number_of_species = self.number_of_species + 1
    --end
end

-- Override from System
function SpeciesSystem:shutdown()
    self.entities:shutdown()
    System.shutdown(self)
end

-- Override from System
function SpeciesSystem:activate()
    --[[ 
    This runs in two (three?) use-cases:
    - First, it runs on game entry from main menu -- in this case, it must set up for new game
    - Second, it runs on game entry from editor -- in this case, it can set up the new species 
        (in conjunction with stuff in editor, called on finish-click)
    - Third (possibly) it might run on load.  
    --]]
end

function SpeciesSystem:doMassExtinction()
    --TODO: implement this
end

-- Override from System
function SpeciesSystem:update(_, milliseconds)
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds
    while self.timeSinceLastCycle > SPECIES_SIM_INTERVAL do
        -- do mutation-management here
        --update population numbers and split/extinct species as needed
        local numberOfSpecies = self.number_of_species
        for i = 1, numberOfSpecies do
            local index = numberOfSpecies - i + 1   --traversing the population backwards to avoid
                                                    --"chopping down the branch i'm sitting in"

            currentSpecies = self.species[index]
            currentSpecies:updatePopulation()
            local population = currentSpecies.population

            --extinction
            if population < MIN_POP_SIZE then
                print("Extinguishing someone")
                currentSpecies:extinguish()
                table.remove(self.species, index)
                self.number_of_species = self.number_of_species - 1
            end

            --reproduction/mutation
            if population > MAX_POP_SIZE then
                print("reproducing species")
                local newSpecies = currentSpecies:getChild()
                table.insert(self.speces, newSpecies)
                self.number_of_species = self.number_of_species + 1
            end
        end

        --new species
        if self.number_of_species < MIN_SPECIES then
            print("Creating new species!")
            for i = self.number_of_species, INITIAL_SPECIES - 1 do
                newSpecies = Species:init()
                table.insert(self.species, newSpecies)
                self.number_of_species = self.number_of_species + 1
            end
        end

        --mass extinction
        if self.number_of_species > MAX_SPECIES then
            print("Mass extinction!")
            self:doMassExtinction()
        end

        self.timeSinceLastCycle = self.timeSinceLastCycle - SPECIES_SIM_INTERVAL
    end
end

function SpeciesSystem.initProcessorComponent(entity, speciesComponent)
    local sc = entity:getComponent(SpeciesComponent.TYPE_ID)
    if sc == nil then
        entity:addComponent(speciesComponent)
    end

    local pc = entity:getComponent(ProcessorComponent.TYPE_ID)
    if pc == nil then
        pc = ProcessorComponent()
        entity:addComponent(pc)
    end

    local thresholds = {}

    for compoundID in CompoundRegistry.getCompoundList() do
        compound = CompoundRegistry.getCompoundInternalName(compoundID)
        thresholdData = default_thresholds[compound]
        thresholds[compoundID] = {low = thresholdData.low, high = thresholdData.high, vent = thresholdData.vent}
    end

    if starter_microbes[speciesComponent.name] ~= nil and starter_microbes[speciesComponent.name].thresholds ~= nil then
        local c_thresholds = starter_microbes[speciesComponent.name].thresholds
        for compoundID in CompoundRegistry.getCompoundList() do
            compound = CompoundRegistry.getCompoundInternalName(compoundID)
            if c_thresholds[compound] ~= nil then
                local t = c_thresholds[compound]
                if t.low ~= nil then thresholds[compoundID].low = t.low end
                if t.high ~= nil then thresholds[compoundID].high = t.high end
                if t.vent ~= nil then thresholds[compoundID].vent = t.vent end
            end
        end
    end

    for compoundID, t in ipairs(thresholds) do
        pc:setThreshold(compoundID, t.low, t.high, t.vent)
    end

    local capacities = {}
    for _, organelle in pairs(speciesComponent.organelles) do
        if organelles[organelle.name] ~= nil then
            if organelles[organelle.name]["processes"] ~= nil then
                for process, capacity in pairs(organelles[organelle.name]["processes"]) do
                    if capacities[process] == nil then
                        capacities[process] = 0
                    end
                    capacities[process] = capacities[process] + capacity
                end
            end
        end
    end
    for bioProcessID in BioProcessRegistry.getList() do
        local name = BioProcessRegistry.getInternalName(bioProcessID)
        if capacities[name] ~= nil then
            pc:setCapacity(bioProcessID, capacities[name])
        end
    end
end

-- Given a newly-created microbe, this sets the organelles and all other species-specific microbe data
--  like agent codes, for example.
function SpeciesSystem.template(microbe, species)
    -- TODO: Make this also set the microbe's ProcessorComponent
    microbe.microbe.speciesName = species.name
    microbe:setMembraneColour(species.colour)
    -- give it organelles
    for _, orgdata in pairs(species.organelles) do
        organelle = OrganelleFactory.makeOrganelle(orgdata)
        microbe:addOrganelle(orgdata.q, orgdata.r, orgdata.rotation, organelle)
    end

    for compoundID, amount in pairs(species.avgCompoundAmounts) do
        if amount ~= 0 then
            microbe:storeCompound(compoundID, amount, false)
        end
    end
    return microbe
end

function SpeciesSystem.fromMicrobe(microbe, species)
    local microbe_ = microbe.microbe -- shouldn't break, I think
    -- self.name = microbe_.speciesName
    species.colour = microbe:getComponent(MembraneComponent.TYPE_ID):getColour()
    --print("self.name: "..self.name)
    -- Create species' organelle data
    for i, organelle in pairs(microbe_.organelles) do
        --print(i)
        local data = {}
        data.name = organelle.name
        data.q = organelle.position.q
        data.r = organelle.position.r
        data.rotation = organelle.rotation
        species.organelles[i] = data
    end
    -- TODO: make this update the ProcessorComponent based on microbe thresholds
end
