-- QuickSaveSystem

QuickSaveSystem = class(
   LuaSystem,
   function(self)

      LuaSystem.create(self)

   end
)

function QuickSaveSystem:init(gameState)

   LuaSystem.init(self, "QuickSaveSystem", gameState)

   self.saveDown = false 
   self.loadDown = false 

end

function QuickSaveSystem:update(renderTime, logicTime)
   local saveDown = Engine.keyboard:isKeyDown(KEYCODE.KC_F4)
   local loadDown = Engine.keyboard:isKeyDown(KEYCODE.KC_F10)
   if saveDown and not self.saveDown then
      Engine:save("quick.sav")
   end
   if loadDown and not self.loadDown then
      Engine:load("quick.sav")
   end
   self.saveDown = saveDown
   self.loadDown = loadDown
end




