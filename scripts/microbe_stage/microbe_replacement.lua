-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

-- Global reference to a microbe that needs transfer between microbe stage and microbe editor
global_transferMicrobe = nil

function MicrobeReplacementSystem:__init()
    System.__init(self)
end

function MicrobeReplacementSystem:activate()
    if global_transferMicrobe ~= nil then
       
        Entity(PLAYER_NAME):destroy() 
        player = self:recreateMicrobe(global_transferMicrobe)
        -- We need to steal the name instead of giving it to the microbe creation function 
        -- otherwise we just get a reference to the to-be-deleted entity under creation
        player.entity:stealName(PLAYER_NAME)
        global_transferMicrobe = nil--player -- For transfer back
    end
end

-- Recreates currentMicrobe in the active gamestate and returns it (necessary for transfer between gamestates)
function MicrobeReplacementSystem:recreateMicrobe(microbe) 
    local newMicrobe = Microbe.createMicrobeEntity(false)  
    for _, organelle in pairs(microbe.microbe.organelles) do
        -- copy organelle
        local organelleStorage = organelle:storage()
        local organelle = Organelle.loadOrganelle(organelleStorage)
        local q = organelle.position.q
        local r = organelle.position.r
        newMicrobe:addOrganelle(q, r, organelle)
    end
    newMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 20, false)
    return newMicrobe
end

function MicrobeReplacementSystem:update(milliseconds)

end
