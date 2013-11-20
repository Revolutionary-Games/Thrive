class 'SwitchGameStateSystem' (System)

function SwitchGameStateSystem:__init()
    System.__init(self)
end


function SwitchGameStateSystem:update(milliseconds)
    if Engine.keyboard:wasKeyPressed(Keyboard.KC_F1) then
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


