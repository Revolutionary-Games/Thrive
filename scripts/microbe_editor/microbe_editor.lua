--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
-- See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of hex related code.
--------------------------------------------------------------------------------
MicrobeEditor = class(
    function(self, hudSystem)

        self.currentMicrobe = nil
        self.organelleCount = 0
        self.activeActionName = nil
        self.hudSystem = hudSystem
        self.nextMicrobeEntity = nil
        self.gridSceneNode = nil
        self.gridVisible = true
        self.mutationPoints = 50
        self.placementFunctions = {["nucleus"] = MicrobeEditor.createNewMicrobe,
            ["flagellum"] = MicrobeEditor.addOrganelle,
            ["cytoplasm"] = MicrobeEditor.addOrganelle,
            ["mitochondrion"] = MicrobeEditor.addOrganelle,
            ["chloroplast"] = MicrobeEditor.addOrganelle,
            ["oxytoxy"] = MicrobeEditor.addOrganelle,
            ["vacuole"] = MicrobeEditor.addOrganelle,
            ["remove"] = MicrobeEditor.removeOrganelle}
        self.actionHistory = nil
        self.actionIndex = 0
        self.organelleRot = 0
        self.occupiedHexes = {}
        -- 0 is no symmetry, 1 is x-axis symmetry, 2 is 4-way symmetry, and 3 is 6-way symmetry.
        self.symmetry = 0
    end
)

function MicrobeEditor:createHexComponent(q, r)
    local x, y = axialToCartesian(q, r)
    local s = encodeAxial(q, r)
    self.occupiedHexes[s] = Entity.new(g_luaEngine.currentGameState.wrapper)
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.transform.position = Vector3(x, y, 0)
    sceneNode.transform:touch()
    sceneNode.meshName = "hex.mesh"
    sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    self.occupiedHexes[s]:addComponent(sceneNode)
    self.occupiedHexes[s]:setVolatile(true)
end

-- checks whether the hex at q, r has an organelle in its surroundeing hexes.
function MicrobeEditor:surroundsOrganelle(q, r)
    return  MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q + 0, r - 1) or
            MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q + 1, r - 1) or
			MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q + 1, r + 0) or
			MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q + 0, r + 1) or
			MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q - 1, r + 1) or
			MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q - 1, r + 0)
end

function MicrobeEditor:init(gameState)
    ent = Entity.new(gameState.wrapper)
    local sceneNode = OgreSceneNodeComponent.new()
    sceneNode.planeTexture = "EditorGridMaterial"
    ent:addComponent(sceneNode)
    sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, 1)
    sceneNode.transform:touch()
    
    self.gridSceneNode = sceneNode
end

function MicrobeEditor:activate()
    local creatureState = g_luaEngine:getLuaStateFromWrapper(
        Engine:playerData():activeCreatureGamestate())
    
    if creatureState.name == GameState.MICROBE.name or
    creatureState.name == GameState.MICROBE_TUTORIAL.name then
        
        microbeStageMicrobe = Entity.new(Engine:playerData():activeCreature(),
                                         GameState.MICROBE.wrapper)

        self.nextMicrobeEntity = Entity.new(
            g_luaEngine:transferEntityGameState(microbeStageMicrobe.id,
                                                creatureState.entityManager,
                                                GameState.MICROBE_EDITOR),
            GameState.MICROBE_EDITOR.wrapper)
        
        -- Transfer the compounds
        Microbe.new(self.nextMicrobeEntity, true, GameState.MICROBE_EDITOR)
        MicrobeSystem.transferCompounds(microbeStageMicrobe, self.nextMicrobeEntity)
        
        self.nextMicrobeEntity:stealName("working_microbe")
        Engine:playerData():setBool("edited_microbe", true)
        Engine:playerData():setActiveCreature(self.nextMicrobeEntity.id,
                                              GameState.MICROBE_EDITOR.wrapper)
    end
    

    self.mutationPoints = 50
    self.actionHistory = {} -- where all user actions will  be registered
    self.actionIndex = 0 -- marks the last action that has been done (not undone, but possibly redone), is 0 if there is none
    for _, cytoplasm in pairs(self.occupiedHexes) do
        cytoplasm:destroy()
    end
    
    self.currentMicrobe = Microbe.new(self.nextMicrobeEntity, true, GameState.MICROBE_EDITOR)
    self.currentMicrobe.sceneNode.transform.orientation = Quaternion.new(
        Radian.new(Degree(0)), Vector3(0, 0, 1))-- Orientation
    self.currentMicrobe.sceneNode.transform.position = Vector3(0, 0, 0)
    self.currentMicrobe.sceneNode.transform:touch()
    
    for _, organelle in pairs(self.currentMicrobe.microbe.organelles) do
        for s, hex in pairs(organelle._hexes) do
            self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
        end
    end
