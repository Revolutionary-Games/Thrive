#include "microbe_editor.as"


const array<string> MICROBE_EDITOR_AMBIENT_TRACKS = {
    "microbe-editor-theme-1","microbe-editor-theme-2", "microbe-editor-theme-3",
    "microbe-editor-theme-4", "microbe-editor-theme-5"
};



class MicrobeEditorHudSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this._world = cast<MicrobeEditorWorld>(w);

        assert(this.world !is null, "MicrobeEditorHudSystem didn't get proper world");

        @editor = MicrobeEditor(this);


    //     auto root = this.gameState.guiWindow;
    //     this.mpLabel = root.getChild("MpPanel"):getChild("MpBar"):getChild("NumberLabel");
    //     this.mpProgressBar = root.getChild("MpPanel"):getChild("MpBar");
    //     this.mpProgressBar.setProperty("ThriveGeneric/MpBar", "FillImage");

    //     auto nucleusButton = root.getChild("NewButton");
    //     auto flagellumButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddFlagellum");
    //     auto cytoplasmButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddCytoplasm");
    //     auto mitochondriaButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddMitochondrion");
    //     auto vacuoleButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddVacuole");
    //     auto toxinButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddToxinVacuole");
    //     auto chloroplastButton = root.getChild("EditPanel"):getChild("StructurePanel"):getChild("StructureScroll"):getChild("AddChloroplast");

    //     this.organelleButtons["nucleus"] = nucleusButton;
    //     this.organelleButtons["flagellum"] = flagellumButton;
    //     this.organelleButtons["cytoplasm"] = cytoplasmButton;
    //     this.organelleButtons["mitochondrion"] = mitochondriaButton;
    //     this.organelleButtons["chloroplast"] = chloroplastButton;
    //     this.organelleButtons["vacuole"] = vacuoleButton;
    //     this.organelleButtons["Toxin"] = toxinButton;
    //     this.activeButton = null;

    //     // nucleusButton.registerEventHandler("Clicked", function() this.nucleusClicked() });
    //     // flagellumButton.registerEventHandler("Clicked", function() this.flagellumClicked() });
    //     // cytoplasmButton.registerEventHandler("Clicked", function() this.cytoplasmClicked() });
    //     // mitochondriaButton.registerEventHandler("Clicked", function() this.mitochondriaClicked() });
    //     // chloroplastButton.registerEventHandler("Clicked", function() this.chloroplastClicked() });
    //     // vacuoleButton.registerEventHandler("Clicked", function() this.vacuoleClicked() });
    //     // toxinButton.registerEventHandler("Clicked", function() this.toxinClicked() });

    //     // this.saveLoadPanel = root.getChild("SaveLoadPanel")
    //     // this.creationsListbox = this.saveLoadPanel.getChild("SavedCreations")
    //     this.undoButton = root.getChild("UndoButton");
    //     // this.undoButton.registerEventHandler("Clicked", function() this.editor.undo() });
    //     this.redoButton = root.getChild("RedoButton");
    //     // this.redoButton.registerEventHandler("Clicked", function() this.editor.redo() });
    //     this.symmetryButton = root.getChild("SymmetryButton");
    //     // this.symmetryButton.registerEventHandler("Clicked", function() this.changeSymmetry() });

    //     root.getChild("FinishButton"):registerEventHandler("Clicked", playClicked);
    //     //root.getChild("BottomSection"):getChild("MenuButton"):registerEventHandler("Clicked", this.menuButtonClicked)
    //     // root.getChild("MenuButton"):registerEventHandler("Clicked", function() this.menuButtonClicked() });
    //     // root.getChild("PauseMenu"):getChild("MainMenuButton"):registerEventHandler("Clicked", function() this.menuMainMenuClicked() });
    //     // root.getChild("PauseMenu"):getChild("ResumeButton"):registerEventHandler("Clicked", function() this.resumeButtonClicked() });
    //     // root.getChild("PauseMenu"):getChild("CloseHelpButton"):registerEventHandler("Clicked", function() this.closeHelpButtonClicked() });
    //     // root.getChild("PauseMenu"):getChild("QuitButton"):registerEventHandler("Clicked", function() this.quitButtonClicked() });
    //     //root.getChild("SaveMicrobeButton"):registerEventHandler("Clicked", function() this.saveCreationClicked() })
    //     //root.getChild("LoadMicrobeButton"):registerEventHandler("Clicked", function() this.loadCreationClicked() })

    //     this.helpPanel = root.getChild("PauseMenu"):getChild("HelpPanel");
    //     root.getChild("PauseMenu"):getChild("HelpButton"):registerEventHandler("Clicked", function() this.helpButtonClicked() });

    // // This was commented out in the Lua code
    // // // Set species name and cut it off if it is too long.
    // // //[[ auto name = this.nameLabel.getText()
    // // if(string.len(name) > 18){
    // //     name = string.sub(name, 1, 15);
    // //     name = name .. "...";
    // // }
    // // this.nameLabel.setText(name); //]]


        LOG_WRITE("TODO: lock locked organelles");
        // for(typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons)){
        //     print(typeName);
        //     if(Engine.playerData():lockedMap():isLocked(typeName)){
        //         button.disable();
        //     } else {
        //         button.enable();
        //     }
        // }
    }


    private AudioSource@ _playRandomEditorAmbience()
    {
        AudioSource@ audio = GetEngine().GetSoundDevice().Play2DSound("Data/Sound/" +
            MICROBE_EDITOR_AMBIENT_TRACKS[GetEngine().GetRandom().GetNumber(0, MICROBE_EDITOR_AMBIENT_TRACKS.length() - 1)] +
            ".ogg", false, true);

        if (audio is null)
        {
            LOG_ERROR("Failed to create ambience sound source");
        }

        return audio;
    }


    void handleAmbientSound()
    {
        //randomize ambient sounds out of all available sounds
        // The isPlaying check will start a new track when the previous ends
        if (@ambienceSounds is null || !ambienceSounds.Get().isPlaying())
        {
            @ambienceSounds = _playRandomEditorAmbience();
            ambienceSounds.Get().play();
        }
    }

    //for stoppiong the music when you leave the editor
    void Suspend()
    {
        LOG_INFO("Suspending microbe editor background sounds");
        if(ambienceSounds !is null)
            ambienceSounds.Get().pause();
    }

    void Release()
    {

    }

    int counter = 0;

    void Run()
    {
        int logicTime = TICKSPEED;
        ++counter;

        // Ogre::Quaternion rot(Float4(GetEngine().GetRandom().GetNumber(0.f, 1.f),
        //         GetEngine().GetRandom().GetNumber(0.f, 1.f),
        //         GetEngine().GetRandom().GetNumber(0.f, 1.f),
        //         GetEngine().GetRandom().GetNumber(0.f, 1.f)).Normalize());

        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this
        for(uint i = 0; i < hoverHex.length(); ++i){

            auto node = world.GetComponent_RenderNode(hoverHex[i]);
            node.Node.setPosition(Float3(0, 0, 0));



            // LOG_WRITE("This stuff: " + rot.w + ", " + rot.x + ", " + rot.y + ", " + rot.z);
            // Ogre::Quaternion rot(Ogre::Degree(counter), Ogre::Vector3(0, 1, 0));
            // Ogre::Quaternion rot = Ogre::Quaternion(Ogre::Degree(-90), Ogre::Vector3::UNIT_Z) *
            //     Ogre::Quaternion(Ogre::Degree(-45), Ogre::Vector3::UNIT_Y);
            // Ogre::Quaternion rot = Ogre::Quaternion(
            //     Ogre::Degree(counter), Ogre::Vector3::UNIT_X) *
            //     Ogre::Quaternion(Ogre::Degree(counter), Ogre::Vector3::UNIT_Z) *
            //     Ogre::Quaternion(Ogre::Degree(counter), Ogre::Vector3::UNIT_Y);


            Ogre::Quaternion rot(0.40118, 0.791809, 0.431951, 0.0381477);

            node.Node.setOrientation(rot);
            node.Hidden = true;
            node.Marked = true;
        }

        for(uint i = 0; i < hoverOrganelle.length(); ++i){
            auto node = world.GetComponent_RenderNode(hoverOrganelle[i]);
            node.Node.setPosition(Float3(0, 0, 0));
            node.Hidden = true;
            node.Marked = true;
        }

        this.editor.update(logicTime);

        //since this is ran every step this is a good place to do music code
        handleAmbientSound();
    }

    // Nodes not used
    void Clear() {}
    void CreateAndDestroyNodes() {}


    // Called when the editor is entered. Performs initialization again to make sure the
    // editor works the same on each time it is entered
    void setupHUDAfterEditorEntry()
    {
        // Let go of old resources
        hoverHex.resize(0);
        hoverOrganelle.resize(0);


        // Prepare for a new edit
        editor.init();

        // This seems really cluttered, there must be a better way.
        for(int i = 0; i < 42; ++i){

            ObjectID hex = world.CreateEntity();
            auto node = world.Create_RenderNode(hex);
            // auto pos = world.Create_Position(hex, Float3(0, 0, 0), Float4::IdentityQuaternion);
            world.Create_Model(hex, node.Node, "hex.mesh");
            // world.Create_Model(hex, node.Node, "nucleus.mesh");
            node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
            node.Marked = true;
            node.Node.setPosition(Ogre::Vector3(0, 0, 0));
            // node.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(-90), Ogre::Vector3(0, 1, 0)));
            hoverHex.insertLast(hex);
        }

        for(int i = 0; i < 6; ++i){
            ObjectID hex = world.CreateEntity();
            auto node = world.Create_RenderNode(hex);
            node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
            node.Marked = true;
            node.Node.setPosition(Ogre::Vector3(0, 0, 0));
            hoverOrganelle.insertLast(hex);
        }

        editor.activate();
    }

    void setActiveAction(const string &in actionName)
    {
        this.editor.setActiveAction(actionName);

        if(actionName == "nucleus"){
            // For now we simply create a new microbe with the nucleus button
            this.editor.performLocationAction();
        }
    }

    void updateMutationPoints()
    {
        GenericEvent@ event = GenericEvent("MutationPointsUpdated");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("mp", editor.getMutationPoints()));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    MicrobeEditorWorld@ world
    {
        get
        {
            return _world;
        }
    }

    private AudioSource@ ambienceSounds;
    MicrobeEditor@ editor = null;
    private MicrobeEditorWorld@ _world;

    // TODO: it isn't very clean that the editor directly touches these
    array<ObjectID> hoverHex;
    // Scene nodes for the organelle cursors for symmetry.
    array<ObjectID> hoverOrganelle;

    // this.saveLoadPanel = null;
    // this.creationsListbox = null;

    // Map from player creation name to filepath
    dictionary creationFileMap;

    // this.activeButton = null; // stores button, not name
    bool helpPanelOpen = false;
    bool menuOpen = false;
    void nucleusClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
    setActiveAction("nucleus");
 }

void flagellumClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["flagellum"]
//     this.activeButton.disable()
    setActiveAction("flagellum");
 }

void cytoplasmClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["cytoplasm"]
//     this.activeButton.disable()
    setActiveAction("cytoplasm");
}

void mitochondriaClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["mitochondrion"]
//     this.activeButton.disable()
   setActiveAction("mitochondrion");
}

void chloroplastClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["chloroplast"]
//     this.activeButton.disable()
    setActiveAction("chloroplast");
}

void vacuoleClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["vacuole"]
//     this.activeButton.disable()
    setActiveAction("vacuole");
}

void plastidClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["Toxin"]
//     this.activeButton.disable()
    setActiveAction("nitrogenfixingplastid");
}

void chemoplastClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["Toxin"]
//     this.activeButton.disable()
    setActiveAction("chemoplast");
}

void pilusClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["Toxin"]
//     this.activeButton.disable()
//this.setActiveAction("pilus")
}

void toxinClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["Toxin"]
//     this.activeButton.disable()
    setActiveAction("oxytoxy");
}


void removeClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = null
    setActiveAction("remove");
}
}

// Callbacks from the key handlers
// if(Engine.mouse.wasButtonPressed(Mouse.MB_Left)){
//     this.editor.performLocationAction();
// }
// if(Engine.mouse.wasButtonPressed(Mouse.MB_Right)){
//     this.removeClicked();
//     this.editor.performLocationAction();
// }
// if(keyCombo(kmp.togglemenu)){
//     this.menuButtonClicked();
// } else if(keyCombo(kmp.newmicrobe)){
//     // These global event handlers are defined in microbe_editor_hud.lua
//     this.nucleusClicked();
// } else if(keyCombo(kmp.redo)){
//     this.editor.redo();
// } else if(keyCombo(kmp.remove)){
//     this.removeClicked();
//     this.editor.performLocationAction();
// } else if(keyCombo(kmp.undo)){
//     this.editor.undo();
// } else if(keyCombo(kmp.vacuole)){
//     this.vacuoleClicked();
//     this.editor.performLocationAction();
// } else if(keyCombo(kmp.oxytoxyvacuole)){
//     if(not Engine.playerData():lockedMap():isLocked("Toxin")){
//         this.toxinClicked();
//         this.editor.performLocationAction();
//     }
// } else if(keyCombo(kmp.flagellum)){
//     this.flagellumClicked();
//     this.editor.performLocationAction();
// } else if(keyCombo(kmp.mitochondrion)){
//     this.mitochondriaClicked();
//     this.editor.performLocationAction();
//     //} else if(Engine.keyboard.wasKeyPressed(Keyboard.KC_A) and this.editor.currentMicrobe !is null){
//     //    this.aminoSynthesizerClicked()
//     //    this.editor.performLocationAction()
// } else if(keyCombo(kmp.chloroplast)){
//     if(not Engine.playerData():lockedMap():isLocked("Chloroplast")){
//         this.chloroplastClicked();
//         this.editor.performLocationAction();
//     }
// } else if(keyCombo(kmp.togglegrid)){
//     if(this.editor.gridVisible){
//         this.editor.gridSceneNode.visible = false;
//         this.editor.gridVisible = false;
//     } else {
//         this.editor.gridSceneNode.visible = true;
//         this.editor.gridVisible = true;
//     }
// } else if(keyCombo(kmp.gotostage)){
//     playClicked();
// } else if(keyCombo(kmp.rename)){
//     this.updateMicrobeName();
// }

