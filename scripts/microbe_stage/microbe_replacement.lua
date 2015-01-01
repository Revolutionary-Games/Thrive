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

        -- first we'll decouple workingMicrobe from microbe stage, then we'll decouple from editor
        workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR))
        if workingMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) < 10 then
            workingMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
        end
        -- solution placeholder for species naming :
        --workingMicrobe.microbe.speciesName = "species" .. self.globalSpeciesNameCounter
        speciesEntity = Entity(workingMicrobe.microbe.speciesName..self.globalSpeciesNameCounter, GameState.MICROBE)
        species = SpeciesComponent(workingMicrobe.microbe.speciesName)
        species.populationBonusFactor = 1.2
        speciesEntity:addComponent(species)
        self.globalSpeciesNameCounter = self.globalSpeciesNameCounter + 1

        -- Initiate entity transfer to microbe stage
        -- TODO replace entity transfer with microbe initialization from species

        newMicrobeEntity = workingMicrobe.entity
        newPlayerMicrobe = newMicrobeEntity:transfer(GameState.MICROBE)
        newPlayerMicrobe:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newPlayerMicrobe.id, GameState.MICROBE)
    end
   
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
