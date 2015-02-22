
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

    self.organelles = {} -- stores a table of organelle {q,r,name} tables

    self.avgCompoundAmounts = {} -- maps each compound name to the amount a new spawn should get. Nonentries are zero.
                                 -- we could also add some measure of variability to make things more ...variable.
    self.compoundPriorities = {} -- maps compound name to priority.
end

--is this still todo?
--todo - store moar data
function SpeciesComponent:load(storage)
    Component.load(self, storage)
    self.name = storage:get("name", "")
    self.compoundPriorities = {}
    priorityData = storage:get("compoundPriorities", nil)
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

function SpeciesComponent:mutate(aiControlled, population)
    --[[
    SpeciesComponent:mutate should call the correct evolution-handler
    (Ie, trigger an entry into microbe editor for player; and auto-evo 
    for AI), and then create and add the new species to the system.
    - uses the population to access information about 
    
    For player mutation, this would be the entry point into the editor

    - note, this architecture may be reconsidered

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
    -- complimentary serving of atp
    --newMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
    return microbe
end

--[[
Modify the species, using a microbe as the template for the new genome.

]]
function SpeciesComponent:fromMicrobe(microbe)
    microbe = microbe.microbe -- shouldn't break, I think
    self.name = microbe.speciesName
    --print("self.name: "..self.name)
    -- Create species' organelle data
    for i, organelle in pairs(microbe.organelles) do
        --print(i)
        local data = {}
        data.name = organelle.name
        data.q = organelle.position.q
        data.r = organelle.position.r
        self.organelles[i] = data
    end
end

REGISTER_COMPONENT("SpeciesComponent", SpeciesComponent)

SPECIES_SIM_INTERVAL = 20000

--------------------------------------------------------------------------------
-- SpeciesSystem
--
-- System for estimating and simulating population count for various species
--------------------------------------------------------------------------------
class 'SpeciesSystem' (System)

function SpeciesSystem:__init()
    System.__init(self)
    
    self.entities = EntityFilter(
        {
            SpeciesComponent
        },
        true
    )
    self.timeSinceLastCycle = 0
end

-- Override from System
function SpeciesSystem:init(gameState)
    System.init(self, gameState)
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
