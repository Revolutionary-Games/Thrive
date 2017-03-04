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

        -- The current main GameState. Touching this directly WILL cause problems
        -- (unless you are careful, but... don't do it)
        self.currentGameState = nil

        -- List of all created GameStates
        self.gameStates = {}

        

        -- The console object attaches itself here
        self.console = nil
        self.consoleGUIWindow = nil

        -- std::list<std::tuple<EntityId, EntityId, GameState*, GameState*>> m_entitiesToTransferGameState;


        -- This is a table of systems that are going to be moved to prevShutdownSystems once
        -- the GameState changes. So this is used to get all the systems that the current state
        -- wants to timed shutdown
        self.nextShutdownSystems = {}

        -- This is a table of currently running systems that need to be shutdown
        self.prevShutdownSystems = {}
        
    end
)

--! @brief Initializes the lua side of the engine
--! @param cppSide the engine object received from
--! c++ code
function LuaEngine:init(cppSide)

    assert(cppSide ~= nil)

    self.Engine = cppSide

    self.initialized = true

    print("LuaEngine init started")
    
    self.consoleGUIWindow = CEGUIWindow.new("Console")

    -- Store current state
    local previousGameState = self.currentGameState

    -- Initialize states that have been created while loading all the scripts
    for _,s in pairs(self.gameStates) do

        self.currentGameState = s

        s:init()
        
    end
    
    -- Restore the state
    self.currentGameState = previousGameState
    
end

--! @brief Shutsdown all systems
function LuaEngine:shutdown()

    for _,s in pairs(self.gameStates) do

        s:shutdown()
        
    end
end

--! @param name Unique name of the system
--! @param systems Array of systems that are in the new GameState.
--! Must be created with `table.insert(systems, s)`
--! @param physics If true creates a physics state in the GameState
--! @todo Make sure that .destroy() is called on these objects
--! @param extraInitializer Function to be ran just after the GameState
--! is initialized. The first parameter to the function is the gameState
function LuaEngine:createGameState(name,
                                   systems,
                                   physics,
                                   guiLayoutName,
                                   extraInitializer)

    assert(self.initialized ~= true,
           "LuaEngine: trying to create state after init. State wouldn't be initialized!")
    
    if extraInitializer ~= nil then

        -- Initializer must be a function
        assert(type(extraInitializer) == "function",
               "extraInitializer must be a function")

    end
    -- Type check everything for bad calls
    assert(type(name) == "string")
    assert(type(systems) == "table")
    assert(type(physics) == "boolean")
    assert(type(guiLayoutName) == "string")
    
    assert(self.gameStates[name] == nil, "Duplicate GameState name")

    local newState = GameState.new(name, systems, self, physics,
                                   guiLayoutName, extraInitializer)

    self.gameStates[name] = newState
    
    return newState
end

--! @brief Runs updates on some core systems and the current GameState
function LuaEngine:update(milliseconds)

    self.Engine:update(milliseconds)

    -- Update GameStates
    
    if self.nextGameState ~= nil then
        
        self:activateGameState(self.nextGameState)
        self.nextGameState = nil
        
    end

    if self.currentGameState == nil then
        error("currentGameState is nil")
    end
    
    -- Update current GameState
    local updateTime = milliseconds

    if self.Engine.paused then
        updateTime = 0
    end
    
    self.currentGameState:update(milliseconds, updateTime)

    -- Update console
    self.console:update()


    -- Update any timed shutdown systems
    -- Reverse iterate to safely remove items
    for i = #self.prevShutdownSystems, 1, -1 do

        local delayed  = self.prevShutdownSystems[i]
        
        local updateTime = min(delayed.timeLeft, milliseconds);

        
        local pauseHelper = updateTime

        if self.Engine.paused then

            pauseHelper = 0
            
        end

        delayed.system:update(updateTime, pauseHelper)
        
        delayed.timeLeft = delayed.timeLeft - updateTime

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

    local state = self:gameStateFromCpp(system)

    if system ~= nil then
        system = state
    end

    table.insert(self.prevShutdownSystems, { timeLeft = milliseconds, ["system"] = system })

