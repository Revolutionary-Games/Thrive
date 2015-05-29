-- Updates the hud with relevant information
class 'MicrobeEditorHudSystem' (System)

function MicrobeEditorHudSystem:__init()
    System.__init(self)
    self.organelleButtons = {}
    self.initialized = false
    self.editor = MicrobeEditor(self)
    self.hoverHex = nil
    self.saveLoadPanel = nil
    self.creationsListbox = nil
    self.creationFileMap = {} -- Map from player creation name to filepath
    self.activeButton = nil -- stores button, not name
    self.helpPanelOpen = false
    self.organelleScrollPane = nil
end


function MicrobeEditorHudSystem:init(gameState)
    System.init(self, gameState)
    self.editor:init(gameState)
    self.hoverHex = Entity("hover-hex")
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.transform.position = Vector3(0,0,110)
    sceneNode.transform:touch()
    sceneNode.meshName = "hex.mesh"
    self.hoverHex:addComponent(sceneNode)
    local root = gameState:rootGUIWindow()
    self.mpLabel = root:getChild("MpPanel"):getChild("MpLabel")
    self.nameLabel = root:getChild("SpeciesNamePanel"):getChild("SpeciesNameLabel")
    self.nameTextbox = root:getChild("SpeciesNamePanel"):getChild("NameTextbox")
    root:getChild("SpeciesNamePanel"):registerEventHandler("Clicked", 
        function() global_activeMicrobeEditorHudSystem:nameClicked() end)
    -- self.mpProgressBar = root:getChild("BottomSection"):getChild("MutationPoints"):getChild("MPBar")
    self.organelleScrollPane = root:getChild("scrollablepane");
    local nucleusButton = root:getChild("NewMicrobe")
    local flagellumButton = root:getChild("scrollablepane"):getChild("AddFlagellum")
    local mitochondriaButton = root:getChild("scrollablepane"):getChild("AddMitochondria")
    local vacuoleButton = root:getChild("scrollablepane"):getChild("AddVacuole")
    local toxinButton = root:getChild("scrollablepane"):getChild("AddToxinVacuole")
    local chloroplastButton = root:getChild("scrollablepane"):getChild("AddChloroplast")
    self.organelleButtons["nucleus"] = nucleusButton
    self.organelleButtons["flagellum"] = flagellumButton
    self.organelleButtons["mitochondrion"] = mitochondriaButton
    self.organelleButtons["chloroplast"] = chloroplastButton
    self.organelleButtons["vacuole"] = vacuoleButton
    self.organelleButtons["Toxin"] = toxinButton
    self.activeButton = nil
    nucleusButton:registerEventHandler("Clicked", function() self:nucleusClicked() end)
    flagellumButton:registerEventHandler("Clicked", function() self:flagellumClicked() end)
    mitochondriaButton:registerEventHandler("Clicked", function() self:mitochondriaClicked() end)
    chloroplastButton:registerEventHandler("Clicked", function() self:chloroplastClicked() end)
    vacuoleButton:registerEventHandler("Clicked", function() self:vacuoleClicked() end)
    toxinButton:registerEventHandler("Clicked", function() self:toxinClicked() end)
    
    -- self.saveLoadPanel = root:getChild("SaveLoadPanel")
    -- self.creationsListbox = self.saveLoadPanel:getChild("SavedCreations")
    self.undoButton = root:getChild("UndoButton")
    self.undoButton:registerEventHandler("Clicked", function() self.editor:undo() end)
    self.redoButton = root:getChild("RedoButton")
    self.redoButton:registerEventHandler("Clicked", function() self.editor:redo() end)

    root:getChild("FinishButton"):registerEventHandler("Clicked", playClicked)
    --root:getChild("BottomSection"):getChild("MenuButton"):registerEventHandler("Clicked", self:menuButtonClicked)
    root:getChild("MenuButton"):registerEventHandler("Clicked", menuMainMenuClicked)
    --root:getChild("MenuPanel"):getChild("QuitButton"):registerEventHandler("Clicked", self:quitButtonClicked)
    root:getChild("SaveMicrobeButton"):registerEventHandler("Clicked", function() self:saveCreationClicked() end)
    --root:getChild("LoadMicrobeButton"):registerEventHandler("Clicked", function() self:loadCreationClicked() end)

    self.helpPanel = root:getChild("HelpPanel")
    root:getChild("HelpButton"):registerEventHandler("Clicked", function() self:helpButtonClicked() end)
end


function MicrobeEditorHudSystem:activate()
    global_activeMicrobeEditorHudSystem = self -- Global reference for event handlers
    self.editor:activate()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        print(typeName)
        if Engine:playerData():lockedMap():isLocked(typeName) then
            button:disable()
        else
            button:enable()
        end
    end    
end

