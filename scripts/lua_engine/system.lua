-- Lua System base class

LuaSystem = class(
   --! @brief Constructs a new System. Should be called from derived classes with
   --! `LuaSystem.create(self)`
   function(self)

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

   self.name = name
   self.gameState = gameState
end




