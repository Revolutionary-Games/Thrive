-- Updates the hud with relevant information
class 'MainMenuHudSystem' (System)

function MainMenuHudSystem:__init()
    System.__init(self)
end

function MainMenuHudSystem:init(gameState)
    System.init(self, gameState)
    local root = gameState:rootGUIWindow()
    local microbeButton = root:getChild("Background"):getChild("MicrobeButton")
    local microbeEditorButton = root:getChild("Background"):getChild("MicrobeEditorButton")
    local mitochondriaButton = root:getChild("Background"):getChild("QuitButton")
    
    root:getChild("Background"):getChild("MicrobeButton"):registerEventHandler("Clicked", mainMenuMicrobeStageButtonClicked)
    root:getChild("Background"):getChild("MicrobeEditorButton"):registerEventHandler("Clicked", mainMenuMicrobeEditorButtonClicked)
    root:getChild("Background"):getChild("QuitButton"):registerEventHandler("Clicked", quitButtonClicked)
end

function MainMenuHudSystem:update(renderTime, logicTime)
end

function mainMenuMicrobeStageButtonClicked()
    Engine:setCurrentGameState(GameState.MICROBE)
end

function mainMenuMicrobeEditorButtonClicked()
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end