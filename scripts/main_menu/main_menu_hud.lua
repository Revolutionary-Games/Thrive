-- Updates the hud with relevant information
class 'MainMenuHudSystem' (System)

function MainMenuHudSystem:__init()
    System.__init(self)
end

function MainMenuHudSystem:init(gameState)
    System.init(self, gameState)
    local root = gameState:rootGUIWindow()
    local microbeButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("NewGameButton")
    local microbeEditorButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("EditorMenuButton")
    local quitButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("ExitGameButton")
    local loadButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton")   
    microbeButton:registerEventHandler("Clicked", mainMenuMicrobeStageButtonClicked)
    microbeEditorButton:registerEventHandler("Clicked", mainMenuMicrobeEditorButtonClicked)
    loadButton:registerEventHandler("Clicked", mainMenuLoadButtonClicked)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
end

function MainMenuHudSystem:update(renderTime, logicTime)
end

function mainMenuLoadButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:load("quick.sav")
    print("Game loaded");
end

function mainMenuMicrobeStageButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE)
end

function mainMenuMicrobeEditorButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

-- quitButtonClicked is already defined in microbe_stage_hud.lua
