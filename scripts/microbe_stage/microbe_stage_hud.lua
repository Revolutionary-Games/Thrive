
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
	self.compoundListBox = nil
	self.hitpointsCountLabel = nil
	self.hitpointsBar = nil
	self.compoundListItems = {}
    self.rootGUIWindow = nil
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
    self.menuOpen = true
end

function HudSystem:init(gameState)
    System.init(self, gameState) ---[[
    self.rootGUIWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGUIWindow:getChild("CompoundsOpen"):getChild("CompoundsLabel")
    self.hitpointsBar = self.rootGUIWindow:getChild("HealthPanel"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    self.nameLabel = self.rootGUIWindow:getChild("SpeciesNamePanel"):getChild("SpeciesNameLabel")
    local menuButton = self.rootGUIWindow:getChild("MenuButton") 
    --local collapseButton = self.rootGUIWindow:getChild() collapseButtonClicked
    local helpButton = self.rootGUIWindow:getChild("HelpButton")
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
    --local returnButton = self.rootGUIWindow:getChild("MenuButton")
    local compoundButton = self.rootGUIWindow:getChild("CompoundsClosed")
    local compoundPanel = self.rootGUIWindow:getChild("CompoundsOpen")
    local quitButton = self.rootGUIWindow:getChild("QuitButton")
    menuButton:registerEventHandler("Clicked", function() self:menuButtonClicked() end)
    helpButton:registerEventHandler("Clicked", function() self:helpButtonClicked() end)
    self.editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
    --returnButton:registerEventHandler("Clicked", returnButtonClicked)
    compoundButton:registerEventHandler("Clicked", function() self:openCompoundPanel() end)
    compoundPanel:registerEventHandler("Clicked", function() self:closeCompoundPanel() end)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked) -- in microbe_editor_hud.lua
end


function HudSystem:update(renderTime)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)
    self.nameLabel:setText(playerMicrobe.microbe.speciesName)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    
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
    self.compoundListBox:listboxHandleUpdatedItemData() --]]
    
    if  Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
        self:menuButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
        self:editorButtonClicked()
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
    offset.z = newZVal --]]
end

function showReproductionDialog() global_activeMicrobeStageHudSystem:showReproductionDialog() end

function HudSystem:showReproductionDialog()
    print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
    --global_activeMicrobeStageHudSystem.rootGUIWindow:getChild("ReproductionPanel"):show()
    self.editorButton:enable()
end

function showMessage(msg)
    print(msg.." (note, in-game messages currently disabled)")
    --local messagePanel = Engine:currentGameState():rootGUIWindow():getChild("MessagePanel")
    --messagePanel:getChild("MessageLabel"):setText(msg)
    --messagePanel:show()
end

--Event handlers
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
        self.menuOpen = true
    else
        self.rootGUIWindow:getChild("StatsButton"):playAnimation("MoveToMenuButtonD0");
        self.rootGUIWindow:getChild("HelpButton"):playAnimation("MoveToMenuButtonD2");
        self.rootGUIWindow:getChild("OptionsButton"):playAnimation("MoveToMenuButtonD1");
        self.rootGUIWindow:getChild("LoadGameButton"):playAnimation("MoveToMenuButtonD4");
        self.rootGUIWindow:getChild("SaveGameButton"):playAnimation("MoveToMenuButtonD3");
        self.menuOpen = false
    end
end

function HudSystem:openCompoundPanel()
    self.rootGUIWindow:getChild("CompoundsOpen"):show()
    self.rootGUIWindow:getChild("CompoundsClosed"):hide()
end

function HudSystem:closeCompoundPanel()
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