class 'QuickSaveSystem' (System)

function QuickSaveSystem:__init()
    System.__init(self)
end


function QuickSaveSystem:update(milliseconds)
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_F4)) then
        Engine:save("quick.sav")
    end
    if (Engine.keyboard:isKeyDown(KeyboardSystem.KC_F10)) then
        Engine:load("quick.sav")
    end
end

ADD_SYSTEM(QuickSaveSystem)
