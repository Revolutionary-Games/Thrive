--
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
--hints setting up
hintsPanelOpned = false
healthHint = false
atpHint = false
glucoseHint = false
ammoniaHint = false
oxygenHint = false
toxinHint = false
chloroplastHint = false
activeHints = {}
hintN = 0
currentHint = 1
HHO = false
AHO = false
GHO = false
AMHO = false
OHO = false
THO = false
CHO = false
glucoseNeeded = 0
atpNeeded = 0
ammoniaNeeded = 0
oxygenNeeded = 0
chloroplastNeeded = 0
toxinNeeded = 0

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
	local nextHint = self.rootGUIWindow:getChild("HintsPanel"):getChild("NextHint")
	local lastHint = self.rootGUIWindow:getChild("HintsPanel"):getChild("LastHint")
    --local collapseButton = self.rootGUIWindow:getChild() collapseButtonClicked
	local hintsButton = self.rootGUIWindow:getChild("HintsButton")
    local helpButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpButton")
    local helpPanel = self.rootGUIWindow:getChild("PauseMenu"):getChild("HelpPanel")
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
    local suicideButton = self.rootGUIWindow:getChild("SuicideButton")
    --local returnButton = self.rootGUIWindow:getChild("MenuButton")
    local compoundButton = self.rootGUIWindow:getChild("CompoundExpandButton")
    --local compoundPanel = self.rootGUIWindow:getChild("CompoundsOpen")
    local quitButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("QuitButton")
	nextHint:registerEventHandler("Clicked", function() self:nextHintButtonClicked() end)
	lastHint:registerEventHandler("Clicked", function() self:lastHintButtonClicked() end)
	hintsButton:registerEventHandler("Clicked", function() self:hintsButtonClicked() end)
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
	
	self.fattyacidsBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("fattyacids"))))
    self.fattyacidsMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("fattyacids"))))
	
	self.oxytoxyBar:progressbarSetProgress(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxytoxy"))/(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyCountLabel:setText("".. math.floor(playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxytoxy"))))
    self.oxytoxyMaxLabel:setText("/ ".. math.floor(playerMicrobe.microbe.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("oxytoxy"))))

    local playerSpecies = playerMicrobe:getSpeciesComponent()
	--notification setting up
        if b1 == true and t1 < 300 then
        t1 = t1 + 2
if hintsPanelOpned == true then
			self:hintsButtonClicked()
			end        
        if t1 == 300 then
            global_activeMicrobeStageHudSystem:chloroplastNotificationdisable()
			self:hintsButtonClicked()
        end
    end

    if b2 == true and t2 < 300 then
        t2 = t2 + 2
if hintsPanelOpned == true then
			self:hintsButtonClicked()
			end        
        if t2 == 300 then
            global_activeMicrobeStageHudSystem:toxinNotificationdisable()
			self:hintsButtonClicked()
        end
    end

    if b3 == true and t3 < 300 then
        t3 = t3 + 2
if hintsPanelOpned == true then
			self:hintsButtonClicked()
			end        
        if t3 == 300 then
            global_activeMicrobeStageHudSystem:editornotificationdisable()
        end
    end
	--suicideButton setting up 
local atp = playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp"))
if atp == 0 and boolean2 == false then 
	self.rootGUIWindow:getChild("SuicideButton"):enable()
	elseif atp > 0 or boolean2 == true then
	global_activeMicrobeStageHudSystem:suicideButtondisable()
end
if boolean == true then
playerMicrobe:kill()
boolean = false
boolean2 = true
end
    --Hints setup
	local glucose = playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("glucose"))
	local ammonia = playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("ammonia"))
	local oxygen = playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("oxygen"))
	atpNeeded = math.floor (30 - atp)
	glucoseNeeded = math.floor (16 - glucose)
	ammoniaNeeded = math.floor (12 - ammonia)
	oxygenNeeded = math.floor (15 - oxygen)
	chloroplastNeeded = math.floor (3 - chloroplast_Organelle_Number)
	toxinNeeded = math.floor (3 - toxin_Organelle_Number)
if playerMicrobe.microbe.hitpoints < playerMicrobe.microbe.maxHitpoints and healthHint == false and HHO == false then
activeHints["healthHint"] = hintN + 1
hintN = activeHints["healthHint"]
HHO = true
elseif playerMicrobe.microbe.hitpoints == playerMicrobe.microbe.maxHitpoints and healthHint == true and HHO == true then
activeHints["healthHint"] = nil
hintN = hintN - 1
healthHint = false
HHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end

if atp < 15 and atpHint == false and AHO == false then 
activeHints["atpHint"] = hintN + 1
hintN = activeHints["atpHint"]
AHO = true
elseif atp > 30 and atpHint == true and AHO == true then
activeHints["atpHint"] = nil
hintN = hintN - 1
atpHint = false
AHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end