function MicrobeEditorHudSystem:setActiveAction(actionName)
    self.editor:setActiveAction(actionName)
    if actionName == "nucleus" then
        -- For now we simply create a new microbe with the nucleus button
        self.editor:performLocationAction()
    end
end


function MicrobeEditorHudSystem:update(renderTime, logicTime)
    self.editor:update(renderTime, logicTime)
    -- Render the hex under the cursor
    local sceneNode = self.hoverHex:getComponent(OgreSceneNodeComponent.TYPE_ID)
    if CEGUIWindow.getWindowUnderMouse():getName() == 'root' then
        local x, y = axialToCartesian(self.editor:getMouseHex())
        local translation = Vector3(-x, -y, 0)
        
        sceneNode.transform.position = translation
    else
        sceneNode.transform.position = Vector3(0,0,100)
    end
    sceneNode.transform:touch()
    
    -- Handle input
    if Engine.mouse:wasButtonPressed(Mouse.MB_Left) then
        self.editor:performLocationAction()
    end
    if Engine.mouse:wasButtonPressed(Mouse.MB_Right) then
        self:removeClicked()
        self.editor:performLocationAction()
    end	            
    if keyCombo(kmp.newmicrobe) then
        -- These global event handlers are defined in microbe_editor_hud.lua
        self:nucleusClicked()
    elseif keyCombo(kmp.redo) then
        self.editor:redo()
    elseif keyCombo(kmp.remove) then
        self:removeClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.undo) then
        self.editor:undo()
    elseif keyCombo(kmp.vacuole) then
        self:vacuoleClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.oxytoxyvacuole) then
        if not Engine:playerData():lockedMap():isLocked("Toxin") then
            self:toxinClicked()
            self.editor:performLocationAction()
        end
    elseif keyCombo(kmp.flagellum) then
        self:flagellumClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.mitochondrion) then
        self:mitochondriaClicked()  
        self.editor:performLocationAction()
    --elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_A) and self.editor.currentMicrobe ~= nil then
    --    self:aminoSynthesizerClicked()
    --    self.editor:performLocationAction()
    elseif keyCombo(kmp.chloroplast) then
        if not Engine:playerData():lockedMap():isLocked("Chloroplast") then
            self:chloroplastClicked()
            self.editor:performLocationAction()
        end
    elseif keyCombo(kmp.togglegrid) then
        if self.editor.gridVisible then
            self.editor.gridSceneNode.visible = false;
            self.editor.gridVisible = false
        else
            self.editor.gridSceneNode.visible = true;
            self.editor.gridVisible = true
        end
    elseif keyCombo(kmp.gotostage) then
        playClicked()
    elseif keyCombo(kmp.rename) then
        self:updateMicrobeName()
    end
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    end

    if Engine.keyboard:isKeyDown(Keyboard.KC_LSHIFT) then 
        properties = Entity(CAMERA_NAME .. 3):getComponent(OgreCameraComponent.TYPE_ID).properties
        newFovY = properties.fovY + Degree(Engine.mouse:scrollChange()/10)
        if newFovY < Degree(10) then
            newFovY = Degree(10)
        elseif newFovY > Degree(120) then
            newFovY = Degree(120)
        end
        properties.fovY = newFovY
        properties:touch()
    else
        local organelleScrollVal = self.organelleScrollPane:scrollingpaneGetVerticalPosition() + Engine.mouse:scrollChange()/1000
        if organelleScrollVal < 0 then
            organelleScrollVal = 0
        elseif organelleScrollVal > 1.0 then
            organelleScrollVal = 1.0
        end
        self.organelleScrollPane:scrollingpaneSetVerticalPosition(organelleScrollVal)
        
    end
end

function MicrobeEditorHudSystem:updateMutationPoints() 
    --self.mpProgressBar:progressbarSetProgress(self.editor.mutationPoints/100)
    self.mpLabel:setText("" .. self.editor.mutationPoints)
end

-----------------------------------------------------------------
-- Event handlers -----------------------------------------------

function playClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE)
end

function menuPlayClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    playClicked()
end

function menuMainMenuClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MAIN_MENU)
end

-- the rest of the event handlers are MicrobeEditorHudSystem methods

function MicrobeEditorHudSystem:nameClicked()
    self.nameLabel:hide()
    self.nameTextbox:show()
    self.nameTextbox:setFocus()
end

function MicrobeEditorHudSystem:updateMicrobeName()
    self.editor.currentMicrobe.microbe.speciesName = self.nameTextbox:getText()
    self.nameLabel:setText(self.editor.currentMicrobe.microbe.speciesName)
    self.nameTextbox:hide()
    self.nameLabel:show()
end

function MicrobeEditorHudSystem:helpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    if self.helpPanelOpen then
        self.helpPanel:hide()
    else
        self.helpPanel:show()
    end
    self.helpPanelOpen = not self.helpPanelOpen
