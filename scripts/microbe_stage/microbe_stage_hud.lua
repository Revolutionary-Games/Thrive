-- TODO: merge the common things in microbe_stage_tutorial_hud
-- notification setting up
t1 = 0
t2 = 0
t3 = 0
b1 = false
b2 = false
b3 = false
--suicideButton setting up
boolean = false
boolean2 = false

-- Camera limits
CAMERA_MIN_HEIGHT = 20
CAMERA_MAX_HEIGHT = 120
CAMERA_VERTICAL_SPEED = 0.015

-- Updates the hud with relevant information

HudSystem = class(
   LuaSystem,
   function(self)
      
      LuaSystem.create(self)

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
)

-- This methods would get overriden by their duplicates below.
--[[
function HudSystem:init(gameState)
   LuaSystem.init(self, "HudSystem", gameState)
end

function HudSystem:update(renderTime, logicTime)
   -- TODO: use the QuickSaveSystem here? is this duplicated functionality?
   local saveDown = Engine.keyboard:isKeyDown(KEYCODE.KC_F4)
   local loadDown = Engine.keyboard:isKeyDown(KEYCODE.KC_F10)
   if saveDown and not self.saveDown then
      Engine:save("quick.sav")
   end
   if loadDown and not self.loadDown then
      Engine:load("quick.sav")
   end
   self.saveDown = saveDown
   self.loadDown = loadDown
end
]]

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
    self:updateLoadButton()

    self:chloroplastNotificationdisable()
    self:toxinNotificationdisable()
    self:editornotificationdisable()
end