end

function MicrobeEditor:update(renderTime, logicTime)
    local q, r = self:getMouseHex()
    
    if self.symmetry == 0 then    
        self:renderHighlightedOrganelle(1, q, r, self.organelleRot)
    elseif self.symmetry == 1 then
        self:renderHighlightedOrganelle(1, q, r, self.organelleRot)
        self:renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*self.organelleRot))
    elseif self.symmetry == 2 then
        self:renderHighlightedOrganelle(1, q, r, self.organelleRot)
        self:renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*self.organelleRot))
        self:renderHighlightedOrganelle(3, -1*q, -1*r, (self.organelleRot+180) % 360)
        self:renderHighlightedOrganelle(4, q, -1*(r+q), 540+(-1*self.organelleRot) % 360)
    elseif self.symmetry == 3 then
        self:renderHighlightedOrganelle(1, q, r, self.organelleRot)
        self:renderHighlightedOrganelle(2, -1*r, r+q, (self.organelleRot+60) % 360)
        self:renderHighlightedOrganelle(3, -1*(r+q), q, (self.organelleRot+120) % 360)
        self:renderHighlightedOrganelle(4, -1*q, -1*r, (self.organelleRot+180) % 360)
        self:renderHighlightedOrganelle(5, r, -1*(r+q), (self.organelleRot+240) % 360)
        self:renderHighlightedOrganelle(6, r+q, -1*q, (self.organelleRot+300) % 360)
    end
        
    self.hudSystem:updateMutationPoints()
end

-- The first parameter states which sceneNodes to use, starting with "start" and going up 6.
function MicrobeEditor:renderHighlightedOrganelle(start, q, r, rotation)
    -- Render the hex under the cursor
    local sceneNode = {}
    sceneNode[1] = getComponent(self.hudSystem.hoverOrganelle[start], OgreSceneNodeComponent)
    for i=2, 8 do
        sceneNode[i] = getComponent(self.hudSystem.hoverHex[i-1+(start-1)*7],
                                    OgreSceneNodeComponent)
    end
    
    if self.activeActionName then
        local oldData = {["name"]=self.activeActionName, ["q"]=-q, ["r"]=-r, ["rotation"]=(180+rotation) % 360}
        local hexes = OrganelleFactory.checkSize(oldData)
        local colour = ColourValue(2, 0, 0, 0.4)
		local touching = false;
        for _, hex in ipairs(hexes) do
            if self:surroundsOrganelle(-hex.q + q, -hex.r + r) then
                colour = ColourValue(0, 2, 0, 0.4)
			end
		end
        for _, hex in ipairs(hexes) do
            local organelle = MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, -hex.q + q, -hex.r + r)
            if organelle then
                if organelle.name ~= "cytoplasm" then
                    colour = ColourValue(2, 0, 0, 0.4)
                end
            end
		end
        if CEGUIWindow.getWindowUnderMouse():getName() == 'root' then

			local newData = {
                ["name"]=self.activeActionName,
                ["q"]=-q,
                ["r"]=-r,
                ["sceneNode"]=sceneNode,
                ["rotation"]=(180+rotation) % 360,
                ["colour"]=colour
            }

            OrganelleFactory.renderOrganelles(newData)
			for i=1, 8 do
                sceneNode[i].transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE) --Vector3(1,1,1)
				sceneNode[i].transform:touch()
			end
		end
    end
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
    local rayPoint = getComponent(CAMERA_NAME .. "3", g_luaEngine.currentGameState,
                                  OgreCameraComponent
    ):getCameraToViewportRay(mousePosition.x, mousePosition.y):getPoint(0)
    
    -- Convert to the hex the cursor is currently located over. 
    local q, r = cartesianToAxial(rayPoint.x, -1*rayPoint.y) -- Negating X to compensate for the fact that we are looking at the opposite side of the normal coordinate system
    local qr, rr = cubeToAxial(cubeHexRound(axialToCube(q, r))) -- This requires a conversion to hex cube coordinates and back for proper rounding.
    --print(qr, rr)
    return qr, rr
