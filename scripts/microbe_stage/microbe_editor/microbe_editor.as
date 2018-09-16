#include "microbe_editor_hud.as"
#include "microbe_operations.as"
/*
////////////////////////////////////////////////////////////////////////////////
// MicrobeEditor
//
// Contains the functionality associated with creating and augmenting microbes
// See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of hex related code.
////////////////////////////////////////////////////////////////////////////////
*/

//
const int BASE_MUTATION_POINTS = 100;

funcdef void EditorActionApply(EditorAction@ action, MicrobeEditor@ editor);

class EditorAction{

    EditorAction(int cost, EditorActionApply@ redo, EditorActionApply@ undo){

        this.cost = cost;
        @this.redo = redo;
        @this.undo = undo;
    }

    int cost;
    EditorActionApply@ redo;
    EditorActionApply@ undo;
}

funcdef void PlacementFunctionType(const string &in actionName);


class MicrobeEditor{

    MicrobeEditor(MicrobeEditorHudSystem@ hud){

        @hudSystem = hud;

        // Register for organelle changing events
        @eventListener = EventListener(null, OnGenericEventCallback(this.onGeneric));
        eventListener.RegisterForEvent("MicrobeEditorOrganelleSelected");

        placementFunctions = {
            {"nucleus", PlacementFunctionType(this.createNewMicrobe)},
            {"flagellum", PlacementFunctionType(this.addOrganelle)},
            {"cytoplasm", PlacementFunctionType(this.addOrganelle)},
            {"mitochondrion", PlacementFunctionType(this.addOrganelle)},
            {"chloroplast", PlacementFunctionType(this.addOrganelle)},
            {"oxytoxy", PlacementFunctionType(this.addOrganelle)},
            {"vacuole", PlacementFunctionType(this.addOrganelle)},
            {"nitrogenfixingplastid", PlacementFunctionType(this.addOrganelle)},
            {"chemoplast", PlacementFunctionType(this.addOrganelle)},
            {"remove", PlacementFunctionType(this.removeOrganelle)}
        };
    }

    //! This is called each time the editor is entered so this needs to properly reset state
    void init(){

        gridSceneNode = hudSystem.world.CreateEntity();
        auto node = hudSystem.world.Create_RenderNode(gridSceneNode);
        node.Scale = Float3(HEX_SIZE, 1, HEX_SIZE);
        node.Marked = true;

        auto plane = hudSystem.world.Create_Plane(gridSceneNode, node.Node,
            "EditorGridMaterial", Ogre::Plane(Ogre::Vector3(0, 1, 0), 0), Float2(100, 100),
            // This is the UV coordinates direction
            Ogre::Vector3(0, 0, 1));

        // Move to an early render queue
        hudSystem.world.GetScene().getRenderQueue().setRenderQueueMode(
            2, Ogre::RenderQueue::FAST);

        plane.GraphicalObject.setRenderQueueGroup(2);

        mutationPoints = BASE_MUTATION_POINTS;
        // organelleCount = 0;
        gridVisible = true;

        actionIndex = 0;
        organelleRot = 0;
        symmetry = 0;

        // Reset to cytoplasm if nothing is selected
        if(activeActionName == ""){

            LOG_INFO("Selecting cytoplasm");

            GenericEvent@ event = GenericEvent("MicrobeEditorOrganelleSelected");
            NamedVars@ vars = event.GetNamedVars();

            vars.AddValue(ScriptSafeVariableBlock("organelle", "cytoplasm"));

            GetEngine().GetEventHandler().CallEvent(event);
        }
    }

