--! @file Gamestate, holds systems and updates the game systems and draws stuff

-- This is what is said about the state in the C++ code:
-- GameState Represents a distinct set of active systems and entities
-- 
-- The game has to switch between different states. Examples of a state are
-- "main menu", "microbe gameplay" or "microbe editor". These states usually
-- share very few entities and even fewer systems, so it is sensible to
-- separate them completely (and, if necessary, share data over other channels).
-- 
-- Each GameState has its own EntityManager and its own set of systems. Game
-- states are identified by their name, a unique string.
-- 
-- GameStates cannot be created directly. Use LuaEngine:createGameState to create
-- new GameStates.


GameState = class(
   --! @brief Initializes a state to be used.
   --! @note calls this only through LuaEngine:createGameState
   function(self, name, systems, engine, physics, guiLayoutName)

      assert(name ~= nil)
      assert(systems ~= nil)
      assert(type(systems) == "table")
      assert(engine ~= nil)
      -- Physics must be true or false
      assert(physics ~= nil)

      -- Systems container
      self.systems = systems
      self.name = name
      self.engine = engine
      self.guiLayoutName = guiLayoutName


      -- This is passed to C++ systems
      self.cppData = GameStateData.new(self)

      -- Init systems
      for i,s in ipairs(self.systems) do

         s:init(self)
         
      end

      --! @brief Adds physics to this GameState
      if physics == true then
         
         self.physicsWorld = PhysicalWorld.new()
         
      end

      -- Create entity manager
      self.entityManager = EntityManager.new()

      self.guiWindow = CEGUIWindow.new(self.guiLayoutName)

   end
)

--! @brief Must be called when this gamestate is no longer needed
--!
--! Shuts down all systems and releases the C++ data object
function GameState:shutdown()

   for i,s in ipairs(self.systems) do

      s:shutdown()
      
   end
   
   self.cppData = nil
   self.physicsWorld = nil
   self.entityManager = nil
   self.guiWindow = nil
end


--! @brief Called when this gamestate is made the active one
function GameState:active()

   self.guiWindow:show()
   
   CEGUIWindow.getRootWindow():addChild(self.guiWindow)

   for i,s in ipairs(self.systems) do

      s:activate()
      
   end

end

--! @brief Called when another gamestate becomes active
function GameState:deactivate()

   for i,s in ipairs(self.systems) do

      s:deactivate()
      
   end

   self.guiWindow:hide()
   
   m_impl->m_guiWindow.hide()
   CEGUIWindow.getRootWindow():removeChild(self.guiWindow)

end


--! @brief Updates game logic
function GameState:update(renderTime, logicTime)

   for i,s in ipairs(self.systems) do
      if s.enabled then

         --Uncomment to debug mystical crashes and other anomalies
         -- print("Updating system " .. s.name)
         s:update(renderTime, logicTime)
         -- print("Done updating system " .. s.name)
         
      end
   end
   
   self.entityManager:processRemovals()

end


--! @brief Restores saved entities from storage
--! @param storage the StorageContainer that was created with a previous call to
--! GameState:storage
function GameState:load(storage)

   local entities = storage:get("entities")
   
   self.entityManager:clear()

   self.entityManager:restore(entities, Engine:componentFactory())
   
end

--! @brief Saves all current entities into a StorageContainer
--! @see GameState:load
--! @returns StorageContainer
function GameState:storage()
   
   local entities = self.entityManager:storage(Engine:componentFactory())

   local storage = StorageContainer.new()
   storage:set("entities", entities)
   
   return storage
end