end

function MicrobeEditorHudSystem:nucleusClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self:setActiveAction("nucleus")
end

function MicrobeEditorHudSystem:flagellumClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["flagellum"]
    self.activeButton:disable()
    self:setActiveAction("flagellum")
end

function MicrobeEditorHudSystem:mitochondriaClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["mitochondrion"]
    self.activeButton:disable()
    self:setActiveAction("mitochondrion")
end

function MicrobeEditorHudSystem:chloroplastClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["chloroplast"]
    self.activeButton:disable()
    self:setActiveAction("chloroplast")
end

function MicrobeEditorHudSystem:aminoSynthesizerClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["aminosynthesizer"]
    self.activeButton:disable()
    self:setActiveAction("aminosynthesizer")
end

function MicrobeEditorHudSystem:vacuoleClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["vacuole"]
    self.activeButton:disable()
    self:setActiveAction("vacuole")
end

function MicrobeEditorHudSystem:toxinClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["Toxin"]
    self.activeButton:disable()
    self:setActiveAction("oxytoxy")
end


function MicrobeEditorHudSystem:removeClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = nil
    self:setActiveAction("remove")
end

function MicrobeEditorHudSystem:rootSaveCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print ("Save button clicked")
    --[[
    panel = self.saveLoadPanel
    panel:getChild("SaveButton"):show()
    panel:getChild("NameTextbox"):show()
    panel:getChild("CreationNameDialogLabel"):show()
    panel:getChild("LoadButton"):hide()
    panel:getChild("SavedCreations"):hide()
    panel:show()--]]
end

function MicrobeEditorHudSystem:rootLoadCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    panel = self.saveLoadPanel
    panel:getChild("SaveButton"):hide()
    panel:getChild("NameTextbox"):hide()
    panel:getChild("CreationNameDialogLabel"):hide()
    panel:getChild("LoadButton"):show()
    panel:getChild("SavedCreations"):show()
    panel:show()
    self.creationsListbox:itemListboxResetList()
    self.creationFileMap = {}
    i = 0
    pathsString = Engine:getCreationFileList("microbe")
    -- using pattern matching for splitting on spaces
    for path in string.gmatch(pathsString, "%S+")  do
        -- this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
        item = CEGUIWindow("Thrive/ListboxItem", "creationItems"..i)
        pathSep = package.config:sub(1,1) -- / for unix, \ for windows
        text = string.sub(path, string.len(path) - string.find(path:reverse(), pathSep) + 2)
        item:setText(text)
        self.creationsListbox:itemListboxAddItem(item)
        self.creationFileMap[text] = path
        i = i + 1
    end
    self.creationsListbox:itemListboxHandleUpdatedItemData()
end

function MicrobeEditorHudSystem:saveCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    name = self.editor.currentMicrobe.microbe.speciesName
    print("saving "..name)
    -- Todo: Additional input sanitation
    name, _ = string.gsub(name, "%s+", "_") -- replace whitespace with underscore
    if string.match(name, "^[%w_]+$") == nil then
        print("unsanitary name: "..name) -- should we do the test before whitespace sanitization?
    elseif string.len(name) > 0 then
        Engine:saveCreation(self.editor.currentMicrobe.entity.id, name, "microbe")
    end
end

function MicrobeEditorHudSystem:loadCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    item = self.creationsListbox:itemListboxGetLastSelectedItem()
    if not item:isNull() then 
        entity = Engine:loadCreation(self.creationFileMap[item:getText()])
        self.editor:loadMicrobe(entity)
        panel:hide()
    end
end

-- useful debug functions

function MicrobeEditorHudSystem:loadByName(name)
    if string.find(name, ".microbe") then 
        print("note, you don't need to add the .microbe extension") 
    else 
        name = name..".microbe"
    end
    name, _ = string.gsub(name, "%s+", "_")
    creationFileMap = {}
    i = 0
    pathsString = Engine:getCreationFileList("microbe")
    -- using pattern matching for splitting on spaces
    for path in string.gmatch(pathsString, "%S+")  do
        -- this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
        pathSep = package.config:sub(1,1) -- / for unix, \ for windows
        text = string.sub(path, string.len(path) - string.find(path:reverse(), pathSep) + 2)
        creationFileMap[text] = path
        i = i + 1
    end
    entity = Engine:loadCreation(creationFileMap[name])
    self.editor:loadMicrobe(entity)
    self.nameLabel:setText(self.editor.currentMicrobe.microbe.speciesName)
end

function saveMicrobe() global_activeMicrobeEditorHudSystem:saveCreationClicked() end
function loadMicrobe(name) global_activeMicrobeEditorHudSystem:loadByName(name) end
