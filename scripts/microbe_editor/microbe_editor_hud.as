#include "microbe_editor.as"

class MicrobeEditorHudSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this.world = cast<MicrobeEditorWorld>(w);

        assert(this.world !is null, "MicrobeEditorHudSystem didn't get proper world");

        @editor = MicrobeEditor(this);
        editor.init();

    //     // This seems really cluttered, there must be a better way.
    //     for(i=1, 42){
    //         this.hoverHex[i] = Entity("hover-hex" .. i, gameState.wrapper);
    //         auto sceneNode = OgreSceneNodeComponent();
    //         sceneNode.transform.position = Vector3(0,0,0);
    //         sceneNode.transform.touch();
    //         sceneNode.meshName = "hex.mesh";
    //         sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
    //         this.hoverHex[i]:addComponent(sceneNode);
    //     }
    //     for(i=1, 6){
    //         this.hoverOrganelle[i] = Entity("hover-organelle" .. i, gameState.wrapper);
    //         auto sceneNode = OgreSceneNodeComponent();
    //         sceneNode.transform.position = Vector3(0,0,0);
    //         sceneNode.transform.touch();
    //         sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
    //         this.hoverOrganelle[i]:addComponent(sceneNode);
    //     }


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

        // for(typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons)){
        //     print(typeName);
        //     if(Engine.playerData():lockedMap():isLocked(typeName)){
        //         button.disable();
        //     } else {
        //         button.enable();
        //     }
        // }
    }

    void Release(){

    }

    void Run(){

        int logicTime = TICKSPEED;
        this.editor.update(logicTime);

        // for(i=1, 42){
        //     auto sceneNode = getComponent(this.hoverHex[i], OgreSceneNodeComponent);
        //     sceneNode.transform.position = Vector3(0,0,0);
        //     sceneNode.transform.scale = Vector3(0,0,0);
        //     sceneNode.transform.touch();
        // }
        // for(i=1, 6){
        //     auto sceneNode = getComponent(this.hoverOrganelle[i], OgreSceneNodeComponent);
        //     sceneNode.transform.position = Vector3(0,0,0);
        //     sceneNode.transform.scale = Vector3(0,0,0);
        //     sceneNode.transform.touch();
        // }

        // // This is totally the wrong place to have this
        // // Handle input
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
    }

    // Nodes not used
    void Clear(){
    }

    void CreateAndDestroyNodes(){
    }

    private MicrobeEditor@ editor = null;
    private MicrobeEditorWorld@ world;

    // // Scene nodes for the organelle cursors for symmetry.
    // this.hoverHex = {};
    // this.hoverOrganelle = {};

    // this.saveLoadPanel = null;
    // this.creationsListbox = null;
    // this.creationFileMap = {} // Map from player creation name to filepath
    // this.activeButton = null; // stores button, not name
    // this.helpPanelOpen = false;
    // this.menuOpen = false;
}

// void MicrobeEditorHudSystem.loadmicrobeSelectionChanged(){
//     getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
//     ):playSound("button-hover-click")
// }

// void MicrobeEditorHudSystem.setActiveAction(actionName){
//     this.editor.setActiveAction(actionName)
//     if(actionName == "nucleus"){
//         // For now we simply create a new microbe with the nucleus button
//         this.editor.performLocationAction()
//     }
// end


// void MicrobeEditorHudSystem.update(renderTime, logicTime){

// void MicrobeEditorHudSystem.updateMutationPoints() {
//     this.mpProgressBar.progressbarSetProgress(this.editor.mutationPoints/50)
//     this.mpLabel.setText("" .. this.editor.mutationPoints)
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

// void MicrobeEditorHudSystem.nucleusClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.setActiveAction("nucleus")
// }

// void MicrobeEditorHudSystem.flagellumClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["flagellum"]
//     this.activeButton.disable()
//     this.setActiveAction("flagellum")
// }

// void MicrobeEditorHudSystem.cytoplasmClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["cytoplasm"]
//     this.activeButton.disable()
//     this.setActiveAction("cytoplasm")
// }

// void MicrobeEditorHudSystem.mitochondriaClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["mitochondrion"]
//     this.activeButton.disable()
//     this.setActiveAction("mitochondrion")
// }

// void MicrobeEditorHudSystem.chloroplastClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["chloroplast"]
//     this.activeButton.disable()
//     this.setActiveAction("chloroplast")
// }

// void MicrobeEditorHudSystem.aminoSynthesizerClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["aminosynthesizer"]
//     this.activeButton.disable()
//     this.setActiveAction("aminosynthesizer")
// }

// void MicrobeEditorHudSystem.vacuoleClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["vacuole"]
//     this.activeButton.disable()
//     this.setActiveAction("vacuole")
// }

// void MicrobeEditorHudSystem.toxinClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = this.organelleButtons["Toxin"]
//     this.activeButton.disable()
//     this.setActiveAction("oxytoxy")
// }


// void MicrobeEditorHudSystem.removeClicked(){
//     if(this.activeButton !is null){
//         this.activeButton.enable()
//     }
//     this.activeButton = null
//     this.setActiveAction("remove")
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
