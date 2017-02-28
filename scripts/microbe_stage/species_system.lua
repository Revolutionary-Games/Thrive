--------------------------------------------------------------------------------
-- Species class
--
-- Class for representing an individual species
--------------------------------------------------------------------------------
Species = class(
    function(self)

        self.population = INITIAL_POPULATION
        self.name = "Species_" .. tostring(math.random()) --gotta use the latin names
        
        local stringSize = math.random(MIN_INITIAL_LENGTH, MAX_INITIAL_LENGTH)
        self.stringCode = organelleTable.nucleus.gene .. organelleTable.cytoplasm.gene --it should always have a nucleus and a cytoplasm.
        for i = 1, stringSize do
            self.stringCode = self.stringCode .. getRandomLetter()
        end
        
        local organelles = positionOrganelles(self.stringCode)

        self.colour = {
            r = randomColour(),
            g = randomColour(),
            b = randomColour()
        }

        self.template = createSpeciesTemplate(self.name, organelles, self.colour, DEFAULT_INITIAL_COMPOUNDS, nil)
        self:setupSpawn()
        
    end
)

--limits the size of the initial stringCodes
local MIN_INITIAL_LENGTH = 5
local MAX_INITIAL_LENGTH = 15

local DEFAULT_SPAWN_DENSITY = 1/25000

local DEFAULT_SPAWN_RADIUS = 60

local MIN_COLOR = 0.3
local MAX_COLOR = 1.0

local MUTATION_CREATION_RATE = 0.1
local MUTATION_DELETION_RATE = 0.1

-- Why is the latest created species system accessible globally here?
-- this will cause problems in the future
local gSpeciesSystem = nil

local function randomColour()
    return  math.random() * (MAX_COLOR - MIN_COLOR) + MIN_COLOR
end

local DEFAULT_INITIAL_COMPOUNDS =
{
    atp = {priority=10,amount=40},
    glucose = {amount = 10},
    reproductase = {priority = 8},
    oxygen = {amount = 10},
    oxytoxy = {amount = 1}
}

