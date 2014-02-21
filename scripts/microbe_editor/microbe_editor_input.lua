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
        end
            
    end
end


