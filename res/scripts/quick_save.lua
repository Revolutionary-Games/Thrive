class 'QuickSaveSystem' (System)

function QuickSaveSystem:__init()
    System.__init(self)
    self.saveDown = false
    self.loadDown = false
end


function QuickSaveSystem:update(milliseconds)
    local saveDown = Engine.keyboard:isKeyDown(Keyboard.KC_F4)
    local loadDown = Engine.keyboard:isKeyDown(Keyboard.KC_F10)
    if saveDown and not self.saveDown then
        print("Saving")
        Engine:save("quick.sav")
    end
    if loadDown and not self.loadDown then
        Engine:load("quick.sav")
    end
    self.saveDown = saveDown
    self.loadDown = loadDown
end

