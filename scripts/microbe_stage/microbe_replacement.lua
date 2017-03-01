-- Needs to be the first system
MicrobeReplacementSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)
        
        --prefix,cofix,suffix list
        self.speciesNamePrefix = {' Ce', ' Ar',' Sp', ' Th',' Co', ' So', ' Pu', ' Cr', ' Cy',
                                  ' Gr', ' Re', ' Ty', ' Tr', ' Ac',' Pr' }
        self.speciesNameCofix = { 'nan', 'mo', 'na', 'yt', 'yn', 'il', 'li','le', 'op', 'un',
                                  'rive','ec', 'ro','lar','im' }
        self.speciesNameSuffix = { 'pien', 'olera', 'rius', 'nien', 'ster', 'ilia', 'canus',
                                   'tus', 'cys','ium','um'} 
        
    end
)

-- Global boolean for whether a new microbe is avaliable in the microbe editor.
global_newEditorMicrobe = false
--set it up so the game knows whether or not to replace the genus.
global_genusPicked = false

function MicrobeReplacementSystem:init(gameState)
    LuaSystem.init(self, "MicrobeReplacementSystem", gameState)
end

function MicrobeReplacementSystem:activate()
	
    if Engine:playerData():isBoolSet("edited_microbe") then
        Engine:playerData():setBool("edited_microbe", false)

        activeCreatureId = Engine:playerData():activeCreature()
        local workingMicrobe = Microbe(Entity(activeCreatureId, GameState.MICROBE_EDITOR), true)
        
        if not global_genusPicked  then
            global_genusPicked = true;
            global_genusName = workingMicrobe.microbe.speciesName
        end
			
        newSpeciesName = self:generateSpeciesName();
        local speciesEntity = Entity(newSpeciesName)
        local species = SpeciesComponent(newSpeciesName)
        speciesEntity:addComponent(species)
        SpeciesSystem.fromMicrobe(workingMicrobe, species)
        SpeciesSystem.initProcessorComponent(speciesEntity, species)

        local newMicrobe = Microbe.createMicrobeEntity(nil, false, newSpeciesName)
        newMicrobe:divide()
                
        print(": "..newMicrobe.microbe.speciesName)
        
        newMicrobe.collisionHandler:addCollisionGroup("powerupable")
        newMicrobeEntity = newMicrobe.entity:transfer(GameState.MICROBE)
        newMicrobeEntity:stealName(PLAYER_NAME)
        
        global_newEditorMicrobe = false
        Engine:playerData():setActiveCreature(newMicrobeEntity.id, GameState.MICROBE)
        
        workingMicrobe.entity:destroy()
    end
end

--Faux-latin name generation routine (Move to own file eventually?)
function MicrobeReplacementSystem:generateSpeciesName()
    --Generate random seed
    math.randomseed(os.time())    
    local speciesGenName = (self.speciesNamePrefix[math.random(#self.speciesNamePrefix)]) .. (self.speciesNameCofix[math.random(#self.speciesNameCofix)]) .. (self.speciesNameSuffix[math.random(#self.speciesNameSuffix)])
    return global_genusName .. speciesGenName;
end

function MicrobeReplacementSystem:update(renderTime, logicTime)
end
