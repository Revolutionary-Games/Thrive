-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false
--global_speciesNameCounter = 1  SERVES NO PURPOSE--
global_speciesNamePrefix = { ' Co', ' So', ' Pu', ' Cr', ' Cy', ' Gr', ' Re', ' Ty', ' Tr' }
global_speciesNameCofix = { 'nan', 'mo', 'na', 'yt', 'yn', 'il', 'li', 'op', 'un' }
global_speciesNameSuffix = { 'pien', 'olera', 'rius', 'nien', 'ster', 'ilia', 'canus', 'tus', 'cys'}
global_Genus_Picked = 0


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
        
        if global_Genus_Picked == 0 then
            global_Genus_Name = workingMicrobe.microbe.speciesName
            global_Genus_Picked = 1
        end
            
        math.randomseed(os.time())
        global_speciesGenName = (global_speciesNamePrefix[math.random(9)]) .. (global_speciesNameCofix[math.random(9)]) .. (global_speciesNameSuffix[math.random(9)])
        local new_species_name = global_Genus_Name .. global_speciesGenName
        global_speciesPreviousName = global_speciesNamePrefix
        global_speciesNamePrefix = { ' Co', ' So', ' Pu', ' Cr', ' Cy', ' Gr', ' Re', ' Ty', ' Tr' }
        global_speciesNameCofix = { 'nan', 'mo', 'na', 'yt', 'yn', 'il', 'li', 'op', 'un' }
        global_speciesNameSuffix = { 'pien', 'olera', 'rius', 'nien', 'ster', 'ilia', 'canus', 'tus', 'cys'}
        
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
