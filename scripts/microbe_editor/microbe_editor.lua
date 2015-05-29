--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
-- See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of hex related code.
--------------------------------------------------------------------------------
class 'MicrobeEditor'

FLAGELIUM_MOMENTUM = 12.5 -- what the heck is this for, and why is it here?

function MicrobeEditor:__init(hudSystem)
    self.currentMicrobe = nil
    self.organelleCount = 0
    self.activeActionName = nil
    self.hudSystem = hudSystem
    self.nextMicrobeEntity = nil
    self.gridSceneNode = nil
    self.gridVisible = true
    self.mutationPoints = 100
    self.placementFunctions = {["nucleus"] = MicrobeEditor.createNewMicrobe,
                               ["flagellum"] = MicrobeEditor.addOrganelle,
                               ["mitochondrion"] = MicrobeEditor.addOrganelle,
                               ["chloroplast"] = MicrobeEditor.addOrganelle,
                               ["oxytoxy"] = MicrobeEditor.addOrganelle,
                               
                               ["vacuole"] = MicrobeEditor.addOrganelle,
                             --  ["aminosynthesizer"] = MicrobeEditor.addProcessOrganelle,
                               ["remove"] = MicrobeEditor.removeOrganelle}
    self.actionHistory = nil
    self.actionIndex = 0
end

function MicrobeEditor:init(gameState)
    ent = Entity()
    sceneNode = OgreSceneNodeComponent()
    sceneNode.planeTexture = "EditorGridMaterial"
    ent:addComponent(sceneNode)
    self.gridSceneNode = sceneNode
end

function MicrobeEditor:activate()
    if Engine:playerData():activeCreatureGamestate():name() == GameState.MICROBE:name() then 
        microbeStageMicrobe = Entity(Engine:playerData():activeCreature(), GameState.MICROBE)
        self.nextMicrobeEntity = microbeStageMicrobe:transfer(GameState.MICROBE_EDITOR)
        self.nextMicrobeEntity:stealName("working_microbe")
        Engine:playerData():setBool("edited_microbe", true)
        Engine:playerData():setActiveCreature(self.nextMicrobeEntity.id, GameState.MICROBE_EDITOR)
    end
    self.mutationPoints = 100
    self.actionHistory = {} -- where all user actions will  be registered
    self.actionIndex = 0 -- marks the last action that has been done (not undone, but possibly redone), is 0 if there is none
end

function MicrobeEditor:update(renderTime, logicTime)
    -- self.nextMicrobeEntity being a temporary used to pass the microbe from game to editor
    if self.nextMicrobeEntity ~= nil then
        self.currentMicrobe = Microbe(self.nextMicrobeEntity)
        self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
        self.currentMicrobe.sceneNode.transform.position = Vector3(0, 0, 0)
        self.currentMicrobe.sceneNode.transform:touch()
        self.nextMicrobeEntity = nil
    end
    for _, organelle in pairs(self.currentMicrobe.microbe.organelles) do
        if organelle.flashDuration ~= nil then
            organelle.flashDuration = nil
            organelle._colour = organelle._originalColour
            organelle._needsColourUpdate = true
        end
        if organelle._needsColourUpdate then
            organelle:_updateHexColours()
        end
    end
    self.hudSystem:updateMutationPoints()
end

function MicrobeEditor:takeMutationPoints(amount)
    if amount <= self.mutationPoints then
        self.mutationPoints = self.mutationPoints - amount
        return true
    else
        return false
    end
end

function MicrobeEditor:performLocationAction()
    if (self.activeActionName) then
        local func = self.placementFunctions[self.activeActionName]
        func(self, self.activeActionName)
    end
end

function MicrobeEditor:setActiveAction(actionName)
    self.activeActionName = actionName
end

-- Instead of executing a command, put it in a table with a redo() and undo() function to make it use the Undo-/Redo-Feature.
-- Enqueuing it will execute it automatically, so you don't have to write things twice.
-- The cost of the action can also be incorporated into this by making it a member of the parameter table. It will be used automatically.
function MicrobeEditor:enqueueAction(action)
    if not action.cost or self:takeMutationPoints(action.cost) then
        while #self.actionHistory > self.actionIndex do
            table.remove(self.actionHistory)
        end
        self.hudSystem.undoButton:enable()
        self.hudSystem.redoButton:disable()
        action.redo()
        table.insert(self.actionHistory, action)
        self.actionIndex = self.actionIndex + 1
    end
end

function MicrobeEditor:undo()
    if self.actionIndex > 0 then
        local action = self.actionHistory[self.actionIndex]
        action.undo()
        if action.cost then
            self.mutationPoints = self.mutationPoints + action.cost
        end
        self.actionIndex = self.actionIndex - 1
    end
    -- nothing left to undo? disable undo
    if self.actionIndex <= 0 then
        self.hudSystem.undoButton:disable()
    end
    -- upon undoing, redoing is possible
    self.hudSystem.redoButton:enable()
end

function MicrobeEditor:redo()
    if self.actionIndex < #self.actionHistory then
        self.actionIndex = self.actionIndex + 1
        local action = self.actionHistory[self.actionIndex]
        action.redo()
        if action.cost then
            self.mutationPoints = self.mutationPoints - action.cost
        end
    end
    -- nothing left to redo? disable redo
    if self.actionIndex >= #self.actionHistory then
        self.hudSystem.redoButton:disable()
    end
    -- upon redoing, undoing is possible
    self.hudSystem.undoButton:enable()
