-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false

function MicrobeReplacementSystem:__init()
    System.__init(self)
end

function MicrobeReplacementSystem:activate()
    activeCreatureId = Engine:playerData():activeCreature()
    if Engine:playerData():isBoolSet("edited_microbe") then
        Engine:playerData():setBool("edited_microbe", false);
        workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR))
        if workingMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) == 0 then
            workingMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
        end
        newMicrobeEntity = workingMicrobe.entity
        newPlayerMicrobe = newMicrobeEntity:transfer(GameState.MICROBE)
        newPlayerMicrobe:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newPlayerMicrobe.id, GameState.MICROBE)
    end
   
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
