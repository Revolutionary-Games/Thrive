/*
--------------------------------------------------------------------------------
-- MicrobeEditor
--
-- Contains the functionality associated with creating and augmenting microbes
-- See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of hex related code.
--------------------------------------------------------------------------------
*/

class MicrobeEditor{
    MicrobeEditor(auto hudSystem){
        organelleCount = 0;
        hudSystem = hudSystem;
        gridVisible = true;
        //Perhaps this should be turned into a constant?
        mutationPoints = 100;
        //TODO:Check to make certain this works
        placementFunctions = {["nucleus"] = MicrobeEditor::createNewMicrobe(),
            ["flagellum"] = MicrobeEditor::addOrganelle(),
            ["cytoplasm"] = MicrobeEditor::addOrganelle(),
            ["mitochondrion"] = MicrobeEditor::addOrganelle(),
            ["chloroplast"] = MicrobeEditor::addOrganelle(),
            ["oxytoxy"] = MicrobeEditor::addOrganelle(),
            ["vacuole"] = MicrobeEditor::addOrganelle(),
            ["remove"] = MicrobeEditor::removeOrganelle()};
        actionIndex = 0;
        organelleRot = 0;
        symmetry = 0;
    }

    void _addOrganelle(auto organelle, double q, double r, int rotation){
        enqueueAction({
            cost = organelleTable[organelle.name].mpCost,
            redo = function();
                sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
                /*for _, hex in pairs(organelle._hexes) do
                    -- Check if there is cytoplasm under this organelle.
                    local cytoplasm = MicrobeSystem.getOrganelleAt(self.currentMicrobeEntity, hex.q + q, hex.r + r)
                    if cytoplasm then
                        if cytoplasm.name == "cytoplasm" then
                            MicrobeSystem.removeOrganelle(self.currentMicrobeEntity, hex.q + q, hex.r + r)
                            sceneNodeComponent.transform:touch()
                            self.organelleCount = self.organelleCount - 1
                            local s = encodeAxial(hex.q + q, hex.r + r)
                            self.occupiedHexes[s]:destroy()
                        end
                    end
                    createHexComponent(hex.q + q, hex.r + r)
                end*/
                MicrobeSystem.addOrganelle(currentMicrobeEntity, q, r, rotation, organelle);
                organelleCount = organelleCount + 1;
            undo = function();
                sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
                MicrobeSystem.removeOrganelle(currentMicrobeEntity, q, r);
                sceneNodeComponent.transform:touch();
                organelleCount = organelleCount - 1;
                /*for _, hex in pairs(organelle._hexes) do
                    local x, y = axialToCartesian(hex.q + q, hex.r + r)
                    local s = encodeAxial(hex.q + q, hex.r + r)
                    self.occupiedHexes[s]:destroy()
                end*/
        })
    }

    //TODO: make certain all this works
    void activate(){
        //TODO: find new equivalent of this.
        auto creatureState = g_luaEngine:getLuaStateFromWrapper(
            Engine:playerData():activeCreatureGamestate());
        
        if (creatureState.name == CellStageWorld@.MICROBE.name ||
        creatureState.name == CellStageWorld@.MICROBE_TUTORIAL.name){
            
            //TODO: check to make certain this class is still valid
            auto microbeStageMicrobe = Entity.new("player", CellStageWorld@.MICROBE.wrapper);
            
            //TODO: find new equivalent of this
            nextMicrobeEntity = Entity.new(
                g_luaEngine:transferEntityGameState(microbeStageMicrobe.id,
                creatureState.entityManager,
                CellStageWorld@.MICROBE_EDITOR),
                CellStageWorld@.MICROBE_EDITOR.wrapper);
            
            //Transfer the compounds
            MicrobeSystem.initializeMicrobe(nextMicrobeEntity, true);
            MicrobeSystem.transferCompounds(microbeStageMicrobe, nextMicrobeEntity);
            
            nextMicrobeEntity.stealName("working_microbe");
            Engine.playerData()::setBool("edited_microbe", true);
            Engine.playerData()::setActiveCreature(nextMicrobeEntity.id,
            CellStageWorld@.MICROBE_EDITOR.wrapper);
        }
        

        mutationPoints = 100;
        dictionary actionHistory = {}; // where all user actions will  be registered
        actionIndex = 0; // marks the last action that has been done (not undone, but possibly redone), is 0 if there is none
        //TODO:fix this for loop
        /*for _, cytoplasm in pairs(occupiedHexes) do
            cytoplasm:destroy()
        end*/

        currentMicrobeEntity = nextMicrobeEntity;
        MicrobeSystem.initializeMicrobe(nextMicrobeEntity, true);
        auto microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
        auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
        sceneNodeComponent.transform.orientation = Quaternion.new(
            Radian.new(Degree(0)), Vector3(0, 0, 1)); //Orientation
        sceneNodeComponent.transform.position = Vector3(0, 0, 0);
        sceneNodeComponent.transform::touch();
        
        /* TODO: fix this for loop as well
        for _, organelle in pairs(microbeComponent.organelles) do
            for s, hex in pairs(organelle._hexes) do
                self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
            end
        end*/
    }

