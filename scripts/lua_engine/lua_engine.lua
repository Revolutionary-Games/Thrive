--! @file Lua versions of functions in engine.cpp
--!
--! This is done to allow the main loop of the game to be in lua and
--! that way reduce calls from C++ to Lua. With JIT the Lua code
--! is fast enough to be the "glue" between C++ systems and Lua
--! systems and main loop

LuaEngine = class(
   --! @brief Initializes the lua side of the engine
   --! @param cppSide the engine object received from
   --! c++ code
   function(self)

      -- The state the engine is switching to on next frame
      self.nextGameState = nil
      self.currentGameState = nil

      -- The console object attaches itself here
      self.console = nil
      self.consoleGUIWindow = nil

      -- std::list<std::tuple<EntityId, EntityId, GameState*, GameState*>> m_entitiesToTransferGameState;
      -- std::map<System*, int>* m_nextShutdownSystems;
      -- std::map<System*, int>* m_prevShutdownSystems;
   end
)

function LuaEngine:attachCpp(cppSide)

   assert(cppSide ~= nil)

   self.Engine = cppSide

   
   self.consoleGUIWindow = CEGUIWindow.new("Console")

   -- Initialize states that have been created while loading all the scripts
   
   GameState* previousGameState = m_impl->m_currentGameState;
   for (const auto& pair : m_impl->m_gameStates) {
      const auto& gameState = pair.second;
      m_impl->m_currentGameState = gameState.get();
      gameState->init();
                                                 }

   m_impl->m_currentGameState = previousGameState;

   
end

function LuaEngine:shutdown()

   for (const auto& pair : m_impl->m_gameStates) {
      const auto& gameState = pair.second;
      gameState->shutdown();
                                                 }

end

--! @param name Unique name of the system
--! @param systems Array of systems that are in the new GameState.
--! Must be created with `table.insert(systems, s)`
--! @param physics If true creates a physics state in the GameState
--! @todo Make sure that .destroy() is called on these objects
function LuaEngine:createGameState(name, systems, physics, guiLayoutName)

   local newState = GameState.new(name, systems, self, physics, guiLayoutName)

   assert(m_impl->m_gameStates.find(name) == m_impl->m_gameStates.end() &&
"Duplicate GameState name");
std::unique_ptr<GameState> gameState(new GameState(
                                           *this,
                                        name,
                                        std::move(systems),
                                        initializer,
                                        guiLayoutName
                                    ));
GameState* rawGameState = gameState.get();
m_impl->m_gameStates.insert(std::make_pair(
                               name,
                               std::move(gameState)
                           ));
return rawGameState;

   
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

   assert(gameState != nullptr && "GameState must not be null");
   m_impl->m_nextGameState = gameState;
   for (auto& pair : *m_impl->m_prevShutdownSystems){
      //Make sure systems are deactivated before any potential reactivations
      pair.first->deactivate();
                                                    }
   m_impl->m_prevShutdownSystems = m_impl->m_nextShutdownSystems;
   m_impl->m_nextShutdownSystems = m_impl->m_prevShutdownSystems;
   m_impl->m_nextShutdownSystems->clear();

end



--! @brief Retrieves a game state
--! @param name The game state's name
--! @return The GameState with the name or nil
function LuaEngine:getGameState(name)

         auto iter = m_impl->m_gameStates.find(name);
         if (iter != m_impl->m_gameStates.end()) {
   return iter->second.get();

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
--!  The old entitymanager which is currently handling the entity. EntityManager type
--!
--! @param newGameState
--!  The new gamestate to transfer the entity to
--! @return The new entity id in the new gamestate
function LuaEngine:transferEntityGameState(oldEntityId,
                                           oldEntityManager,
                                           newGameState
                                          )

   -- TODO: Detect if newGameState is a c++ object GameStateData or a lua GameState object

   EntityId newEntity;
   const std::string* nameMapping = oldEntityManager->getNameMappingFor(oldEntityId);
   if (nameMapping){
      newEntity = newGameState->entityManager().getNamedId(*nameMapping, true);
                   }
   else{
         newEntity = newGameState->entityManager().generateNewId();
   }
      oldEntityManager->transferEntity(oldEntityId, newEntity, newGameState->entityManager(), m_impl->m_componentFactory);
      return newEntity;
   
   
end



--! @protected @brief Changes the current GameState right now. May not
--! be called during an update!
function LuaEngine:activateGameState(gameState)

   if self.currentGameState ~= nil then

      self.currentGameState:deactivate()
      
   end

   self.currentGameState = gameState
   
   if self.currentGameState ~= nil then
      
      gameState:activate()
      
      gameState:rootGUIWindow():addChild(self.consoleGUIWindow)
      
      self.console:registerEvents(gameState)
   end

end


--! @protected @brief Called from C++ side to load game states from a StorageContainer
--! @param saveGame StorageContainer with saved data
function LuaEngine:loadSavegameGameStates(saveGame)

   GameState* previousGameState = m_currentGameState;
   this->activateGameState(nullptr);
   StorageContainer gameStates = savegame.get<StorageContainer>("gameStates");
   for (const auto& pair : m_gameStates) {
      if (gameStates.contains(pair.first)) {
         // In case anything relies on the current game state
         // during loading, temporarily switch it
         m_currentGameState = pair.second.get();
         pair.second->load(
            gameStates.get<StorageContainer>(pair.first)
                          );
                                           }
      else {
            pair.second->entityManager().clear();
      }
         }
for (auto& kv : *m_prevShutdownSystems) {
   kv.first->deactivate();
                                        }
for (auto& kv : *m_nextShutdownSystems) {
   kv.first->deactivate();
                                        }
m_prevShutdownSystems->clear();
m_nextShutdownSystems->clear();
m_currentGameState = nullptr;
// Switch gamestate
std::string gameStateName = savegame.get<std::string>("currentGameState");
auto iter = m_gameStates.find(gameStateName);
if (iter != m_gameStates.end()) {
   this->activateGameState(iter->second.get());
                                }
else {
      this->activateGameState(previousGameState);
      // TODO: Log error
}


   

   end

--! @protected @brief Called from C++ side to load game states from a StorageContainer
--! @param saveGame StorageContainer to be filled with saved data
function LuaEngine:saveCurrentStates(saveGame)

   savegame.set("currentGameState", m_currentGameState->name());
   savegame.set("playerData", m_playerData.storage());
   StorageContainer gameStates;
   for (const auto& pair : m_gameStates) {
      gameStates.set(pair.first, pair.second->storage());
                                         }
   savegame.set("gameStates", std::move(gameStates));

   
end


g_luaEngine = LuaEngine.new()


