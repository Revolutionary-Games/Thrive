--------------------------------------------------------------------------------
-- PatchComponent
-- 
-- Controls population management within a certain region of space
--------------------------------------------------------------------------------

class "PatchComponent" (Component)

function PatchComponent:__init()
	Component.__init(self)
	-- map species to populations
    -- model environment
end

--[[
We need a bunch of stuff handled here:
Compound movement:
- environment -> species
- species -> environment
- predation: species -> other species and environment

Population changes:
- immigration -- add tentative populations, keep those viable enough to displace 
	another species (and give them resources of species displaced, with leakage)
- extinction, by culling
- speciation? We may need to slightly control here when it happens

Culling:
- when population drops below sustainable level, free all compounds
- when population mutates, and criteria for branching not met, move compounds 
	from ancestor to new, with leakage
- another case?

--]]

REGISTER_COMPONENT("PatchComponent", PatchComponent)

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
Whatever population calculations the patch does that would be useful to factor out will go here

Getting the effective population number from the lockedCompounds pool is a bit complicated
- each organelle will need an amortized cost, in locked compounds (protein, lipids, polysaccharides)
- SpeciesComponent should calculate average organism cost
- divide out current locked pool by per-unit cost, pick lowest
- fudge
--]]

--------------------------------------------------------------------------------
-- PatchSystem
--
-- System for simulating populations of species and their spatial distributions
--------------------------------------------------------------------------------

class "PatchSystem" (System)

PATCH_SIM_INTERVAL = 1200

function PatchSystem:__init()
	System.__init(self)

    self.entities = EntityFilter(
        {
            PatchComponent
        },
        true
    )
    self.timeSinceLastCycle = 0
end


-- Override from System
function PatchSystem:init(gameState)
    System.init(self, gameState)
    self.entities:init(gameState)
end

-- Override from System
function PatchSystem:shutdown()
    self.entities:shutdown()
    System.shutdown(self)
end

-- Override from System
function PatchSystem:activate()
    --[[
    This handles two (three?) cases:
    - First, it runs on game entry from main menu -- orchestrate with rest of world-generation
    - Second, game entry from editor -- split patches, 
    	move patches, merge patches
    - Third (possibly) it might run on load.  
    --]]
end

-- Override from System
function PatchSystem:update(_, milliseconds)
    self.timeSinceLastCycle = self.timeSinceLastCycle + milliseconds
    while self.timeSinceLastCycle > SPECIES_SIM_INTERVAL do
        -- do population-management here
        self.timeSinceLastCycle = self.timeSinceLastCycle - PATCH_SIM_INTERVAL
    end
end
