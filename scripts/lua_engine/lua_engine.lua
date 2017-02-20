--! @file Lua versions of functions in engine.cpp
--!
--! This is done to allow the main loop of the game to be in lua and
--! that way reduce calls from C++ to Lua. With JIT the Lua code
--! is fast enough to be the "glue" between C++ systems and Lua
--! systems and main loop

LuaEngine = class(
   function(self)

      -- The state the engine is switching to on next frame
      self.nextGameState = nil
      
   end
)

--! @brief Initializes the lua side of the engine
--! @param cppSide the engine object received from
--! c++ code
function LuaEngine:init(cppSide)

   assert(cppSide ~= nil)

   self.Engine = cppSide
   
end

--! @param name Unique name of the system
--! @param systems Array of systems that are in the new GameState.
--! Must be created with `table.insert(systems, s)`
--! @param physics If true creates a physics state in the GameState
--! @todo Make sure that .destroy() is called on these objects
function LuaEngine:createGameState(name, systems, physics, guiLayoutName)

   local newState = GameState.new(name, systems, self, physics, guiLayoutName)

   return newState
end

--! @brief Runs updates on some core systems and the current GameState
function update(milliseconds)

   self.Engine.update(milliseconds)

   -- Update GameStates
   
   if self.nextGameState ~= nil then
      
      self:activeGameState(self.nextGameState)
      self.nextGameState = nil
      
   end

   if self.currentGameState == nil then
      error("currentGameState is nil"
   end
   
   -- Update current GameState
   local updateTime = if self.Engine.paused then 0 else milliseconds end
   
   self.currentGameState:update(milliseconds, updateTime)

   -- Update console
   self.console:update()


   -- Update any timed shutdown systems
   -- Reverse iterate to safely remove items
   for i = #self.prevShutdownSystems, 1, -1 do

      local delayed  = self.prevShutdownSystems[i]
      
      local updateTime = min(delayed.timeLeft, milliseconds);

      
      local pauseHelper = if self.Engine.paused then 0 else updateTime end

      delayed.system:update(updateTime, pauseHelper)
      
      delayed.timeLeft -= updateTime

      if delayed.timeLeft <= 0 then

         -- Remove systems that had timed out
         delayed.system:deactivate()
         table.remove(self.prevShutdownSystems, i)
         
      end
   end
end


-- Timed shutdown functions

--! @brief Keeps a system alive after being shut down for a specified amount of  time
--! 
--! Note that this causes update to be called for the specified duration so be careful
--! to ensure that the system is not enabled or it will get update calls twice.
--! 
--! @param system
--! The system to keep updated
--! 
--! @param milliseconds
--! The number of milliseconds to keep the system updated for
--! 
function LuaEngine:timedSystemShutdown(system, milliseconds)

   table.insert(self.prevShutdownSystems, { timeLeft = milliseconds, ["system"] = system })

end

--! @brief Returns true if system is already queued for shutdown
function LuaEngine:isSystemTimedShutdown(system)

   for i,p in ipairs(self.prevShutdownSystems) do

      if p.system == system then
         return true
      end
      
   end

   return false

end


--! @brief Sets the current game state
--! 
--! The game state will be activated at the beginning of the next frame.
--! 
--! \a gameState must not be \c null. It's passed by pointer as a
--! convenience for the Lua bindings (which can't handle references well).
--! 
--! @param gameState GameState The new game state
function LuaEngine:setCurrentGameState(gameState)

   

end



--! @brief Retrieves a game state
--! @param name The game state's name
--! @return The GameState with the name or nil
function LuaEngine:getGameState(name)


end

--! @brief Returns the currently active game state or nil
function LuaEngine:currentGameState()

   
   
end




--! @brief Transfers an entity from one gamestate to another
--!
--! @param oldEntityId
--!  The id of the entity to transfer in the old entitymanager
--!
--! @param oldEntityManager
--!  The old entitymanager which is currently handling the entity
--!
--! @param newGameState
--!  The new gamestate to transfer the entity to
--! @return The new entity id in the new gamestate
function LuaEngine:transferEntityGameState(oldEntityId,
                                           oldEntityManager,
                                           newGameState
                                          )

   
   
   
end







g_luaEngine = LuaEngine.new()


