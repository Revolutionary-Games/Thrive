-- QuickSaveSystem class usable as a System

-- Override methods for System
QuickSaveSystem = {}

function QuickSaveSystem:__init()

   self.saveDown = false
   self.loadDown = false   
end

function QuickSaveSystem:init(gameState)
   System.init(self, "QuickSaveSystem", gameState)
end

function QuickSaveSystem:update(renderTime, logicTime)
   local saveDown = Engine.keyboard:isKeyDown(Keyboard.KC_F4)
   local loadDown = Engine.keyboard:isKeyDown(Keyboard.KC_F10)
   if saveDown and not self.saveDown then
      Engine:save("quick.sav")
   end
   if loadDown and not self.loadDown then
      Engine:load("quick.sav")
   end
   self.saveDown = saveDown
   self.loadDown = loadDown
end

QuickSaveSystem = createLuaSystem(QuickSaveSystem)


