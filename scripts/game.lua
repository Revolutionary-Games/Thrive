-- Main file 

function printStartMessage()

   print("Thrive version " .. " with " .. _VERSION .. " from " .. jit.version ..
            " ready to go. "
         --.. "Let's rock"
   )

end


-- Main loop for lua
--! @param cppGame thrive::Game object
function enterLuaMain(cppGame)

   printStartMessage()

   local fpsCount = 0

   local fpsTime = 0

   local lastUpdate = Game.now()
   
   while cppGame.shouldQuit == false do

      local now = Game.now();
      
      local milliseconds = Game.asMS(Game.delta(now, lastUpdate))
      
      lastUpdate = now;

      -- Update engine stuff and GameStates
      g_luaEngine:update(milliseconds)

      local frameDuration = Game.delta(Game.now(), now);

      -- sleep if we are going too fast
      Game.sleepIfNeeded(frameDuration)
         
      -- update fps counter
      fpsCount = fpsCount + 1;
      fpsTime = fpsTime + Game.asMS(frameDuration)
      
      if fpsTime >= 1000 then
         
         local fps = 1000 * (fpsCount / fpsTime)
         
         print("FPS: " .. fps)
         
         fpsCount = 0
         fpsTime = 0
         
      end
   end
end



                       




