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

    -- Counts frame times
    local frameTimes = {}
    
    while cppGame.shouldQuit == false do

        local now = Game.now()
        
        local milliseconds = Game.asMS(Game.delta(now, lastUpdate))
        
        lastUpdate = now

        -- Update engine stuff and GameStates
        g_luaEngine:update(milliseconds)

        local frameDuration = Game.delta(Game.now(), now)

        table.insert(frameTimes, Game.asSeconds(frameDuration))
        
        -- sleep if we are going too fast
        cppGame:sleepIfNeeded(frameDuration)
        
        -- update fps counter
        fpsCount = fpsCount + 1

        local fpsTime = Game.asMS(Game.delta(now, lastSecond))
        if fpsTime >= 1000 then
            
            local fps = 1000 * (fpsCount / fpsTime)

            local avgFrameTime = 0

            for i,t in ipairs(frameTimes) do

                avgFrameTime = avgFrameTime + t
                
            end

            avgFrameTime = (avgFrameTime / #frameTimes) * 1000

            print(string.format("FPS: %.4f avg frame duration: %.5f ms", fps,
                                avgFrameTime))

            -- Use to debug resource leaks in lua
            --print("Used memory: " .. Engine.luaMemory)
            
            frameTimes = {}
            lastSecond = now
            fpsCount = 0
        end
    end
end








