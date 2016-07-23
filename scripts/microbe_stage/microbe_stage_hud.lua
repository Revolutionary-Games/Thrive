
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
    self.menuOpen = false
    self:updateLoadButton();
end

function HudSystem:init(gameState)
    System.init(self, "MicrobeStageHudSystem", gameState)
    self.rootGUIWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGUIWindow:getChild("CompoundsOpen"):getChild("CompoundsLabel")
    self.hitpointsBar = self.rootGUIWindow:getChild("HealthPanel"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    self.nameLabel = self.rootGUIWindow:getChild("SpeciesNamePanel"):getChild("SpeciesNameLabel")
    local menuButton = self.rootGUIWindow:getChild("MenuButton")
    local saveButton = self.rootGUIWindow:getChild("SaveGameButton") 
    local loadButton = self.rootGUIWindow:getChild("LoadGameButton")	
    --local collapseButton = self.rootGUIWindow:getChild() collapseButtonClicked
    local helpButton = self.rootGUIWindow:getChild("HelpButton")
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
    --local returnButton = self.rootGUIWindow:getChild("MenuButton")
    local compoundButton = self.rootGUIWindow:getChild("CompoundsClosed")
    local compoundPanel = self.rootGUIWindow:getChild("CompoundsOpen")
    local quitButton = self.rootGUIWindow:getChild("QuitButton")
    saveButton:registerEventHandler("Clicked", function() self:saveButtonClicked() end)
    loadButton:registerEventHandler("Clicked", function() self:loadButtonClicked() end)
    menuButton:registerEventHandler("Clicked", function() self:menuButtonClicked() end)
    helpButton:registerEventHandler("Clicked", function() self:helpButtonClicked() end)
    self.editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
    --returnButton:registerEventHandler("Clicked", returnButtonClicked)
    compoundButton:registerEventHandler("Clicked", function() self:openCompoundPanel() end)
    compoundPanel:registerEventHandler("Clicked", function() self:closeCompoundPanel() end)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked) -- in microbe_editor_hud.lua
    self:updateLoadButton();
end


function HudSystem:update(renderTime)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)
    local name = playerMicrobe.microbe.speciesName
    if string.len(name) > 18 then
        name = string.sub(playerMicrobe.microbe.speciesName, 1, 15)
        name = name .. "..."
    end
    self.nameLabel:setText(name)

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
        playerMicrobe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 3)
    elseif keyCombo(kmp.reproduce) then
        playerMicrobe:reproduce()
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
        self.rootGUIWindow:getChild("LoadGameButton"):enable();
    else
        self.rootGUIWindow:getChild("LoadGameButton"):disable();
    end
end

--Event handlers
function HudSystem:saveButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:save("quick.sav")
    print("Game Saved");
	--Because using update load button here doesn't seem to work unless you press save twice
    self.rootGUIWindow:getChild("LoadGameButton"):enable();
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
    if not self.menuOpen then
        self.rootGUIWindow:getChild("StatsButton"):playAnimation("MoveToStatsButton");
        self.rootGUIWindow:getChild("HelpButton"):playAnimation("MoveToHelpButton");
        self.rootGUIWindow:getChild("OptionsButton"):playAnimation("MoveToOptionsButton");
        self.rootGUIWindow:getChild("LoadGameButton"):playAnimation("MoveToLoadGameButton");
        self.rootGUIWindow:getChild("SaveGameButton"):playAnimation("MoveToSaveGameButton");
        self:updateLoadButton();
        self.menuOpen = true
    else
        self.rootGUIWindow:getChild("StatsButton"):playAnimation("MoveToMenuButtonD0");
        self.rootGUIWindow:getChild("HelpButton"):playAnimation("MoveToMenuButtonD2");
        self.rootGUIWindow:getChild("OptionsButton"):playAnimation("MoveToMenuButtonD1");
        self.rootGUIWindow:getChild("LoadGameButton"):playAnimation("MoveToMenuButtonD4");
        self.rootGUIWindow:getChild("SaveGameButton"):playAnimation("MoveToMenuButtonD3");
        self:updateLoadButton();
        self.menuOpen = false
    end
end


function HudSystem:openCompoundPanel()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("CompoundsOpen"):show()
    self.rootGUIWindow:getChild("CompoundsClosed"):hide()
end

function HudSystem:closeCompoundPanel()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("CompoundsOpen"):hide()
    self.rootGUIWindow:getChild("CompoundsClosed"):show()
end

function HudSystem:helpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    --Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        if self.helpOpen then
            Engine:resumeGame()
            self.rootGUIWindow:getChild("HelpPanel"):hide()
        else
            Engine:pauseGame()
            self.rootGUIWindow:getChild("HelpPanel"):show()
        end
        self.helpOpen = not self.helpOpen
    end
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
