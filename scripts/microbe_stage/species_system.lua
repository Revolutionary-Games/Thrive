--------------------------------------------------------------------------------
-- SpeciesSystem
--
-- System for estimating and simulating population count for various species
--------------------------------------------------------------------------------

SPECIES_SIM_INTERVAL = 20000

class 'SpeciesSystem' (System)

function SpeciesSystem:__init()
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

-- Override from System
function SpeciesSystem:update(_, milliseconds)
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds
    while self.timeSinceLastCycle > SPECIES_SIM_INTERVAL do
        -- do mutation-management here
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
    -- This microbes compound amounts will be the new population average.
    species.avgCompoundAmounts = {}
    for compoundID in CompoundRegistry.getCompoundList() do
        local amount = microbe:getCompoundAmount(compoundID)
        species.avgCompoundAmounts["" .. compoundID] = amount
    end
    -- TODO: make this update the ProcessorComponent based on microbe thresholds
end
