--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
--------------------------------------------------------------------------------
class 'MicrobeEditor'

function MicrobeEditor:__init()
    self.currentHexQ = 0
    self.currentHexR = 0
    self.settingHexQ = true -- True, the players numbers are interpreted as a Q coordinate, false: R coordinate
    self.currentMicrobe = nil
    self.nextMinus = false
end


function MicrobeComponent:load(storage)
    Component.load(self, storage)
    local organelles = storage:get("organelles", {})
    for i = 1,organelles:size() do
        local organelleStorage = organelles:get(i)
        local organelle = Organelle.loadOrganelle(organelleStorage)
        local q = organelle.position.q
        local r = organelle.position.r
        local s = encodeAxial(q, r)
        self.organelles[s] = organelle
    end
end


function MicrobeComponent:storage() 
    local storage = Component.storage(self)
    -- Organelles
    local organelles = StorageList()
    for _, organelle in pairs(self.organelles) do
        local organelleStorage = organelle:storage()
        organelles:append(organelleStorage)
    end
    storage:set("organelles", organelles)
    return storage
end


-- Recreates currentMicrobe in the active gamestate and returns it (necessary for transfer between gamestates)
function MicrobeEditor:recreateMicrobe(name) 


    local newMicrobe = Microbe.createMicrobeEntity(name, false)
    
  --    local storageOrganelle = StorageOrganelle(AgentRegistry.getAgentId("atp"), 100.0)
  --  storageOrganelle:addHex(0, 0)
  --  storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
  --  newMicrobe:addOrganelle(0, 0, storageOrganelle)
    
    
    
  --  local forwardOrganelle = MovementOrganelle(
  --      Vector3(0, 50, 0.0),
  --      300
  --  )
  --  forwardOrganelle:addHex(0, 0)
  --  forwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
  --  newMicrobe:addOrganelle(0, 1, forwardOrganelle)
    
    
    
   -- local sceneNode = newMicrobe.entity:getComponent(OgreSceneNodeComponent.TYPE_ID)
    
    
  --  sceneNode.transform.position = player.entity:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position
 --   sceneNode.transform.position.x = sceneNode.transform.position.x
  --  sceneNode.transform:touch()
    
    
  
    
    local microbeStorage = self.currentMicrobe.microbe:storage()
    
    local organelles = microbeStorage:get("organelles", {})
    for i = 1,organelles:size() do
        local organelleStorage = organelles:get(i)
        local organelle = Organelle.loadOrganelle(organelleStorage)
        local q = organelle.position.q
        local r = organelle.position.r
        newMicrobe:addOrganelle(q, r, organelle)
    end
    newMicrobe:storeAgent(AgentRegistry.getAgentId("atp"), 10)
    
  --  newMicrobe.microbe:load(microbeStorage)
    
    return newMicrobe
    
    


end

function MicrobeEditor:setNextMinus()
    self.nextMinus = true
end

function MicrobeEditor:setHexCoordinate(value)
    if self.settingHexQ then
        if self.nextMinus then
            self.currentHexQ = -value
            self.nextMinus = false
            print("Set Q coordinate to " .. -value)
        else
            self.currentHexQ = value
            print("Set Q coordinate to " .. value)
        end
        self.settingHexQ = false
        
    else
        if self.nextMinus then
            self.currentHexR = -value
            self.nextMinus = false
            print("Set R coordinate to " .. -value)
        else
            self.currentHexR = value
            print("Set R coordinate to " .. value)
        end
        self.settingHexQ = true
        
    end
end

function MicrobeEditor:removeOrganelle()
    self.currentMicrobe:removeOrganelle(self.currentHexQ, self.currentHexR) -- currently does not take into consideration special handling of vacuoles etc. (Or is that automatic?)
end

function MicrobeEditor:addStorageOrganelle()
    local storageOrganelle = StorageOrganelle(AgentRegistry.getAgentId("atp"), 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    self.currentMicrobe:addOrganelle(self.currentHexQ, self.currentHexR, storageOrganelle)
end

function MicrobeEditor:addMovementOrganelle(momentumX, momentumY) -- I cant remember how movement organelles work atm
    local forwardOrganelle = MovementOrganelle(
        Vector3(momentumX, momentumY, 0.0),
        300
    )
    forwardOrganelle:addHex(0, 0)
    forwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
    self.currentMicrobe:addOrganelle(self.currentHexQ, self.currentHexR, forwardOrganelle)
end

function MicrobeEditor:addProcessOrganelle(poType)

    if poType == "mitochondria" then
        local processOrganelle1 = ProcessOrganelle(20000) -- 20 second minimum time between producing oxytoxy
        processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("glucose"), 1)
        processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("oxygen"), 6)
        processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("atp"), 38)
        processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("co2"), 6)
        processOrganelle1:addHex(0, 0)
        processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
        self.currentMicrobe:addOrganelle(self.currentHexQ, self.currentHexR, processOrganelle1)
    end
end

function MicrobeEditor:createNewMicrobe()
    self.currentMicrobe = Microbe.createMicrobeEntity("working_microbe", false)
end