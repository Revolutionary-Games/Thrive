-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false
global_speciesNameCounter = 1

function MicrobeReplacementSystem:__init()
    System.__init(self)
end

function MicrobeReplacementSystem:init()
    System.init(self, "MicrobeReplacementSystem", gameState)
end

function MicrobeReplacementSystem:activate()
    if Engine:playerData():isBoolSet("edited_microbe") then
        Engine:playerData():setBool("edited_microbe", false)

        activeCreatureId = Engine:playerData():activeCreature()
        local workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR), true)

        local new_species_name = workingMicrobe.microbe.speciesName .. global_speciesNameCounter
        global_speciesNameCounter = global_speciesNameCounter + 1

        local speciesEntity = Entity(new_species_name)
        local species = SpeciesComponent(new_species_name)
        speciesEntity:addComponent(species)

        SpeciesSystem.fromMicrobe(workingMicrobe, species)
        workingMicrobe.entity:destroy()

        species.avgCompoundAmounts = {}
        species.avgCompoundAmounts["" .. CompoundRegistry.getCompoundId("atp")] = 10
        species.avgCompoundAmounts["" .. CompoundRegistry.getCompoundId("glucose")] = 20
        species.avgCompoundAmounts["" .. CompoundRegistry.getCompoundId("oxygen")] = 30

        SpeciesSystem.initProcessorComponent(speciesEntity, species)

        local newMicrobe = Microbe.createMicrobeEntity(nil, false, new_species_name)
        print(": "..newMicrobe.microbe.speciesName)

        newMicrobe.collisionHandler:addCollisionGroup("powerupable")
        newMicrobeEntity = newMicrobe.entity:transfer(GameState.MICROBE)
        newMicrobeEntity:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newMicrobeEntity.id, GameState.MICROBE)
    end
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
