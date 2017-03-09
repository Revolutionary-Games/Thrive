-- Lua System base class

LuaSystem = class(
    --! @brief Constructs a new System. Should be called from derived classes with
    --! `LuaSystem.create(self)`
    function(self)

        -- Sanity check that fails if obj doesn't derive from system
        assert(self:is_a(LuaSystem),
               "LuaSystem.construct called on table that isn't a LuaSystem")

        -- This is no longer used to determine which systems to run
        self.enabled = true

        self.isLuaSystem = true


        
    end
)

-- default implementations
function LuaSystem:update(renderTime, logicTime)

    error("default LuaSystem:update called")
    
end

function LuaSystem:destroy()

    self.gameState = nil

end

--! Base init. Must be called from derived classes 
function LuaSystem:init(name, gameState)

    assert(name ~= nil)
    assert(gameState ~= nil)

    assert(type(name) == "string")

    self.name = name
    self.gameState = gameState
end


--! Base shutdown. Does nothing. Doesn't need to be called
function LuaSystem:shutdown()

end

-- Looks like derived systems are really bad at calling these things
-- so these are now not required to be called as they do nothing

function LuaSystem:activate()

end

function LuaSystem:deactivate()

end