end

--! @brief Returns true if system is already queued for shutdown
function LuaEngine:isSystemTimedShutdown(system)

    local state = self:gameStateFromCpp(system)

    if system ~= nil then
        system = state
    end

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
--! \a gameState must not be \c null. 
--! 
--! @param gameState GameState The new game state
function LuaEngine:setCurrentGameState(gameState)

    assert(gameState ~= nil, "GameState must not be null")
    
    self.nextGameState = gameState;

    --Make sure systems are deactivated before any potential reactivations

    for _,p in pairs(self.prevShutdownSystems) do

        p.system:deactivate()
        
    end

    self.prevShutdownSystems = self.nextShutdownSystems
    self.nextShutdownSystems = {}

end



--! @brief Retrieves a game state
--! @param name The game state's name
--! @return The GameState with the name or nil
function LuaEngine:getGameState(name)

    return self.gameStates[name]
    
end

--! @brief Returns a system that has the potential C++ side object
function LuaEngine:gameStateFromCpp(cppObj)
    -- TODO: Detect if newGameState is a c++ object GameStateData or a lua GameState object
    print("type thing: " .. type(cppObj))
    error("todo:")

    for _,s in pairs(self.gameStates) do

        if s.wrapper == cppObj then

            return s

        end
        
    end

    return nil
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
                                           newGameState)

    local state = self:gameStateFromCpp(newGameState)

    if state ~= nil then
        newGameState = state
    end

    local newEntity -- EntityId
    
    local nameMapping = oldEntityManager:getNameMappingFor(oldEntityId)
    
    if nameMapping ~= nil then
        
        newEntity = newGameState.entityManager:getNamedId(nameMapping, true)

    else
        newEntity = newGameState.entityManager:generateNewId()
    end
    
    oldEntityManager:transferEntity(
        oldEntityId, newEntity, newGameState.entityManager, Engine.componentFactory);

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

    local previousGameState = self.currentGameState

    self:activateGameState(nil)

    local gameStatesContainer = savegame:get("gameStates")

    for name, system in pairs(self.gameStates) do

        if gameStatesContainer:contains(name) then
            
            -- In case anything relies on the current game state
            -- during loading, temporarily switch it
            self.currentGameState = system
            
            system:load(gameStatesContainer:get(name))

        else 
            system.entityManager:clear()
        end  
        
    end

    for _,p in pairs(self.prevShutdownSystems) do

        p.system:deactivate()
        
    end

    for _,p in pairs(self.nextShutdownSystems) do

        p.system:deactivate()
        
    end

    self.nextShutdownSystems = {}
    self.prevShutdownSystems = {}
    
    
    self.currentGameState = nil
    
    -- Switch gamestate
    local gameStateName = savegame:get("currentGameState")

    local gameState = self:getGameState(gameStateName)

    if gameState ~= nil then

        self:activateGameState(gameState)

    else
        
        self:activateGameState(previousGameState)
        print("Error loading GameStates: unkown name for 'currentGameState'")
        
    end

end


--! @protected @brief Called from C++ side to load game states from a StorageContainer
--! @param saveGame StorageContainer to be filled with saved data
function LuaEngine:saveCurrentStates(saveGame)

    savegame:set("currentGameState", self.currentGameState.name)
    
    local gameStatesContainer = StorageContainer.new()

    for name, system in pairs(self.gameStates) do

        gameStatesContainer:set(name, system:storage())
        
    end
    
    savegame:set("gameStates", gameStatesContainer)
    
end

--! Sets the console object. Called from console.lua
function LuaEngine:registerConsoleObject(console)

    self.console = console;
    
end

--! Global LuaEngine instance
g_luaEngine = LuaEngine.new()


