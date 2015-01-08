--------------------------------------------------------------------------------
-- PatchComponent
-- 
-- Controls population management within a certain region of space
--------------------------------------------------------------------------------

class "PatchComponent" (Component)

function PatchComponent:__init()
	Component.__init(self)
	-- stuff
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
