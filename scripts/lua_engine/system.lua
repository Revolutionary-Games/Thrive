-- Lua System base class

LuaSystem = class(
    --! @brief Constructs a new System. Should be called from derived classes with
    --! `LuaSystem.create(self)`
    function(self)

        -- Sanity check that fails if obj doesn't derive from system
        assert(self:is_a(LuaSystem),
               "LuaSystem.construct called on table that isn't a LuaSystem")

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

    -- Set not enabled, just for fun
    -- This should be destroyed anyway after this method
    self.enabled = false
    
end

--! C++ system compatibility
function LuaSystem:setEnabled(enabled)
    
    self.enabled = enabled
    
end

function LuaSystem:activate()

    self.enabled = true

end

function LuaSystem:deactivate()

    self.enabled = false

end

