----------------------
-- A number of useful functions to be called from the console
----------------------

global_consoleEmitter = nil
local guiMode = false

function help()
    commands()
end

function commands()
    print("Extra console commands are: \n - spawnCompounds(name, amount) \n - reproduce() \n - suicide() \n - unlockAll() \n - mutationPoints() \n - toggleGuiMove()")
end

function toggleGuiMove()
    guiMode = not guiMode
    CEGUIWindow.setGuiMoveMode(guiMode)
end

function spawnCompounds(name, amount)
    if Engine:currentGameState():name() ~= GameState.MICROBE:name() then
        print("Must be in microbe stage to spawn compounds")
        return
    end
    if global_consoleEmitter == nil then
        global_consoleEmitter = Entity()
        local emitterComponent = CompoundEmitterComponent()
        emitterComponent.emissionRadius = 0
        emitterComponent.maxInitialSpeed = 0
        emitterComponent.minInitialSpeed = 0
        emitterComponent.particleLifetime = 100000
        global_consoleEmitter:addComponent(emitterComponent)
        local sceneNode = OgreSceneNodeComponent()
        global_consoleEmitter:addComponent(sceneNode)
    end
    local playerCreature = Microbe(Entity(Engine:playerData():activeCreature()))
    local compoundId = CompoundRegistry.getCompoundId(name)
    local emitterSceneNode = global_consoleEmitter:getComponent(OgreSceneNodeComponent.TYPE_ID)
    emitterSceneNode.transform.position = playerCreature.microbe.facingTargetPoint
    emitterSceneNode.transform:touch()
    local remainingAmount = amount
    while remainingAmount > 0 do
        local compoundEmitterComponent = global_consoleEmitter:getComponent(CompoundEmitterComponent.TYPE_ID)
        compoundAmount = math.min(3, remainingAmount)
        compoundEmitterComponent:emitCompound(compoundId, compoundAmount, 0, 0)  
        remainingAmount = remainingAmount - compoundAmount
    end
end


function reproduce()
    if Engine:currentGameState():name() ~= GameState.MICROBE:name() then
        print("Must be in microbe stage to reproduce")
        return
    end
    playerCreature = Microbe(Entity(Engine:playerData():activeCreature()))
    playerCreature:reproduce()
end

function suicide()
    if Engine:currentGameState():name() ~= GameState.MICROBE:name() then
        print("Must be in microbe stage to suicide")
        return
    end
    playerCreature = Microbe(Entity(Engine:playerData():activeCreature()))
    playerCreature:kill()
end

function unlockAll()
    lockMap = Engine:playerData():lockedMap()
    for lock in lockMap:locksList() do 
        lockMap:unlock(lock)
    end

end

function mutationPoints()
    if Engine:currentGameState():name() ~= GameState.MICROBE_EDITOR:name() then
        print("Must be in microbe editor to add mutation points")
        return
    end
    global_activeMicrobeEditorHudSystem.editor.mutationPoints = 9999
end
