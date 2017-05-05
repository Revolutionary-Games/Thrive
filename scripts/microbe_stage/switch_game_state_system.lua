SwitchGameStateSystem = class(
    LuaSystem,
    function(self)
        
        LuaSystem.create(self)

    end
)

function SwitchGameStateSystem:init()
    LuaSystem.init(self, "SwitchGameStateSystem", gameState)
end

function SwitchGameStateSystem:update(renderTime, logicTime)
    if keyCombo(kmp.altuniverse) then
        local currentState = Engine:currentGameState()
        local nextGameState
        if Engine:currentGameState():name() == GameState.MICROBE:name() then
            nextGameState = GameState.MICROBE_ALTERNATE
        else
            nextGameState = GameState.MICROBE
        end
        Engine:setCurrentGameState(nextGameState)
    end
end