end

function MicrobeEditor:isValidPlacement(organelleType, q, r, rotation)
    local data = {["name"]=organelleType, ["q"]=q, ["r"]=r, ["rotation"]=rotation}
    local newOrganelle = OrganelleFactory.makeOrganelle(data)
    local empty = true
    local touching = false;
    for s, hex in pairs(OrganelleFactory.checkSize(data)) do
        local organelle = MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, hex.q + q, hex.r + r)
        if organelle then
            if organelle.name ~= "cytoplasm" then
                empty = false 
            end
        end
		if  self:surroundsOrganelle(hex.q + q, hex.r + r) then
			touching = true;
		end
    end
    
    if empty and touching then
        newOrganelle.rotation = data.rotation
        return newOrganelle
    else
        return nil
    end
end

function MicrobeEditor:addOrganelle(organelleType)
    local q, r = self:getMouseHex()
    
    if self.symmetry == 0 then
        local organelle = self:isValidPlacement(organelleType, q, r, self.organelleRot)
        
        if organelle then
            if organelleTable[organelle.name].mpCost > self.mutationPoints then return end
            self:_addOrganelle(organelle, q, r, self.organelleRot)
        end
    elseif self.symmetry == 1 then
        -- Makes sure that the organelle doesn't overlap on the existing ones.
        local organelle = self:isValidPlacement(organelleType, q, r, self.organelleRot)
        if (q ~= -1*q or r ~= r+q) then -- If two organelles aren't overlapping
            local organelle2 = self:isValidPlacement(organelleType, -1*q, r+q, 360+(-1*self.organelleRot))
            
            -- If the organelles were successfully created and have enough MP...
            if organelle and organelle2 and organelleTable[organelle.name].mpCost*2 <= self.mutationPoints then            
                -- Add the organelles to the microbe.
                self:_addOrganelle(organelle, q, r, self.organelleRot)
                self:_addOrganelle(organelle2, -1*q, r+q, 360+(-1*self.organelleRot))
            end
        else
            if organelle and organelleTable[organelle.name].mpCost <= self.mutationPoints then            
                -- Add a organelle to the microbe.
                self:_addOrganelle(organelle, q, r, self.organelleRot)
            end
        end
    elseif self.symmetry == 2 then
        local organelle = self:isValidPlacement(organelleType, q, r, self.organelleRot)
        if q ~= -1*q or r ~= r+q then -- If two organelles aren't overlapping, none are
            local organelle2 = self:isValidPlacement(organelleType, -1*q, r+q, 360+(-1*self.organelleRot))
            local organelle3 = self:isValidPlacement(organelleType, -1*q, -1*r, (self.organelleRot+180) % 360)
            local organelle4 = self:isValidPlacement(organelleType, q, -1*(r+q), (540+(-1*self.organelleRot)) % 360)
            
            if organelle and organelle2 and organelle3 and organelle4 and organelleTable[organelle.name].mpCost*4 <= self.mutationPoints then
                self:_addOrganelle(organelle, q, r, self.organelleRot)
                self:_addOrganelle(organelle2, -1*q, r+q, 360+(-1*self.organelleRot))
                self:_addOrganelle(organelle3, -1*q, -1*r, (self.organelleRot+180) % 360)
                self:_addOrganelle(organelle4, q, -1*(r+q), (540+(-1*self.organelleRot)) % 360)
            end
        else
            if organelle and organelleTable[organelle.name].mpCost <= self.mutationPoints then
                self:_addOrganelle(organelle, q, r, self.organelleRot)
            end
        end
    elseif self.symmetry == 3 then
        local organelle = self:isValidPlacement(organelleType, q, r, self.organelleRot)
        if q ~= -1*r or r ~= r+q then -- If two organelles aren't overlapping, none are
            local organelle2 = self:isValidPlacement(organelleType, -1*r, r+q, (self.organelleRot+60) % 360)
            local organelle3 = self:isValidPlacement(organelleType, -1*(r+q), q, (self.organelleRot+120) % 360)
            local organelle4 = self:isValidPlacement(organelleType, -1*q, -1*r, (self.organelleRot+180) % 360)
            local organelle5 = self:isValidPlacement(organelleType, r, -1*(r+q), (self.organelleRot+240) % 360)
            local organelle6 = self:isValidPlacement(organelleType, r+q, -1*q, (self.organelleRot+300) % 360)
            
            if organelle and organelle2 and organelle3 and organelle4 and organelle5 and organelle6 
                         and organelleTable[organelle.name].mpCost*6 <= self.mutationPoints then
                self:_addOrganelle(organelle, q, r, self.organelleRot)
                self:_addOrganelle(organelle2, -1*r, r+q, (self.organelleRot+60) % 360)
                self:_addOrganelle(organelle3, -1*(r+q), q, (self.organelleRot+120) % 360)
                self:_addOrganelle(organelle4, -1*q, -1*r, (self.organelleRot+180) % 360)
                self:_addOrganelle(organelle5, r, -1*(r+q), (self.organelleRot+240) % 360)
                self:_addOrganelle(organelle6, r+q, -1*q, (self.organelleRot+300) % 360)
            end
        else
            if organelle and organelleTable[organelle.name].mpCost <= self.mutationPoints then 
                 self:_addOrganelle(organelle, q, r, self.organelleRot)
            end
        end
    end   
