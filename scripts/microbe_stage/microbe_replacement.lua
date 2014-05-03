-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false

function MicrobeReplacementSystem:__init()
    System.__init(self)
end

function MicrobeReplacementSystem:activate()
    if global_newEditorMicrobe then
        workingMicrobe = Microbe(Entity("working_microbe", GameState.MICROBE_EDITOR))
        if workingMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) == 0 then
            workingMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
        end
        newMicrobeEntity = workingMicrobe.entity
        newPlayerMicrobe = newMicrobeEntity:transfer(GameState.MICROBE)
        newPlayerMicrobe:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
    end
    
end

function MicrobeReplacementSystem:update(milliseconds)
end