if glucose < 1 and glucoseHint == false and GHO == false then 
activeHints["glucoseHint"] = hintN + 1
hintN = activeHints["glucoseHint"]
GHO = true
elseif glucose >= 16 and glucoseHint == true and GHO == true then
activeHints["glucoseHint"] = nil
hintN = hintN - 1
glucoseHint = false
GHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end
if ammonia < 1 and ammoniaHint == false and AMHO == false then 
activeHints["ammoniaHint"] = hintN + 1
hintN = activeHints["ammoniaHint"]
AMHO = true
elseif ammonia >= 12 and ammoniaHint == true and AMHO == true then
activeHints["ammoniaHint"] = nil
hintN = hintN - 1
ammoniaHint = false
AMHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end

if oxygen < 1 and oxygenHint == false and OHO == false then 
activeHints["oxygenHint"] = hintN + 1
hintN = activeHints["oxygenHint"]
OHO = true
elseif oxygen >= 12 and oxygenHint == true and OHO == true then
activeHints["oxygenHint"] = nil
hintN = hintN - 1
oxygenHint = false
OHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end

if toxin_Organelle_Number < 3 and toxinHint == false and THO == false then 
activeHints["toxinHint"] = hintN + 1
hintN = activeHints["toxinHint"]
THO = true
elseif toxin_Organelle_Number >= 3 and toxinHint == true and THO == true then
activeHints["toxinHint"] = nil
hintN = hintN - 1
toxinHint = false
THO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end

if chloroplast_Organelle_Number < 3 and chloroplastHint == false and CHO == false then 
activeHints["chloroplastHint"] = hintN + 1
hintN = activeHints["chloroplastHint"]
CHO = true
elseif chloroplast_Organelle_Number >= 3 and chloroplastHint == true and CHO == true then
activeHints["chloroplastHint"] = nil
hintN = hintN - 1
chloroplastHint = false
CHO = false
if next(activeHints) ~= nil then
currentHint = currentHint + 1
end
end
--print (toxin_Organelle_Number .. " " .. chloroplast_Organelle_Number)
if healthHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("Your cell is damaged! Collect ammonia and glucose to make amino acids, which can heal it.")
end

if atpHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("You're running short of ATP! ATP is used to move and engulf. Get " .. atpNeeded .. " to be safe!")
end

if glucoseHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("You need more glucose! It's used to make ATP and amino acids. Collect " .. glucoseNeeded .. " to be safe.")
end

if ammoniaHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("You have little ammonia, used to make amino acids to heal and reproduce. Get " .. ammoniaNeeded .. " more.")
end

if oxygenHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("You need oxygen to produce ATP and OxyToxy. Collect " .. oxygenNeeded .. " oxygen to do this!")
end

if chloroplastHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("Pick " .. chloroplastNeeded .. " green blobs to unlock Chloroplasts, which transform CO2 into glucose and oxygen.")
end

if toxinHint == true then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("Collect " .. toxinNeeded .. " blue blobs to unlock Toxin Vacuoles, used to shoot harmful agents at other cells.")
end

for hintnam,hintnum in pairs(activeHints) do
if hintnum == currentHint then
if hintnam == "atpHint" then
atpHint = true
else
atpHint = false
end
if hintnam == "healthHint" then
healthHint = true
else
healthHint = false
end
if hintnam == "glucoseHint" then
glucoseHint = true
else
glucoseHint = false
end
if hintnam == "ammoniaHint" then
ammoniaHint = true
else
ammoniaHint = false
end
if hintnam == "oxygenHint" then
oxygenHint = true
else
oxygenHint = false
end
if hintnam == "chloroplastHint" then
chloroplastHint = true
else
chloroplastHint = false
end
if hintnam == "toxinHint" then
toxinHint = true
else
toxinHint = false
end
end
end
if next(activeHints) == nil then
self.rootGUIWindow:getChild("HintsPanel"):getChild("HelpText"):setText("there is no available hints for now!")
end

if currentHint > hintN then
currentHint = 1
end

if currentHint < 1 then
currentHint = hintN
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
        playerMicrobe:toggleEngulfMode()
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

function HudSystem:hintsButtonClicked()
if hintsPanelOpned == false then
self.rootGUIWindow:getChild("HintsPanel"):show()
self.rootGUIWindow:getChild("HintsButton"):setText("")
self.rootGUIWindow:getChild("HintsButton"):getChild("HintsIcon"):hide()
self.rootGUIWindow:getChild("HintsButton"):getChild("HintsContractIcon"):show()
self.rootGUIWindow:getChild("HintsButton"):setProperty("Hide the hints panel", "TooltipText")
hintsPanelOpned = true
 elseif hintsPanelOpned == true then
self.rootGUIWindow:getChild("HintsPanel"):hide()
self.rootGUIWindow:getChild("HintsButton"):setText("Hints")
self.rootGUIWindow:getChild("HintsButton"):getChild("HintsIcon"):show()
self.rootGUIWindow:getChild("HintsButton"):getChild("HintsContractIcon"):hide()
self.rootGUIWindow:getChild("HintsButton"):setProperty("Open the hints panel", "TooltipText")
hintsPanelOpned = false
end
end
function HudSystem:nextHintButtonClicked()
currentHint = currentHint + 1
end
function HudSystem:lastHintButtonClicked()
currentHint = currentHint - 1
end
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
    SpeciesSystem.restoreOrganelleLayout(playerMicrobe, playerMicrobe:getSpeciesComponent()) 

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