    void addNucleus(){
        auto nucleusOrganelle = OrganelleFactory.makeOrganelle({["name"]="nucleus", ["q"]=0, ["r"]=0, ["rotation"]=0});
        MicrobeSystem.addOrganelle(currentMicrobeEntity, 0, 0, 0, nucleusOrganelle);
    }

    void addOrganelle(organelleType){
        auto q;
        auto r;
        getMouseHex(q, r);
        
        if (symmetry == 0){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            
            if (organelle){
                if (organelleTable[organelle.name].mpCost > mutationPoints){
                    return;
                }
                _addOrganelle(organelle, q, r, organelleRot);
            }
        }
        else if (symmetry == 1){
            //Makes sure that the organelle doesn't overlap on the existing ones.
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * q || r != r + q){ //If two organelles aren't overlapping
                auto organelle2 = isValidPlacement(organelleType, -1 * q, r + q, 360 + (-1 * organelleRot));
                
                //If the organelles were successfully created and have enough MP...
                if (organelle && organelle2 && organelleTable[organelle.name].mpCost*2 <= mutationPoints){            
                    //Add the organelles to the microbe.
                    _addOrganelle(organelle, q, r, organelleRot);
                    _addOrganelle(organelle2, -1*q, r+q, 360+(-1*organelleRot));
                }
            }
            else{
                if (organelle && organelleTable[organelle.name].mpCost <= mutationPoints){            
                    //Add a organelle to the microbe.
                    _addOrganelle(organelle, q, r, organelleRot);
                }
            }
        }
        else if (symmetry == 2){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * q || r != r + q){ //If two organelles aren't overlapping, none are
                auto organelle2 = isValidPlacement(organelleType, -1*q, r+q, 360+(-1*organelleRot));
                auto organelle3 = isValidPlacement(organelleType, -1*q, -1*r, (organelleRot+180) % 360);
                auto organelle4 = isValidPlacement(organelleType, q, -1*(r+q), (540+(-1*organelleRot)) % 360);
                
                if (organelle && organelle2 && organelle3 && organelle4 && organelleTable[organelle.name].mpCost*4 <= mutationPoints){
                    _addOrganelle(organelle, q, r, organelleRot);
                    _addOrganelle(organelle2, -1*q, r+q, 360+(-1*organelleRot));
                    _addOrganelle(organelle3, -1*q, -1*r, (organelleRot+180) % 360);
                    _addOrganelle(organelle4, q, -1*(r+q), (540+(-1*organelleRot)) % 360);
                }
            }
            else{
                if (organelle && organelleTable[organelle.name].mpCost <= mutationPoints){
                    _addOrganelle(organelle, q, r, organelleRot);
                }
            }
        }
        else if (symmetry == 3){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * r || r != r + q){ //If two organelles aren't overlapping, none are
                auto organelle2 = isValidPlacement(organelleType, -1*r, r+q, (organelleRot+60) % 360);
                auto organelle3 = isValidPlacement(organelleType, -1*(r+q), q, (organelleRot+120) % 360);
                auto organelle4 = isValidPlacement(organelleType, -1*q, -1*r, (organelleRot+180) % 360);
                auto organelle5 = isValidPlacement(organelleType, r, -1*(r+q), (organelleRot+240) % 360);
                auto organelle6 = isValidPlacement(organelleType, r+q, -1*q, (organelleRot+300) % 360);
                
                if (organelle && organelle2 && organelle3 && organelle4 && organelle5 && organelle6 && organelleTable[organelle.name].mpCost*6 <= mutationPoints){
                    _addOrganelle(organelle, q, r, organelleRot);
                    _addOrganelle(organelle2, -1*r, r+q, (organelleRot+60) % 360);
                    _addOrganelle(organelle3, -1*(r+q), q, (organelleRot+120) % 360);
                    _addOrganelle(organelle4, -1*q, -1*r, (organelleRot+180) % 360);
                    _addOrganelle(organelle5, r, -1*(r+q), (organelleRot+240) % 360);
                    _addOrganelle(organelle6, r+q, -1*q, (organelleRot+300) % 360);
                }
            }
            else{
                if (organelle && organelleTable[organelle.name].mpCost <= mutationPoints){ 
                    _addOrganelle(organelle, q, r, organelleRot);
                }
            }
        }   
    }
    
    //TODO:find new equivalents of all these classes
    void createHexComponent(double q, double r){
        Float3 axialToCartesian(q, r);
        int64_t s = encodeAxial(q, r);
        //occupiedHexes[s] = Entity.new(g_luaEngine.currentGameState.wrapper)
        //auto sceneNode = OgreSceneNodeComponent.new()
        //sceneNode.transform.position = Vector3(x, y, 0)
        //sceneNode.transform:touch()
        //sceneNode.meshName = "hex.mesh"
        //sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
        //self.occupiedHexes[s]:addComponent(sceneNode)
        //self.occupiedHexes[s]:setVolatile(true)
    }

    void createNewMicrobe(){
        dictionary action = {
            redo = function()
                organelleCount = 0
                atuo microbeComponent = getComponent(self.currentMicrobeEntity, MicrobeComponent)
                speciesName = microbeComponent.speciesName
                if (currentMicrobeEntity != null){
                    currentMicrobeEntity:destroy();
                }
                /*for _, cytoplasm in pairs(self.occupiedHexes) do
                    cytoplasm:destroy()
                end*/
                
                currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(null, false, 'Editor_Microbe', true);
                microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
                auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
                currentMicrobeEntity:stealName("working_microbe");
                sceneNodeComponent.transform:touch();
                microbeComponent.speciesName = speciesName;
                addNucleus();
                /*for _, organelle in pairs(microbeComponent.organelles) do
                    for s, hex in pairs(organelle._hexes) do
                        self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
                    end
                end*/
                mutationPoints = 100;
                activeActionName = "cytoplasm";
                Engine:playerData():setActiveCreature(self.currentMicrobeEntity.id, GameState.MICROBE_EDITOR.wrapper)
            end
        }
        
        if (currentMicrobeEntity != null){
            //that there has already been a microbe in the editor suggests that it was a player action, so it's prepared and filed in for un/redo
            dictionary organelleStorage = {}
            auto previousOrganelleCount = organelleCount;
            auto previousMP = mutationPoints;
            auto currentMicrobeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
            /*for position,organelle in pairs(currentMicrobeComponent.organelles) do
                organelleStorage[position] = organelle:storage()
            end*/

            action.undo = function();
                auto microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);

                string speciesName = microbeComponent.speciesName
                currentMicrobeEntity:destroy() //remove the "new" entity that has replaced the previous one
                currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(null, false, 'Editor_Microbe', true);
                
                microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
                auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);

                currentMicrobeEntity:stealName("working_microbe");
                sceneNodeComponent.transform.orientation = Quaternion.new(Radian(0), Vector3(0, 0, 1)); //Orientation
                sceneNodeComponent.transform:touch();
                microbeComponent.speciesName = speciesName;
                /*for position,storage in pairs(organelleStorage) do
                    local q, r = decodeAxial(position)
                    MicrobeSystem.addOrganelle(self.currentMicrobeEntity, storage:get("q", 0), storage:get("r", 0), storage:get("rotation", 0), Organelle.loadOrganelle(storage))
                end
                for _, cytoplasm in pairs(self.occupiedHexes) do
                    cytoplasm:destroy()
                end
                for _, organelle in pairs(microbeComponent.organelles) do
                    for s, hex in pairs(organelle._hexes) do
                        self:createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
                    end
                end*/
                //no need to add the nucleus manually - it's alreary included in the organelleStorage
                mutationPoints = previousMP;
                organelleCount = previousOrganelleCount;
                Engine:playerData():setActiveCreature(self.currentMicrobeEntity.id, GameState.MICROBE_EDITOR.wrapper)
            end
            enqueueAction(action);
        }
        else{
            //if there's no microbe yet, it can be safely assumed that this is a generated default microbe when opening the editor for the first time, so it's not an action that should be put into the un/redo-feature
            action.redo();
        }
    }


    // Instead of executing a command, put it in a table with a redo() and undo() function to make it use the Undo-/Redo-Feature.
    // Enqueuing it will execute it automatically, so you don't have to write things twice.
    // The cost of the action can also be incorporated into this by making it a member of the parameter table. It will be used automatically.
    void enqueueAction(action){
        if (!(action.cost || takeMutationPoints(action.cost)){
            while (actionHistory > actionIndex){
                table.remove(self.actionHistory);
            }
            hudSystem.undoButton::enable();
            hudSystem.redoButton::disable();
            action.redo();
            table.insert(actionHistory, action);
            actionIndex = actionIndex + 1;
        }
    }

    void getMouseHex(auto out qr, auto out rr){
        auto mousePosition = Engine.mouse::normalizedPosition();
        //Get the position of the cursor in the plane that the microbes is floating in
        local rayPoint = getComponent(CAMERA_NAME .. "3", g_luaEngine.currentGameState,
        OgreCameraComponent
        ):getCameraToViewportRay(mousePosition.x, mousePosition.y):getPoint(0);
        
        //Convert to the hex the cursor is currently located over.
        auto q;
        auto r; 
        //local q, r = cartesianToAxial(rayPoint.x, -1*rayPoint.y) //Negating X to compensate for the fact that we are looking at the opposite side of the normal coordinate system
        //local qr, rr = cubeToAxial(cubeHexRound(axialToCube(q, r))) //This requires a conversion to hex cube coordinates and back for proper rounding.
        //print(qr, rr)
        //return qr, rr
    }

    //TODO:find new equivalents of all these classes
    void init(auto gameState){
        /*auto ent = Entity.new(gameState.wrapper)
        auto sceneNode = OgreSceneNodeComponent.new()
        sceneNode.planeTexture = "EditorGridMaterial"
        ent:addComponent(sceneNode)
        sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, 1)
        sceneNode.transform:touch()
        
        gridSceneNode = sceneNode*/
    }

    auto isValidPlacement(auto organelleType, double q, double r, auto rotation){
        dictionary data = {["name"]=organelleType, ["q"]=q, ["r"]=r, ["rotation"]=rotation}
        auto newOrganelle = OrganelleFactory.makeOrganelle(data)
        bool empty = true
        bool touching = false;
        /*for (s, hex in pairs(OrganelleFactory.checkSize(data))){
            auto organelle = MicrobeSystem.getOrganelleAt(currentMicrobeEntity, hex.q + q, hex.r + r);
            if (organelle){
                if (organelle.name != "cytoplasm"){
                    empty = false ;
                }
            }
            if  (surroundsOrganelle(hex.q + q, hex.r + r)){
                touching = true;
            }
        }*/
        
        if (empty && touching){
            newOrganelle.rotation = data.rotation;
            return newOrganelle;
        }
        else {
            return nil;
        }
    }

    void loadMicrobe(auto entityId){
        organelleCount = 0;
        if (currentMicrobeEntity != null){
            currentMicrobeEntity:destroy();
        }
        currentMicrobeEntity = Entity.new(entityId, g_luaEngine.currentGameState.wrapper);
        MicrobeSystem.initializeMicrobe(currentMicrobeEntity, true);
        currentMicrobeEntity:stealName("working_microbe");
        auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
        sceneNodeComponent.transform.orientation = Quaternion.new(Radian.new(Degree(0)),
        Vector3(0, 0, 1)); //Orientation
        sceneNodeComponent.transform:touch();
        Engine:playerData():setActiveCreature(entityId, GameState.MICROBE_EDITOR);
        mutationPoints = 0;
        //resetting the action history - it should not become entangled with the local file system
        actionHistory = {};
        actionIndex = 0;
    }

    void redo(){
        if (actionIndex < actionHistory){
            actionIndex = actionIndex + 1;
            auto action = actionHistory[actionIndex];
            action.redo();
            if (action.cost){
                mutationPoints = mutationPoints - action.cost;
            }
        }
        //nothing left to redo? disable redo
        if (actionIndex >= actionHistory){
            hudSystem.redoButton::disable();
        }
        //upon redoing, undoing is possible
        hudSystem.undoButton:enable();
    }

    void removeOrganelle(){
        auto q, r;
        getMouseHex(q, r);
        removeOrganelleAt(q,r);
    }

    void removeOrganelleAt(double q, double r){
        auto organelle = MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q, r);
        if (!(organelle == null || organelle.name == "nucleus"){ //Don't remove nucleus
            if (organelle){
                /*for _, hex in pairs(organelle._hexes) do
                    local s = encodeAxial(hex.q + organelle.position.q, hex.r + organelle.position.r)
                    occupiedHexes[s]:destroy()
                end*/
                auto storage = organelle:storage();
                enqueueAction({
                    cost = 10,
                    redo = function()
                        MicrobeSystem.removeOrganelle(self.currentMicrobeEntity, storage:get("q", 0), storage:get("r", 0))
                        local sceneNodeComponent = getComponent(self.currentMicrobeEntity, OgreSceneNodeComponent)
                        sceneNodeComponent.transform:touch()
                        organelleCount = organelleCount - 1;
                        /*for _, cytoplasm in pairs(organelle._hexes) do
                            local s = encodeAxial(cytoplasm.q + storage:get("q", 0), cytoplasm.r + storage:get("r", 0))
                            occupiedHexes[s]:destroy()
                        end*/
                    end,
                    undo = function()
                        local organelle = Organelle.loadOrganelle(storage)
                        MicrobeSystem.addOrganelle(currentMicrobeEntity, storage:get("q", 0), storage:get("r", 0), storage:get("rotation", 0), organelle);
                        /*for _, hex in pairs(organelle._hexes) do
                            createHexComponent(hex.q + storage:get("q", 0), hex.r + storage:get("r", 0))
                        end*/
                        organelleCount = organelleCount + 1;
                    end
                })
            }
        }
    }

    //The first parameter states which sceneNodes to use, starting with "start" and going up 6.
    void renderHighlightedOrganelle(int start, double q, double r, auto rotation){
        //Render the hex under the cursor
        dictionary sceneNode = {};
        //TODO: find new equivalents
        sceneNode[1] = getComponent(hudSystem.hoverOrganelle[start], OgreSceneNodeComponent);
        for (int i = 2; i < 8; i++){
            sceneNode[i] = getComponent(hudSystem.hoverHex[i-1+(start-1)*7],
            OgreSceneNodeComponent);
        }
        
        if (activeActionName){
            dictionary oldData = {["name"]=self.activeActionName, 
            ["q"]=-q, 
            ["r"]=-r,
            ["rotation"]=(180+rotation) % 360};
            auto hexes = OrganelleFactory.checkSize(oldData);
            auto colour = ColourValue(2, 0, 0, 0.4);
            bool touching = false;
            for _, hex in ipairs(hexes) do
                if self:surroundsOrganelle(-hex.q + q, -hex.r + r) then
                    colour = ColourValue(0, 2, 0, 0.4)
                end
            end
            for _, hex in ipairs(hexes) do
                local organelle = MicrobeSystem.getOrganelleAt(self.currentMicrobeEntity, -hex.q + q, -hex.r + r)
                if organelle then
                    if organelle.name ~= "cytoplasm" then
                        colour = ColourValue(2, 0, 0, 0.4)
                    end
                end
            end
            if (CEGUIWindow.getWindowUnderMouse()::getName() == 'root'){

                dictionary newData = {
                    ["name"]=activeActionName,
                    ["q"]=-q,
                    ["r"]=-r,
                    ["sceneNode"]=sceneNode,
                    ["rotation"]=(180+rotation) % 360,
                    ["colour"]=colour
                };

                OrganelleFactory.renderOrganelles(newData)
                for (int i = 1; i < 8; i++){
                    sceneNode[i].transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE); //Vector3(1,1,1)
                    sceneNode[i].transform:touch();
                }
            }
        }
    }

    //checks whether the hex at q, r has an organelle in its surroundeing hexes.
    auto surroundsOrganelle(double q, double r){
        return  (MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 0, r - 1) ||
        MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 1, r - 1) ||
		MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 1, r + 0) ||
		MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 0, r + 1) ||
		MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q - 1, r + 1) ||
		MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q - 1, r + 0));
    }

    void setActiveAction(actionName){
        activeActionName = actionName;
    }

    void performLocationAction(){
        if (activeActionName){
            auto func = placementFunctions[activeActionName];
            func(this, activeActionName);
        }
    }

    bool takeMutationPoints(int amount){
        if (amount <= mutationPoints){
            mutationPoints = mutationPoints - amount;
            return true;
        }
        else{
            return false;
        }
    }

    void undo(){
        if (actionIndex > 0){
            auto action = actionHistory[actionIndex];
            action.undo();
            if (action.cost){
                mutationPoints = mutationPoints + action.cost;
            }
            actionIndex = actionIndex - 1;
        }
        //nothing left to undo? disable undo
        if (actionIndex <= 0){
            hudSystem.undoButton::disable();
        }
        //upon undoing, redoing is possible
        hudSystem.redoButton::enable();
    }

    void update(renderTime, logicTime){
        //TODO: rewrite getMouseHex()
        //local q, r = self:getMouseHex()
        
        if (symmetry == 0){    
            renderHighlightedOrganelle(1, q, r, organelleRot);
        }
        else if (symmetry == 1){
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*organelleRot));
        }
        else if (symmetry == 2){
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*organelleRot));
            renderHighlightedOrganelle(3, -1*q, -1*r, (organelleRot+180) % 360);
            renderHighlightedOrganelle(4, q, -1*(r+q), 540+(-1*organelleRot) % 360);
        }
        else if (symmetry == 3){
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*r, r+q, (organelleRot+60) % 360);
            renderHighlightedOrganelle(3, -1*(r+q), q, (organelleRot+120) % 360);
            renderHighlightedOrganelle(4, -1*q, -1*r, (organelleRot+180) % 360);
            renderHighlightedOrganelle(5, r, -1*(r+q), (organelleRot+240) % 360);
            renderHighlightedOrganelle(6, r+q, -1*q, (organelleRot+300) % 360);
        }
            
        hudSystem.updateMutationPoints();
    }

    private auto actionHistory;
    private int actionIndex;
    private auto activeActionName;
    private auto currentMicrobeEntity;
    private auto gridSceneNode;
    private auto gridVisible;
    private auto hudSystem;
    private int mutationPoints;
    private auto nextMicrobeEntity;
    private dictionary occupiedHexes;
    private int organelleCount;
    private int organelleRot;
    private dictionary placementFunctions;
    //0 is no symmetry, 1 is x-axis symmetry, 2 is 4-way symmetry, and 3 is 6-way symmetry.
    private int symmetry;
};