
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
    self.rootGUIWindow = nil
    self.scrollChange = 0
end

global_if_already_displayed = false

function HudSystem:activate()
    global_activeMicrobeStageHudSystem = self -- Global reference for event handlers
    lockedMap = Engine:playerData():lockedMap()
    if lockedMap ~= nil and not lockedMap:isLocked("Toxin") and not ss and not global_if_already_displayed then
        showMessage("'E' Releases Toxin")
        global_if_already_displayed = true
    end
    self.helpOpen = true
    self.menuOpen = true
    self.compoundsOpen = true
    Engine:resumeGame()
    self:updateLoadButton()
    -- Always start the game without being able to reproduce.
    
    self:hideReproductionDialog()
end

function HudSystem:init(gameState)
    System.init(self, "MicrobeStageHudSystem", gameState)
    self.rootGUIWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGUIWindow:getChild("CompoundsOpen")
    self.hitpointsBar = self.rootGUIWindow:getChild("HealthPanel"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    local menuButton = self.rootGUIWindow:getChild("MenuButton")
    local saveButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("SaveGameButton") 
    local loadButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton")
    local resumeButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("ResumeButton")
    local closeHelpButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("CloseHelpButton")
    --local collapseButton = self.rootGUIWindow:getChild() collapseButtonClicked
    local helpButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpButton")
    local helpPanel = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpPanel")
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
    local editorInfoOpen = self.rootGUIWindow:getChild("EditorInfoOpen")
    local editorInfoClosed = self.rootGUIWindow:getChild("EditorInfoClosed")
    local suicideButton = self.rootGUIWindow:getChild("SuicideButton")
    --local returnButton = self.rootGUIWindow:getChild("MenuButton")
    local compoundButton = self.rootGUIWindow:getChild("CompoundExpandButton")
    --local compoundPanel = self.rootGUIWindow:getChild("CompoundsOpen")
    local quitButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("QuitButton")
    saveButton:registerEventHandler("Clicked", function() self:saveButtonClicked() end)
    loadButton:registerEventHandler("Clicked", function() self:loadButtonClicked() end)
    menuButton:registerEventHandler("Clicked", function() self:menuButtonClicked() end)
    resumeButton:registerEventHandler("Clicked", function() self:resumeButtonClicked() end)
    closeHelpButton:registerEventHandler("Clicked", function() self:closeHelpButtonClicked() end)
    helpButton:registerEventHandler("Clicked", function() self:helpButtonClicked() end)
    editorInfoOpen:registerEventHandler("Clicked", function() self:editorInfoOpenClicked() end)
    editorInfoClosed:registerEventHandler("Clicked", function() self:editorInfoClosedClicked() end)
    suicideButton:registerEventHandler("Clicked", function() self:suicideButtonClicked() end)
    self.editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
    --returnButton:registerEventHandler("Clicked", returnButtonClicked)
    compoundButton:registerEventHandler("Clicked", function() self:openCompoundPanel() end)
    --compoundPanel:registerEventHandler("Clicked", function() self:closeCompoundPanel() end)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("PauseMenu"):getChild("MainMenuButton"):registerEventHandler("Clicked", function() self:menuMainMenuClicked() end)
    self:updateLoadButton();
end


function HudSystem:update(renderTime)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    local playerSpecies = playerMicrobe:getSpeciesComponent()
    --TODO display population in home patch here
    for compoundID in CompoundRegistry.getCompoundList() do
            
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), playerMicrobe:getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
           -- TODO: fix this colour
           self.compoundListItems[compoundID] = StandardItemWrapper("[colour='FF004400']" .. compoundsString, compoundID)
           -- The object will be deleted by CEGUI so make sure that it isn't touched after destroying the layout
           self.compoundListBox:listWidgetAddItem(self.compoundListItems[compoundID])
        else
           self.compoundListBox:listWidgetUpdateItem(self.compoundListItems[compoundID],
                                                      "[colour='FF004400']" .. compoundsString)
        end
    end
    
    if keyCombo(kmp.togglemenu) then
        self:menuButtonClicked()
    elseif keyCombo(kmp.gotoeditor) then
        self:editorButtonClicked()
    elseif keyCombo(kmp.shootoxytoxy) then
        playerMicrobe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 1)
    elseif keyCombo(kmp.reproduce) then
        playerMicrobe:readyToReproduce()
    end
    local direction = Vector3(0, 0, 0)
    if keyCombo(kmp.forward) then
        playerMicrobe.soundSource:playSound("microbe-movement-2")
    end
    if keyCombo(kmp.backward) then
        playerMicrobe.soundSource:playSound("microbe-movement-2")
    end
    if keyCombo(kmp.leftward) then
        playerMicrobe.soundSource:playSound("microbe-movement-1")
    end
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    end
    if keyCombo(kmp.rightward) then
        playerMicrobe.soundSource:playSound("microbe-movement-1")
    end
    if (Engine.keyboard:wasKeyPressed(Keyboard.KC_G)) then
        playerMicrobe:toggleEngulfMode()
    end
    
    local offset = Entity(CAMERA_NAME):getComponent(OgreCameraComponent.TYPE_ID).properties.offset
    
    if Engine.mouse:scrollChange()/10 ~= 0 then
        self.scrollChange = self.scrollChange + Engine.mouse:scrollChange()/10
    elseif keyCombo(kmp.plus) or keyCombo(kmp.add) then
        self.scrollChange = self.scrollChange - 5
    elseif keyCombo(kmp.minus) or keyCombo(kmp.subtract) then
        self.scrollChange = self.scrollChange + 5
    end
    
    local newZVal = offset.z
    if self.scrollChange >= 1 then
        newZVal = newZVal + 2.5
        self.scrollChange = self.scrollChange - 1
    elseif self.scrollChange <= -1 then
        newZVal = newZVal - 2.5
        self.scrollChange = self.scrollChange + 1
    end
    
    if newZVal < 10 then
        newZVal = 10
        self.scrollChange = 0
    elseif newZVal > 60 then
        newZVal = 60
        self.scrollChange = 0
    end
    
    offset.z = newZVal