end

function MicrobeEditor:_addOrganelle(organelle, q, r, rotation)
    self:enqueueAction({
        cost = organelleTable[organelle.name].mpCost,
        redo = function()
            for _, hex in pairs(organelle._hexes) do
                -- Check if there is cytoplasm under this organelle.
                local cytoplasm = MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, hex.q + q, hex.r + r)
                if cytoplasm then
                    if cytoplasm.name == "cytoplasm" then
                        MicrobeSystem.removeOrganelle(self.currentMicrobe.entity, hex.q + q, hex.r + r)
                        self.currentMicrobe.sceneNode.transform:touch()
                        self.organelleCount = self.organelleCount - 1
                        local s = encodeAxial(hex.q + q, hex.r + r)
                        self.occupiedHexes[s]:destroy()
                    end
                end
                self:createHexComponent(hex.q + q, hex.r + r)
            end
            MicrobeSystem.addOrganelle(self.currentMicrobe.entity, q, r, rotation, organelle)
            self.organelleCount = self.organelleCount + 1
        end,
        undo = function()
            MicrobeSystem.removeOrganelle(self.currentMicrobe.entity, q, r)
            self.currentMicrobe.sceneNode.transform:touch()
            self.organelleCount = self.organelleCount - 1
            for _, hex in pairs(organelle._hexes) do
                local x, y = axialToCartesian(hex.q + q, hex.r + r)
                local s = encodeAxial(hex.q + q, hex.r + r)
                self.occupiedHexes[s]:destroy()
            end
        end
    })
end

function MicrobeEditor:removeOrganelleAt(q,r)
    local organelle = MicrobeSystem.getOrganelleAt(self.currentMicrobe.entity, q, r)
    if not (organelle == nil or organelle.name == "nucleus") then -- Don't remove nucleus
        if organelle then
            for _, hex in pairs(organelle._hexes) do
                local s = encodeAxial(hex.q + organelle.position.q, hex.r + organelle.position.r)
                self.occupiedHexes[s]:destroy()
            end
            local storage = organelle:storage()
            self:enqueueAction({
                cost = 10,
                redo = function()
                    MicrobeSystem.removeOrganelle(self.currentMicrobe.entity, storage:get("q", 0), storage:get("r", 0))
                    self.currentMicrobe.sceneNode.transform:touch()
                    self.organelleCount = self.organelleCount - 1
					for _, cytoplasm in pairs(organelle._hexes) do
						local s = encodeAxial(cytoplasm.q + storage:get("q", 0), cytoplasm.r + storage:get("r", 0))
						self.occupiedHexes[s]:destroy()
					end
                end,
                undo = function()
                    local organelle = Organelle.loadOrganelle(storage)
                    MicrobeSystem.addOrganelle(self.currentMicrobe.entity, storage:get("q", 0), storage:get("r", 0), storage:get("rotation", 0), organelle)
                    for _, hex in pairs(organelle._hexes) do
                        self:createHexComponent(hex.q + storage:get("q", 0), hex.r + storage:get("r", 0))
                    end
                    self.organelleCount = self.organelleCount + 1
                end
            })
        end
    end
