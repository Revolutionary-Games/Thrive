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
    if self.hoverHex == nil then
        self.hoverHex = Entity("hover-hex")
        local sceneNode = OgreSceneNodeComponent()
        self.hoverHex:setVolatile(true)
        sceneNode.transform.position = Vector3(0,0,110)
        sceneNode.transform:touch()
        sceneNode.meshName = "hex.mesh"
        self.hoverHex:addComponent(sceneNode)
    end
end



function MicrobeEditorHudSystem:activate()
    -- Try and move this to init(gamestate)
    if not self.initialized then
        local root = CEGUIWindow.getRootWindow():getChild("MicrobeEditorRoot")
        local Nucleus = root:getChild("EditorTools"):getChild("Nucleus")
        local Flagelium = root:getChild("EditorTools"):getChild("Flagelium")
        local Mitochondria = root:getChild("EditorTools"):getChild("Mitochondria")
        local Vacuole = root:getChild("EditorTools"):getChild("Vacuole")
        local Remove = root:getChild("EditorTools"):getChild("Remove")
        self.organelleButtons["Nucleus"] = Nucleus
        self.organelleButtons["Flagelium"] = Flagelium
        self.organelleButtons["Mitochondria"] = Mitochondria
        self.organelleButtons["Vacuole"] = Vacuole
        self.organelleButtons["Remove"] = Remove
        Nucleus:registerEventHandler("Clicked", nucleusClicked)
        Flagelium:registerEventHandler("Clicked", flageliumClicked)
        Mitochondria:registerEventHandler("Clicked", mitochondriaClicked)
        Vacuole:registerEventHandler("Clicked", vacuoleClicked)
        Remove:registerEventHandler("Clicked", removeClicked)
        root:getChild("BottomSection"):getChild("MicrobeStageButton"):registerEventHandler("Clicked", playClicked)
    end
    activeMicrobeEditorHudSystem = self
end

function MicrobeEditorHudSystem:setActiveAction(actionName)
    self.editor:setActiveAction(actionName)
    if actionName == "nucleus" then
        -- For now we simply create a new microbe with the nucleus button
        self.editor:performLocationAction()
    end
end

function MicrobeEditorHudSystem:setNewPlayerMicrobe()
    newPlayerAvaliable = self.editor
end

function MicrobeEditorHudSystem:update(milliseconds)
    local x, y = axialToCartesian(self.editor:getMouseHex())
    local translation = Vector3(-x, -y, 0)
    local sceneNode = Entity("hover-hex"):getComponent(OgreSceneNodeComponent.TYPE_ID)
    sceneNode.transform.position = translation
    sceneNode.transform:touch()
    
    if self.editor ~= nil then
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
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
            playClicked()
        end
            
    end    
end

-- Event handlers
function nucleusClicked()
    for _,button in pairs(activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    activeMicrobeEditorHudSystem:setActiveAction("nucleus")
end
function flageliumClicked()
    for _,button in pairs(activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    activeMicrobeEditorHudSystem.organelleButtons["Flagelium"]:disable()
    activeMicrobeEditorHudSystem:setActiveAction("flagelium")
end
function mitochondriaClicked()
    for _,button in pairs(activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    activeMicrobeEditorHudSystem.organelleButtons["Mitochondria"]:disable()
    activeMicrobeEditorHudSystem:setActiveAction("mitochondria")
end
function vacuoleClicked()
    for _,button in pairs(activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    activeMicrobeEditorHudSystem.organelleButtons["Vacuole"]:disable()
    activeMicrobeEditorHudSystem:setActiveAction("vacuole")
end
function removeClicked()
    for _,button in pairs(activeMicrobeEditorHudSystem.organelleButtons) do
        button:enable()
    end
    activeMicrobeEditorHudSystem.organelleButtons["Remove"]:disable()
    activeMicrobeEditorHudSystem:setActiveAction("remove")
end
function playClicked()
    activeMicrobeEditorHudSystem:setNewPlayerMicrobe()
    Engine:setCurrentGameState(GameState.MICROBE)
end