end

function MicrobeEditor:getMouseHex()
    local mousePosition = Engine.mouse:normalizedPosition() 
    -- Get the position of the cursor in the plane that the microbes is floating in
    local rayPoint =  Entity(CAMERA_NAME .. "3"):getComponent(OgreCameraComponent.TYPE_ID):getCameraToViewportRay(mousePosition.x, mousePosition.y):getPoint(0)
    -- Convert to the hex the cursor is currently located over. 
    local q, r = cartesianToAxial(-rayPoint.x, rayPoint.y) -- Negating X to compensate for the fact that we are looking at the opposite side of the normal coordinate system
    local qr, rr = cubeToAxial(cubeHexRound(axialToCube(q, r))) -- This requires a conversion to hex cube coordinates and back for proper rounding.
    return qr, rr
end

function MicrobeEditor:addOrganelle(organelleType)
    local q, r = self:getMouseHex()
    if self.currentMicrobe:getOrganelleAt(q, r) == nil then
        local data = {["name"]=organelleType, ["q"]=q, ["r"]=r}
        self:enqueueAction({
            cost = Organelle.mpCosts[organelleType],
            redo = function()
                self.currentMicrobe:addOrganelle(q,r, OrganelleFactory.makeOrganelle(data))
                self.organelleCount = self.organelleCount + 1
            end,
            undo = function()
                self.currentMicrobe:removeOrganelle(q, r)
                self.currentMicrobe.sceneNode.transform:touch()
                self.organelleCount = self.organelleCount - 1
            end
        })
    end
end

function MicrobeEditor:removeOrganelle()
    local q, r = self:getMouseHex()
    if not (q == 0 and r == 0) then -- Don't remove nucleus
        local organelle = self.currentMicrobe:getOrganelleAt(q,r)
        if organelle then
            local storage = organelle:storage()
            self:enqueueAction{
                cost = 10,
                redo = function()
                    self.currentMicrobe:removeOrganelle(q, r)
                    self.currentMicrobe.sceneNode.transform:touch()
                    self.organelleCount = self.organelleCount - 1
                end,
                undo = function()
                    self.currentMicrobe:addOrganelle(q, r, Organelle.loadOrganelle(storage))
                    self.organelleCount = self.organelleCount + 1
                end
            }
        end
    end
end

function MicrobeEditor:addNucleus()
    local nucleusOrganelle = OrganelleFactory.makeOrganelle({["name"]="nucleus"})
    self.currentMicrobe:addOrganelle(0, 0, nucleusOrganelle)
end

function MicrobeEditor:loadMicrobe(entityId)
    self.organelleCount = 0
    if self.currentMicrobe ~= nil then
        self.currentMicrobe.entity:destroy()
    end
    self.currentMicrobe = Microbe(Entity(entityId))
    self.currentMicrobe.entity:stealName("working_microbe")
    self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
    self.currentMicrobe.sceneNode.transform:touch()
    Engine:playerData():setActiveCreature(entityId, GameState.MICROBE_EDITOR)
    self.mutationPoints = 0
    -- resetting the action history - it should not become entangled with the local file system
    self.actionHistory = {}
    self.actionIndex = 0
end

function MicrobeEditor:createNewMicrobe()
    local action = {
        redo = function()
            self.organelleCount = 0
            if self.currentMicrobe ~= nil then
                self.currentMicrobe.entity:destroy()
            end
            self.currentMicrobe = Microbe.createMicrobeEntity(nil, false)
            self.currentMicrobe.entity:stealName("working_microbe")
            self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
            self.currentMicrobe.sceneNode.transform:touch()
            self:addNucleus()
            self.mutationPoints = 100
            Engine:playerData():setActiveCreature(self.currentMicrobe.entity.id, GameState.MICROBE_EDITOR)
        end
    }
    
    if self.currentMicrobe ~= nil then
         -- that there has already been a microbe in the editor suggests that it was a player action, so it's prepared and filed in for un/redo
        local organelleStorage = {} -- self.currentMicrobe.microbe.organelles
        local previousOrganelleCount = self.organelleCount
        local previousMP = self.mutationPoints
        for position,organelle in pairs(self.currentMicrobe.microbe.organelles) do
            organelleStorage[position] = organelle:storage()
        end
        action.undo = function()
            self.currentMicrobe.entity:destroy() -- remove the "new" entity that has replaced the previous one
            self.currentMicrobe = Microbe.createMicrobeEntity(nil, false)
            self.currentMicrobe.entity:stealName("working_microbe")
            self.currentMicrobe.sceneNode.transform.orientation = Quaternion(Radian(Degree(180)), Vector3(0, 0, 1))-- Orientation
            self.currentMicrobe.sceneNode.transform:touch()
            for position,storage in pairs(organelleStorage) do
                local q, r = decodeAxial(position)
                self.currentMicrobe:addOrganelle(q, r, Organelle.loadOrganelle(storage))
            end
            -- no need to add the nucleus manually - it's alreary included in the organelleStorage
            self.mutationPoints = previousMP
            self.organelleCount = previousOrganelleCount
            Engine:playerData():setActiveCreature(self.currentMicrobe.entity.id, GameState.MICROBE_EDITOR)
        end
        self:enqueueAction(action)
    else
        -- if there's no microbe yet, it can be safely assumed that this is a generated default microbe when opening the editor for the first time, so it's not an action that should be put into the un/redo-feature
        action.redo()
    end
end
