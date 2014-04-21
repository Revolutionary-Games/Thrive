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
    self.editor:init()
    self.hoverHex = Entity("hover-hex")
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.transform.position = Vector3(0,0,110)
    sceneNode.transform:touch()
    sceneNode.meshName = "hex.mesh"
    self.hoverHex:addComponent(sceneNode)
    local root = gameState:rootGUIWindow()
    local nucleusButton = root:getChild("EditorTools"):getChild("Nucleus")
    local flageliumButton = root:getChild("EditorTools"):getChild("Flagelium")
    local mitochondriaButton = root:getChild("EditorTools"):getChild("Mitochondria")
    local vacuoleButton = root:getChild("EditorTools"):getChild("Vacuole")
    local removeButton = root:getChild("EditorTools"):getChild("Remove")
    self.organelleButtons["Nucleus"] = nucleusButton
    self.organelleButtons["Flagelium"] = flageliumButton
    self.organelleButtons["Mitochondria"] = mitochondriaButton
    self.organelleButtons["Vacuole"] = vacuoleButton
    self.organelleButtons["Remove"] = removeButton
    nucleusButton:registerEventHandler("Clicked", nucleusClicked)
    flageliumButton:registerEventHandler("Clicked", flageliumClicked)
    mitochondriaButton:registerEventHandler("Clicked", mitochondriaClicked)
    vacuoleButton:registerEventHandler("Clicked", vacuoleClicked)
    removeButton:registerEventHandler("Clicked", removeClicked)
 
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
end

function MicrobeEditorHudSystem:setActiveAction(actionName)
    self.editor:setActiveAction(actionName)
    if actionName == "nucleus" then
        -- For now we simply create a new microbe with the nucleus button
        self.editor:performLocationAction()
    end
end


function MicrobeEditorHudSystem:setNewPlayerMicrobe()
    global_transferMicrobe = self.editor.currentMicrobe
end


function MicrobeEditorHudSystem:update(milliseconds)
     -- Render the hex under the cursor
     local x, y = axialToCartesian(self.editor:getMouseHex())
     local translation = Vector3(-x, -y, 0)
     local sceneNode = self.hoverHex:getComponent(OgreSceneNodeComponent.TYPE_ID)
     if sceneNode == nil then
     print("sceneNode niul")
     end
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
    for _,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    global_activeMicrobeEditorHudSystem:setActiveAction("nucleus")
end

function flageliumClicked()
    for _,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Flagelium"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("flagelium")
end

function mitochondriaClicked()
    for _,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Mitochondria"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("mitochondria")
end

function vacuoleClicked()
    for _,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Vacuole"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("vacuole")
end

function removeClicked()
    for _,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    global_activeMicrobeEditorHudSystem.organelleButtons["Remove"]:disable()
    global_activeMicrobeEditorHudSystem:setActiveAction("remove")
end

function playClicked()
    global_activeMicrobeEditorHudSystem:setNewPlayerMicrobe()
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