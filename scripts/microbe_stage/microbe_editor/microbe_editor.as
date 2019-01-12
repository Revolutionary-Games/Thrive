#include "microbe_editor_hud.as"
#include "microbe_operations.as"
#include "organelle_placement.as"
/*
////////////////////////////////////////////////////////////////////////////////
// MicrobeEditor
//
// Contains the functionality associated with creating and augmenting microbes
// See http://www.redblobgames.com/grids/hexagons/ for mathematical basis of hex related code.
////////////////////////////////////////////////////////////////////////////////
*/


funcdef void EditorActionApply(EditorAction@ action, MicrobeEditor@ editor);

class EditorAction{

    EditorAction(int cost, EditorActionApply@ redo, EditorActionApply@ undo)
    {
        this.cost = cost;
        @this.redo = redo;
        @this.undo = undo;
    }

    int cost;
    EditorActionApply@ redo;
    EditorActionApply@ undo;
    dictionary data;
}

funcdef void PlacementFunctionType(const string &in actionName);


class MicrobeEditor{

    MicrobeEditor(MicrobeEditorHudSystem@ hud)
    {
        @hudSystem = hud;

        // Register for organelle changing events
        @eventListener = EventListener(null, OnGenericEventCallback(this.onGeneric));
        eventListener.RegisterForEvent("MicrobeEditorOrganelleSelected");
        eventListener.RegisterForEvent("SymmetryClicked");
        eventListener.RegisterForEvent("MicrobeEditorClicked");
        eventListener.RegisterForEvent("MicrobeEditorExited");
        eventListener.RegisterForEvent("PressedRightRotate");
        eventListener.RegisterForEvent("PressedLeftRotate");
        eventListener.RegisterForEvent("NewCellClicked");
        eventListener.RegisterForEvent("RedoClicked");
        eventListener.RegisterForEvent("UndoClicked");

        placementFunctions = {
            {"nucleus", PlacementFunctionType(this.createNewMicrobe)},
            {"nucleus", PlacementFunctionType(this.addOrganelle)},
            {"flagellum", PlacementFunctionType(this.addOrganelle)},
            {"cytoplasm", PlacementFunctionType(this.addOrganelle)},
            {"mitochondrion", PlacementFunctionType(this.addOrganelle)},
            {"chloroplast", PlacementFunctionType(this.addOrganelle)},
            {"oxytoxy", PlacementFunctionType(this.addOrganelle)},
            {"vacuole", PlacementFunctionType(this.addOrganelle)},
            {"nitrogenfixingplastid", PlacementFunctionType(this.addOrganelle)},
            {"chemoplast", PlacementFunctionType(this.addOrganelle)},
            {"chromatophors", PlacementFunctionType(this.addOrganelle)},
            {"metabolosome", PlacementFunctionType(this.addOrganelle)},
            {"chemoSynthisizingProtiens", PlacementFunctionType(this.addOrganelle)},
            {"remove", PlacementFunctionType(this.removeOrganelle)}
        };
    }

    //! This is called each time the editor is entered so this needs to properly reset state
    void init()
    {
        updateGuiButtonStatus(checkIsNucleusPresent());
        gridSceneNode = hudSystem.world.CreateEntity();
        auto node = hudSystem.world.Create_RenderNode(gridSceneNode);
        node.Scale = Float3(HEX_SIZE, 1, HEX_SIZE);
        // Move to line up with the hexes
        node.Node.setPosition(Float3(0.72f, 0, 0.18f));
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
        setUndoButtonStatus(false);
        setRedoButtonStatus(false);
        //Check generation and set it here.
        hudSystem.updateGeneration();

    }

