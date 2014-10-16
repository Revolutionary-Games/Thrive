
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
	self.compoundListBox = nil
	self.hitpointsCountLabel = nil
	self.hitpointsBar = nil
	self.compoundListItems = {}
    self.rootGuiWindow = nil
    self.populationNumberLabel = nil
end

global_if_already_displayed = false

function HudSystem:activate()
    global_activeMicrobeStageHudSystem = self -- Global reference for event handlers
    lockedMap = Engine:playerData():lockedMap()
    if lockedMap ~= nil and not lockedMap:isLocked("Toxin") and not ss and not global_if_already_displayed then
        showMessage("'E' Releases Toxin")
        global_if_already_displayed = true
    end
end

function HudSystem:init(gameState)
    System.init(self, gameState)
    self.rootGuiWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGuiWindow:getChild("BottomSection"):getChild("CompoundList")
    self.hitpointsBar = self.rootGuiWindow:getChild("BottomSection"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    self.populationNumberLabel = self.rootGuiWindow:getChild("BottomSection"):getChild("Population"):getChild("Number")
    local menuButton = self.rootGuiWindow:getChild("BottomSection"):getChild("MenuButton")
    local helpButton = self.rootGuiWindow:getChild("BottomSection"):getChild("HelpButton")
    local editorButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("EditorButton")
    local editorButton2 = self.rootGuiWindow:getChild("ReproductionPanel"):getChild("EditorButton")
    local returnButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("ReturnButton")
    local returnButton2 = self.rootGuiWindow:getChild("HelpPanel"):getChild("ReturnButton")
    local returnButton3 = self.rootGuiWindow:getChild("MessagePanel"):getChild("ReturnButton")
    local returnButton4 = self.rootGuiWindow:getChild("ReproductionPanel"):getChild("ReturnButton")
    local quitButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("QuitButton")
    menuButton:registerEventHandler("Clicked", menuButtonClicked)
    helpButton:registerEventHandler("Clicked", helpButtonClicked)
    editorButton:registerEventHandler("Clicked", editorButtonClicked)
    editorButton2:registerEventHandler("Clicked", editorButtonClicked)
    returnButton:registerEventHandler("Clicked", returnButtonClicked)
    returnButton2:registerEventHandler("Clicked", returnButtonClicked)
    returnButton3:registerEventHandler("Clicked", returnButtonClicked)
    returnButton4:registerEventHandler("Clicked", returnButtonClicked)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGuiWindow:getChild("MenuPanel"):getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked)
end


function HudSystem:update(renderTime)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    local playerSpecies = playerMicrobe:getSpeciesComponent()
    self.populationNumberLabel:setText("" .. math.floor(playerSpecies.currentPopulation))
    for compoundID in CompoundRegistry.getCompoundList() do
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), playerMicrobe:getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.compoundListItems[compoundID] = ListboxItem(compoundsString)
            self.compoundListItems[compoundID]:setTextColours(0.0, 0.25, 0.0)
            self.compoundListBox:listboxAddItem(self.compoundListItems[compoundID])
        else
            self.compoundListItems[compoundID]:setText(compoundsString)
        end
    end
    self.compoundListBox:listboxHandleUpdatedItemData()
    
    if  Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
        menuButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
        editorButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_E) then
        playerMicrobe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 3)
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_P) then
        playerMicrobe:reproduce()
    end
    local direction = Vector3(0, 0, 0)
    if (Engine.keyboard:wasKeyPressed(Keyboard.KC_W)) then
        playerMicrobe.soundSource:playSound("microbe-movement-2")
    end
    if (Engine.keyboard:wasKeyPressed(Keyboard.KC_S)) then
        playerMicrobe.soundSource:playSound("microbe-movement-2")
    end
    if (Engine.keyboard:wasKeyPressed(Keyboard.KC_A)) then
        playerMicrobe.soundSource:playSound("microbe-movement-1")
    end
    if (Engine.keyboard:wasKeyPressed(Keyboard.KC_D)) then
        playerMicrobe.soundSource:playSound("microbe-movement-1")
    end
    
    offset = Entity(CAMERA_NAME):getComponent(OgreCameraComponent.TYPE_ID).properties.offset
    newZVal = offset.z + Engine.mouse:scrollChange()/10
    if newZVal < 10 then
        newZVal = 10
    elseif newZVal > 80 then
        newZVal = 80
    end
    offset.z = newZVal
end

function showReproductionDialog()
    global_activeMicrobeStageHudSystem.rootGuiWindow:getChild("ReproductionPanel"):show()
end

function showMessage(msg)
    local messagePanel = Engine:currentGameState():rootGUIWindow():getChild("MessagePanel")
    messagePanel:getChild("MessageLabel"):setText(msg)
    messagePanel:show()
end

--Event handlers
function menuButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):show()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):hide()
    end
    Engine:pauseGame()
end

function helpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):show()
    end
end

function editorButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

function returnButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):hide()
        Engine:currentGameState():rootGUIWindow():getChild("MessagePanel"):hide()
        Engine:currentGameState():rootGUIWindow():getChild("ReproductionPanel"):hide()
        Engine:resumeGame()
    elseif Engine:currentGameState():name() == "microbe_editor" then
        Engine:currentGameState():rootGUIWindow():getChild("SaveLoadPanel"):hide()
    end
end

function quitButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:quit()
end