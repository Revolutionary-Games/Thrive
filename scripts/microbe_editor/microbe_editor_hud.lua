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
end


function MicrobeEditorHudSystem:init(gameState)
    System.init(self, gameState)
    self.hoverHex = Entity("hover-hex")
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.transform.position = Vector3(0,0,110)
    sceneNode.transform:touch()
    sceneNode.meshName = "hex.mesh"
    self.hoverHex:addComponent(sceneNode)
    local root = gameState:rootGUIWindow()
    local nucleusButton = root:getChild("EditorTools"):getChild("NucleusItem")
    local flageliumButton = root:getChild("EditorTools"):getChild("FlageliumItem")
    local mitochondriaButton = root:getChild("EditorTools"):getChild("MitochondriaItem")
    local vacuoleButton = root:getChild("EditorTools"):getChild("VacuoleItem")
    local toxinButton = root:getChild("EditorTools"):getChild("ToxinItem")
    --local aminoSynthesizerButton = root:getChild("EditorTools"):getChild("AminoSynthesizerItem")
    local removeButton = root:getChild("EditorTools"):getChild("RemoveItem")
    self.organelleButtons["Nucleus"] = nucleusButton
    self.organelleButtons["Flagelium"] = flageliumButton
    self.organelleButtons["Mitochondria"] = mitochondriaButton
    self.organelleButtons["Vacuole"] = vacuoleButton
    self.organelleButtons["Toxin"] = toxinButton
    --self.organelleButtons["AminoSynthesizer"] = aminoSynthesizerButton
    self.organelleButtons["Remove"] = removeButton
    self.activeButton = nil
    nucleusButton:getChild("Nucleus"):registerEventHandler("Clicked", nucleusClicked)
    flageliumButton:getChild("Flagelium"):registerEventHandler("Clicked", flageliumClicked)
    mitochondriaButton:getChild("Mitochondria"):registerEventHandler("Clicked", mitochondriaClicked)
    vacuoleButton:getChild("Vacuole"):registerEventHandler("Clicked", vacuoleClicked)
    toxinButton:getChild("Toxin"):registerEventHandler("Clicked", toxinClicked)
    --aminoSynthesizerButton:getChild("AminoSynthesizer"):registerEventHandler("Clicked", aminoSynthesizerClicked)
    removeButton:getChild("Remove"):registerEventHandler("Clicked", removeClicked)
    
    self.saveLoadPanel = root:getChild("SaveLoadPanel")
    self.creationsListbox = self.saveLoadPanel:getChild("SavedCreations")
    
    root:getChild("BottomSection"):getChild("MicrobeStageButton"):registerEventHandler("Clicked", playClicked)
    root:getChild("BottomSection"):getChild("MenuButton"):registerEventHandler("Clicked", menuButtonClicked)
    root:getChild("MenuPanel"):getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked)
    root:getChild("MenuPanel"):getChild("PlayButton"):registerEventHandler("Clicked", menuPlayClicked)
    root:getChild("MenuPanel"):getChild("ReturnButton"):registerEventHandler("Clicked", returnButtonClicked)
    root:getChild("MenuPanel"):getChild("QuitButton"):registerEventHandler("Clicked", quitButtonClicked)
    root:getChild("BottomSection"):getChild("SaveButton"):registerEventHandler("Clicked", rootSaveCreationClicked)
    root:getChild("BottomSection"):getChild("LoadButton"):registerEventHandler("Clicked", rootLoadCreationClicked)
    root:getChild("SaveLoadPanel"):getChild("ReturnButton"):registerEventHandler("Clicked", returnButtonClicked)
    root:getChild("SaveLoadPanel"):getChild("SaveButton"):registerEventHandler("Clicked", saveCreationClicked)
    root:getChild("SaveLoadPanel"):getChild("LoadButton"):registerEventHandler("Clicked", loadCreationClicked)
end


function MicrobeEditorHudSystem:activate()
    global_activeMicrobeEditorHudSystem = self -- Global reference for event handlers
    self.editor:activate()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
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