    void activate()
    {
        GetThriveGame().playerData().setBool("edited_microbe", true);

        SpeciesComponent@ playerSpecies = MicrobeOperations::getSpeciesComponent(
            GetThriveGame().getCellStage(), GetThriveGame().playerData().activeCreature());

        assert(playerSpecies !is null, "didn't find edited species");
        LOG_INFO("Edited species is " + playerSpecies.name);

        // We now just fetch the organelles in the player's species
        mutationPoints = BASE_MUTATION_POINTS;
        actionHistory.resize(0);

        actionIndex = 0;

        // Get the species organelles to be edited
        auto@ templateOrganelles = cast<array<SpeciesStoredOrganelleType@>>(
            playerSpecies.organelles);

        editedMicrobe.resize(0);
        for(uint i = 0; i < templateOrganelles.length(); ++i){
            editedMicrobe.insertLast(cast<PlacedOrganelle>(templateOrganelles[i]));
        }

        LOG_INFO("Starting microbe editor with: " + editedMicrobe.length() +
            " organelles in the microbe");

        // Reset to cytoplasm if nothing is selected
        if(activeActionName == ""){
            LOG_INFO("Selecting cytoplasm");

            GenericEvent@ event = GenericEvent("MicrobeEditorOrganelleSelected");
            NamedVars@ vars = event.GetNamedVars();

            vars.AddValue(ScriptSafeVariableBlock("organelle", "cytoplasm"));

            GetEngine().GetEventHandler().CallEvent(event);
        }
    }

    void update(int logicTime)
    {
        // TODO: this is really dirty to call this all the time
        // This updates the mutation point counts to the GUI
        hudSystem.updateMutationPoints();
        hudSystem.updateSize();

        usedHoverHex = 0;

        if(activeActionName != ""){
            int q, r;
            this.getMouseHex(q, r);

            // Can place stuff at all?
            isPlacementProbablyValid = isValidPlacement(activeActionName, q, r, organelleRot);

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
        }

        // Show the current microbe
        for(uint i = 0; i < editedMicrobe.length(); ++i){

            const PlacedOrganelle@ organelle = editedMicrobe[i];
            auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);

            for(uint a = 0; a < hexes.length(); ++a){

                const Float3 pos = Hex::axialToCartesian(hexes[a].q + organelle.q,
                    hexes[a].r + organelle.r);

                bool duplicate = false;

                // Skip if there is something here already
                for(uint alreadyUsed = 0; alreadyUsed < usedHoverHex; ++alreadyUsed){
                    ObjectID hex = hudSystem.hoverHex[alreadyUsed];
                    auto node = hudSystem.world.GetComponent_RenderNode(hex);
                    if(pos == node.Node.getPosition()){
                        duplicate = true;
                        break;
                    }
                }

                if(duplicate)
                    continue;

                ObjectID hex = hudSystem.hoverHex[usedHoverHex++];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                node.Node.setPosition(pos);
                node.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(90),
                        Ogre::Vector3(0, 1, 0)) * Ogre::Quaternion(Ogre::Degree(180),
                            Ogre::Vector3(0, 0, 1)));
                node.Hidden = false;
                node.Marked = true;

