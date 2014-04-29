-- Updates the hud with relevant information
class 'MicrobeEditorHudSystem' (System)

function MicrobeEditorHudSystem:__init()
    System.__init(self)
    self.organelleButtons = {}
    self.initialized = false
    self.editor = MicrobeEditor(self)
    self.hoverHex = nil
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
    local removeButton = root:getChild("EditorTools"):getChild("RemoveItem")
    self.organelleButtons["Nucleus"] = nucleusButton
    self.organelleButtons["Flagelium"] = flageliumButton
    self.organelleButtons["Mitochondria"] = mitochondriaButton
    self.organelleButtons["Vacuole"] = vacuoleButton
    self.organelleButtons["Toxin"] = toxinButton
    self.organelleButtons["Remove"] = removeButton
    nucleusButton:getChild("Nucleus"):registerEventHandler("Clicked", nucleusClicked)
    flageliumButton:getChild("Flagelium"):registerEventHandler("Clicked", flageliumClicked)
    mitochondriaButton:getChild("Mitochondria"):registerEventHandler("Clicked", mitochondriaClicked)
    vacuoleButton:getChild("Vacuole"):registerEventHandler("Clicked", vacuoleClicked)
    toxinButton:getChild("Toxin"):registerEventHandler("Clicked", toxinClicked)
    removeButton:getChild("Remove"):registerEventHandler("Clicked", removeClicked)
 
    root:getChild("BottomSection"):getChild("MicrobeStageButton"):registerEventHandler("Clicked", playClicked)
    root:getChild("BottomSection"):getChild("MenuButton"):registerEventHandler("Clicked", menuButtonClicked)
     
    root:getChild("MenuPanel"):getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked)
    root:getChild("MenuPanel"):getChild("PlayButton"):registerEventHandler("Clicked", menuPlayClicked)
    root:getChild("MenuPanel"):getChild("ReturnButton"):registerEventHandler("Clicked", returnButtonClicked)
    root:getChild("MenuPanel"):getChild("QuitButton"):registerEventHandler("Clicked", quitButtonClicked)
end

function MicrobeEditorHudSystem:activate()
    global_activeMicrobeEditorHudSystem = self -- Global reference for event handlers
    self.editor:activate()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:disable()
        else
            button:enable()
        end
    end
    global_newEditorMicrobe = true
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
         toxinClicked()
         self.editor:performLocationAction()
     elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F) and self.editor.currentMicrobe ~= nil then
         flageliumClicked()
         self.editor:performLocationAction()
     elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_M) and self.editor.currentMicrobe ~= nil then
         mitochondriaClicked()
         self.editor:performLocationAction()
     elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
         menuButtonClicked()
     elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
         playClicked()
     end
end


-- Event handlers
function nucleusClicked()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:enable()
        end
    end
    global_activeMicrobeEditorHudSystem:setActiveAction("nucleus")
end

function flageliumClicked()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:enable()
        end
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Flagelium"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("flagelium")
end

function mitochondriaClicked()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:enable()
        end
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Mitochondria"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("mitochondria")
end

function vacuoleClicked()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:enable()
        end
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Vacuole"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("vacuole")
end

function toxinClicked()
    if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked("Toxin") then
        for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
            if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
                button:enable()
            end
        end
        global_activeMicrobeEditorHudSystem.organelleButtons["Toxin"]:disable()
        global_activeMicrobeEditorHudSystem:setActiveAction("toxin")
    end
end

function removeClicked()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        if not global_activeMicrobeEditorHudSystem.editor.lockedMap:isLocked(typeName) then
            button:enable()
        end
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Remove"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("remove")
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