    void activate(){
        // auto creatureState = g_luaEngine.getLuaStateFromWrapper(
        //     Engine.playerData().activeCreatureGamestate());

        // if (creatureState.name == CellStageWorld@.MICROBE.name ||
        //     creatureState.name == CellStageWorld@.MICROBE_TUTORIAL.name){

        //     //TODO: check to make certain this class is still valid
        //     auto microbeStageMicrobe = Entity("player", CellStageWorld@.MICROBE.wrapper);

        //     //TODO: find new equivalent of this
        //     nextMicrobeEntity = Entity(
        //         g_luaEngine.transferEntityGameState(microbeStageMicrobe.id,
        //             creatureState.entityManager,
        //             CellStageWorld@.MICROBE_EDITOR),
        //         CellStageWorld@.MICROBE_EDITOR.wrapper);

        //     //Transfer the compounds
        //     MicrobeSystem.initializeMicrobe(nextMicrobeEntity, true);
        //     MicrobeSystem.transferCompounds(microbeStageMicrobe, nextMicrobeEntity);

        //     nextMicrobeEntity.stealName("working_microbe");
        GetThriveGame().playerData().setBool("edited_microbe", true);

        const ObjectID microbe = GetThriveGame().playerData().activeCreature();
        CellStageWorld@ world = GetThriveGame().getCellStage();

        assert(world !is null, "world is null");
        LOG_INFO("Microbe ID is " + microbe);

        // We now just fetch the organelles in the player's creature
        ScriptComponent@ component = world.GetScriptComponentHolder(
            "MicrobeComponent").Find(microbe);

        assert(component !is null, "component retrieve failed");

        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(component);

        assert(microbeComponent !is null, "player MicrobeComponent cast failed");

        // This is failing here
        SpeciesComponent@ playerSpecies = MicrobeOperations::getSpeciesComponent(
            world, microbeComponent.speciesName);

        assert(playerSpecies !is null, "player creature species is invalid");
        LOG_INFO(playerSpecies.name);


        //     Engine.playerData().setActiveCreature(nextMicrobeEntity.id,
        //         CellStageWorld@.MICROBE_EDITOR.wrapper);
        // }

        mutationPoints = BASE_MUTATION_POINTS;
        actionHistory.resize(0);

        actionIndex = 0;
        // //TODO.fix this for loop
        // /*for(_, cytoplasm in pairs(occupiedHexes)){
        // cytoplasm.destroy()
        // }*/

        auto@ templateOrganelles = cast<array<SpeciesStoredOrganelleType@>>(
            playerSpecies.organelles);

        editedMicrobe.organelles.resize(0);

        for(uint i = 0; i < templateOrganelles.length(); ++i){

            editedMicrobe.organelles.insertLast(cast<PlacedOrganelle>(templateOrganelles[i]));
        }

        LOG_INFO("Starting microbe editor with: " + editedMicrobe.organelles.length() +
            " organelles in the microbe");

        // /* TODO: fix this for loop as well
        // for(_, organelle in pairs(microbeComponent.organelles)){
        // for(s, hex in pairs(organelle._hexes)){
        // this.createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
        // }
        // }*/
    }

