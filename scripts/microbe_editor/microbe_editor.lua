--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
-- See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of code.
--------------------------------------------------------------------------------
class 'MicrobeEditor'

function MicrobeEditor:__init()
    self.currentMicrobe = nil
    self.firstOrganelle = true
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

    self.firstOrganelle = true
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

function MicrobeEditor:getMouseHex()
    if self.firstOrganelle then -- We want the first organelle to be at the origin
        self.firstOrganelle = false
        return 0, 0
    else
        local mousePosition = Engine.mouse:normalizedPosition() 
        -- Get the position of the cursor in the plane that the microbes is floating in
         -- The 25 is a magic value (30 would make sense as is the camera distance). This will is an imperfect "hacked" solution due to my lacking understanding.
        local rayPoint =  Entity(CAMERA_NAME .. "3"):getComponent(OgreCameraComponent.TYPE_ID):getCameraToViewportRay(mousePosition.x, mousePosition.y):getPoint(25)
        -- Convert to the hex the cursor is currently located over. 
        local q, r = cartesianToAxial(-rayPoint.x, rayPoint.y) -- Negating X to compensate for the fact that we are looking at the opposite side of the normal coordinate system
        local qr, rr = cubeToAxial(cubeHexRound(axialToCube(q, r))) -- This requires a conversion to hex cube coordinates and back for proper rounding.
        return qr, rr
    end
end

function MicrobeEditor:removeOrganelle()
    local q, r = self:getMouseHex()
    self.currentMicrobe:removeOrganelle(q,r) -- currently does not take into consideration special handling of vacuoles etc. (Or is that automatic?)
end

function MicrobeEditor:addStorageOrganelle()
    local storageOrganelle = StorageOrganelle(AgentRegistry.getAgentId("atp"), 100.0)
    storageOrganelle:addHex(0, 0)
    storageOrganelle:setColour(ColourValue(0, 1, 0, 1))
    local q, r = self:getMouseHex()
    self.currentMicrobe:addOrganelle(q, r, storageOrganelle)
end

function MicrobeEditor:addMovementOrganelle(momentumX, momentumY) -- I cant remember how movement organelles work atm
    local forwardOrganelle = MovementOrganelle(
        Vector3(momentumX, momentumY, 0.0),
        300
    )
    forwardOrganelle:addHex(0, 0)
    forwardOrganelle:setColour(ColourValue(1, 0, 0, 1))
    local q, r = self:getMouseHex()
    self.currentMicrobe:addOrganelle(q,r, forwardOrganelle)
end

function MicrobeEditor:addProcessOrganelle(poType)

    if poType == "mitochondria" then         
        local processOrganelle1 = ProcessOrganelle(20000) -- 20 second minimum time between producing
        processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("glucose"), 1)
        processOrganelle1:addRecipyInput(AgentRegistry.getAgentId("oxygen"), 6)
        processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("atp"), 38)
        processOrganelle1:addRecipyOutput(AgentRegistry.getAgentId("co2"), 6)
        processOrganelle1:addHex(0, 0)
        processOrganelle1:setColour(ColourValue(1, 0, 1, 0))
        local q, r = self:getMouseHex()
        self.currentMicrobe:addOrganelle(q,r, processOrganelle1)
    end
end

function MicrobeEditor:createNewMicrobe()
    self.currentMicrobe = Microbe.createMicrobeEntity("working_microbe", false)
    self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
    self.currentMicrobe.sceneNode.transform:touch()
        
  --  local q,r = normalizedPixelToAxial((Engine.mouse:normalizedPosition().x - 0.5)*2, (Engine.mouse:normalizedPosition().y - 0.5)*2)
  --  local x, y = axialToCartesian(0,1)
  --  print(x .. " " .. y)
     --print((Engine.mouse:normalizedPosition().y - 0.5)*2)
 --   print(q .. " " .. r)
end