                auto model = hudSystem.world.GetComponent_Model(hex);
                model.GraphicalObject.setDatablockOrMaterialName("EditorHexMaterial");
            }
        }
    }


    private void _addOrganelle(PlacedOrganelle@ organelle)
    {
        // 1 - you put nucleus but you already have it
        // 2 - you put organelle that need nucleus and you don't have it
        if((organelle.organelle.name == "nucleus" && checkIsNucleusPresent()) ||
            (organelle.organelle.prokaryoteChance == 0 && !checkIsNucleusPresent()) && organelle.organelle.chanceToCreate != 0 )
                return;

        EditorAction@ action = EditorAction(organelle.organelle.mpCost,
            // redo
            function(EditorAction@ action, MicrobeEditor@ editor){

                PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                // Need to set this here to make sure the pointer is updated
                @action.data["organelle"]=organelle;
                // Check if there is cytoplasm under this organelle.
                auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                for(uint i = 0; i < hexes.length(); ++i){
                    int posQ = int(hexes[i].q) + organelle.q;
                    int posR = int(hexes[i].r) + organelle.r;

                    auto organelleHere = OrganellePlacement::getOrganelleAt(
                        editor.editedMicrobe, Int2(posQ, posR));

                    if(organelleHere !is null &&
                        organelleHere.organelle.name == "cytoplasm")
                    {
                        LOG_INFO("replaced cytoplasm");
                        OrganellePlacement::removeOrganelleAt(editor.editedMicrobe,
                            Int2(posQ, posR));
                    }
                }

                LOG_INFO("Placing organelle '" + organelle.organelle.name + "' at: " +
                    organelle.q + ", " + organelle.r);
                editor.editedMicrobe.insertLast(organelle);

                // send to gui current status of cell
                editor.updateGuiButtonStatus(editor.checkIsNucleusPresent());
            },
            // undo
            function(EditorAction@ action, MicrobeEditor@ editor){
                // TODO: this doesn't restore cytoplasm
                LOG_INFO("Undo called");
                const PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                for(uint c = 0; c < hexes.length(); ++c){
                    int posQ = int(hexes[c].q) + organelle.q;
                    int posR = int(hexes[c].r) + organelle.r;
                    auto organelleHere = OrganellePlacement::getOrganelleAt(
                        editor.editedMicrobe, Int2(posQ, posR));
                    if(organelleHere !is null){
                        OrganellePlacement::removeOrganelleAt(editor.editedMicrobe,
                            Int2(posQ, posR));
                    }

                }

                // send to gui current status of cell
                editor.updateGuiButtonStatus(editor.checkIsNucleusPresent());
            });

        @action.data["organelle"] = organelle;

        enqueueAction(action);
    }

    void addOrganelle(const string &in organelleType)
    {
        int q;
        int r;
        getMouseHex(q, r);
        switch (symmetry){
            case 0: {
                if (isValidPlacement(organelleType, q, r, organelleRot)){
                    auto organelle = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        q, r, organelleRot);

                    if (organelle.organelle.mpCost > mutationPoints){
                        return;
                    }

                    _addOrganelle(organelle);
                }
            }
            break;
            case 1: {
                if (isValidPlacement(organelleType, q, r, organelleRot)){
                    auto organelle = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        q, r, organelleRot);
                    if (organelle.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle);
                 }
                if ((q != -1 * q || r != r + q)){
                    if (isValidPlacement(organelleType,-1*q, r+q, 360+(-1*organelleRot))){
                        auto organelle2 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                            -1*q, r+q, 360+(-1*organelleRot));
                        if (organelle2.organelle.mpCost > mutationPoints){
                            return;
                        }
                        _addOrganelle(organelle2);
                    }
                }
            }
            break;
            case 2: {
                if (isValidPlacement(organelleType, q, r, organelleRot)){
                    auto organelle = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        q, r, organelleRot);

                    if (organelle.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle);
                }
                if ((q != -1 * q || r != r + q)){
                if (isValidPlacement(organelleType,-1*q, r+q, 360+(-1*organelleRot))){
                    auto organelle2 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        -1*q, r+q, 360+(-1*organelleRot));
                    if (organelle2.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle2);
                 }
                if (isValidPlacement(organelleType, -1*q, -1*r,(organelleRot+180) % 360)){
                    auto organelle3 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        -1*q, -1*r,(organelleRot+180) % 360);
                    if (organelle3.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle3);
                 }
                if (isValidPlacement(organelleType, q, -1*(r+q),
                    (540+(-1*organelleRot)) % 360)){
                    auto organelle4 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        q, -1*(r+q),(540+(-1*organelleRot)) % 360);
                    if (organelle4.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle4);
                 }
                 }
            }
            break;
            case 3: {
                if (isValidPlacement(organelleType, q, r, organelleRot)){
                    auto organelle = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        q, r, organelleRot);

                    if (organelle.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle);
                }
                if ((q != -1 * q || r != r + q)){
                if (isValidPlacement(organelleType, -1*r, r+q,(organelleRot+60) % 360)){
                    auto organelle2 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        -1*r, r+q,(organelleRot+60) % 360);
                    if (organelle2.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle2);
                 }
                if (isValidPlacement(organelleType, -1*(r+q), q,(organelleRot+120) % 360)){
                    auto organelle3 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                         -1*(r+q), q,(organelleRot+120) % 360);
                    if (organelle3.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle3);
                 }
                if (isValidPlacement(organelleType, -1*q, -1*r,(organelleRot+180) % 360)){
                    auto organelle4 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        -1*q, -1*r,(organelleRot+180) % 360);
                    if (organelle4.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle4);
                 }
                if (isValidPlacement(organelleType, r, -1*(r+q),(organelleRot+240) % 360)){
                    auto organelle5 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        r, -1*(r+q),(organelleRot+240) % 360);
                    if (organelle5.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle5);
                 }
                if (isValidPlacement(organelleType, r+q, -1*q,(organelleRot+300) % 360)){
                    auto organelle6 = PlacedOrganelle(getOrganelleDefinition(organelleType),
                        r+q, -1*q,(organelleRot+300) % 360);
                    if (organelle6.organelle.mpCost > mutationPoints){
                        return;
                    }
                    _addOrganelle(organelle6);
                 }
                 }
            }
            break;
        }
    }

    void createNewMicrobe(const string &in)
    {
        // organelleCount = 0;
        auto previousMP = mutationPoints;
        // Copy current microbe to a new array
        array<PlacedOrganelle@> oldEditedMicrobe = editedMicrobe;

        EditorAction@ action = EditorAction(0,
            // redo
            function(EditorAction@ action, MicrobeEditor@ editor){
                // Delete the organelles (all except the nucleus) and set mutation points
                // (just undoing and redoing the cost like other actions doesn't work in this case due to its nature)
                editor.setMutationPoints(BASE_MUTATION_POINTS);
                for(uint i = editor.editedMicrobe.length()-1; i > 0; --i){
                    const PlacedOrganelle@ organelle = editor.editedMicrobe[i];
                    auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                    for(uint c = 0; c < hexes.length(); ++c){
                        int posQ = int(hexes[c].q) + organelle.q;
                        int posR = int(hexes[c].r) + organelle.r;
                        auto organelleHere = OrganellePlacement::getOrganelleAt(
                            editor.editedMicrobe, Int2(posQ, posR));
                        if(organelleHere !is null){
                            OrganellePlacement::removeOrganelleAt(editor.editedMicrobe,
                                Int2(posQ, posR));
                            }

                    }
                }

            },
            function(EditorAction@ action, MicrobeEditor@ editor){
                editor.editedMicrobe.resize(0);
                editor.setMutationPoints(int(action.data["previousMP"]));
                // Load old microbe
                array<PlacedOrganelle@> oldEditedMicrobe =
                    cast<array<PlacedOrganelle@>>(action.data["oldEditedMicrobe"]);
                for(uint i = 0; i < oldEditedMicrobe.length(); ++i){
                    editor.editedMicrobe.insertLast(cast<PlacedOrganelle>(oldEditedMicrobe[i]));
                }
            });
            @action.data["oldEditedMicrobe"] = oldEditedMicrobe;
            action.data["previousMP"] = previousMP;
            enqueueAction(action);

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
    void enqueueAction(EditorAction@ action)
    {
        if(action.cost != 0)
            if(!takeMutationPoints(action.cost))
                return;
        //We resize always and insert at the index so that
        //if we undo something then add something, the new
        //action is in the right spot on the array.
        //since we only enqueue when we add new actions
        actionHistory.resize(actionHistory.length()+1);

        setUndoButtonStatus(true);
        setRedoButtonStatus(false);

        action.redo(action, this);
        actionHistory.insertAt(actionIndex,action);
        actionIndex++;

        //Only called when an action happens, because its an expensive method
        hudSystem.updateSpeed();

    }

    //! \todo Clean this up
    void getMouseHex(int &out qr, int &out rr)
    {
        float x, y;
        GetEngine().GetWindowEntity().GetNormalizedRelativeMouse(x, y);

        const auto ray = hudSystem.world.CastRayFromCamera(x, y);

        float distance;
        bool intersects = ray.intersects(Ogre::Plane(Ogre::Vector3(0, 1, 0), 0),distance);

        // Get the position of the cursor in the plane that the microbes is floating in
        const auto rayPoint = ray.getPoint(distance);

        // LOG_WRITE("Mouse point: " + rayPoint.x + ", " + rayPoint.y + ", " + rayPoint.z);

        // Convert to the hex the cursor is currently located over.

        //Negating X to compensate for the fact that we are looking at
        //the opposite side of the normal coordinate system

        float hexOffsetX;
        float hexOffsetY;

        if (rayPoint.z <0){
            hexOffsetY = -(HEX_SIZE/2);
            }
        else {
            hexOffsetY = (HEX_SIZE/2);
        }
        if (rayPoint.x <0){
            hexOffsetX = -(HEX_SIZE/2);
            }
        else {
            hexOffsetX = (HEX_SIZE/2);
        }

        const auto tmp1 = Hex::cartesianToAxial(rayPoint.x+hexOffsetX, -1*(rayPoint.z+hexOffsetY));

        // This requires a conversion to hex cube coordinates and back
        // for proper rounding.
        const auto qrrr = Hex::cubeToAxial(Hex::cubeHexRound(
                Float3(Hex::axialToCube(tmp1.X, tmp1.Y))));

        qr = qrrr.X;
        rr = qrrr.Y;

        // LOG_WRITE("Mouse hex: " + qr + ", " + rr);
    }

    bool checkIsNucleusPresent()
    {
        for(uint i = 0; i < editedMicrobe.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(editedMicrobe[i]);
            if (organelle.organelle.name == "nucleus"){
                return true;
            }
        }
        return false;
    }

    void updateGuiButtonStatus(bool nucleusIsPresent){

        GenericEvent@ event = GenericEvent("MicrobeEditorNucleusIsPresent");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("nucleus", nucleusIsPresent));
        GetEngine().GetEventHandler().CallEvent(event);
    }

    bool isValidPlacement(const string &in organelleType, int q, int r,
        int rotation)
    {
        auto organelle = getOrganelleDefinition(organelleType);

        bool empty = true;
        bool touching = false;

        Organelle@ toBePlacedOrganelle = getOrganelleDefinition(activeActionName);

        assert(toBePlacedOrganelle !is null, "invalid action name in microbe editor");

        auto hexes = toBePlacedOrganelle.getRotatedHexes(rotation);

        for(uint i = 0; i < hexes.length(); ++i){

            int posQ = int(hexes[i].q+q);
            int posR = int(hexes[i].r+r);

            auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobe,
                Int2(posQ, posR));

            if(organelleHere !is null)
            {
                // Allow replacing cytoplasm (but not with other cytoplasm)
                if(organelleHere.organelle.name == "cytoplasm" &&
                    activeActionName != "cytoplasm"){

                } else {
                    return false;
                }
            }

            // Check touching
            if(surroundsOrganelle(posQ, posR))
                touching = true;
        }

        return touching;
    }

    //! Checks whether the hex at q, r has an organelle in its surroundeing hexes.
    bool surroundsOrganelle(int q, int r)
    {
        return
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q + 0, r - 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q + 1, r - 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q + 1, r + 0)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q + 0, r + 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q - 1, r + 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobe, Int2(q - 1, r + 0)) !is null;
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
        if (actionIndex < int(actionHistory.length())-1){
            actionIndex += 1;
            auto action = actionHistory[actionIndex-1];
            action.redo(action, this);
            if (action.cost > 0){
                mutationPoints -= action.cost;
            }
            //upon redoing, undoing is possible
            setUndoButtonStatus(true);
        }

        //nothing left to redo? disable redo
        if (actionIndex >= int(actionHistory.length()-2)){
            setRedoButtonStatus(false);
        }
    }

    void undo()
    {
        //LOG_INFO("Attempting to call undo beginning");
        if (actionIndex > 0){
            //LOG_INFO("Attempting to call undo");
            auto action = actionHistory[actionIndex-1];
            if (action.cost > 0){
                mutationPoints += action.cost;
            }
            action.undo(action, this);
            actionIndex -= 1;
            //upon undoing, redoing is possible
            setRedoButtonStatus(true);
        }

        //nothing left to undo? disable undo
        if (actionIndex <= 0){
            setUndoButtonStatus(false);
        }
    }

    // Don't call directly. Should be used through actions
    void removeOrganelle(const string &in)
    {
        switch (symmetry){
            case 0: {
                int q, r;
                getMouseHex(q, r);
                removeOrganelleAt(q,r);
            }
            break;
            case 1: {
                int q, r;
                getMouseHex(q, r);
                removeOrganelleAt(q,r);

                if ((q != -1 * q || r != r + q)){
                    removeOrganelleAt(-1*q, r+q);
                }
            }
            break;
            case 2: {
                int q, r;
                getMouseHex(q, r);
                removeOrganelleAt(q,r);

                if ((q != -1 * q || r != r + q)){
                    removeOrganelleAt(-1*q, r+q);
                    removeOrganelleAt(-1*q, -1*r);
                    removeOrganelleAt(q, -1*(r+q));
                }
            }
            break;
            case 3: {
                int q, r;
                getMouseHex(q, r);
                removeOrganelleAt(q,r);

                if ((q != -1 * q || r != r + q)){
                    removeOrganelleAt(-1*r, r+q);
                    removeOrganelleAt(-1*(r+q), q);
                    removeOrganelleAt(-1*q, -1*r);
                    removeOrganelleAt(r, -1*(r+q));
                    removeOrganelleAt(r, -1*(r+q));
                    removeOrganelleAt(r+q, -1*q);
                 }
            }
            break;
        }
    }

    void removeOrganelleAt(int q, int r)
    {
        auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobe,
                Int2(q, r));
        PlacedOrganelle@ organelle = cast<PlacedOrganelle>(organelleHere);

        if(organelleHere !is null){
            if(!(organelleHere.organelle.name == "nucleus")) {
                EditorAction@ action = EditorAction(ORGANELLE_REMOVE_COST,
                // redo We need data about the organelle we removed, and the location so we can "redo" it
                 function(EditorAction@ action, MicrobeEditor@ editor){
                    LOG_INFO("Redo called");
                    int q = int(action.data["q"]);
                    int r = int(action.data["r"]);
                    // Remove the organelle
                   OrganellePlacement::removeOrganelleAt(editor.editedMicrobe,Int2(q, r));
                },
                // undo
                function(EditorAction@ action, MicrobeEditor@ editor){
                 PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                // Need to set this here to make sure the pointer is updated
                @action.data["organelle"]=organelle;
                // Check if there is cytoplasm under this organelle.
                auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                for(uint i = 0; i < hexes.length(); ++i){
                    int posQ = int(hexes[i].q) + organelle.q;
                    int posR = int(hexes[i].r) + organelle.r;
                    auto organelleHere = OrganellePlacement::getOrganelleAt(
                        editor.editedMicrobe, Int2(posQ, posR));
                    if(organelleHere !is null &&
                        organelleHere.organelle.name == "cytoplasm")
                    {
                        LOG_INFO("replaced cytoplasm");
                        OrganellePlacement::removeOrganelleAt(editor.editedMicrobe,
                            Int2(posQ, posR));
                    }
                }
                editor.editedMicrobe.insertLast(organelle);
                });
                // Give the action access to some data
                @action.data["organelle"] = organelle;
                action.data["q"] = q;
                action.data["r"] = r;
                enqueueAction(action);
                }
            }
    }


    //The first parameter states which sceneNodes to use, starting with "start" and going up 6.
    void renderHighlightedOrganelle(int start, int q, int r, int rotation)
    {
        if (activeActionName != ""){

            // If not hovering over an organelle render the to-be-placed organelle
            Organelle@ toBePlacedOrganelle = getOrganelleDefinition(activeActionName);

            assert(toBePlacedOrganelle !is null, "invalid action name in microbe editor");

            auto hexes = toBePlacedOrganelle.getRotatedHexes(rotation);

            for(uint i = 0; i < hexes.length(); ++i){

                int posQ = int(hexes[i].q) + q;
                int posR = int(hexes[i].r) + r;

                const Float3 pos = Hex::axialToCartesian(posQ, posR);

                ObjectID hex = hudSystem.hoverHex[usedHoverHex++];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                node.Node.setPosition(pos);
                node.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(90),
                        Ogre::Vector3(0, 1, 0)) * Ogre::Quaternion(Ogre::Degree(180),
                            Ogre::Vector3(0, 0, 1)));
                node.Hidden = false;
                node.Marked = true;

                // Detect can it be placed there
                auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobe,
                    Int2(posQ, posR));

                auto model = hudSystem.world.GetComponent_Model(hex);

                if(isPlacementProbablyValid == false ||
                    (organelleHere !is null && organelleHere.organelle.name != "cytoplasm"))
                {
                    // Invalid place
                    model.GraphicalObject.setDatablockOrMaterialName(
                        "EditorHexMaterialInvalid");
                } else {
                    model.GraphicalObject.setDatablockOrMaterialName(
                        "EditorHexMaterial");
                }
            }
        }
    }

    void setActiveAction(const string &in actionName)
    {
        activeActionName = actionName;
    }

    void performLocationAction()
    {
        if (activeActionName.length() > 0){
            auto func = cast<PlacementFunctionType>(placementFunctions[activeActionName]);
            func(activeActionName);
        }
    }

    bool takeMutationPoints(int amount)
    {
        if (amount <= mutationPoints){
            mutationPoints = mutationPoints - amount;
            return true;
        } else{
            return false;
        }
    }

    void setMutationPoints(int amount)
    {
        mutationPoints = amount;
    }

    int getMutationPoints() const
    {
        return mutationPoints;
    }

    int getMicrobeSize() const
    {
        return editedMicrobe.length();
    }

    // Make sure this is only called when you add organelles, as it is an expensive
    double getMicrobeSpeed() const
    {
        double finalSpeed = 0;
        int flagCount=0;
        double lengthMicrobe = double(editedMicrobe.length());
        for(uint i = 0; i < editedMicrobe.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(editedMicrobe[i]);
            auto name = organelle.organelle.name;
            if (name=="flagellum"){
                flagCount++;
            }
        }
        //This is complex, i Know
        //LOG_INFO(""+flagCount);
        finalSpeed= ((CELL_BASE_THRUST+((flagCount/(lengthMicrobe-flagCount))*FLAGELLA_BASE_FORCE))+
            (CELL_DRAG_MULTIPLIER-(CELL_SIZE_DRAG_MULTIPLIER*lengthMicrobe)));
        return finalSpeed;
    }
    // Maybe i should do this in the non-editor code instead, to make sure its more decoupled from the player
    int getMicrobeGeneration() const
    {
        auto playerSpecies = MicrobeOperations::getSpeciesComponent(GetThriveGame().getCellStage(), "Default");
        // Its plus one because you are updating the next generation
        return (playerSpecies.generation+1);
    }


    int onGeneric(GenericEvent@ event)
    {
        auto type = event.GetType();

        if(type == "MicrobeEditorOrganelleSelected")
        {
            NamedVars@ vars = event.GetNamedVars();

            activeActionName = string(vars.GetSingleValueByName("organelle"));
            LOG_INFO("Editor action is now: " + activeActionName);
            return 1;
        } else if(type == "MicrobeEditorClicked"){

            NamedVars@ vars = event.GetNamedVars();

            bool secondary = bool(vars.GetSingleValueByName("secondary"));

            if(!secondary){
                performLocationAction();
            } else {
                removeOrganelle("");
            }

            return 1;

        } else if(type == "MicrobeEditorExited"){
            LOG_INFO("MicrobeEditor: applying changes to player Species");

            // We need to grab the player's species
            SpeciesComponent@ playerSpecies = MicrobeOperations::getSpeciesComponent(
                GetThriveGame().getCellStage(), GetThriveGame().playerData().activeCreature());

            assert(playerSpecies !is null, "didn't find edited species");

            // Apply changes to the species organelles
            auto@ templateOrganelles = cast<array<SpeciesStoredOrganelleType@>>(
                playerSpecies.organelles);

            // It is easiest to just replace all
            array<SpeciesStoredOrganelleType@> newOrganelles;

            for(uint i = 0; i < editedMicrobe.length(); ++i){

                newOrganelles.insertLast(editedMicrobe[i]);
            }

            templateOrganelles = newOrganelles;


            // Grab render and physics of player cell
            auto node =  GetThriveGame().getCellStage().GetComponent_RenderNode(GetThriveGame().playerData().activeCreature());
            auto physics = GetThriveGame().getCellStage().GetComponent_Physics(GetThriveGame().playerData().activeCreature());

            //! Change player species cell size
            if(checkIsNucleusPresent()) {
                playerSpecies.isBacteria = false;
                node.Scale = Float3(1.0, 1.0, 1.0);
                node.Marked = true;

                physics.ChangeShape(GetThriveGame().getCellStage().GetPhysicalWorld(),
                    GetThriveGame().getCellStage().GetPhysicalWorld().CreateSphere(HEX_SIZE));
            }
            else {
                playerSpecies.isBacteria = true;
                node.Scale = Float3(0.5, 0.5, 0.5);   
                node.Marked = true;
                physics.ChangeShape(GetThriveGame().getCellStage().GetPhysicalWorld(),
                    GetThriveGame().getCellStage().GetPhysicalWorld().CreateSphere(HEX_SIZE/2));
            }
            
            LOG_INFO("MicrobeEditor: updated organelles for species: " + playerSpecies.name);
            return 1;
        } else if (type == "SymmetryClicked"){
            //Set Variable
            NamedVars@ vars = event.GetNamedVars();
            symmetry = int(vars.GetSingleValueByName("symmetry"));
            return 1;
        } else if (type == "PressedRightRotate"){
            organelleRot+=(360/6);
            return 1;
        }else if (type == "PressedLeftRotate"){
            organelleRot-=(360/6);
            return 1;
        } else if (type == "NewCellClicked"){
            //Create New Microbe
            createNewMicrobe("");
            return 1;
        }else if (type == "UndoClicked"){
            //Call Undo
            undo();
            return 1;
        }else if (type == "RedoClicked"){
            //Call Redo
            redo();
            return 1;
        }

        LOG_ERROR("Microbe editor got unknown event: " + type);
        return -1;
    }

    //! This is used to keep track of used hover organelles
    private uint usedHoverHex = 0;

    //! This is a global assesment if the currently being placed
    //! organelle is valid (if not all hover hexes will be shown as
    //! invalid)
    bool isPlacementProbablyValid = false;

    // where all user actions will  be registered
    private array<EditorAction@> actionHistory;
    // marks the last action that has been done (not undone, but
    // possibly redone), is 0 if there is none
    private int actionIndex;
    private string activeActionName;
    // This is the container that has the edited organelles in it.
    // This is populated when entering and used to update the player's species template on exit
    // TODO: rename to editedMicrobeOrganelles
    // This is not private because anonymous callbacks want to access this
    array<PlacedOrganelle@> editedMicrobe;
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
    private int symmetry = 0;

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