// if(Engine.keyboard.wasKeyPressed(KEYCODE.KC_LEFT) or
//     Engine.keyboard.wasKeyPressed(KEYCODE.KC_A)){

//     this.editor.organelleRot = (this.editor.organelleRot + 60)%360;
// }
// if(Engine.keyboard.wasKeyPressed(KEYCODE.KC_RIGHT) or
//     Engine.keyboard.wasKeyPressed(KEYCODE.KC_D)){

//     this.editor.organelleRot = (this.editor.organelleRot - 60)%360;
// }

// if(Engine.keyboard.isKeyDown(KEYCODE.KC_LSHIFT)){
//     properties = getComponent(CAMERA_NAME .. 3, this.gameState, OgreCameraComponent).properties;
//     newFovY = properties.fovY + Degree(Engine.mouse.scrollChange()/10);
//     if(newFovY < Degree(10)){
//         newFovY = Degree(10);
//     } else if(newFovY > Degree(120)){
//         newFovY = Degree(120);
//     }
//     properties.fovY = newFovY;
//     properties.touch();
// } else {

// }



// void MicrobeEditorHudSystem.loadmicrobeSelectionChanged(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
// }


// ////////////////////////////////////////////////////////////////-
// // Event handlers //////////////////////////////////////////////-


// void playClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     g_luaEngine.setCurrentGameState(GameState.MICROBE)
// }

