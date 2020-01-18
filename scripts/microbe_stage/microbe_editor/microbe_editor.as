#include "microbe_editor_hud.as"
#include "microbe_operations.as"
#include "organelle_placement.as"
#include "calculate_effectiveness.as"

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
        eventListener.RegisterForEvent("MicrobeEditorSelectedTab");
        eventListener.RegisterForEvent("MicrobeEditorSelectedNewPatch");
        eventListener.RegisterForEvent("MicrobeEditorNameChanged");
        eventListener.RegisterForEvent("MicrobeEditorMembraneSelected");
        eventListener.RegisterForEvent("MicrobeEditorColourSelected");
        eventListener.RegisterForEvent("MicrobeEditorRigidityChanged");

        placementFunctions = {
            // {"nucleus", PlacementFunctionType(this.createNewMicrobe)},
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
            {"chemoSynthisizingProteins", PlacementFunctionType(this.addOrganelle)},
            {"rusticyanin", PlacementFunctionType(this.addOrganelle)},
            {"nitrogenase", PlacementFunctionType(this.addOrganelle)},
            {"oxytoxyProteins", PlacementFunctionType(this.addOrganelle)},
            {"pilus", PlacementFunctionType(this.addOrganelle)},
            {"remove", PlacementFunctionType(this.removeOrganelle)}
        };

        @invalidMaterial = getBasicTransparentMaterialWithTexture("single_hex_invalid.png");
        @validMaterial = getBasicTransparentMaterialWithTexture("single_hex.png");
    }

    //! This is called each time the editor is entered so this needs to properly reset state
    void init()
    {
        mutationPoints = BASE_MUTATION_POINTS;

        actionIndex = 0;
        organelleRot = 0;
        symmetry = 0;
        newName = "";
        membrane = 0;
        colour = Float4(1, 1, 1, 1);
        rigidity = 0;
        setUndoButtonStatus(false);
        setRedoButtonStatus(false);

        // The world is reset each time so these are gone
        placedHexes.resize(0);
        placedModels.resize(0);

        //Check generation and set it here.
        hudSystem.updateGeneration();
    }

    void activate()
    {
        GetThriveGame().playerData().setBool("edited_microbe", true);

        if(GetThriveGame().playerData().isFreeBuilding()){
            LOG_INFO("Editor going to freebuild mode because player has activated freebuild");
            freeBuilding = true;
        } else {
            // Make sure freebuilding doesn't get stuck on
            freeBuilding = false;
        }

        // Sent freebuild value to GUI
        GenericEvent@ freebuildEvent = GenericEvent("MicrobeEditorFreeBuildStatus");
        NamedVars@ freebuildVars = freebuildEvent.GetNamedVars();
        freebuildVars.AddValue(ScriptSafeVariableBlock("freebuild", freeBuilding));

        GetEngine().GetEventHandler().CallEvent(freebuildEvent);

        LOG_INFO("Elapsing time on editor entry");
        // TODO: select which units will be used for the master elapsed time counter
        GetThriveGame().getCellStage().GetTimedWorldOperations().onTimePassed(1);

        // Send info to the GUI about the organelle effectiveness in the current patch
        calculateOrganelleEffectivenessInPatch();

        // Reset this, GUI will tell us to enable it again
        showHover = false;
        targetPatch = -1;

        Species@ playerSpecies = MicrobeOperations::getSpecies(
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

        editedMicrobeOrganelles.resize(0);
        playerSpecies.stringCode="";
        for(uint i = 0; i < templateOrganelles.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(templateOrganelles[i]);
            editedMicrobeOrganelles.insertLast(organelle);
            playerSpecies.stringCode += organelle.organelle.gene;
            // This will always be added after each organelle so its safe to assume its there
            playerSpecies.stringCode+=","+organelle.q+","+
            organelle.r+","+
            organelle.rotation;
            if (i != templateOrganelles.length()-1){
                playerSpecies.stringCode+="|";
            }
        }

        LOG_INFO("Starting microbe editor with: " + editedMicrobeOrganelles.length() +
            " organelles in the microbe, genes: " + playerSpecies.stringCode);

        // We need to set the membrane type here so the ATP balance bar can take it into account (the bar is updated in _onEditedCellChanged)
        membrane = playerSpecies.membraneType;

        // Show existing organelles
        _onEditedCellChanged();

        // Update GUI buttons now that we have correct organelles
        updateGuiButtonStatus(checkIsNucleusPresent());

        // Create a mutated version of the current species code to compete against the player
        if(!GetThriveGame().getCellStage().GetPatchManager().getCurrentMap().getCurrentPatch()
            .addSpecies(createMutatedSpecies(playerSpecies),
                GetEngine().GetRandom().GetNumber(INITIAL_SPLIT_POPULATION_MIN,
                    INITIAL_SPLIT_POPULATION_MAX)))
        {
            LOG_ERROR("Failed to create a mutated copy of the player species");
        }

        // Reset to cytoplasm if nothing is selected
        if(activeActionName == ""){
            LOG_INFO("Selecting cytoplasm");

            GenericEvent@ event = GenericEvent("MicrobeEditorOrganelleSelected");
            NamedVars@ vars = event.GetNamedVars();

            vars.AddValue(ScriptSafeVariableBlock("organelle", "cytoplasm"));

            GetEngine().GetEventHandler().CallEvent(event);
        }

        newName = playerSpecies.genus + " " + playerSpecies.epithet;
        rigidity = playerSpecies.membraneRigidity;
        colour = playerSpecies.colour;
        GenericEvent@ event = GenericEvent("MicrobeEditorActivated");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("name", newName));
        vars.AddValue(ScriptSafeVariableBlock("membrane", SimulationParameters::membraneRegistry().getInternalName(membrane)));
        vars.AddValue(ScriptSafeVariableBlock("colourR", colour.X));
        vars.AddValue(ScriptSafeVariableBlock("colourG", colour.Y));
        vars.AddValue(ScriptSafeVariableBlock("colourB", colour.Z));
        vars.AddValue(ScriptSafeVariableBlock("rigidity", rigidity));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    void update(float elapsed)
    {
        // TODO: this is really dirty to call this all the time
        // This updates the mutation point counts to the GUI
        hudSystem.updateMutationPoints();
        hudSystem.updateSize();

        // This is also highly non-optimal to update the hex locations
        // and materials all the time

        // Reset colour of each already placed hex
        for(uint i = 0; i < placedHexes.length(); ++i){

            ObjectID hex = placedHexes[i];
            auto model = hudSystem.world.GetComponent_Model(hex);

            @model.ObjectMaterial = validMaterial;
            model.Marked = true;
        }

        usedHoverHex = 0;
        usedHoverOrganelle = 0;

        // Show the organelle that is about to be placed
        if(activeActionName != "" && showHover){
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
    }

    // Called whenever the edited cell changes
    void _onEditedCellChanged()
    {
        _updateAlreadyPlacedVisuals();

        // Calculate and send energy balance to the GUI
        calculateEnergyBalanceWithOrganellesAndMembraneType(editedMicrobeOrganelles, membrane, targetPatch);
    }

    // This destroys and creates again entities to represent all the
    // currently placed organelles. Call this whenever editedMicrobeOrganelles
    // is changed
    void _updateAlreadyPlacedVisuals()
    {
        uint nextFreeHex = 0;
        uint nextFreeOrganelle = 0;

        placedModels;

        // Build the entities to show the current microbe
        for(uint i = 0; i < editedMicrobeOrganelles.length(); ++i){

            const PlacedOrganelle@ organelle = editedMicrobeOrganelles[i];
            auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);

            for(uint a = 0; a < hexes.length(); ++a){

                const Float3 pos = Hex::axialToCartesian(hexes[a].X + organelle.q,
                    hexes[a].Y + organelle.r);

                if(nextFreeHex >= placedHexes.length()){
                    // New hex needed
                    placedHexes.insertLast(createEditorHexEntity());
                }

                ObjectID hex = placedHexes[nextFreeHex++];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                node.Node.SetPosition(pos);
                node.Hidden = false;
                node.Marked = true;
            }

            // Model of the organelle
            if(organelle.organelle.mesh != ""){

                const auto cartesianPosition = Hex::axialToCartesian(organelle.q, organelle.r);

                if(nextFreeOrganelle >= placedModels.length()){
                    // New organelle model needed
                    placedModels.insertLast(createEditorOrganelleModel());
                }

                ObjectID organelleModel = placedModels[nextFreeOrganelle++];
                auto node = hudSystem.world.GetComponent_RenderNode(organelleModel);
                node.Node.SetPosition(cartesianPosition +
                    organelle.organelle.calculateModelOffset());

                node.Node.SetOrientation(createRotationForOrganelle(organelle.rotation));
                node.Hidden = false;
                node.Marked = true;

                auto model = hudSystem.world.GetComponent_Model(organelleModel);

                if(model.MeshName != organelle.organelle.mesh){
                    model.MeshName = organelle.organelle.mesh;
                    @model.ObjectMaterial = getOrganelleMaterialWithTexture(
                        organelle.organelle.texture);
                    model.Marked = true;
                }
            }
        }

        // Delete excess entities
        while(nextFreeHex < placedHexes.length()){
            hudSystem.world.DestroyEntity(placedHexes[placedHexes.length() - 1]);
            placedHexes.removeLast();
        }

        while(nextFreeOrganelle < placedModels.length()){
            hudSystem.world.DestroyEntity(placedModels[placedModels.length() - 1]);
            placedModels.removeLast();
        }
    }


    private void _addOrganelle(PlacedOrganelle@ organelle)
    {
        // 1 - you put nucleus but you already have it
        // 2 - you put organelle that need nucleus and you don't have it
        if((organelle.organelle.name == "nucleus" && checkIsNucleusPresent()) ||
            (organelle.organelle.prokaryoteChance == 0 && !checkIsNucleusPresent())
            && organelle.organelle.chanceToCreate != 0 )
                return;

        int cost = organelle.organelle.mpCost;

        EditorAction@ action = EditorAction(cost,
            // redo
            function(EditorAction@ action, MicrobeEditor@ editor){

                PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                // Need to set this here to make sure the pointer is updated
                @action.data["organelle"]=organelle;
                // Check if there is cytoplasm under this organelle.
                auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                for(uint i = 0; i < hexes.length(); ++i){
                    int posQ = int(hexes[i].X) + organelle.q;
                    int posR = int(hexes[i].Y) + organelle.r;

                    auto organelleHere = OrganellePlacement::getOrganelleAt(
                        editor.editedMicrobeOrganelles, Int2(posQ, posR));

                    if(organelleHere !is null &&
                        organelleHere.organelle.name == "cytoplasm")
                    {
                        // First we save the organelle data and then delete it
                        @action.data["replacedCyto"] = organelleHere;

                        LOG_INFO("replaced cytoplasm");
                        OrganellePlacement::removeOrganelleAt(editor.editedMicrobeOrganelles,
                            Int2(posQ, posR));
                    }
                }

                LOG_INFO("Placing organelle '" + organelle.organelle.name + "' at: " +
                    organelle.q + ", " + organelle.r);
                editor.editedMicrobeOrganelles.insertLast(organelle);

                editor._onEditedCellChanged();

                // send to gui current status of cell
                editor.updateGuiButtonStatus(editor.checkIsNucleusPresent());
            },
            // undo
            function(EditorAction@ action, MicrobeEditor@ editor){
                LOG_INFO("Undo called");
                const PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                for(uint c = 0; c < hexes.length(); ++c){
                    int posQ = int(hexes[c].X) + organelle.q;
                    int posR = int(hexes[c].Y) + organelle.r;
                    auto organelleHere = OrganellePlacement::getOrganelleAt(
                        editor.editedMicrobeOrganelles, Int2(posQ, posR));
                    if(organelleHere !is null){
                        OrganellePlacement::removeOrganelleAt(editor.editedMicrobeOrganelles,
                            Int2(posQ, posR));

                        // If an cyto was replaced here, we have to replace it back on undo of this action
                        if(action.data.exists("replacedCyto")) {
                            PlacedOrganelle@ replacedCyto =  cast<PlacedOrganelle>(action.data["replacedCyto"]);

                            LOG_INFO("Replacing " + replacedCyto.organelle.name + "' at: " +
                                replacedCyto.q + ", " + replacedCyto.r);
                            editor.editedMicrobeOrganelles.insertLast(replacedCyto);
                        }
                    }
                }

                editor._onEditedCellChanged();
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

    // Wipes clean the current cell. Seems to work fine
    void createNewMicrobe(const string &in)
    {
        // organelleCount = 0;
        auto previousMP = mutationPoints;
        // Copy current microbe to a new array
        array<PlacedOrganelle@> oldEditedMicrobeOrganelles = editedMicrobeOrganelles;

        EditorAction@ action = EditorAction(0,
            // redo
            function(EditorAction@ action, MicrobeEditor@ editor){
                // Delete the organelles (all except the nucleus) and set mutation points
                // (just undoing and redoing the cost like other actions doesn't work in this case due to its nature)
                editor.setMutationPoints(BASE_MUTATION_POINTS);
                for(uint i = editor.editedMicrobeOrganelles.length()-1; i > 0; --i){
                    const PlacedOrganelle@ organelle = editor.editedMicrobeOrganelles[i];
                    auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                    for(uint c = 0; c < hexes.length(); ++c){
                        int posQ = int(hexes[c].X) + organelle.q;
                        int posR = int(hexes[c].Y) + organelle.r;
                        auto organelleHere = OrganellePlacement::getOrganelleAt(
                            editor.editedMicrobeOrganelles, Int2(posQ, posR));
                        if(organelleHere !is null){
                            OrganellePlacement::removeOrganelleAt(editor.editedMicrobeOrganelles,
                                Int2(posQ, posR));
                            }

                    }
                }

                editor._onEditedCellChanged();

            },
            function(EditorAction@ action, MicrobeEditor@ editor){
                editor.editedMicrobeOrganelles.resize(0);
                editor.setMutationPoints(int(action.data["previousMP"]));
                // Load old microbe
                array<PlacedOrganelle@> oldEditedMicrobeOrganelles =
                    cast<array<PlacedOrganelle@>>(action.data["oldEditedMicrobeOrganelles"]);
                for(uint i = 0; i < oldEditedMicrobeOrganelles.length(); ++i){
                    editor.editedMicrobeOrganelles.insertLast(cast<PlacedOrganelle>(oldEditedMicrobeOrganelles[i]));
                }

                editor._onEditedCellChanged();
            });
            @action.data["oldEditedMicrobeOrganelles"] = oldEditedMicrobeOrganelles;
            action.data["previousMP"] = previousMP;
            enqueueAction(action);

    }

    // TODO: the other status setting functions are in the hud
    // class. These should be moved there as well
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

    void updateGuiButtonStatus(bool nucleusIsPresent)
    {
        GenericEvent@ event = GenericEvent("MicrobeEditorNucleusIsPresent");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("nucleus", nucleusIsPresent));
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
        if(action.cost != 0 && !freeBuilding)
            if(!takeMutationPoints(action.cost))
                return;

        // Every time we make a new action if is in the middle of history of actions
        // We have to erase all redo history from that point
        while(actionIndex < int(actionHistory.length()))
            actionHistory.removeLast();

        actionHistory.insertLast(action);

        setUndoButtonStatus(true);
        setRedoButtonStatus(false);

        actionIndex++;
        action.redo(action, this);

        // Only called when an action happens, because its an expensive method
        hudSystem.updateSpeed();
    }

    //! \todo Clean this up
    //! This would make more sense to be in the hud file
    void getMouseHex(int &out qr, int &out rr)
    {
        // Get the position of the cursor in the plane that the microbes is floating in
        const auto rayPoint = PlayerMicrobeControlSystem::getTargetPoint(hudSystem.world);

        // Convert to the hex the cursor is currently located over.
        const auto tmp1 = Hex::cartesianToAxial(rayPoint.X, rayPoint.Z);

        qr = tmp1.X;
        rr = tmp1.Y;
    }

    bool checkIsNucleusPresent()
    {
        for(uint i = 0; i < editedMicrobeOrganelles.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(editedMicrobeOrganelles[i]);
            if (organelle.organelle.name == "nucleus"){
                return true;
            }
        }
        return false;
    }

    bool isValidPlacement(const string &in organelleType, int q, int r,
        int rotation)
    {
        auto organelle = getOrganelleDefinition(organelleType);

        bool empty = true;
        bool touching = false;

        const auto@ toBePlacedOrganelle = getOrganelleDefinition(activeActionName);

        assert(toBePlacedOrganelle !is null, "invalid action name in microbe editor");

        auto hexes = toBePlacedOrganelle.getRotatedHexes(rotation);

        for(uint i = 0; i < hexes.length(); ++i){

            int posQ = int(hexes[i].X + q);
            int posR = int(hexes[i].Y + r);

            auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles,
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
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q + 0, r - 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q + 1, r - 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q + 1, r + 0)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q + 0, r + 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q - 1, r + 1)) !is null ||
            OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles, Int2(q - 1, r + 0)) !is null;
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
            auto action = actionHistory[actionIndex];
            actionIndex += 1;
            action.redo(action, this);
            hudSystem.updateSpeed();

            if (action.cost > 0 && !freeBuilding){
                mutationPoints -= action.cost;
            }
            //upon redoing, undoing is possible
            setUndoButtonStatus(true);
        }

        //nothing left to redo? disable redo
        if (actionIndex >= int(actionHistory.length())){
            setRedoButtonStatus(false);
        }
    }

    void undo()
    {
        //LOG_INFO("Attempting to call undo beginning");
        if (actionIndex > 0){
            //LOG_INFO("Attempting to call undo");
            auto action = actionHistory[actionIndex-1];
            if (action.cost > 0 && !freeBuilding){
                mutationPoints += action.cost;
            }
            action.undo(action, this);
            hudSystem.updateSpeed();

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
        int q, r;
        getMouseHex(q, r);

        switch (symmetry){
            case 0: {
                removeOrganelleAt(q,r);
            }
            break;
            case 1: {
                removeOrganelleAt(q,r);

                if ((q != -1 * q || r != r + q)){
                    removeOrganelleAt(-1*q, r+q);
                }
            }
            break;
            case 2: {
                removeOrganelleAt(q,r);

                if ((q != -1 * q || r != r + q)){
                    removeOrganelleAt(-1*q, r+q);
                    removeOrganelleAt(-1*q, -1*r);
                    removeOrganelleAt(q, -1*(r+q));
                }
            }
            break;
            case 3: {
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
        auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles,
                Int2(q, r));
        PlacedOrganelle@ organelle = cast<PlacedOrganelle>(organelleHere);

        int cost = ORGANELLE_REMOVE_COST;

        if(organelleHere !is null){
            // Dont allow deletion of nucleus or the last organelle
            // TODO: allow deleting the last cytoplasm if an organelle is about to be placed
            if(!(organelleHere.organelle.name == "nucleus") && getMicrobeSize() > 1) {
                EditorAction@ action = EditorAction(cost,
                // redo We need data about the organelle we removed, and the location so we can "redo" it
                 function(EditorAction@ action, MicrobeEditor@ editor){
                    LOG_INFO("Redo called");
                    int q = int(action.data["q"]);
                    int r = int(action.data["r"]);
                    // Remove the organelle
                   OrganellePlacement::removeOrganelleAt(editor.editedMicrobeOrganelles,Int2(q, r));
                   editor._onEditedCellChanged();
                },
                // undo
                function(EditorAction@ action, MicrobeEditor@ editor){
                    PlacedOrganelle@ organelle = cast<PlacedOrganelle>(action.data["organelle"]);
                    // Need to set this here to make sure the pointer is updated
                    @action.data["organelle"]=organelle;
                    // Check if there is cytoplasm under this organelle.
                    auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
                    for(uint i = 0; i < hexes.length(); ++i){
                        int posQ = int(hexes[i].X) + organelle.q;
                        int posR = int(hexes[i].Y) + organelle.r;
                        auto organelleHere = OrganellePlacement::getOrganelleAt(
                            editor.editedMicrobeOrganelles, Int2(posQ, posR));
                        if(organelleHere !is null &&
                            organelleHere.organelle.name == "cytoplasm")
                        {
                            LOG_INFO("replaced cytoplasm");
                            OrganellePlacement::removeOrganelleAt(editor.editedMicrobeOrganelles,
                                Int2(posQ, posR));
                        }
                    }
                    editor.editedMicrobeOrganelles.insertLast(organelle);
                    editor._onEditedCellChanged();
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
        if (activeActionName == "")
            return;

        // If not hovering over an organelle render the to-be-placed organelle
        const auto@ toBePlacedOrganelle = getOrganelleDefinition(activeActionName);

        assert(toBePlacedOrganelle !is null, "invalid action name in microbe editor");

        auto hexes = toBePlacedOrganelle.getRotatedHexes(rotation);

        bool showModel = true;

        for(uint i = 0; i < hexes.length(); ++i){

            int posQ = int(hexes[i].X) + q;
            int posR = int(hexes[i].Y) + r;

            const Float3 pos = Hex::axialToCartesian(posQ, posR);

            // Detect can it be placed there
            auto organelleHere = OrganellePlacement::getOrganelleAt(editedMicrobeOrganelles,
                Int2(posQ, posR));

            bool canPlace = false;

            if(isPlacementProbablyValid == false ||
                (organelleHere !is null && (organelleHere.organelle.name != "cytoplasm" ||
                    toBePlacedOrganelle.name == "cytoplasm")))
            {
                canPlace = false;
            } else {
                canPlace = true;
            }

            bool duplicate = false;

            // Skip if there is a placed organelle here already
            for(uint placedIndex = 0; placedIndex < placedHexes.length();
                ++placedIndex){

                ObjectID hex = placedHexes[placedIndex];
                auto node = hudSystem.world.GetComponent_RenderNode(hex);
                if(pos == node.Node.GetPosition()){
                    duplicate = true;

                    if(!canPlace){
                        // Mark as invalid
                        auto model = hudSystem.world.GetComponent_Model(hex);
                        @model.ObjectMaterial = invalidMaterial;
                        model.Marked = true;

                        showModel = false;
                    }

                    break;
                }
            }

            if(duplicate)
                continue;

            ObjectID hex = hudSystem.hoverHex[usedHoverHex++];
            auto node = hudSystem.world.GetComponent_RenderNode(hex);
            node.Node.SetPosition(pos);
            node.Hidden = false;
            node.Marked = true;

            auto model = hudSystem.world.GetComponent_Model(hex);

            if(canPlace){
                @model.ObjectMaterial = validMaterial;
            } else {
                @model.ObjectMaterial = invalidMaterial;
            }

            model.Marked = true;
        }

        // Model
        if(toBePlacedOrganelle.mesh != "" && showModel){

            const auto cartesianPosition = Hex::axialToCartesian(q, r);

            ObjectID organelleModel = hudSystem.hoverOrganelle[usedHoverOrganelle++];
            auto node = hudSystem.world.GetComponent_RenderNode(organelleModel);
            node.Node.SetPosition(cartesianPosition +
                toBePlacedOrganelle.calculateModelOffset());
            node.Node.SetOrientation(createRotationForOrganelle(rotation));
            node.Hidden = false;
            node.Marked = true;

            auto model = hudSystem.world.GetComponent_Model(organelleModel);

            if(model.MeshName != toBePlacedOrganelle.mesh){
                model.MeshName = toBePlacedOrganelle.mesh;
                @model.ObjectMaterial = getOrganelleMaterialWithTexture(
                    toBePlacedOrganelle.texture);
                model.Marked = true;
            }
        }
    }

    //! Creates a hex entity
    ObjectID createEditorHexEntity()
    {
        ObjectID hex = hudSystem.world.CreateEntity();
        auto node = hudSystem.world.Create_RenderNode(hex);
        hudSystem.world.Create_Model(hex, "hex.fbx", validMaterial);
        node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        node.Hidden = true;
        node.Marked = true;
        node.Node.SetPosition(Float3(0, 0, 0));
        // Quaternion rot(0.40118, 0.791809, 0.431951, 0.0381477);
        node.Node.SetOrientation(Quaternion(Float3(0, 1, 0), Degree(90))
            * Quaternion(Float3(0, 0, 1), Degree(180)));
        return hex;
    }

    //! Creates an entity for showing the model of an organelle
    ObjectID createEditorOrganelleModel()
    {
        ObjectID organelle = hudSystem.world.CreateEntity();
        auto node = hudSystem.world.Create_RenderNode(organelle);
        node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
        node.Hidden = true;
        node.Marked = true;
        node.Node.SetPosition(Float3(0, 0, 0));

        hudSystem.world.Create_Model(organelle, "", Material());

        return organelle;
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
        return editedMicrobeOrganelles.length();
    }

    int getActualMicrobeSize() const
    {
        int lengthMicrobe = 0;
        for(uint i = 0; i < editedMicrobeOrganelles.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(editedMicrobeOrganelles[i]);
            lengthMicrobe += organelle.organelle.getHexCount();
        }
        return lengthMicrobe;
    }

    // Make sure this is only called when you add organelles (or change membrane types), as it is expensive
    double getMicrobeSpeed() const
    {
        double finalSpeed = 0;
        int flagCount=0;
        double lengthMicrobe = 0;
        for(uint i = 0; i < editedMicrobeOrganelles.length(); ++i){
            auto organelle = cast<PlacedOrganelle>(editedMicrobeOrganelles[i]);
            lengthMicrobe+=organelle.organelle.getHexCount();
            auto name = organelle.organelle.name;
            if (name=="flagellum"){
                flagCount++;
            }
        }
        // This is complex, I know
        finalSpeed = (((CELL_BASE_THRUST +
            ((flagCount / (lengthMicrobe - flagCount)) * FLAGELLA_BASE_FORCE)) +
            (CELL_DRAG_MULTIPLIER - (CELL_SIZE_DRAG_MULTIPLIER * lengthMicrobe)))) *
            (SimulationParameters::membraneRegistry().getTypeData(membrane).movementFactor -
            rigidity * MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);
        return finalSpeed;
    }

    // Maybe this should be done in the non-editor code instead, to make sure it's more decoupled from the player
    int getMicrobeGeneration() const
    {
        auto playerSpecies = MicrobeOperations::getSpecies(
            GetThriveGame().getCellStage(), "Default");

        // Its plus one because you are updating the next generation
        return (playerSpecies.generation + 1);
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

            auto world = GetThriveGame().getCellStage();
            auto player = GetThriveGame().playerData().activeCreature();

            // We need to grab the player's species
            Species@ playerSpecies = MicrobeOperations::getSpecies(
                world, player);

            assert(playerSpecies !is null, "didn't find edited species");

            // Apply changes to the species organelles
            auto@ templateOrganelles = cast<array<SpeciesStoredOrganelleType@>>(
                playerSpecies.organelles);

            // It is easiest to just replace all
            array<SpeciesStoredOrganelleType@> newOrganelles;

            for(uint i = 0; i < editedMicrobeOrganelles.length(); ++i){

                newOrganelles.insertLast(editedMicrobeOrganelles[i]);
            }

            templateOrganelles = newOrganelles;

            // TODO: if it is in the future possible to edit
            // non-player species then this needs a check for that
            // before updating the player's active creature

            // Grab render node of player cell
            auto node =  world.GetComponent_RenderNode(player);
            auto absorber = world.GetComponent_CompoundAbsorberComponent(
            player);

            // Change player species cell size depending on whether they are a bacteria or not
            if(checkIsNucleusPresent()) {
                playerSpecies.isBacteria = false;
                node.Scale = Float3(1.0, 1.0, 1.0);
                node.Marked = true;
                absorber.setGrabScale(1.0f);
            } else {
                playerSpecies.isBacteria = true;
                node.Scale = Float3(0.5, 0.5, 0.5);
                node.Marked = true;
                absorber.setGrabScale(0.5f);
            }

            LOG_INFO("MicrobeEditor: updated organelles for species: " + playerSpecies.name);

            if(targetPatch != -1){
                LOG_INFO("MicrobeEditor: applying player move to patch: " + targetPatch);
                GetThriveGame().playerMovedToPatch(targetPatch);
            }

            // Change species name
            if(newName.split(" ").length() - 1 == 1){
                playerSpecies.genus = newName.split(" ")[0];
                playerSpecies.epithet = newName.split(" ")[1];
                LOG_INFO("MicrobeEditor: Player species name is now " + playerSpecies.genus + " " + playerSpecies.epithet);

                // Send new name to GUI
                GenericEvent@ nameEvent = GenericEvent("updateSpeciesName");
                NamedVars@  vars = nameEvent.GetNamedVars();
                vars.AddValue(ScriptSafeVariableBlock("speciesName", playerSpecies.genus + " " + playerSpecies.epithet));
                GetEngine().GetEventHandler().CallEvent(nameEvent);
            } else {
                LOG_INFO("MicrobeEditor: Invalid newName: " + newName);
            }

            auto membraneComponent = world.GetComponent_MembraneComponent(player);
            // Change species membrane
            playerSpecies.membraneType = membrane;
            LOG_INFO("MicrobeEditor: Player species membrane type is now " + int(playerSpecies.membraneType));
            membraneComponent.setMembraneType(membrane);
            // Change species membrane color
            playerSpecies.colour = colour;
            LOG_INFO("MicrobeEditor: Player species colour is now RGB: " + playerSpecies.colour.X + " " + playerSpecies.colour.Y + " " + playerSpecies.colour.Z);
            membraneComponent.setColour(colour);
            membraneComponent.clear();
            playerSpecies.membraneRigidity = rigidity;
            // Change player health
            MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(world.GetScriptComponentHolder("MicrobeComponent").Find(player));
            float hpFraction = microbeComponent.hitpoints / microbeComponent.maxHitpoints;
            microbeComponent.maxHitpoints = SimulationParameters::membraneRegistry().getTypeData(membrane).hitpoints +
                microbeComponent.species.membraneRigidity * MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
            microbeComponent.hitpoints = microbeComponent.maxHitpoints * hpFraction;

            return 1;
        } else if (type == "SymmetryClicked"){
            NamedVars@ vars = event.GetNamedVars();
            symmetry = int(vars.GetSingleValueByName("symmetry"));
            return 1;
        } else if (type == "PressedRightRotate"){
            organelleRot += 360 / 6;

            if (organelleRot >= 360)
                organelleRot -= 360;

            return 1;
        } else if (type == "PressedLeftRotate"){
            organelleRot -= 360 / 6;

            if (organelleRot < 0)
                organelleRot += 360;

            return 1;
        } else if (type == "NewCellClicked"){
            createNewMicrobe("");
            return 1;
        } else if (type == "UndoClicked"){
            undo();
            return 1;
        } else if (type == "RedoClicked"){
            redo();
            return 1;
        } else if(type == "MicrobeEditorSelectedTab"){
            NamedVars@ vars = event.GetNamedVars();
            showHover = string(vars.GetSingleValueByName("tab")) == "cell";
            return 1;
        } else if(type == "MicrobeEditorSelectedNewPatch"){
            NamedVars@ vars = event.GetNamedVars();
            targetPatch = int(vars.GetSingleValueByName("patchId"));
            // Update organelle effectiveness
            calculateOrganelleEffectivenessInPatch(targetPatch);
            return 1;
        } else if(type == "MicrobeEditorNameChanged"){
            NamedVars@ vars = event.GetNamedVars();
            newName = string(vars.GetSingleValueByName("value"));
            return 1;
        } else if(type == "MicrobeEditorMembraneSelected"){
            NamedVars@ vars = event.GetNamedVars();
            int cost = SimulationParameters::membraneRegistry().getTypeData(string(vars.GetSingleValueByName("membrane"))).editorCost;

            EditorAction@ action = EditorAction(cost,
                // redo
                function(EditorAction@ action, MicrobeEditor@ editor){
                    editor.membrane = MembraneTypeId(action.data["membrane"]);
                    GenericEvent@ event = GenericEvent("MicrobeEditorMembraneUpdated");
                    NamedVars@ vars = event.GetNamedVars();
                    vars.AddValue(ScriptSafeVariableBlock("membrane", SimulationParameters::membraneRegistry().getInternalName(editor.membrane)));
                    GetEngine().GetEventHandler().CallEvent(event);
                    // Calculate and send energy balance to the GUI
                    calculateEnergyBalanceWithOrganellesAndMembraneType(editor.editedMicrobeOrganelles, editor.membrane, editor.targetPatch); // not using _onEditedCellChange due to visuals not needing update
                },
                // undo
                function(EditorAction@ action, MicrobeEditor@ editor){
                    editor.membrane = MembraneTypeId(action.data["prevMembrane"]);
                    GenericEvent@ event = GenericEvent("MicrobeEditorMembraneUpdated");
                    NamedVars@ vars = event.GetNamedVars();
                    vars.AddValue(ScriptSafeVariableBlock("membrane", SimulationParameters::membraneRegistry().getInternalName(editor.membrane)));
                    GetEngine().GetEventHandler().CallEvent(event);
                    // Calculate and send energy balance to the GUI
                    calculateEnergyBalanceWithOrganellesAndMembraneType(editor.editedMicrobeOrganelles, editor.membrane, editor.targetPatch);
                }
            );

            action.data["membrane"] = SimulationParameters::membraneRegistry().getTypeId(string(vars.GetSingleValueByName("membrane")));
            action.data["prevMembrane"] = membrane;

            enqueueAction(action);
            return 1;
        } else if(type == "MicrobeEditorColourSelected"){
            NamedVars@ vars = event.GetNamedVars();
            colour = Float4(vars.GetSingleValueByName("r"), vars.GetSingleValueByName("g"), vars.GetSingleValueByName("b"), 1);
            return 1;
        } else if(type == "MicrobeEditorRigidityChanged"){
            NamedVars@ vars = event.GetNamedVars();
            float newRigidity = float(vars.GetSingleValueByName("rigidity"));
            int cost = int(abs(newRigidity - rigidity) / 2 * 100);

            if (cost > 0) {
                if (cost > mutationPoints){
                    newRigidity = rigidity + (newRigidity < rigidity ? -mutationPoints : mutationPoints) * 2 / 100.f;
                    cost = mutationPoints;
                }

                EditorAction@ action = EditorAction(cost,
                    // redo
                    function(EditorAction@ action, MicrobeEditor@ editor){
                        editor.rigidity = float(action.data["rigidity"]);
                        GenericEvent@ event = GenericEvent("MicrobeEditorRigidityUpdated");
                        NamedVars@ vars = event.GetNamedVars();
                        vars.AddValue(ScriptSafeVariableBlock("rigidity", editor.rigidity));
                        GetEngine().GetEventHandler().CallEvent(event);
                    },
                    // undo
                    function(EditorAction@ action, MicrobeEditor@ editor){
                        editor.rigidity = float(action.data["prevRigidity"]);
                        GenericEvent@ event = GenericEvent("MicrobeEditorRigidityUpdated");
                        NamedVars@ vars = event.GetNamedVars();
                        vars.AddValue(ScriptSafeVariableBlock("rigidity", editor.rigidity));
                        GetEngine().GetEventHandler().CallEvent(event);
                    }
                );

                action.data["rigidity"] = newRigidity;
                action.data["prevRigidity"] = rigidity;

                enqueueAction(action);
            }

            return 1;
        }

        LOG_ERROR("Microbe editor got unknown event: " + type);
        return -1;
    }

    //! When true nothing costs MP
    bool freeBuilding = false;

    //! Hover hexes and models are only shown if this is true
    bool showHover = false;

    //! Where the player wants to move after editing
    int targetPatch = -1;

    //! This is used to keep track of used hover organelles
    private uint usedHoverHex = 0;
    private uint usedHoverOrganelle = 0;

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
    // This is not private because anonymous callbacks want to access this
    array<PlacedOrganelle@> editedMicrobeOrganelles;

    // This is the already placed hexes
    private array<ObjectID> placedHexes;

    // This is the already placed organelle models
    private array<ObjectID> placedModels;

    private MicrobeEditorHudSystem@ hudSystem;

    private int mutationPoints;

    private int organelleRot;

    private dictionary placementFunctions;

    //0 is no symmetry, 1 is x-axis symmetry, 2 is 4-way symmetry, and 3 is 6-way symmetry.
    // TODO: change to enum
    private int symmetry = 0;

    private string newName;

    // Not private so anonymous callbacks can access this, same as editedMicrobeOrganelles
    MembraneTypeId membrane;

    private Float4 colour;

    // Not private so anonymous callbacks can access this, same as the two above...
    float rigidity;

    private bool microbeHasBeenInEditor = false;

    private EventListener@ eventListener;

    private Material@ invalidMaterial;
    private Material@ validMaterial;
};
