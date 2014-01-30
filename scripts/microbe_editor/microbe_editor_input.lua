class 'MicrobeEditorInputSystem' (System)

function MicrobeEditorInputSystem:__init()
    System.__init(self)
    
    self.editor = nil
end

function MicrobeEditorInputSystem:setEditor(microbeEditor)
    self.editor = microbeEditor
end

function MicrobeEditorInputSystem:update(milliseconds)
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
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_MINUS) then
            self.editor:setNextMinus()
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_0) then
            self.editor:setHexCoordinate(0)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_1) then
            self.editor:setHexCoordinate(1)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_2) then
            self.editor:setHexCoordinate(2)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_3) then
            self.editor:setHexCoordinate(3)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_4) then
            self.editor:setHexCoordinate(4)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_5) then
            self.editor:setHexCoordinate(5)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_6) then
            self.editor:setHexCoordinate(6)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_7) then
            self.editor:setHexCoordinate(7)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_8) then
            self.editor:setHexCoordinate(8)
        elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_9) then
            self.editor:setHexCoordinate(9)
        end
            
    end
end