// void menuPlayClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     g_luaEngine.currentGameState.guiWindow.getChild("MenuPanel"):hide()
//     playClicked()
// }

// void MicrobeEditorHudSystem.menuMainMenuClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     g_luaEngine.setCurrentGameState(GameState.MAIN_MENU)
// }

// void MicrobeEditorHudSystem.quitButtonClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     Engine.quit()
// }

// // the rest of the event handlers are MicrobeEditorHudSystem methods

// void MicrobeEditorHudSystem.helpButtonClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("HelpPanel"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("CloseHelpButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("ResumeButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("QuicksaveButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("SaveGameButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("LoadGameButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("StatsButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("HelpButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("OptionsButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("MainMenuButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("QuitButton"):hide()
//     this.helpOpen = not this.helpOpen
// }

// void MicrobeEditorHudSystem.closeHelpButtonClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("HelpPanel"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("CloseHelpButton"):hide()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("ResumeButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("QuicksaveButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("SaveGameButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("LoadGameButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("StatsButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("HelpButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("OptionsButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("MainMenuButton"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):getChild("QuitButton"):show()
//     this.helpOpen = not this.helpOpen
// }



// void MicrobeEditorHudSystem.rootSaveCreationClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     print ("Save button clicked")
//     //[[
//     panel = this.saveLoadPanel
//     panel.getChild("SaveButton"):show()
//     panel.getChild("NameTextbox"):show()
//     panel.getChild("CreationNameDialogLabel"):show()
//     panel.getChild("LoadButton"):hide()
//     panel.getChild("SavedCreations"):hide()
//     panel.show()//]]
// }
// //[[
// void MicrobeEditorHudSystem.rootLoadCreationClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     panel = this.saveLoadPanel
//     // panel.getChild("SaveButton"):hide()
//     // root.getChild("CreationNameDialogLabel"):hide()
//     panel.getChild("LoadButton"):show()
//     panel.getChild("SavedCreations"):show()
//     panel.show()
//     this.creationsListbox.listWidgetResetList()
//     this.creationFileMap = {}
//     i = 0
//     pathsString = Engine.getCreationFileList("microbe")
//     // using pattern matching for splitting on spaces
//     for(path in string.gmatch(pathsString, "%S+") ){
//         // this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
//         pathSep = package.config.sub(1,1) // / for unix, \ for windows
//         text = string.sub(path, string.len(path) - string.find(path.reverse(), pathSep) + 2)
//         this.creationsListbox.listWidgetAddItem(text)
//         this.creationFileMap[text] = path
//         i = i + 1
//     }
//     // this.creationsListbox.itemListboxHandleUpdatedItemData()
// }