    void update(int logicTime){

        // TODO: this is really dirty to call this all the time
        hudSystem.updateMutationPoints();

        usedHoverHex = 0;

        int q, r;
        this.getMouseHex(q, r);

        switch (symmetry)
        {
        case 0:
            renderHighlightedOrganelle(1, q, r, organelleRot);
            break;
        case 1:
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*organelleRot));
            break;
        case 2:
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*q, r+q, 360+(-1*organelleRot));
            renderHighlightedOrganelle(3, -1*q, -1*r, (organelleRot+180) % 360);
            renderHighlightedOrganelle(4, q, -1*(r+q), 540+(-1*organelleRot) % 360);
            break;
        case 3:
            renderHighlightedOrganelle(1, q, r, organelleRot);
            renderHighlightedOrganelle(2, -1*r, r+q, (organelleRot+60) % 360);
            renderHighlightedOrganelle(3, -1*(r+q), q, (organelleRot+120) % 360);
            renderHighlightedOrganelle(4, -1*q, -1*r, (organelleRot+180) % 360);
            renderHighlightedOrganelle(5, r, -1*(r+q), (organelleRot+240) % 360);
            renderHighlightedOrganelle(6, r+q, -1*q, (organelleRot+300) % 360);
            break;
        }

        // Show the current microbe
        for(uint i = 0; i < editedMicrobe.organelles.length(); ++i){

            const PlacedOrganelle@ organelle = editedMicrobe.organelles[i];

            const auto basePos = organelle.cartesianPosition;

            // TODO: not sure if this rotation should be here
            auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);

            for(uint a = 0; a < hexes.length(); ++a){

                const Float3 pos = Hex::axialToCartesian(hexes[a].q, hexes[a].r) + basePos;

                ObjectID hex = hudSystem.hoverHex[usedHoverHex++];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                node.Node.setPosition(pos);
                node.Hidden = false;
                node.Marked = true;
            }
        }


    }


    private void _addOrganelle(PlacedOrganelle@ organelle){
        enqueueAction(EditorAction(organelle.organelle.mpCost,
                // redo
                function(EditorAction@ action, MicrobeEditor@ editor){
                    LOG_INFO("Redo called");
                    // sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
                /*for(_, hex in pairs(organelle._hexes)){
                    // Check if there is cytoplasm under this organelle.
                    auto cytoplasm = MicrobeSystem.getOrganelleAt(this.currentMicrobeEntity, hex.q + q, hex.r + r)
                    if(cytoplasm){
                        if(cytoplasm.name == "cytoplasm"){
                            MicrobeSystem.removeOrganelle(this.currentMicrobeEntity, hex.q + q, hex.r + r)
                            sceneNodeComponent.transform.touch()
                            this.organelleCount = this.organelleCount - 1
                            auto s = encodeAxial(hex.q + q, hex.r + r)
                            this.occupiedHexes[s].destroy()
                        }
                    }
                    createHexComponent(hex.q + q, hex.r + r)
                }*/
                    // MicrobeSystem.addOrganelle(currentMicrobeEntity, q, r, rotation, organelle);
                    // ++editor.organelleCount;
                },
                // undo
                function(EditorAction@ action, MicrobeEditor@ editor){
                    LOG_INFO("Undo called");
                    // sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
                    // MicrobeSystem.removeOrganelle(currentMicrobeEntity, q, r);
                    // sceneNodeComponent.transform.touch();
                    // --editor.organelleCount;
                    /*for(_, hex in pairs(organelle._hexes)){
                    local x, y = axialToCartesian(hex.q + q, hex.r + r)
                    auto s = encodeAxial(hex.q + q, hex.r + r)
                    this.occupiedHexes[s].destroy()
                    }*/
                }));
    }

    void addOrganelle(const string &in organelleType){
        int q;
        int r;
        getMouseHex(q, r);

        if (symmetry == 0){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);

            if (organelle !is null){
                if (organelle.organelle.mpCost > mutationPoints){
                    return;
                }

                _addOrganelle(organelle);
            }
        }
        else if (symmetry == 1){
            //Makes sure that the organelle doesn't overlap on the existing ones.
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * q || r != r + q){
                //If two organelles aren't overlapping

                auto organelle2 = isValidPlacement(organelleType, -1 * q, r + q,
                    360 + (-1 * organelleRot));

                //If the organelles were successfully created and have enough MP...
                if (organelle !is null && organelle2 !is null &&
                    organelle.organelle.mpCost * 2 <= mutationPoints)
                {
                    //Add the organelles to the microbe.
                    _addOrganelle(organelle);
                    _addOrganelle(organelle2);
                }
            }
            else{
                if (organelle !is null && organelle.organelle.mpCost <= mutationPoints){
                    //Add a organelle to the microbe.
                    _addOrganelle(organelle);
                }
            }
        }
        else if (symmetry == 2){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * q || r != r + q){ //If two organelles aren't overlapping, none are
                auto organelle2 = isValidPlacement(organelleType, -1*q, r+q,
                    360+(-1*organelleRot));
                auto organelle3 = isValidPlacement(organelleType, -1*q, -1*r,
                    (organelleRot+180) % 360);
                auto organelle4 = isValidPlacement(organelleType, q, -1*(r+q),
                    (540+(-1*organelleRot)) % 360);

                if (organelle !is null && organelle2 !is null && organelle3 !is null &&
                    organelle4 !is null && organelle.organelle.mpCost * 4 <= mutationPoints)
                {
                    _addOrganelle(organelle);
                    _addOrganelle(organelle2);
                    _addOrganelle(organelle3);
                    _addOrganelle(organelle4);
                }
            } else{
                if (organelle !is null && organelle.organelle.mpCost <= mutationPoints){
                    _addOrganelle(organelle);
                }
            }
        }
        else if (symmetry == 3){
            auto organelle = isValidPlacement(organelleType, q, r, organelleRot);
            if (q != -1 * r || r != r + q){ //If two organelles aren't overlapping, none are
                auto organelle2 = isValidPlacement(organelleType, -1*r, r+q,
                    (organelleRot+60) % 360);
                auto organelle3 = isValidPlacement(organelleType, -1*(r+q), q,
                    (organelleRot+120) % 360);
                auto organelle4 = isValidPlacement(organelleType, -1*q, -1*r,
                    (organelleRot+180) % 360);
                auto organelle5 = isValidPlacement(organelleType, r, -1*(r+q),
                    (organelleRot+240) % 360);
                auto organelle6 = isValidPlacement(organelleType, r+q, -1*q,
                    (organelleRot+300) % 360);

                if (organelle !is null && organelle2 !is null && organelle3 !is null &&
                    organelle4 !is null && organelle5 !is null && organelle6 !is null &&
                    organelle.organelle.mpCost * 6 <= mutationPoints)
                {
                    _addOrganelle(organelle);
                    _addOrganelle(organelle2);
                    _addOrganelle(organelle3);
                    _addOrganelle(organelle4);
                    _addOrganelle(organelle5);
                    _addOrganelle(organelle6);
                }
            } else{
                if (organelle !is null && organelle.organelle.mpCost <= mutationPoints){
                    _addOrganelle(organelle);
                }
            }
        }
    }

    //TODO.find new equivalents of all these classes
    void createHexComponent(double q, double r){
        Hex::axialToCartesian(q, r);
        int64 s = Hex::encodeAxial(q, r);
        //occupiedHexes[s] = Entity(g_luaEngine.currentGameState.wrapper)
        //auto sceneNode = OgreSceneNodeComponent()
        //sceneNode.transform.position = Vector3(x, y, 0)
        //sceneNode.transform.touch()
        //sceneNode.meshName = "hex.mesh"
        //sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
        //this.occupiedHexes[s].addComponent(sceneNode)
        //this.occupiedHexes[s].setVolatile(true)
    }

    // This can only work when creating a new cell so put this inside the new method once done
    // void addNucleus(){
    //     auto nucleusOrganelle = OrganelleFactory.makeOrganelle({["name"]="nucleus", ["q"]=0, ["r"]=0, ["rotation"]=0});
    //     MicrobeSystem.addOrganelle(currentMicrobeEntity, 0, 0, 0, nucleusOrganelle);
    // }


    void createNewMicrobe(const string &in){
    mutationPoints = BASE_MUTATION_POINTS;
    // organelleCount = 0;
    EditorAction@ action = EditorAction(0,
        // redo
        function(EditorAction@ action, MicrobeEditor@ editor){
            // auto microbeComponent = getComponent(this.currentMicrobeEntity, MicrobeComponent);
            // speciesName = microbeComponent.speciesName;
            // if (currentMicrobeEntity != null){
            //     currentMicrobeEntity.destroy();
            // }
            /*for(_, cytoplasm in pairs(this.occupiedHexes)){
            cytoplasm.destroy()
            }*/

            // currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(null, false, 'Editor_Microbe', true);
            // microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
            // auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
            // currentMicrobeEntity.stealName("working_microbe");
            // sceneNodeComponent.transform.touch();
            // microbeComponent.speciesName = speciesName;
            // addNucleus();
            // /*for(_, organelle in pairs(microbeComponent.organelles)){
            // for(s, hex in pairs(organelle._hexes)){
            // this.createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
            // }
            // }*/
             //activeActionName = "cytoplasm";
            // Engine.playerData().setActiveCreature(this.currentMicrobeEntity.id, GameState.MICROBE_EDITOR.wrapper);
        },
        null);

        if (microbeHasBeenInEditor){

            //that there has already been a microbe in the editor
            //suggests that it was a player action, so it's prepared
            //and filed in for un/redo
            microbeHasBeenInEditor = true;

            LOG_WRITE("TODO: fix this part about already been stuff");

            dictionary organelleStorage = {};
            // auto previousOrganelleCount = organelleCount;
            auto previousMP = mutationPoints;
            // auto currentMicrobeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
            /*for(position,organelle in pairs(currentMicrobeComponent.organelles)){
                organelleStorage[position] = organelle.storage()
            }*/

            @action.undo = function(EditorAction@ action, MicrobeEditor@ editor){
                // auto microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);

                // string speciesName = microbeComponent.speciesName;
                // currentMicrobeEntity.destroy(); //remove the "new" entity that has replaced the previous one
                // currentMicrobeEntity = MicrobeSystem.createMicrobeEntity(null, false, 'Editor_Microbe', true);

                // microbeComponent = getComponent(currentMicrobeEntity, MicrobeComponent);
                // auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);

                // currentMicrobeEntity.stealName("working_microbe");
                // sceneNodeComponent.transform.orientation = Quaternion(Radian(0), Vector3(0, 0, 1)); //Orientation
                // sceneNodeComponent.transform.touch();
                // microbeComponent.speciesName = speciesName;
                /*for(position,storage in pairs(organelleStorage)){
                    local q, r = decodeAxial(position);
                    MicrobeSystem.addOrganelle(this.currentMicrobeEntity, storage.get("q", 0), storage.get("r", 0), storage.get("rotation", 0), Organelle.loadOrganelle(storage))
                }
                for(_, cytoplasm in pairs(this.occupiedHexes)){
                    cytoplasm.destroy()
                }
                for(_, organelle in pairs(microbeComponent.organelles)){
                    for(s, hex in pairs(organelle._hexes)){
                        this.createHexComponent(hex.q + organelle.position.q, hex.r + organelle.position.r)
                    }
                }*/
                //no need to add the nucleus manually - it's alreary included in the organelleStorage
                // mutationPoints = previousMP;
                // organelleCount = previousOrganelleCount;
            //     Engine.playerData().setActiveCreature(this.currentMicrobeEntity.id, GameState.MICROBE_EDITOR.wrapper)
            };
            enqueueAction(action);

        } else{
            //if there's no microbe yet, it can be safely assumed that
            //this is a generated default microbe when opening the
            //editor for the first time, so it's not an action that
            //should be put into the un/redo-feature
            action.redo(action, this);
        }
    }

    void setRedoButtonStatus(bool enabled)
    {
        GenericEvent@ event = GenericEvent("EditorRedoButtonStatus");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("enabled", enabled));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    void setUndoButtonStatus(bool enabled)
    {
        GenericEvent@ event = GenericEvent("EditorUndoButtonStatus");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("enabled", enabled));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    // Instead of executing a command, put it in a table with a redo()
    // and undo() void to make it use the Undo-/Redo-Feature. Do
    // Enqueuing it will execute it automatically, so you don't have
    // to write things twice.  The cost of the action can also be
    // incorporated into this by making it a member of the parameter
    // table. It will be used automatically.
    void enqueueAction(EditorAction@ action){
        if(action.cost != 0)
            if(!takeMutationPoints(action.cost))
                return;

        if(actionHistory.length() > uint(actionIndex + 1)){

            actionHistory.resize(actionIndex + 1);
        }

        setUndoButtonStatus(true);
        setRedoButtonStatus(false);

        action.redo(action, this);
        actionHistory.insertLast(action);
        actionIndex++;
    }

    //! \todo Clean this up
    void getMouseHex(int &out qr, int &out rr){

        float x, y;
        GetEngine().GetWindowEntity().GetNormalizedRelativeMouse(x, y);


        const auto ray = hudSystem.world.CastRayFromCamera(x, y);

        float distance;
        bool intersects = ray.intersects(Ogre::Plane(Ogre::Vector3(0, 1, 0), 0), distance);

        //Get the position of the cursor in the plane that the microbes is floating in
        const auto rayPoint = ray.getPoint(distance);

        //Convert to the hex the cursor is currently located over.

        //Negating X to compensate for the fact that we are looking at
        //the opposite side of the normal coordinate system
        const auto tmp1 = Hex::cartesianToAxial(rayPoint.x, -1*rayPoint.z);
        //This requires a conversion to hex cube coordinates and back
        //for proper rounding.
        const auto qrrr = Hex::cubeToAxial(Hex::cubeHexRound(
                Float3(Hex::axialToCube(tmp1.X, tmp1.Y))));
        qr = qrrr.X;
        rr = qrrr.Y;

        // LOG_WRITE("Mouse hex: " + qr + ", " + rr);
    }


    PlacedOrganelle@ isValidPlacement(const string &in organelleType, int q, int r,
        int rotation)
    {
        auto newOrganelle = PlacedOrganelle(getOrganelleDefinition(organelleType), q, r,
            rotation);

        bool empty = true;
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
            return newOrganelle;
        }
        else {
            return null;
        }
    }

    void loadMicrobe(int entityId){
        mutationPoints = 0;
        // organelleCount = 0;
        //     if (currentMicrobeEntity != null){
        //         currentMicrobeEntity.destroy();
        //     }
        //     currentMicrobeEntity = Entity(entityId, g_luaEngine.currentGameState.wrapper);
        //     MicrobeSystem.initializeMicrobe(currentMicrobeEntity, true);
        //     currentMicrobeEntity.stealName("working_microbe");
        //     auto sceneNodeComponent = getComponent(currentMicrobeEntity, OgreSceneNodeComponent);
        //     sceneNodeComponent.transform.orientation = Quaternion(Radian(Degree(0)),
        //     Vector3(0, 0, 1)); //Orientation
        //     sceneNodeComponent.transform.touch();
        //     Engine.playerData().setActiveCreature(entityId, GameState.MICROBE_EDITOR);
        //     //resetting the action history - it should not become entangled with the local file system
        //     actionHistory = {};
        //     actionIndex = 0;
    }

    void redo()
    {
        if (actionIndex < int(actionHistory.length())){

            actionIndex += 1;

            auto action = actionHistory[actionIndex];
            action.redo(action, this);

            if (action.cost > 0){
                mutationPoints -= action.cost;
            }
        }

        //nothing left to redo? disable redo
        if (actionIndex >= int(actionHistory.length())){

            setRedoButtonStatus(false);
        }

        //upon redoing, undoing is possible
        setUndoButtonStatus(true);
    }

    void undo()
    {
        if (actionIndex >= 0){
            auto action = actionHistory[actionIndex];

            action.undo(action, this);

            if (action.cost > 0){
                mutationPoints += action.cost;
            }
            actionIndex -= 1;

            //upon undoing, redoing is possible
            setRedoButtonStatus(true);
        }

        //nothing left to undo? disable undo
        if (actionIndex < 0){
            setUndoButtonStatus(false);
        }
    }

    // Don't call directly. Should be used through actions
    void removeOrganelle(const string &in){
        int q, r;
        getMouseHex(q, r);
        removeOrganelleAt(q,r);
    }

    void removeOrganelleAt(int q, int r){
        // auto organelle = MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q, r);

        // //Don't remove nucleus
        // if(organelle is null || organelle.organelle.name == "nucleus")
        //     return;

        // /*for(_, hex in pairs(organelle._hexes)){
        // auto s = encodeAxial(hex.q + organelle.position.q, hex.r + organelle.position.r)
        // occupiedHexes[s].destroy()
        // }*/
        // auto storage = organelle.storage();
        // enqueueAction(EditorAction(10,
        //         // redo
        //         function(EditorAction@ action, MicrobeEditor@ editor){
        //             MicrobeSystem.removeOrganelle(this.currentMicrobeEntity, storage.get("q", 0), storage.get("r", 0));
        //             auto sceneNodeComponent = getComponent(this.currentMicrobeEntity, OgreSceneNodeComponent);
        //             sceneNodeComponent.transform.touch();
        //             organelleCount = organelleCount - 1;
        //             /*for(_, cytoplasm in pairs(organelle._hexes)){
        //             auto s = encodeAxial(cytoplasm.q + storage.get("q", 0), cytoplasm.r + storage.get("r", 0));
        //             occupiedHexes[s].destroy();
        //             }*/;
        //         },
        //         // undo
        //         function(EditorAction@ action, MicrobeEditor@ editor){
        //             auto organelle = Organelle.loadOrganelle(storage);
        //             MicrobeSystem.addOrganelle(currentMicrobeEntity, storage.get("q", 0), storage.get("r", 0), storage.get("rotation", 0), organelle);
        //             /*for(_, hex in pairs(organelle._hexes)){
        //             createHexComponent(hex.q + storage.get("q", 0), hex.r + storage.get("r", 0));
        //             }*/
        //             organelleCount = organelleCount + 1;
        //         }
        //     ));
    }


    //The first parameter states which sceneNodes to use, starting with "start" and going up 6.
    void renderHighlightedOrganelle(int start, double q, double r, int rotation){
        //Render the hex under the cursor
        dictionary sceneNode = {};

        //TODO: find new equivalents
        // sceneNode[1] = getComponent(hudSystem.hoverOrganelle[start], OgreSceneNodeComponent);
        // for (int i = 2; i < 8; i++){
        //     sceneNode[i] = getComponent(hudSystem.hoverHex[i-1+(start-1)*7],
        //     OgreSceneNodeComponent);
        // }

        if (activeActionName != ""){
           /* dictionary oldData = {["name"]=this.activeActionName,
             ["q"]=-q,
             ["r"]=-r,
             ["rotation"]=(180+rotation) % 360};
             auto hexes = OrganelleFactory.checkSize(oldData);
             auto colour = ColourValue(2, 0, 0, 0.4);
             bool touching = false;
             for(_, hex in ipairs(hexes)){
                 if(this.surroundsOrganelle(-hex.q + q, -hex.r + r)){
                     colour = ColourValue(0, 2, 0, 0.4)
                 }
           } */

            // If not hovering over an organelle render the to-be-placed organelle
            Organelle@ toBePlacedOrganelle = getOrganelleDefinition(activeActionName);

            assert(toBePlacedOrganelle !is null, "invalid action name in microbe editor");

            auto hexes = toBePlacedOrganelle.getRotatedHexes(rotation);

            for(uint i = 0; i < hexes.length(); ++i){

                double posQ = hexes[i].q + q;
                double posR = hexes[i].r + r;

                const Float3 pos = Hex::axialToCartesian(posQ, posR);

                ObjectID hex = hudSystem.hoverHex[usedHoverHex++];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                node.Node.setPosition(pos);
                node.Hidden = false;
                node.Marked = true;
            }



             // for(_, hex in ipairs(hexes)){
             //     auto organelle = MicrobeSystem.getOrganelleAt(this.currentMicrobeEntity, -hex.q + q, -hex.r + r)
             //     if(organelle){
             //         if(organelle.name ~= "cytoplasm"){
             //            colour = ColourValue(2, 0, 0, 0.4)
             //         }
             //     }
             // }
        //if (CEGUIWindow.getWindowUnderMouse().getName() == 'root'){

        //         dictionary newData = {
        //             ["name"]=activeActionName,
        //             ["q"]=-q,
        //             ["r"]=-r,
        //             ["sceneNode"]=sceneNode,
        //             ["rotation"]=(180+rotation) % 360,
        //             ["colour"]=colour
        //         };

        //         OrganelleFactory.renderOrganelles(newData)
        //         for (int i = 1; i < 8; i++){
        //             sceneNode[i].transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE); //Vector3(1,1,1)
        //             sceneNode[i].transform.touch();
        //         }
        //     }
        //}
        }
    }

    //checks whether the hex at q, r has an organelle in its surroundeing hexes.
    bool surroundsOrganelle(int q, int r){
    //     return  (MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 0, r - 1) ||
    //     MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 1, r - 1) ||
    // MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 1, r + 0) ||
    // MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q + 0, r + 1) ||
    // MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q - 1, r + 1) ||
    // MicrobeSystem.getOrganelleAt(currentMicrobeEntity, q - 1, r + 0));
        return false;
    }

    void setActiveAction(const string &in actionName){
        activeActionName = actionName;
    }

    void performLocationAction(){
        if (activeActionName.length() > 0){
            auto func = cast<PlacementFunctionType>(placementFunctions[activeActionName]);
            func(activeActionName);
        }
    }

    bool takeMutationPoints(int amount){
        if (amount <= mutationPoints){
            mutationPoints = mutationPoints - amount;
            return true;
        } else{
            return false;
        }
    }

    int getMutationPoints() const
    {
        return mutationPoints;
    }

    int onGeneric(GenericEvent@ event){

        auto type = event.GetType();

        if(type == "MicrobeEditorOrganelleSelected")
        {
            NamedVars@ vars = event.GetNamedVars();

            activeActionName = string(vars.GetSingleValueByName("organelle"));
            LOG_INFO("Editor action is now: " + activeActionName);
            return 1;
        }

        LOG_ERROR("Microbe editor got unknown event: " + type);
        return -1;
    }

    //! This is used to keep track of used hover organelles
    private uint usedHoverHex = 0;

    // where all user actions will  be registered
    private array<EditorAction@> actionHistory;
    // marks the last action that has been done (not undone, but
    // possibly redone), is 0 if there is none
    private int actionIndex;
    private string activeActionName;
    // This is the container that has the edited organelles in it.
    // This is populated when entering and used to update the player's species template on exit
    private OrganelleContainer editedMicrobe;

    // private array<SpeciesStoredOrganelleType@> currentOrganelles;
    // private int organelleCount = 0;

    private ObjectID gridSceneNode;
    private bool gridVisible;
    private MicrobeEditorHudSystem@ hudSystem;

    private int mutationPoints;
    // private auto nextMicrobeEntity;
    // private dictionary occupiedHexes;

    private int organelleRot;

    private dictionary placementFunctions;

    //0 is no symmetry, 1 is x-axis symmetry, 2 is 4-way symmetry, and 3 is 6-way symmetry.
    // TODO: change to enum
    private int symmetry;

    private bool microbeHasBeenInEditor = false;

    private EventListener@ eventListener;
};


