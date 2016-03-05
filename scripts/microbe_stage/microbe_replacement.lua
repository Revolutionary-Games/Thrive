-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false

function MicrobeReplacementSystem:__init()
    self.globalSpeciesNameCounter = 1
    System.__init(self)
end

function MicrobeReplacementSystem:activate()
    activeCreatureId = Engine:playerData():activeCreature()
    if Engine:playerData():isBoolSet("edited_microbe") then
        Engine:playerData():setBool("edited_microbe", false)

        workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR))

        speciesEntity = Entity(workingMicrobe.microbe.speciesName, GameState.MICROBE)
        species = SpeciesComponent(workingMicrobe.microbe.speciesName)
        speciesEntity:addComponent(species)
        self.globalSpeciesNameCounter = self.globalSpeciesNameCounter + 1
        species:fromMicrobe(workingMicrobe)

        workingMicrobe.entity:destroy()

        newMicrobe = Microbe.createMicrobeEntity(PLAYER_NAME, false, workingMicrobe.microbe.speciesName)
        species:template(newMicrobe)

        if newMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) < 10 then
            newMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
        end

        newMicrobe.collisionHandler:addCollisionGroup("powerupable")
        newMicrobeEntity = newMicrobe.entity:transfer(GameState.MICROBE)
        newMicrobeEntity:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newMicrobeEntity.id, GameState.MICROBE)
    end
   
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