end

function showReproductionDialog() global_activeMicrobeStageHudSystem:showReproductionDialog() end

function HudSystem:showReproductionDialog()
    self.editorButton:enable()
end

function hideReproductionDialog() global_activeMicrobeStageHudSystem:hideReproductionDialog() end

function HudSystem:hideReproductionDialog()
    self.editorButton:disable()
end

function showMessage(msg)
    print(msg.." (note, in-game messages currently disabled)")
    --local messagePanel = Engine:currentGameState():rootGUIWindow():getChild("MessagePanel")
    --messagePanel:getChild("MessageLabel"):setText(msg)
    --messagePanel:show()
end

function HudSystem:updateLoadButton()
    if Engine:fileExists("quick.sav") then
        self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):enable();
    else
        self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):disable();
    end
end

--Event handlers
function HudSystem:saveButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:save("quick.sav")
    print("Game Saved");
	--Because using update load button here doesn't seem to work unless you press save twice
    self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):enable();
end
function HudSystem:loadButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:load("quick.sav")
    print("Game loaded");
end

function HudSystem:menuButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print("played sound")
    self.rootGUIWindow:getChild("PauseMenu"):show()
    self.rootGUIWindow:getChild("PauseMenu"):moveToFront()
    self:updateLoadButton();
    Engine:pauseGame()
    self.menuOpen = true
    end
end

function HudSystem:resumeButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print("played sound")
    self.rootGUIWindow:getChild("PauseMenu"):hide()
    self:updateLoadButton();
    Engine:resumeGame()
    self.menuOpen = false
end

function HudSystem:toggleCompoundPanel()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    if self.compoundsOpen then
    self.rootGUIWindow:getChild("CompoundPanel"):hide()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundExpandIcon"):hide()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundContractIcon"):show()
    self.compoundsOpen = false
    else
    self.rootGUIWindow:getChild("CompoundPanel"):show()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundExpandIcon"):show()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundContractIcon"):hide()
    self.compoundsOpen = true
    end
end

function HudSystem:helpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpPanel"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("CloseHelpButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("ResumeButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("QuicksaveButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("SaveGameButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("StatsButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("OptionsButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("MainMenuButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("QuitButton"):hide()
    self.helpOpen = not self.helpOpen
end

function HudSystem:closeHelpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpPanel"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("CloseHelpButton"):hide()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("ResumeButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("QuicksaveButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("SaveGameButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("StatsButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("OptionsButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("MainMenuButton"):show()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("QuitButton"):show()
    self.helpOpen = not self.helpOpen
end

function HudSystem:menuMainMenuClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MAIN_MENU)
end

function HudSystem:editorButtonClicked()
    local player = Entity("player")
    local playerMicrobe = Microbe(player)
    -- Return the first cell to its normal, non duplicated cell arangement.
    SpeciesSystem.restoreOrganelleLayout(playerMicrobe, playerMicrobe:getSpeciesComponent()) 
    
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.editorButton:disable()
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)        
end

function HudSystem:editorInfoOpenClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("EditorInfoClosed"):show()
    self.rootGUIWindow:getChild("EditorInfoOpen"):hide()
end

function HudSystem:editorInfoClosedClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("EditorInfoClosed"):hide()
    self.rootGUIWindow:getChild("EditorInfoOpen"):show()
end

--[[
function HudSystem:returnButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    --Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):hide()
        Engine:currentGameState():rootGUIWindow():getChild("MessagePanel"):hide()
        Engine:currentGameState():rootGUIWindow():getChild("ReproductionPanel"):hide()
        Engine:resumeGame()
    elseif Engine:currentGameState():name() == "microbe_editor" then
        Engine:currentGameState():rootGUIWindow():getChild("SaveLoadPanel"):hide()
    end
end --]]

function quitButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:quit()
end
