
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
	self.compoundListBox = nil
	self.hitpointsCountLabel = nil
    self.hitpointsMaxLabel = nil
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
    self.helpOpen = false
    self.menuOpen = false
    self.compoundsOpen = true
    Engine:resumeGame()
    self:updateLoadButton();
end

function HudSystem:init(gameState)
    System.init(self, "MicrobeStageHudSystem", gameState)
    self.rootGUIWindow =  gameState:rootGUIWindow()
    self.hitpointsBar = self.rootGUIWindow:getChild("HealthPanel"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    self.hitpointsMaxLabel = self.rootGUIWindow:getChild("HealthPanel"):getChild("HealthTotal")
    self.hitpointsBar:setProperty("ThriveGeneric/HitpointsBar", "FillImage") 
    local menuButton = self.rootGUIWindow:getChild("MenuButton")
    local saveButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("QuicksaveButton") 
    local loadButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton")
    local resumeButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("ResumeButton")
    local closeHelpButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("CloseHelpButton")
    --local collapseButton = self.rootGUIWindow:getChild() collapseButtonClicked
    local helpButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpButton")
    local helpPanel = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpPanel")
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
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
    suicideButton:registerEventHandler("Clicked", function() self:suicideButtonClicked() end)
    self.editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
    --returnButton:registerEventHandler("Clicked", returnButtonClicked)
    compoundButton:registerEventHandler("Clicked", function() self:toggleCompoundPanel() end)
    --compoundPanel:registerEventHandler("Clicked", function() self:closeCompoundPanel() end)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("PauseMenu"):getChild("MainMenuButton"):registerEventHandler("Clicked", function() self:menuMainMenuClicked() end)
    self:updateLoadButton();

    self.atpBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("ATPBar"):getChild("ATPBar")
    self.atpCountLabel = self.atpBar:getChild("NumberLabel")
    self.atpMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("ATPBar"):getChild("ATPTotal")
    self.atpBar:setProperty("ThriveGeneric/ATPBar", "FillImage")
	
	self.atpCountLabel2 = self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPValue")
	
	self.oxygenBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxygenBar"):getChild("OxygenBar")
    self.oxygenCountLabel = self.oxygenBar:getChild("NumberLabel")
    self.oxygenMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxygenBar"):getChild("OxygenTotal")
    self.oxygenBar:setProperty("ThriveGeneric/OxygenBar", "FillImage")
	
	self.aminoacidsBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AminoAcidsBar"):getChild("AminoAcidsBar")
    self.aminoacidsCountLabel = self.aminoacidsBar:getChild("NumberLabel")
    self.aminoacidsMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AminoAcidsBar"):getChild("AminoAcidsTotal")
    self.aminoacidsBar:setProperty("ThriveGeneric/AminoAcidsBar", "FillImage")
	
	self.ammoniaBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AmmoniaBar"):getChild("AmmoniaBar")
    self.ammoniaCountLabel = self.ammoniaBar:getChild("NumberLabel")
    self.ammoniaMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AmmoniaBar"):getChild("AmmoniaTotal")
    self.ammoniaBar:setProperty("ThriveGeneric/AmmoniaBar", "FillImage")
	
	self.glucoseBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("GlucoseBar"):getChild("GlucoseBar")
    self.glucoseCountLabel = self.glucoseBar:getChild("NumberLabel")
    self.glucoseMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("GlucoseBar"):getChild("GlucoseTotal")
    self.glucoseBar:setProperty("ThriveGeneric/GlucoseBar", "FillImage")
	
	self.co2Bar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("CO2Bar"):getChild("CO2Bar")
    self.co2CountLabel = self.co2Bar:getChild("NumberLabel")
    self.co2MaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("CO2Bar"):getChild("CO2Total")
    self.co2Bar:setProperty("ThriveGeneric/CO2Bar", "FillImage")
	
	self.fattyacidsBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("FattyAcidsBar"):getChild("FattyAcidsBar")
	self.fattyacidsCountLabel = self.fattyacidsBar:getChild("NumberLabel")
    self.fattyacidsMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("FattyAcidsBar"):getChild("FattyAcidsTotal")
    self.fattyacidsBar:setProperty("ThriveGeneric/FattyAcidsBar", "FillImage")
	
	self.oxytoxyBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxyToxyNTBar"):getChild("OxyToxyNTBar")
	self.oxytoxyCountLabel = self.oxytoxyBar:getChild("NumberLabel")
    self.oxytoxyMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxyToxyNTBar"):getChild("OxyToxyNTTotal")
    self.oxytoxyBar:setProperty("ThriveGeneric/OxyToxyBar", "FillImage")
end


function HudSystem:update(renderTime)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    self.hitpointsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.maxHitpoints))

    self.atpBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))))
    self.atpCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp"))))
    self.atpMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))))
	
	self.atpCountLabel2:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp"))))
	
	self.oxygenBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxygen"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxygen"))))
    self.oxygenCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxygen"))))
    self.oxygenMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxygen"))))
	
	self.aminoacidsBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("aminoacids"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("aminoacids"))))
    self.aminoacidsCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("aminoacids"))))
    self.aminoacidsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("aminoacids"))))
	
	self.ammoniaBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("ammonia"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("ammonia"))))
    self.ammoniaCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("ammonia"))))
    self.ammoniaMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("ammonia"))))
	
	self.glucoseBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("glucose"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("glucose"))))
    self.glucoseCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("glucose"))))
    self.glucoseMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("glucose"))))
	
	self.co2Bar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("co2"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("co2"))))
    self.co2CountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("co2"))))
    self.co2MaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("co2"))))
	
	--[[ self.fattyacidsBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids")))) --]]
	
	self.oxytoxyBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxytoxy"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))

    local playerSpecies = playerMicrobe:getSpeciesComponent()
    --TODO display population in home patch here
    
    
    if keyCombo(kmp.togglemenu) then
        self:menuButtonClicked()
    elseif keyCombo(kmp.gotoeditor) then
        self:editorButtonClicked()
    elseif keyCombo(kmp.shootoxytoxy) then
        playerMicrobe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 3)
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
   -- print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
    --global_activeMicrobeStageHudSystem.rootGUIWindow:getChild("ReproductionPanel"):show()
    self.editorButton:enable()
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
    self.rootGUIWindow:getChild("PauseMenu"):hide()
    self.menuOpen = false
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
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.editorButton:disable()
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
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
