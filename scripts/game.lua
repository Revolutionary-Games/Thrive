-- Main file 

function printStartMessage()

    print("Thrive version " .. Engine.thriveVersion ..
              " with " .. _VERSION .. " from " .. jit.version ..
              " ready to go. "
         --.. "Let's rock"
   )

end


-- Main loop for lua
--! @param cppGame thrive::Game object
function enterLuaMain(cppGame)

   printStartMessage()

   local fpsCount = 0

   local lastUpdate = Game.now()

   -- For more accurate FPS counting
   local lastSecond = Game.now()
   
   while cppGame.shouldQuit == false do

      local now = Game.now()
      
      local milliseconds = Game.asMS(Game.delta(now, lastUpdate))
      
      lastUpdate = now

      -- Update engine stuff and GameStates
      g_luaEngine:update(milliseconds)

      local frameDuration = Game.delta(Game.now(), now)

      -- sleep if we are going too fast
      cppGame:sleepIfNeeded(frameDuration)
      
      -- update fps counter
      fpsCount = fpsCount + 1

      local fpsTime = Game.asMS(Game.delta(now, lastSecond))
      if fpsTime >= 1000 then
         
         local fps = 1000 * (fpsCount / fpsTime)
         
         print("FPS: " .. fps)

         lastSecond = now
         fpsCount = 0
      end
   end
end



                       




