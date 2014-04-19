class 'MicrobeEditorInputSystem' (System)

function MicrobeEditorInputSystem:__init()
    System.__init(self)
    
    self.editor = MicrobeEditor()
end

function MicrobeEditorInputSystem:init(gameState)
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


function MicrobeEditorInputSystem:update(milliseconds)
    local x, y = axialToCartesian(self.editor:getMouseHex())
    local translation = Vector3(-x, -y, 0)
    local sceneNode = Entity("hover-hex"):getComponent(OgreSceneNodeComponent.TYPE_ID)
    sceneNode.transform.position = translation
    sceneNode.transform:touch()
    
    if self.editor ~= nil then
        if Engine.keyboard:wasKeyPressed(Keyboard.KC_C) then
            self.editor:createNewMicrobe()
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_R) then
            self.editor:removeOrganelle()
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_S) then
            self.editor:addStorageOrganelle()
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F) then
            self.editor:addMovementOrganelle(0, 50)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_B) then
            self.editor:addMovementOrganelle(0, -50)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_M) then
            self.editor:addProcessOrganelle("mitochondria")
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
            Engine:setCurrentGameState(GameState.MICROBE)
            newPlayerAvaliable = self.editor
        end
            
    end
    
    
    
end