// //! Class for handling drawing hexes in the editor for organelles
// class OrganelleHexDrawer{

//     // Draws the hexes and uploads the models in the editor
//     void renderOrganelles(CellStageWorld@ world, EditorPlacedOrganelle@ data){
//         if(data.name == "remove")
//             return;

//         // Wouldn't it be easier to just use normal PlacedOrganelle and just move it around
//         assert(false, "TODO: use actual PlacedOrganelles to position things");

//         // //Getting the list hexes occupied by this organelle.
//         // if(data.hexes is null){

//         //     // The list needs to be rotated //
//         //     int times = data.rotation / 60;

//         //     //getting the hex table of the organelle rotated by the angle
//         //     @data.hexes = rotateHexListNTimes(organelle.getHexes(), times);
//         // }

//         // occupiedHexList = OrganelleFactory.checkSize(data);

//         // //Used to get the average x and y values.
//         // float xSum = 0;
//         // float ySum = 0;

//         // //Rendering a cytoplasm in each of those hexes.
//         // //Note: each scenenode after the first one is considered a cytoplasm by the
//         // // engine automatically.
//         // // TODO: verify the above claims

//         // Float2 organelleXY = Hex::axialToCartesian(data.q, data.r);

//         // uint i = 2;
//         // for(uint listIndex = 0; listIndex < data.hexes.length(); ++listIndex){

//         //     const Hex@ hex = data.hexes[listIndex];


//         //     Float2 hexXY = Hex::axialToCartesian(hex.q, hex.r);

//         //     float x = organelleXY.X + hexX;
//         //     float y = organelleYY.Y + hexY;
//         //     xSum = xSum + x;
//         //     ySum = ySum + y;
//         //     i = i + 1;
//         // }

//         // //Getting the average x and y values to render the organelle mesh in the middle.
//         // local xAverage = xSum / (i - 2); // Number of occupied hexes = (i - 2).
//         // local yAverage = ySum / (i - 2);

//         // //Rendering the organelle mesh (if it has one).
//         // auto mesh = data.organelle.organelle.mesh;
//         // if(mesh ~= nil) {

//         //     // Create missing components to place the mesh in etc.
//         //     if(world.GetComponent_

//         //     data.sceneNode[1].meshName = mesh;
//         //     data.sceneNode[1].transform.position = Vector3(-xAverage, -yAverage, 0);
//         //     data.sceneNode[1].transform.orientation = Quaternion.new(
//         //         Radian.new(Degree(data.rotation)), Vector3(0, 0, 1));
//         // }
//     }
// }

