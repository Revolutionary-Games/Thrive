-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false
--set it up so the game knows whether or not to replace the genus.
global_Genus_Picked = false


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
		
        if not global_Genus_Picked  then
            global_Genus_Picked = true;
            global_Genus_Name = workingMicrobe.microbe.speciesName
        end
			
        new_species_name = self:generateSpeciesName();
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

--Faux-latin name generation routine (Move to own file eventually?)
function MicrobeReplacementSystem:generateSpeciesName()

    --prefix,cofix,suffix list
    speciesNamePrefix = {' Ce', ' Ar',' Sp', ' Th',' Co', ' So', ' Pu', ' Cr', ' Cy', ' Gr', ' Re', ' Ty', ' Tr', ' Ac',' Pr' }
    speciesNameCofix = { 'nan', 'mo', 'na', 'yt', 'yn', 'il', 'li','le', 'op', 'un', 'rive','ec', 'ro','lar','im' }
    speciesNameSuffix = { 'pien', 'olera', 'rius', 'nien', 'ster', 'ilia', 'canus', 'tus', 'cys','ium','um'}

    --Generate random seed
    math.randomseed(os.time())    
    speciesGenName = (speciesNamePrefix[math.random(#speciesNamePrefix)]) .. (speciesNameCofix[math.random(#speciesNameCofix)]) .. (speciesNameSuffix[math.random(#speciesNameSuffix)])
    local new_species_name = global_Genus_Name .. speciesGenName
    return new_species_name;   
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
