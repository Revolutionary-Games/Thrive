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
    activeCreatureId = Engine:playerData():activeCreature()
    if Engine:playerData():isBoolSet("edited_microbe") then
        Engine:playerData():setBool("edited_microbe", false)

        workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR), true)

        new_species_name = workingMicrobe.microbe.speciesName .. global_speciesNameCounter
        global_speciesNameCounter = global_speciesNameCounter + 1
        print("NEW SPECIES: "..new_species_name)

        speciesEntity = Entity(new_species_name, GameState.MICROBE)
        species = SpeciesComponent(new_species_name)
        speciesEntity:addComponent(species)
        processorComponent = ProcessorComponent()
        speciesEntity:addComponent(processorComponent)
        for compoundID in CompoundRegistry.getCompoundList() do
            processorComponent:setThreshold(compoundID, 10, 50, 100) -- we currently just generate a new processor for the new species
        end
        species:fromMicrobe(workingMicrobe)
        -- below folded into :fromMicrobe()
        -- species.colour = workingMicrobe:getComponent(MembraneComponent.TYPE_ID):getColour()

        newMicrobe = Microbe.createMicrobeEntity(nil, false, new_species_name)
        print(": "..newMicrobe.microbe.speciesName)
        workingMicrobe.entity:destroy()

        -- species:template(newMicrobe)
        -- newMicrobe.compoundBag:setProcessor(Entity(workingMicrobe.microbe.speciesName):getComponent(ProcessorComponent.TYPE_ID))
        -- newMicrobe.compoundBag:setProcessor(processorComponent)

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