function MicrobeEditorHudSystem:update(milliseconds)
    self.editor:update(milliseconds)
    -- Render the hex under the cursor
    local x, y = axialToCartesian(self.editor:getMouseHex())
    local translation = Vector3(-x, -y, 0)
    local sceneNode = self.hoverHex:getComponent(OgreSceneNodeComponent.TYPE_ID)
    sceneNode.transform.position = translation
    sceneNode.transform:touch()
    -- Handle input
    if Engine.mouse:wasButtonPressed(Mouse.MB_Left) then
        self.editor:performLocationAction()
    end
    if Engine.keyboard:wasKeyPressed(Keyboard.KC_C) then
        -- These global event handlers are defined in microbe_editor_hud.lua
        nucleusClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_R) then
        self.editor:setActiveAction("remove")
        self.editor:performLocationAction()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_S) and self.editor.currentMicrobe ~= nil then
        vacuoleClicked()
        self.editor:performLocationAction()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_T) and self.editor.currentMicrobe ~= nil then
        if not Engine:playerData():lockedMap():isLocked("Toxin") then
            toxinClicked()
            self.editor:performLocationAction()
        end
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F) and self.editor.currentMicrobe ~= nil then
        flageliumClicked()
        self.editor:performLocationAction()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_M) and self.editor.currentMicrobe ~= nil then
        mitochondriaClicked()  
        self.editor:performLocationAction()
    --elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_A) and self.editor.currentMicrobe ~= nil then
    --    aminoSynthesizerClicked()
    --    self.editor:performLocationAction()
    elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_P) and self.editor.currentMicrobe ~= nil then
       chloroplastClicked()
       self.editor:performLocationAction()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
        menuButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
        playClicked()
    end
end


-- Event handlers
function nucleusClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem:setActiveAction("nucleus")
end

function flageliumClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = global_activeMicrobeEditorHudSystem.organelleButtons["Flagelium"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("flagelium")
end

function mitochondriaClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = 
        global_activeMicrobeEditorHudSystem.organelleButtons["Mitochondria"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("mitochondria")
end

function chloroplastClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem:setActiveAction("chloroplast")
end
function aminoSynthesizerClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = 
        global_activeMicrobeEditorHudSystem.organelleButtons["AminoSynthesizer"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("aminosynthesizer")
end

function vacuoleClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = 
        global_activeMicrobeEditorHudSystem.organelleButtons["Vacuole"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("vacuole")
end

function toxinClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = 
    global_activeMicrobeEditorHudSystem.organelleButtons["Toxin"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("toxin")
end


function removeClicked()
    if global_activeMicrobeEditorHudSystem.activeButton ~= nil then
        global_activeMicrobeEditorHudSystem.activeButton:enable()
    end
    global_activeMicrobeEditorHudSystem.activeButton = 
        global_activeMicrobeEditorHudSystem.organelleButtons["Remove"]
    global_activeMicrobeEditorHudSystem.activeButton:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("remove")
end

function rootSaveCreationClicked()
    panel = global_activeMicrobeEditorHudSystem.saveLoadPanel
    panel:getChild("SaveButton"):show()
    panel:getChild("NameTextbox"):show()
    panel:getChild("CreationNameDialogLabel"):show()
    panel:getChild("LoadButton"):hide()
    panel:getChild("SavedCreations"):hide()
    panel:show()
end

function rootLoadCreationClicked()
    panel = global_activeMicrobeEditorHudSystem.saveLoadPanel
    panel:getChild("SaveButton"):hide()
    panel:getChild("NameTextbox"):hide()
    panel:getChild("CreationNameDialogLabel"):hide()
    panel:getChild("LoadButton"):show()
    panel:getChild("SavedCreations"):show()
    panel:show()
    global_activeMicrobeEditorHudSystem.creationsListbox:itemListboxResetList()
    global_activeMicrobeEditorHudSystem.creationFileMap = {}
    i = 0
    pathsString = Engine:getCreationFileList("microbe")
    -- using pattern matching for splitting on spaces
    for path in string.gmatch(pathsString, "%S+")  do 
       item = CEGUIWindow("Thrive/ListboxItem", "creationItems"..i)
       pathSep = package.config:sub(1,1) -- / for unix, \ for windows
       text = string.sub(path, string.len(path) - string.find(path:reverse(), pathSep) + 2)
       item:setText(text)
       global_activeMicrobeEditorHudSystem.creationsListbox:itemListboxAddItem(item)
       global_activeMicrobeEditorHudSystem.creationFileMap[text] = path
       i = i + 1
    end
    global_activeMicrobeEditorHudSystem.creationsListbox:itemListboxHandleUpdatedItemData()
end

function saveCreationClicked()
    name = panel:getChild("NameTextbox"):getText()
    -- Todo: Additional input sanitation
    if string.len(name) > 0 then
        Engine:saveCreation(global_activeMicrobeEditorHudSystem.editor.currentMicrobe.entity.id, name, "microbe") 
        panel:hide()
    end
end

function loadCreationClicked()
    item = global_activeMicrobeEditorHudSystem.creationsListbox:itemListboxGetLastSelectedItem()
    if not item:isNull() then 
        entity = Engine:loadCreation(global_activeMicrobeEditorHudSystem.creationFileMap[item:getText()])
        global_activeMicrobeEditorHudSystem.editor:loadMicrobe(entity)
        panel:hide()
    end
end

function playClicked()
    Engine:setCurrentGameState(GameState.MICROBE)
end

function menuPlayClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    playClicked()
end

function menuMainMenuClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    Engine:setCurrentGameState(GameState.MAIN_MENU)
end