// void MicrobeEditorHudSystem.saveCreationClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     name = this.editor.currentMicrobe.microbe.speciesName
//     print("saving "..name)
//     // Todo: Additional input sanitation
//     name, _ = string.gsub(name, "%s+", "_") // replace whitespace with underscore
//     if(string.match(name, "^[%w_]+$") == null){
//         print("unsanitary name: "..name) // should we do the test before whitespace sanitization?
//     } else if(string.len(name) > 0){
//         Engine.saveCreation(this.editor.currentMicrobe.entity.id, name, "microbe")
//     }
// end

// void MicrobeEditorHudSystem.loadCreationClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
// 	item = this.creationsListbox.listWidgetGetFirstSelectedItemText()
//     if(this.creationFileMap[item] !is null){
//         entity = Engine.loadCreation(this.creationFileMap[item])
// 		this.updateMicrobeName(Microbe(Entity(entity), true).microbe.speciesName)
//         this.editor.loadMicrobe(entity)
//         panel.hide()
//     }
// end
// //]]

// // useful debug functions

// void MicrobeEditorHudSystem.loadByName(name){
//     if(string.find(name, ".microbe")){
//         print("note, you don't need to add the .microbe extension")
//     } else
//         name = name..".microbe"
//     }
//     name, _ = string.gsub(name, "%s+", "_")
//     creationFileMap = {}
//     i = 0
//     pathsString = Engine.getCreationFileList("microbe")
//     // using pattern matching for splitting on spaces
//     for(path in string.gmatch(pathsString, "%S+") ){
//         // this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
//         pathSep = package.config.sub(1,1) // / for unix, \ for windows
//         text = string.sub(path, string.len(path) - string.find(path.reverse(), pathSep) + 2)
//         creationFileMap[text] = path
//         i = i + 1
//     }
//     entity = Engine.loadCreation(creationFileMap[name])
//     this.editor.loadMicrobe(entity)
//     //this.nameLabel.setText(this.editor.currentMicrobe.microbe.speciesName)
// }

// void MicrobeEditorHudSystem.changeSymmetry(){
//     this.editor.symmetry = (this.editor.symmetry+1)%4

//     if(this.editor.symmetry == 0){
//         this.symmetryButton.getChild("2xSymmetry"):hide()
//         this.symmetryButton.getChild("4xSymmetry"):hide()
//         this.symmetryButton.getChild("6xSymmetry"):hide()
//     } else if(this.editor.symmetry == 1){
//         this.symmetryButton.getChild("2xSymmetry"):show()
//         this.symmetryButton.getChild("4xSymmetry"):hide()
//         this.symmetryButton.getChild("6xSymmetry"):hide()
//     } else if(this.editor.symmetry == 2){
//         this.symmetryButton.getChild("2xSymmetry"):hide()
//         this.symmetryButton.getChild("4xSymmetry"):show()
//         this.symmetryButton.getChild("6xSymmetry"):hide()
//     } else if(this.editor.symmetry == 3){
//         this.symmetryButton.getChild("2xSymmetry"):hide()
//         this.symmetryButton.getChild("4xSymmetry"):hide()
//         this.symmetryButton.getChild("6xSymmetry"):show()
//     }
// end

// void saveMicrobe() global_activeMicrobeEditorHudSystem.saveCreationClicked() }{
// void loadMicrobe(name) global_activeMicrobeEditorHudSystem.loadByName(name) }{

// void MicrobeEditorHudSystem.menuButtonClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     print("played sound")
//     this.gameState.guiWindow.getChild("PauseMenu"):show()
//     this.gameState.guiWindow.getChild("PauseMenu"):moveToFront()
//     Engine.pauseGame()
//     this.menuOpen = true
// }

// void MicrobeEditorHudSystem.resumeButtonClicked(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
//     print("played sound")
//     this.gameState.guiWindow.getChild("PauseMenu"):hide()
//     Engine.resumeGame()
//     this.menuOpen = false
// }
