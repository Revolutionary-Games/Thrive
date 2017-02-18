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


GameState = createClass()

function GameState.new()

   local self = GameState._createInstance()

   self.name = "unnamed GameState"

   -- Systems container
   self.systems = {}
   
   return self
end

--! @brief Initializes a state to be used.
--! @note calls this only through LuaEngine:createGameState
function GameState:init(name, engine)

   assert(name ~= nil)
   assert(engine ~= nil)

   self.engine = engine
   self.name = name

   -- Init systems
   for i,s in ipairs(self.systems) do

      s:init(self)
      
   end
   
end

--! @brief Adds a system
function GameState:addSystem(system)

   assert(system ~= nil)

   table.insert(self.systems, system)

end

--! @brief Adds physics to this GameState
function GameState:initPhysics()

   self.physicsWorld = PhysicalWorld.new()

   
   
end