function HudSystem:init(gameState)
    LuaSystem.init(self, "MicrobeStageHudSystem", gameState)
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
	local chloroplast_unlock_notification = self.rootGUIWindow:getChild("chloroplastUnlockNotification")
	local toxin_unlock_notification = self.rootGUIWindow:getChild("toxinUnlockNotification")
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
    local player = Entity.new("player", self.gameState.wrapper)
    local playerMicrobe = Microbe.new(player, nil, self.gameState)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    self.hitpointsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.maxHitpoints))

    self.atpBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("atp"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))))
    self.atpCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("atp"))))
    self.atpMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))))
	
	self.atpCountLabel2:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("atp"))))
	
	self.oxygenBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("oxygen"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxygen"))))
    self.oxygenCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("oxygen"))))
    self.oxygenMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxygen"))))
	
	self.aminoacidsBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("aminoacids"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("aminoacids"))))
    self.aminoacidsCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("aminoacids"))))
    self.aminoacidsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("aminoacids"))))
	
	self.ammoniaBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("ammonia"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("ammonia"))))
    self.ammoniaCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("ammonia"))))
    self.ammoniaMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("ammonia"))))
	
	self.glucoseBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("glucose"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("glucose"))))
    self.glucoseCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("glucose"))))
    self.glucoseMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("glucose"))))
	
	self.co2Bar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("co2"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("co2"))))
    self.co2CountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("co2"))))
    self.co2MaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("co2"))))
	
	self.fattyacidsBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("fattyacids"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids"))))
	
	self.oxytoxyBar:progressbarSetProgress(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("oxytoxy"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyCountLabel:setText("".. math.floor(MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))

    local playerSpecies = MicrobeSystem.getSpeciesComponent(player)
	--notification setting up
    if b1 == true and t1 < 300 then
        t1 = t1 + 2

        if t1 == 300 then
            global_activeMicrobeStageHudSystem:chloroplastNotificationdisable()
        end
    end

    if b2 == true and t2 < 300 then
        t2 = t2 + 2

        if t2 == 300 then
            global_activeMicrobeStageHudSystem:toxinNotificationdisable()
        end
    end

    if b3 == true and t3 < 300 then
        t3 = t3 + 2

        if t3 == 300 then
            global_activeMicrobeStageHudSystem:editornotificationdisable()
        end
    end
	--suicideButton setting up 
    local atp = MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("atp"))
    if atp == 0 and boolean2 == false then 
        self.rootGUIWindow:getChild("SuicideButton"):enable()
        elseif atp > 0 or boolean2 == true then
        global_activeMicrobeStageHudSystem:suicideButtondisable()
    end
    if boolean == true then
        MicrobeSystem.kill(player)
        boolean = false
        boolean2 = true
    end
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
    if (Engine.keyboard:wasKeyPressed(KEYCODE.KC_G)) then
        MicrobeSystem.toggleEngulfMode(player)
    end

    -- Changing the camera height according to the player input.
    local offset = getComponent(CAMERA_NAME, self.gameState, OgreCameraComponent).properties.offset
    
    if Engine.mouse:scrollChange() ~= 0 then
        self.scrollChange = self.scrollChange + Engine.mouse:scrollChange() * CAMERA_VERTICAL_SPEED
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
    
    if newZVal < CAMERA_MIN_HEIGHT then
        newZVal = CAMERA_MIN_HEIGHT 
        self.scrollChange = 0
    elseif newZVal > CAMERA_MAX_HEIGHT then
        newZVal = CAMERA_MAX_HEIGHT
        self.scrollChange = 0
    end
    
    offset.z = newZVal
end

function showReproductionDialog()
    assert(global_activeMicrobeStageHudSystem ~= nil,
           "no global active global_activeMicrobeStageHudSystem")
    assert(global_activeMicrobeStageHudSystem.showReproductionDialog)
    global_activeMicrobeStageHudSystem:showReproductionDialog()
end

function HudSystem:showReproductionDialog()
   -- print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
    --global_activeMicrobeStageHudSystem.rootGUIWindow:getChild("ReproductionPanel"):show()
	if b3 == false then
	getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("microbe-pickup-organelle")
self.rootGUIWindow:getChild("editornotification"):show()
b3 = true
end
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
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    Engine:save("quick.sav")
    print("Game Saved");
	--Because using update load button here doesn't seem to work unless you press save twice
    self.rootGUIWindow:getChild("PauseMenu"):getChild("LoadGameButton"):enable();
end
function HudSystem:loadButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    Engine:load("quick.sav")
    print("Game loaded");
    self.rootGUIWindow:getChild("PauseMenu"):hide()
    self.menuOpen = false
end

function HudSystem:menuButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    print("played sound")
    self.rootGUIWindow:getChild("PauseMenu"):show()
    self.rootGUIWindow:getChild("PauseMenu"):moveToFront()
    self:updateLoadButton();
    Engine:pauseGame()
    self.menuOpen = true
end

function HudSystem:resumeButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    print("played sound")
    self.rootGUIWindow:getChild("PauseMenu"):hide()
    self:updateLoadButton();
    Engine:resumeGame()
    self.menuOpen = false
end


function HudSystem:toggleCompoundPanel()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
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

function HudSystem:chloroplastNotificationenable()
getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("microbe-pickup-organelle")
self.rootGUIWindow:getChild("chloroplastUnlockNotification"):show()
b1 = true
self.rootGUIWindow:getChild("toxinUnlockNotification"):hide()
end

function HudSystem:chloroplastNotificationdisable()
self.rootGUIWindow:getChild("chloroplastUnlockNotification"):hide()
end

function HudSystem:toxinNotificationenable()
getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("microbe-pickup-organelle")
self.rootGUIWindow:getChild("toxinUnlockNotification"):show()
b2 = true
self.rootGUIWindow:getChild("chloroplastUnlockNotification"):hide()
end

function HudSystem:toxinNotificationdisable()
self.rootGUIWindow:getChild("toxinUnlockNotification"):hide()
end
function HudSystem:editornotificationdisable()
self.rootGUIWindow:getChild("editornotification"):hide()
end
function HudSystem:helpButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
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

function HudSystem:suicideButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
	if boolean2 == false then
boolean = true
end
		end
		function HudSystem:suicideButtondisable()
		self.rootGUIWindow:getChild("SuicideButton"):disable()
		end
		function HudSystem:suicideButtonreset()
		boolean2 = false
		end
function HudSystem:closeHelpButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
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
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    g_luaEngine:setCurrentGameState(GameState.MAIN_MENU)
end


function HudSystem:editorButtonClicked()
    local player = Entity.new("player", self.gameState.wrapper)
    local playerMicrobe = Microbe.new(player, nil, self.gameState)
    -- Return the first cell to its normal, non duplicated cell arangement.
    SpeciesSystem.restoreOrganelleLayout(playerMicrobe, MicrobeSystem.getSpeciesComponent(player)) 

    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    self.editorButton:disable()
	b3 = false
	t3 = 0
    g_luaEngine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

--[[
function HudSystem:returnButtonClicked()
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
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
    getComponent("gui_sounds", self.gameState, SoundSourceComponent):playSound("button-hover-click")
    Engine:quit()
end