--sets up the spawn of the species
function Species:setupSpawn()
    self.id = currentSpawnSystem:addSpawnType(
        function(pos)
            return microbeSpawnFunctionGeneric(pos, self.name, true, nil)
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


        if agents[compound] then
            thresholdData = default_thresholds[compound]
        else
            thresholdData = compoundTable[compound].default_treshold
        end

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

--updates the population count of the species
function Species:updatePopulation()
    --TODO:
    --fill me
    --with code
    self.population = self.population + math.random(-200, 200)
end

--delete a species
function Species:extinguish()
    currentSpawnSystem:removeSpawnType(self.id)
    --self.template:destroy() --game crashes if i do that.
end

local function mutate(stringCode)
    --moving the stringCode to a table to facilitate changes
    local chromosomes = {}
    local originalStringSize = 0
    for i = 3, string.len(stringCode) do
        table.insert(chromosomes, string.sub(stringCode, i, i))
        originalStringSize = originalStringSize + 1
    end
    
    --try to insert a letter at the end of the table.
    if math.random() < MUTATION_CREATION_RATE then
        table.insert(chromosomes, getRandomLetter())
    end

    --modifies the rest of the table.
    for i = 0, originalStringSize - 1 do
        index = originalStringSize - i

        if math.random() < MUTATION_DELETION_RATE then
            table.remove(chromosomes, index)
        end
        
        if math.random() < MUTATION_CREATION_RATE then
            table.insert(chromosomes, index, getRandomLetter())
        end
    end

    --transforming the table back into a string
    local newString = "NY"
    for _, letter in pairs(chromosomes) do
        newString = newString .. letter
    end

    return newString
end

--returns a mutated version of the species and reduces the species population by half
function Species:getChild(parent, r, g, b)
    self.name = "Species_" .. tostring(math.random())
    self.colour = {}
    self.colour.r = (r + randomColour()) / 2
    self.colour.g = (g + randomColour()) / 2
    self.colour.b = (b + randomColour()) / 2
    self.population = math.floor(parent.population / 2)
    self.stringCode = mutate(parent.stringCode)

    local organelles = positionOrganelles(parent.stringCode)

    self.template = createSpeciesTemplate(self.name, organelles, self.colour, DEFAULT_INITIAL_COMPOUNDS, nil)
    self:setupSpawn()

    parent.population = math.ceil(parent.population / 2)
    return self
end
--------------------------------------------------------------------------------
-- SpeciesSystem
--
-- System for estimating and simulating population count for various species
--------------------------------------------------------------------------------

--How big is a newly created species's population.
INITIAL_POPULATION = 2000

--how much time does it take for the simulation to update.
SPECIES_SIM_INTERVAL = 20000

--if a specie's population goes below this it goes extinct.
MIN_POP_SIZE = 100

--if a specie's population goes above this it gets split in half and a new mutated specie apears.
MAX_POP_SIZE = 5000

--the amount of species at the start of the microbe stage (not counting Default/Player)
INITIAL_SPECIES = 7

--if there are more species than this then all species get their population reduced by half
MAX_SPECIES = 15

--if there are less species than this create new ones.
MIN_SPECIES = 3


SpeciesSystem = class(
    LuaSystem,
    function(self)
        
        LuaSystem.create(self)
        
        gSpawnSystem = self
        
        self.entities = EntityFilter.new(
            {
                SpeciesComponent,
                ProcessorComponent,
            },
            true
        )
        self.timeSinceLastCycle = 0
        
    end
)

function resetAutoEvo()
    if gSpeciesSystem.species ~= nil then
        for _, species in pairs(gSpeciesSystem.species) do
            species:extinguish()
        end

        gSpeciesSystem.species = {}
        gSpeciesSystem.number_of_species = 0
    end
end

-- Override from System
function SpeciesSystem:init(gameState)
    LuaSystem.init(self, "SpeciesSystem", gameState)
    self.entities:init(gameState.wrapper)

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
    gSpeciesSystem = self
end

-- Override from System
function SpeciesSystem:shutdown()
    self.entities:shutdown()
    LuaSystem.shutdown(self)
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
    for _, currentSpecies in pairs(self.species) do
        currentSpecies.population = math.floor(currentSpecies.population / 2)
    end
end

-- Override from System
function SpeciesSystem:update(_, milliseconds)
    gSpeciesSystem = self --so hacky :c
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds
    while self.timeSinceLastCycle > SPECIES_SIM_INTERVAL do
        --update population numbers and split/extinct species as needed
        local numberOfSpecies = self.number_of_species
        for i = 0, numberOfSpecies - 1 do
            local index = numberOfSpecies - i   --traversing the population backwards to avoid
                                                --"chopping down the branch i'm sitting in"

            currentSpecies = self.species[index]
            currentSpecies:updatePopulation()
            local population = currentSpecies.population

            --reproduction/mutation
            if population > MAX_POP_SIZE then
                local newSpecies = Species:getChild(currentSpecies,
                                                    currentSpecies.colour.r,
                                                    currentSpecies.colour.g,
                                                    currentSpecies.colour.b)
                table.insert(self.species, newSpecies)
                self.number_of_species = self.number_of_species + 1
            end

            --extinction
            if population < MIN_POP_SIZE then
                currentSpecies:extinguish()
                table.remove(self.species, index)
                self.number_of_species = self.number_of_species - 1
            end
        end

        --new species
        if self.number_of_species < MIN_SPECIES then
            for i = self.number_of_species, INITIAL_SPECIES - 1 do
                newSpecies = Species()
                table.insert(self.species, newSpecies)
                self.number_of_species = self.number_of_species + 1
            end
        end

        --mass extinction
        if self.number_of_species > MAX_SPECIES then
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
        if compoundTable[compound] then
            thresholdData = compoundTable[compound].default_treshold
        else
            thresholdData = default_thresholds[compound]
        end

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

    SpeciesSystem.restoreOrganelleLayout(microbe, species)
    
    for compoundID, amount in pairs(species.avgCompoundAmounts) do
        if amount ~= 0 then
            microbe:storeCompound(compoundID, amount, false)
        end
    end
    
    return microbe
end

function SpeciesSystem.restoreOrganelleLayout(microbe, species)
    -- delete the the previous organelles.
    for s, organelle in pairs(microbe.microbe.organelles) do
        local q = organelle.position.q
        local r = organelle.position.r
        microbe:removeOrganelle(q, r)
    end
    microbe.microbe.organelles = {}
    -- give it organelles
    for _, orgdata in pairs(species.organelles) do
        organelle = OrganelleFactory.makeOrganelle(orgdata)
        microbe:addOrganelle(orgdata.q, orgdata.r, orgdata.rotation, organelle)
    end
end

function SpeciesSystem.fromMicrobe(microbe, species)
    local microbe_ = microbe.microbe -- shouldn't break, I think
    -- self.name = microbe_.speciesName
    species.colour = microbe:getComponent(MembraneComponent.TYPE_ID):getColour()
    -- Create species' organelle data
    for i, organelle in pairs(microbe_.organelles) do
        local data = {}
        data.name = organelle.name
        data.q = organelle.position.q
        data.r = organelle.position.r
        data.rotation = organelle.rotation
        species.organelles[i] = data
    end
    -- This microbes compound amounts will be the new population average.
    species.avgCompoundAmounts = {}
    for compoundID in CompoundRegistry.getCompoundList() do
        local amount = microbe:getCompoundAmount(compoundID)
        species.avgCompoundAmounts["" .. compoundID] = amount/2
    end
    -- TODO: make this update the ProcessorComponent based on microbe thresholds
end
