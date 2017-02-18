--! @file Lua versions of functions in engine.cpp
--!
--! This is done to allow the main loop of the game to be in lua and
--! that way reduce calls from C++ to Lua. With JIT the Lua code
--! is fast enough to be the "glue" between C++ systems and Lua
--! systems and main loop

LuaEngine = createClass()

function LuaEngine.new()

   local self = LuaEngine._createInstance()

   -- The state the engine is switching to on next frame
   self.nextGameState = nil
   
   return self
end

--! @brief Initializes the lua side of the engine
--! @param cppSide the engine object received from
--! c++ code
function LuaEngine:init(cppSide)

   assert(cppSide ~= nil)
   
   
end

--! @param name Unique name of the system
--! @param systems Array of systems that are in the new GameState
--! @param physics If true creates a physics state in the GameState
function LuaEngine:createGameState(name, systems, physics)

   local newState = GameState.new()

   for i,s in ipairs(systems) do

      newState:addSystem(s)
      
   end

   newState:init(name, self)

   if physics == true then

      newState:initPhysics()
      
   end

   return newState
end

--! @brief Runs updates on some core systems and the current GameState
function update(milliseconds)

   Engine.update(milliseconds)

   -- Update GameStates
   
   if self.nextGameState ~= nil then
      
      self:activeGameState(self.nextGameState)
      self.nextGameState = nil
      
   end

   if self.currentGameState == nil then
      error("currentGameState is nil"
   end
   
   -- Update current GameState
   local updateTime = if Engine.paused then 0 else milliseconds end
   
   self.currentGameState:update(milliseconds, updateTime)

   -- Update console
   self.console:update()


   -- Update any timed shutdown systems
   -- Reverse iterate to safely remove items
   for i = #self.prevShutdownSystems, 1, -1 do

      local delayed  = self.prevShutdownSystems[i]
      
      local updateTime = min(delayed.timeLeft, milliseconds);

      
      local pauseHelper = if Engine.paused then 0 else updateTime end

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

g_luaEngine = LuaEngine.new()


