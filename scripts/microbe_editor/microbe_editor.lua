--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
-- See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of code.
--------------------------------------------------------------------------------
class 'MicrobeEditor'

FLAGELIUM_MOMENTUM = 50

function MicrobeEditor:__init(hudSystem)
    self.currentMicrobe = nil
    self.organelleCount = 0
    self.activeActionName = nil
    self.hudSystem = hudSystem
    self.placementFunctions = {["nucleus"] = MicrobeEditor.createNewMicrobe,
                               ["flagelium"] = MicrobeEditor.addMovementOrganelle,
                               ["mitochondria"] = MicrobeEditor.addProcessOrganelle,
                               ["vacuole"] = MicrobeEditor.addStorageOrganelle,
                               ["remove"] = MicrobeEditor.removeOrganelle}
end


-- Recreates currentMicrobe in the active gamestate and returns it (necessary for transfer between gamestates)
function MicrobeEditor:recreateMicrobe(name) 
    local newMicrobe = Microbe.createMicrobeEntity(name, false)  

    local microbeStorage = self.currentMicrobe.microbe:storage()
    
    local organelles = microbeStorage:get("organelles", {})
    for i = 1,organelles:size() do
        local organelleStorage = organelles:get(i)
        local organelle = Organelle.loadOrganelle(organelleStorage)
        local q = organelle.position.q
        local r = organelle.position.r
        newMicrobe:addOrganelle(q, r, organelle)
    end
    newMicrobe:storeCompound(CompoundRegistry.getCompoundId("atp"), 20, false)
    
  --  newMicrobe.microbe:load(microbeStorage)
    
    return newMicrobe

end

function MicrobeEditor:performLocationAction()
    if (self.activeActionName) then
        local func = self.placementFunctions[self.activeActionName]
        func(self, self.activeActionName)
    end
end

function MicrobeEditor:setActiveAction(actionName)
    self.activeActionName = actionName
    -- self.activeClickFunction = self.placementFunctions[organelleName]
end

function MicrobeEditor:getMouseHex()
    local mousePosition = Engine.mouse:normalizedPosition() 
    -- Get the position of the cursor in the plane that the microbes is floating in
     -- The 25 is a magic value (30 would make sense as is the camera distance). This will is an imperfect "hacked" solution due to my lacking understanding.
    local rayPoint =  Entity(CAMERA_NAME .. "3"):getComponent(OgreCameraComponent.TYPE_ID):getCameraToViewportRay(mousePosition.x, mousePosition.y):getPoint(25)
    -- Convert to the hex the cursor is currently located over. 
    local q, r = cartesianToAxial(-rayPoint.x, rayPoint.y) -- Negating X to compensate for the fact that we are looking at the opposite side of the normal coordinate system
    local qr, rr = cubeToAxial(cubeHexRound(axialToCube(q, r))) -- This requires a conversion to hex cube coordinates and back for proper rounding.
    return qr, rr
end

function MicrobeEditor:removeOrganelle()
    local q, r = self:getMouseHex()
    self.currentMicrobe:removeOrganelle(q,r)
    self.organelleCount = self.organelleCount - 1
end


function MicrobeEditor:addStorageOrganelle(organelleType)
    local q, r = self:getMouseHex()
    if self.currentMicrobe:getOrganelleAt(q, r) == nil then
        local storageOrganelle = StorageOrganelle(100.0)
        storageOrganelle:addHex(0, 0)
        storageOrganelle:setColour(ColourValue(0, 1, 0, 1))  
        self.currentMicrobe:addOrganelle(q, r, storageOrganelle)
        self.organelleCount = self.organelleCount + 1
    end
end


function MicrobeEditor:addMovementOrganelle(organelleType)
    -- Calculate the momentum of the movement organelle based on angle towards nucleus
    local q, r = self:getMouseHex()
    if self.currentMicrobe:getOrganelleAt(q, r) == nil then
        local organelleX, organelleY = axialToCartesian(q, r)
        local nucleusX, nucleusY = axialToCartesian(0, 0)
        local deltaX = nucleusX - organelleX
        local deltaY = nucleusY - organelleY
        local dist = math.sqrt(deltaX^2 + deltaY^2) -- For normalizing vector
        local momentumX = deltaX / dist * FLAGELIUM_MOMENTUM
        local momentumY = deltaY / dist * FLAGELIUM_MOMENTUM
        local movementOrganelle = MovementOrganelle(
            Vector3(momentumX, momentumY, 0.0),
            300
        )
        movementOrganelle:addHex(0, 0)
        movementOrganelle:setColour(ColourValue(1, 0, 0, 1))
        self.currentMicrobe:addOrganelle(q,r, movementOrganelle)
        self.organelleCount = self.organelleCount + 1
    end
end

function MicrobeEditor:addProcessOrganelle(organelleType)
    
    if organelleType == "mitochondria" then         
        local q, r = self:getMouseHex()
        if self.currentMicrobe:getOrganelleAt(q, r) == nil then
            local processOrganelle = ProcessOrganelle()
            local inputCompounds = {[CompoundRegistry.getCompoundId("glucose")] = 1,
                                    [CompoundRegistry.getCompoundId("oxygen")] = 6}
            local outputCompounds = {[CompoundRegistry.getCompoundId("atp")] = 38,
                                    [CompoundRegistry.getCompoundId("co2")] = 6}
            local respiration = Process(0.5, 1.0, inputCompounds, outputCompounds)
            processOrganelle:addProcess(respiration)
            processOrganelle:addHex(0, 0)
            processOrganelle:setColour(ColourValue(1, 0, 1, 0))
            
            self.currentMicrobe:addOrganelle(q,r, processOrganelle)
            self.organelleCount = self.organelleCount + 1
        end
    end
    self.organelleCount = self.organelleCount + 1
end

function MicrobeEditor:createNewMicrobe()
    self.organelleCount = 0
    self.currentMicrobe = Microbe.createMicrobeEntity("working_microbe", false)
    self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
    self.currentMicrobe.sceneNode.transform:touch()
    local nucleusOrganelle = NucleusOrganelle()
    nucleusOrganelle:addHex(0, 0)
    nucleusOrganelle:setColour(ColourValue(0.8, 0.2, 0.8, 1))
    self.currentMicrobe:addOrganelle(0, 0, nucleusOrganelle)
end