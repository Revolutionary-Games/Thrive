-- Lua System base class

LuaSystem = class(
   function(self, name, gameState)

      assert(name ~= nil)
      assert(gameState ~= nil)

      self.name = name
      self.gameState = gameState
      
   end
)

-- default implementations
function LuaSystem:update(renderTime, logicTime)

   error("default LuaSystem:update called")
   
end



function LuaSystem:destroy()

   self.gameState = nil

end