end

function MicrobeEditor:removeOrganelle()
    local q, r = self:getMouseHex()
    self:removeOrganelleAt(q,r)
end

function MicrobeEditor:addNucleus()
    local nucleusOrganelle = OrganelleFactory.makeOrganelle({["name"]="nucleus", ["q"]=0, ["r"]=0, ["rotation"]=0})
    MicrobeSystem.addOrganelle(self.currentMicrobe.entity, 0, 0, 0, nucleusOrganelle)
end

function MicrobeEditor:loadMicrobe(entityId)
    self.organelleCount = 0
    if self.currentMicrobe ~= nil then
        self.currentMicrobe.entity:destroy()
    end
    self.currentMicrobe = Microbe.new(Entity.new(entityId,
                                                 g_luaEngine.currentGameState.wrapper), true,
                                      g_luaEngine.currentGameState)
    self.currentMicrobe.entity:stealName("working_microbe")
    self.currentMicrobe.sceneNode.transform.orientation = Quaternion.new(Radian.new(Degree(0)),
                                                                         Vector3(0, 0, 1))-- Orientation
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
            speciesName = self.currentMicrobe.microbe.speciesName
            if self.currentMicrobe ~= nil then
                self.currentMicrobe.entity:destroy()
            end
            for _, cytoplasm in pairs(self.occupiedHexes) do
                cytoplasm:destroy()
            end
            
            self.currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(nil, false, 'Editor_Microbe', true)
            self.currentMicrobe = Microbe(self.currentMicrobeEntity, true, g_luaEngine.currentGameState)
            self.currentMicrobe.entity:stealName("working_microbe")
            --self.currentMicrobe.sceneNode.transform.orientation = Quaternion.new(Radian.new(Degree(180)), Vector3(0, 0, 1))-- Orientation
            self.currentMicrobe.sceneNode.transform:touch()
            self.currentMicrobe.microbe.speciesName = speciesName
            self:addNucleus()
            for _, organelle in pairs(self.currentMicrobe.microbe.organelles) do
                for s, hex in pairs(organelle._hexes) do
                    self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
                end
            end
            self.mutationPoints = 100
            self.activeActionName = "cytoplasm"
            Engine:playerData():setActiveCreature(self.currentMicrobe.entity.id, GameState.MICROBE_EDITOR.wrapper)
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
            speciesName = self.currentMicrobe.microbe.speciesName
            self.currentMicrobe.entity:destroy() -- remove the "new" entity that has replaced the previous one
            self.currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(nil, false, 'Editor_Microbe', true)
            self.currentMicrobe = Microbe(self.currentMicrobeEntity, true, g_luaEngine.currentGameState)
            self.currentMicrobe.entity:stealName("working_microbe")
            self.currentMicrobe.sceneNode.transform.orientation = Quaternion.new(Radian(0), Vector3(0, 0, 1))-- Orientation
            self.currentMicrobe.sceneNode.transform:touch()
            self.currentMicrobe.microbe.speciesName = speciesName
            for position,storage in pairs(organelleStorage) do
                local q, r = decodeAxial(position)
                MicrobeSystem.addOrganelle(self.currentMicrobe.entity, storage:get("q", 0), storage:get("r", 0), storage:get("rotation", 0), Organelle.loadOrganelle(storage))
            end
            for _, cytoplasm in pairs(self.occupiedHexes) do
                cytoplasm:destroy()
            end
            for _, organelle in pairs(self.currentMicrobe.microbe.organelles) do
                for s, hex in pairs(organelle._hexes) do
                    self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
                end
            end
            -- no need to add the nucleus manually - it's alreary included in the organelleStorage
            self.mutationPoints = previousMP
            self.organelleCount = previousOrganelleCount
            Engine:playerData():setActiveCreature(self.currentMicrobe.entity.id, GameState.MICROBE_EDITOR.wrapper)
        end
        self:enqueueAction(action)
    else
        -- if there's no microbe yet, it can be safely assumed that this is a generated default microbe when opening the editor for the first time, so it's not an action that should be put into the un/redo-feature
        action.redo()
    end
end
