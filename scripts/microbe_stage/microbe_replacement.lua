-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false

function MicrobeReplacementSystem:__init()
    System.__init(self)
end

function MicrobeReplacementSystem:activate()
    if global_newEditorMicrobe then
        newMicrobeEntity = Entity("working_microbe", GameState.MICROBE_EDITOR)
        newPlayerMicrobe = newMicrobeEntity:transfer(GameState.MICROBE)
        newPlayerMicrobe:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
    end
end

function MicrobeReplacementSystem:update(milliseconds)
end
