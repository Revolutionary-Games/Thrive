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
        
        -- solution placeholder for species naming :
        --workingMicrobe.microbe.speciesName = "species" .. self.globalSpeciesNameCounter
        speciesEntity = Entity(workingMicrobe.microbe.speciesName, GameState.MICROBE)
        species = SpeciesComponent(workingMicrobe.microbe.speciesName)
        species.populationBonusFactor = 1.2
        speciesEntity:addComponent(species)
        self.globalSpeciesNameCounter = self.globalSpeciesNameCounter + 1
        species:fromMicrobe(workingMicrobe)

        -- having created a new species from the editor microbe, we now create a new microbe of this species,
        -- and make that the player
        
        workingMicrobeEntity = workingMicrobe.entity
        newMicrobe = Microbe.createMicrobeEntity(PLAYER_NAME, false, workingMicrobe.microbe.speciesName)
        species:template(newMicrobe)

        -- print the names of the organelles of the new microbe
        for i, o in pairs(newMicrobe.microbe.organelles) do print(o.name) end

        -- So, we /know/ things work fine up to here, since we have the organelles

        if newMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) < 10 then
            newMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 10)
        end

        --print("ATP: "..newMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")))
        -- and up to here, since the ATP stored above is present

        newMicrobeEntity = newMicrobe.entity:transfer(GameState.MICROBE) -- the secret is in the transfer
        newMicrobeEntity:stealName(PLAYER_NAME)
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newMicrobeEntity.id, GameState.MICROBE)
        print("finished setting up new microbe")
    end
   
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
