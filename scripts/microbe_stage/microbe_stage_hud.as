
// Now in the c++ camera system
// // Camera limits
// CAMERA_MIN_HEIGHT = 20;
// CAMERA_MAX_HEIGHT = 120;
// CAMERA_VERTICAL_SPEED = 0.015;

bool global_if_already_displayed = false;

const array<string> MICROBE_MUSIC_TRACKS = {
    "microbe-theme-1",
    // This doesn't exist //
    /*"microbe-theme-2",*/ "microbe-theme-3", "microbe-theme-4",
    "microbe-theme-5", "microbe-theme-6", "microbe-theme-7"
};
const array<string> MICROBE_AMBIENT_TRACKS = {
    "microbe-ambience", "microbe-ambience2"
};

//! Updates the hud with relevant information from the player cell
class MicrobeStageHudSystem : ScriptSystem{

    void Init(GameWorld@ world){

        @this.World = cast<CellStageWorld>(world);

        assert(this.World !is null, "MicrobeStageHudSystem didn't get proper world");


        // global_activeMicrobeStageHudSystem = self; // Global reference for event handlers

        // TODO: this is probably supposed to be in the Run method so that once the player
        // unlocks the toxin and exits the editor they get this message
        // // No clue where this ss variable is defined
        // bool ss = false;
        // if(not GetThriveGame().playerData().lockedMap().isLocked("Toxin") and
        //     not ss and not global_if_already_displayed
        // ){
        //     showMessage("'E' Releases Toxin");
        //     global_if_already_displayed = true;
        // }

        // Engine.resumeGame();
        // This updates the microbe stage pause menu load button
        this.updateLoadButton();

        this.chloroplastNotificationdisable();
        this.toxinNotificationdisable();
        this.editornotificationdisable();

        // Store compound ids for lookups in Run
        this.atpId = SimulationParameters::compoundRegistry().getTypeId("atp");
        this.atpVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.atpId).volume;

        this.ammoniaId = SimulationParameters::compoundRegistry().getTypeId("ammonia");
        this.ammoniaVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.ammoniaId).volume;

        this.glucoseId = SimulationParameters::compoundRegistry().getTypeId("glucose");
        this.glucoseVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.glucoseId).volume;


        this.oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
        this.oxytoxyVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.oxytoxyId).volume;

        this.phosphateId = SimulationParameters::compoundRegistry().getTypeId("phosphates");
        this.phosphateVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.phosphateId).volume;

        this.hydrogenSulfideId = SimulationParameters::compoundRegistry().getTypeId("hydrogensulfide");
        this.hydrogenSulfideVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.hydrogenSulfideId).volume;

    }

    void handleAmbientSound()
    {
        //randomize ambient sounds out of all available sounds
        // The isPlaying check will start a new track when the previous ends
        if (@ambienceSounds is null || !ambienceSounds.Get().isPlaying())
        {
            @ambienceSounds = _playRandomMicrobeMusic();
            ambienceSounds.Get().play();
        }

        //play ambient track alongside music and loop it (its meant to be played alongside)
        if (@ambientTrack is null || !ambientTrack.Get().isPlaying())
        {
            @ambientTrack = _playRandomMicrobeAmbience();
            ambientTrack.Get().play();
        }
    }

    void Release(){

    }

    void Run(){

        ObjectID player = GetThriveGame().playerData().activeCreature();

        // Update player stats if there is a cell currently
        if(player != NULL_OBJECT){

            auto bag = World.GetComponent_CompoundBagComponent(player);
            auto playerSpecies = MicrobeOperations::getSpeciesComponent(World, "Default");
            MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
                World.GetScriptComponentHolder("MicrobeComponent").Find(player));

            GenericEvent@ event = GenericEvent("PlayerCompoundAmounts");
            GenericEvent@ changePopulation = GenericEvent("PopulationChange");
            NamedVars@ vars = event.GetNamedVars();
            NamedVars@ populationVars = changePopulation.GetNamedVars();

            // Write data
            vars.AddValue(ScriptSafeVariableBlock("hitpoints",
                    int(microbeComponent.hitpoints)));
            vars.AddValue(ScriptSafeVariableBlock("maxHitpoints",
                    int(microbeComponent.maxHitpoints)));
            populationVars.AddValue(ScriptSafeVariableBlock("populationAmount", playerSpecies.population));

            if(bag is null){

                LOG_ERROR("Player activeCreature has no compound bag");

            } else {

                const auto phosphateAmount = bag.getCompoundAmount(phosphateId);
                const auto maxPhosphate = microbeComponent.capacity;

                const auto hydrogenSulfideAmount = bag.getCompoundAmount(hydrogenSulfideId);
                const auto maxHydrogenSulfide = microbeComponent.capacity;

                const auto atpAmount = bag.getCompoundAmount(atpId);
                const auto maxATP = microbeComponent.capacity;

                const auto ammoniaAmount = bag.getCompoundAmount(ammoniaId);
                const auto maxAmmonia = microbeComponent.capacity;

                const auto glucoseAmount = bag.getCompoundAmount(glucoseId);
                const auto maxGlucose = microbeComponent.capacity;

                const auto oxytoxyAmount = bag.getCompoundAmount(oxytoxyId);
                const auto maxOxytoxy = microbeComponent.capacity;

                // Write data
                vars.AddValue(ScriptSafeVariableBlock("compoundPhosphate", phosphateAmount));
                vars.AddValue(ScriptSafeVariableBlock("PhosphateMax", maxPhosphate));

                vars.AddValue(ScriptSafeVariableBlock("compoundHydrogenSulfide", hydrogenSulfideAmount));
                vars.AddValue(ScriptSafeVariableBlock("HydrogenSulfideMax", maxHydrogenSulfide));

                vars.AddValue(ScriptSafeVariableBlock("compoundATP", atpAmount));
                vars.AddValue(ScriptSafeVariableBlock("ATPMax", maxATP));

                vars.AddValue(ScriptSafeVariableBlock("compoundAmmonia", ammoniaAmount));
                vars.AddValue(ScriptSafeVariableBlock("AmmoniaMax", maxAmmonia));

                vars.AddValue(ScriptSafeVariableBlock("compoundGlucose", glucoseAmount));
                vars.AddValue(ScriptSafeVariableBlock("GlucoseMax", maxGlucose));

                vars.AddValue(ScriptSafeVariableBlock("compoundOxytoxy", oxytoxyAmount));
                vars.AddValue(ScriptSafeVariableBlock("OxytoxyMax", maxOxytoxy));
            }

            // Fire it off so that the GUI scripts will get it and update the GUI state
            GetEngine().GetEventHandler().CallEvent(event);
            GetEngine().GetEventHandler().CallEvent(changePopulation);
        }

        //since this is ran every step this is a good place to do music code
        handleAmbientSound();
    }

    // Nodes not used
    void Clear(){
    }

    void CreateAndDestroyNodes(){
    }

    //! This stops sound while the cell stage world isn't active
    void Suspend(){

        LOG_INFO("Suspending microbe stage background sounds");

        // Pause to allow resuming
        if(ambientTrack !is null)
            ambientTrack.Get().pause();

        if(ambienceSounds !is null)
            ambienceSounds.Get().pause();
    }

    //! This resumes sound when the cell stage world is active again
    void Resume(){

        LOG_INFO("Resuming microbe stage background sounds");

        if(ambientTrack !is null)
            ambientTrack.Get().play();

        if(ambienceSounds !is null)
            ambienceSounds.Get().play();
    }


    void updateLoadButton(){

        if(FileSystem::FileExists("quick.sav")){
            //this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").enable();
        } else {
            //this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").disable();
        }
    }

    void chloroplastNotificationenable(){
        LOG_INFO("TODO: hud");
        // getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
        // ).playSound("microbe-pickup-organelle");
        // this.rootGUIWindow.getChild("chloroplastUnlockNotification").show();
        b1 = true;
        // this.rootGUIWindow.getChild("toxinUnlockNotification").hide();
    }

    void chloroplastNotificationdisable(){
        LOG_INFO("TODO: hud");
        //this.rootGUIWindow.getChild("chloroplastUnlockNotification").hide();
    }

    void toxinNotificationenable(){
        LOG_INFO("TODO: hud");
        // getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
        // ).playSound("microbe-pickup-organelle");
        // this.rootGUIWindow.getChild("toxinUnlockNotification").show();
        b2 = true;
        //this.rootGUIWindow.getChild("chloroplastUnlockNotification").hide();
    }

    void toxinNotificationdisable(){
        //this.rootGUIWindow.getChild("toxinUnlockNotification").hide();
    }
    void editornotificationdisable(){
        //this.rootGUIWindow.getChild("editornotification").hide();
    }

    void showReproductionDialog(){
        // print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
        //global_activeMicrobeStageHudSystem.rootGUIWindow.getChild("ReproductionPanel").show()
        if(b3 == false){

            b3 = true;

            GetEngine().GetSoundDevice().Play2DSoundEffect(
                "Data/Sound/soundeffects/microbe-pickup-organelle.ogg");

            LOG_INFO("Ready to reproduce!");
            GenericEvent@ event = GenericEvent("PlayerReadyToEnterEditor");
            GetEngine().GetEventHandler().CallEvent(event);
        }
    }

    void suicideButtonClicked(){
        // getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
        if(boolean2 == false){
            boolean = true;
        }
    }
    void suicideButtondisable(){
        // this.rootGUIWindow.getChild("SuicideButton").disable();
    }
    void suicideButtonreset(){
        boolean2 = false;
    }

    private AudioSource@ _playRandomMicrobeMusic(){

        AudioSource@ audio = GetEngine().GetSoundDevice().Play2DSound("Data/Sound/" +
            MICROBE_MUSIC_TRACKS[GetEngine().GetRandom().GetNumber(0,
                    MICROBE_MUSIC_TRACKS.length() - 1)] + ".ogg", false, true);

        if (audio is null)
        {
            LOG_ERROR("Failed to create ambience music source");
        }

        return audio;
    }

        private AudioSource@ _playRandomMicrobeAmbience(){
    string track = MICROBE_AMBIENT_TRACKS[GetEngine().GetRandom().GetNumber(0,
                    MICROBE_AMBIENT_TRACKS.length() - 1)] + ".ogg";
        AudioSource@ audio = GetEngine().GetSoundDevice().Play2DSound("Data/Sound/soundeffects/" +track, false, true);
    if (track == "microbe-ambience2.ogg")
    {
    audio.Get().setVolume(0.25);
    }
        if (audio is null)
        {
            LOG_ERROR("Failed to create ambience sound source");
        }

        return audio;
    }
    private CellStageWorld@ World;


    int t1 = 0;
    int t2 = 0;
    int t3 = 0;
    bool b1 = false;
    bool b2 = false;
    bool b3 = false;
    //suicideButton setting up
    // really creative naming scheme
    bool boolean = false;
    bool boolean2 = false;
    //hints setting up
    bool hintsPanelOpned = false;
    bool healthHint = false;
    bool atpHint = false;
    bool glucoseHint = false;
    bool ammoniaHint = false;
    bool phosphateHint = false;
    bool hydrogenSulfideHint = false;
    bool toxinHint = false;
    bool chloroplastHint = false;
    dictionary activeHints = {};
    int hintN = 0;
    int currentHint = 1;
    bool HHO = false;
    bool AHO = false;
    bool GHO = false;
    bool AMHO = false;
    bool OHO = false;
    bool THO = false;
    bool CHO = false;
    int glucoseNeeded = 0;
    int atpNeeded = 0;
    int ammoniaNeeded = 0;
    int chloroplastNeeded = 0;
    int toxinNeeded = 0;

    // TODO: rewrite using Leviathan GuiCollection objects
    bool helpOpen = false;
    bool menuOpen = false;
    // Not this one as this isn't really a collection, just a
    // toggleable panel with a single button
    bool compoundsOpen = true;

    //instantiate our ambient music source
    AudioSource@ ambienceSounds;
    //plays alongside music
    AudioSource@ ambientTrack;

    CompoundId phosphateId;
    float phosphateVolume;

    CompoundId hydrogenSulfideId;
    float hydrogenSulfideVolume;

    CompoundId atpId;
    float atpVolume;

    CompoundId ammoniaId;
    float ammoniaVolume;

    CompoundId glucoseId;
    float glucoseVolume;

    CompoundId oxytoxyId;
    float oxytoxyVolume;

}


// void HudSystem.update(renderTime, logicTime){
// // TODO: use the QuickSaveSystem here? is this duplicated functionality?
// auto saveDown = Engine.keyboard.isKeyDown(KEYCODE.KC_F4)
// auto loadDown = Engine.keyboard.isKeyDown(KEYCODE.KC_F10)
// if(saveDown and not this.saveDown){
// Engine.save("quick.sav")
// }
// if(loadDown and not this.loadDown){
// Engine.load("quick.sav")
// }
// this.saveDown = saveDown
// this.loadDown = loadDown
// }


// void HudSystem.init(gameState){
//     this.rootGUIWindow =  gameState.rootGUIWindow();

//     auto menuButton = this.rootGUIWindow.getChild("MenuButton");
//     auto saveButton = this.rootGUIWindow.getChild("PauseMenu").getChild("QuicksaveButton") ;
//     auto loadButton = this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton");
//     auto resumeButton = this.rootGUIWindow.getChild("PauseMenu").getChild("ResumeButton");
//     auto closeHelpButton = this.rootGUIWindow.getChild("PauseMenu").getChild("CloseHelpButton");
//     local chloroplast_unlock_notification = this.rootGUIWindow.getChild("chloroplastUnlockNotification");
//     local toxin_unlock_notification = this.rootGUIWindow.getChild("toxinUnlockNotification");
//     auto nextHint = this.rootGUIWindow.getChild("HintsPanel").getChild("NextHint");
//     auto lastHint = this.rootGUIWindow.getChild("HintsPanel").getChild("LastHint");
//     //auto collapseButton = this.rootGUIWindow.getChild() collapseButtonClicked
//     auto hintsButton = this.rootGUIWindow.getChild("HintsButton");
//     auto helpButton = this.rootGUIWindow.getChild("PauseMenu").getChild("HelpButton");
//     auto helpPanel = this.rootGUIWindow.getChild("PauseMenu").getChild("HelpPanel");
//     this.editorButton = this.rootGUIWindow.getChild("EditorButton");
//     auto suicideButton = this.rootGUIWindow.getChild("SuicideButton");
//     //auto returnButton = this.rootGUIWindow.getChild("MenuButton")
//     auto compoundButton = this.rootGUIWindow.getChild("CompoundExpandButton");
//     //auto compoundPanel = this.rootGUIWindow.getChild("CompoundsOpen")
//     auto quitButton = this.rootGUIWindow.getChild("PauseMenu").getChild("QuitButton");
//     nextHint.registerEventHandler("Clicked", function() this.nextHintButtonClicked());
//     lastHint.registerEventHandler("Clicked", function() this.lastHintButtonClicked());
//     hintsButton.registerEventHandler("Clicked", function() this.hintsButtonClicked());
//     saveButton.registerEventHandler("Clicked", function() this.saveButtonClicked());
//     loadButton.registerEventHandler("Clicked", function() this.loadButtonClicked());
//     menuButton.registerEventHandler("Clicked", function() this.menuButtonClicked());
//     resumeButton.registerEventHandler("Clicked", function() this.resumeButtonClicked());
//     closeHelpButton.registerEventHandler("Clicked", function() this.closeHelpButtonClicked());
//     helpButton.registerEventHandler("Clicked", function() this.helpButtonClicked());
//     suicideButton.registerEventHandler("Clicked", function() this.suicideButtonClicked());
//     this.editorButton.registerEventHandler("Clicked", function() this.editorButtonClicked());
//     //returnButton.registerEventHandler("Clicked", returnButtonClicked)
//     compoundButton.registerEventHandler("Clicked", function() this.toggleCompoundPanel());
//     //compoundPanel.registerEventHandler("Clicked", function() this.closeCompoundPanel())
//     quitButton.registerEventHandler("Clicked", quitButtonClicked);
//     this.rootGUIWindow.getChild("PauseMenu").getChild("MainMenuButton").registerEventHandler("Clicked", function() this.menuMainMenuClicked());
//     this.updateLoadButton();
// }


// void HudSystem.update(renderTime){
//     auto player = Entity("player", this.gameState.wrapper);
//     auto microbeComponent = getComponent(player, MicrobeComponent);
//     auto soundSourceComponent = getComponent(player, SoundSourceComponent);

//
//     auto playerSpecies = MicrobeSystem.getSpeciesComponent(player);
//     //notification setting up
//     if(b1 == true and t1 < 300){
//         t1 = t1 + 2;
//         if(hintsPanelOpned == true){
//             this.hintsButtonClicked();
//         }
//         if(t1 == 300){
//             global_activeMicrobeStageHudSystem.chloroplastNotificationdisable();
//             this.hintsButtonClicked();
//         }
//     }

//     if(b2 == true and t2 < 300){
//         t2 = t2 + 2;
//         if(hintsPanelOpned == true){
//             this.hintsButtonClicked();
//         }
//         if(t2 == 300){
//             global_activeMicrobeStageHudSystem.toxinNotificationdisable();
//             this.hintsButtonClicked();
//         }
//     }

//     if(b3 == true and t3 < 300){
//         t3 = t3 + 2;
//         if(hintsPanelOpned == true){
//             this.hintsButtonClicked();
//         }
//         if(t3 == 300){
//             global_activeMicrobeStageHudSystem.editornotificationdisable();
//         }
//     }

//     //suicideButton setting up
//     auto atp = MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("atp"));
//     if(atp == 0 and boolean2 == false){
//         this.rootGUIWindow.getChild("SuicideButton").enable();
//     } else if(atp > 0 or boolean2 == true){
//             global_activeMicrobeStageHudSystem.suicideButtondisable();
//     }
//     if(boolean == true){
//         MicrobeSystem.kill(player);
//         boolean = false;
//         boolean2 = true;
//     }

//     //Hints setup
//     auto glucose = MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("glucose"));
//     auto ammonia = MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("ammonia"));
//     auto oxygen = MicrobeSystem.getCompoundAmount(player, CompoundRegistry.getCompoundId("oxygen"));
//     atpNeeded = math.floor (30 - atp);
//     glucoseNeeded = math.floor (16 - glucose);
//     ammoniaNeeded = math.floor (12 - ammonia);
//     oxygenNeeded = math.floor (15 - oxygen);
//     chloroplastNeeded = math.floor (3 - chloroplast_Organelle_Number);
//     toxinNeeded = math.floor (3 - toxin_Organelle_Number);

//     if(microbeComponent.hitpoints < microbeComponent.maxHitpoints and healthHint == false and HHO == false){
//         activeHints["healthHint"] = hintN + 1;
//         hintN = activeHints["healthHint"];
//         HHO = true;
//     } else if (microbeComponent.hitpoints == microbeComponent.maxHitpoints and healthHint == true and HHO == true){
//         activeHints["healthHint"] = null;
//         hintN = hintN - 1;
//         healthHint = false;
//         HHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(atp < 15 and atpHint == false and AHO == false){
//         activeHints["atpHint"] = hintN + 1;
//         hintN = activeHints["atpHint"];
//         AHO = true;
//     } else if(atp > 30 and atpHint == true and AHO == true){
//         activeHints["atpHint"] = null;
//         hintN = hintN - 1;
//         atpHint = false;
//         AHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(glucose < 1 and glucoseHint == false and GHO == false){
//         activeHints["glucoseHint"] = hintN + 1;
//         hintN = activeHints["glucoseHint"];
//         GHO = true;
//     } else if(glucose >= 16 and glucoseHint == true and GHO == true){
//         activeHints["glucoseHint"] = null;
//         hintN = hintN - 1;
//         glucoseHint = false;
//         GHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(ammonia < 1 and ammoniaHint == false and AMHO == false){
//         activeHints["ammoniaHint"] = hintN + 1;
//         hintN = activeHints["ammoniaHint"];
//         AMHO = true;
//     } else if(ammonia >= 12 and ammoniaHint == true and AMHO == true){
//         activeHints["ammoniaHint"] = null;
//         hintN = hintN - 1;
//         ammoniaHint = false;
//         AMHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(oxygen < 1 and oxygenHint == false and OHO == false){
//         activeHints["oxygenHint"] = hintN + 1;
//         hintN = activeHints["oxygenHint"];
//         OHO = true;
//     } else if(oxygen >= 12 and oxygenHint == true and OHO == true){
//         activeHints["oxygenHint"] = null;
//         hintN = hintN - 1;
//         oxygenHint = false;
//         OHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(toxin_Organelle_Number < 3 and toxinHint == false and THO == false){
//         activeHints["toxinHint"] = hintN + 1;
//         hintN = activeHints["toxinHint"];
//         THO = true;
//     } else if(toxin_Organelle_Number >= 3 and toxinHint == true and THO == true){
//         activeHints["toxinHint"] = null;
//         hintN = hintN - 1;
//         toxinHint = false;
//         THO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     if(chloroplast_Organelle_Number < 3 and chloroplastHint == false and CHO == false){
//         activeHints["chloroplastHint"] = hintN + 1;
//         hintN = activeHints["chloroplastHint"];
//         CHO = true;
//     } else if(chloroplast_Organelle_Number >= 3 and chloroplastHint == true and CHO == true){
//         activeHints["chloroplastHint"] = null;
//         hintN = hintN - 1;
//         chloroplastHint = false;
//         CHO = false;
//         if(next(activeHints) !is null){
//             currentHint = currentHint + 1;
//         }
//     }

//     //print (toxin_Organelle_Number .. " " .. chloroplast_Organelle_Number)
//     if(healthHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild(
//             "HelpText").setText(
//                 "Your cell is damaged! Collect ammonia and glucose to make amino acids, which can heal it.");
//     }

//     if(atpHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "You're running short of ATP! ATP is used to move and engulf. Get " .. atpNeeded .. " to be safe!"
//         );
//     }

//     if(glucoseHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "You need more glucose! It's used to make ATP and amino acids. Collect " .. glucoseNeeded .. " to be safe."
//         );
//     }

//     if(ammoniaHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "You have little ammonia, used to make amino acids to heal and reproduce. Get " .. ammoniaNeeded .. " more."
//         );
//     }

//     if(oxygenHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "You need oxygen to produce ATP and OxyToxy. Collect " .. oxygenNeeded .. " oxygen to do this!"
//         );
//     }

//     if(chloroplastHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "Pick " .. chloroplastNeeded .. " green blobs to unlock Chloroplasts, which transform CO2 into glucose and oxygen."
//         );
//     }

//     if(toxinHint == true){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText(
//             "Collect " .. toxinNeeded .. " blue blobs to unlock Toxin Vacuoles, used to shoot harmful agents at other cells."
//         );
//     }

//     for(hintnam,hintnum in pairs(activeHints)){
//         if(hintnum == currentHint){
//             if(hintnam == "atpHint"){
//                 atpHint = true;
//             } else {
//                 atpHint = false;
//             }

//             if(hintnam == "healthHint"){
//                 healthHint = true;
//             } else {
//                 healthHint = false;
//             }

//             if(hintnam == "glucoseHint"){
//                 glucoseHint = true;
//             } else {
//                 glucoseHint = false;
//             }

//             if(hintnam == "ammoniaHint"){
//                 ammoniaHint = true;
//             } else {
//                 ammoniaHint = false;
//             }

//             if(hintnam == "oxygenHint"){
//                 oxygenHint = true;
//             } else {
//                 oxygenHint = false;
//             }

//             if(hintnam == "chloroplastHint"){
//                 chloroplastHint = true;
//             } else {
//                 chloroplastHint = false;
//             }

//             if(hintnam == "toxinHint"){
//                 toxinHint = true;
//             } else {
//                 toxinHint = false;
//             }
//         }
//     }

//     if(next(activeHints) == null){
//         this.rootGUIWindow.getChild("HintsPanel").getChild("HelpText").setText("there is no available hints for now!");
//     }

//     if(currentHint > hintN){
//         currentHint = 1;
//     }

//     if(currentHint < 1){
//         currentHint = hintN;
//     }

//     //TODO display population in home patch here

//     if(keyCombo(kmp.togglemenu)){
//         this.menuButtonClicked();
//     } else if(keyCombo(kmp.gotoeditor)){
//         this.editorButtonClicked();
//     } else if(keyCombo(kmp.shootoxytoxy)){
//         MicrobeSystem.emitAgent(player, CompoundRegistry.getCompoundId("oxytoxy"), 3);
//     } else if(keyCombo(kmp.reproduce)){
//         MicrobeSystem.readyToReproduce(player);
//     }
//     auto direction = Vector3(0, 0, 0);
//     if(keyCombo(kmp.forward)){
//         soundSourceComponent.playSound("microbe-movement-2");
//     }
//     if(keyCombo(kmp.backward)){
//         soundSourceComponent.playSound("microbe-movement-2");
//     }
//     if(keyCombo(kmp.leftward)){
//         soundSourceComponent.playSound("microbe-movement-1");
//     }
//     if(keyCombo(kmp.screenshot)){
//         Engine.screenShot("screenshot.png");
//     }
//     if(keyCombo(kmp.rightward)){
//         soundSourceComponent.playSound("microbe-movement-1");
//     }
//     if((Engine.keyboard.wasKeyPressed(KEYCODE.KC_G))){
//         MicrobeSystem.toggleEngulfMode(player);
//     }


//     // Changing the camera height according to the player input.
//     // Now in the c++ system
//     // auto offset = getComponent(CAMERA_NAME, this.gameState, OgreCameraComponent).properties.offset;

//     // if(Engine.mouse.scrollChange() ~= 0){
//     //     this.scrollChange = this.scrollChange + Engine.mouse.scrollChange() * CAMERA_VERTICAL_SPEED;
//     // } else if(keyCombo(kmp.plus) or keyCombo(kmp.add)){
//     //     this.scrollChange = this.scrollChange - 5;
//     // } else if(keyCombo(kmp.minus) or keyCombo(kmp.subtract)){
//     //     this.scrollChange = this.scrollChange + 5;
//     // }


//     // auto newZVal = offset.z;
//     // if(this.scrollChange >= 1){
//     //     newZVal = newZVal + 2.5;
//     //     this.scrollChange = this.scrollChange - 1;
//     // } else if(this.scrollChange <= -1){
//     //     newZVal = newZVal - 2.5;
//     //     this.scrollChange = this.scrollChange + 1;
//     // }

//     // if(newZVal < CAMERA_MIN_HEIGHT){
//     //     newZVal = CAMERA_MIN_HEIGHT;
//     //     this.scrollChange = 0;
//     // } else if(newZVal > CAMERA_MAX_HEIGHT){
//     //     newZVal = CAMERA_MAX_HEIGHT;
//     //     this.scrollChange = 0;
//     // }

//     // offset.z = newZVal;
// }



// ------------------------------------ //
// Wrappers for calling GUI update things from random places

void showReproductionDialog(GameWorld@ world){
    cast<MicrobeStageHudSystem@>(world.GetScriptSystem("MicrobeStageHudSystem")).
        showReproductionDialog();
}

void showMessage(const string &in msg){
    LOG_INFO(msg + " (note, in-game messages currently disabled)");
    //auto messagePanel = Engine.currentGameState().rootGUIWindow().getChild("MessagePanel")
    //messagePanel.getChild("MessageLabel").setText(msg)
    //messagePanel.show()
}

// //Event handlers

// void HudSystem.hintsButtonClicked(){
//     if(hintsPanelOpned == false){
//         this.rootGUIWindow.getChild("HintsPanel").show();
//         this.rootGUIWindow.getChild("HintsButton").setText("");
//         this.rootGUIWindow.getChild("HintsButton").getChild("HintsIcon").hide();
//         this.rootGUIWindow.getChild("HintsButton").getChild("HintsContractIcon").show();
//         this.rootGUIWindow.getChild("HintsButton").setProperty("Hide the hints panel", "TooltipText");
//         hintsPanelOpned = true;
//     } else if(hintsPanelOpned == true){
//         this.rootGUIWindow.getChild("HintsPanel").hide();
//         this.rootGUIWindow.getChild("HintsButton").setText("Hints");
//         this.rootGUIWindow.getChild("HintsButton").getChild("HintsIcon").show();
//         this.rootGUIWindow.getChild("HintsButton").getChild("HintsContractIcon").hide();
//         this.rootGUIWindow.getChild("HintsButton").setProperty("Open the hints panel", "TooltipText");
//         hintsPanelOpned = false;
//     }
// }

// void HudSystem.nextHintButtonClicked(){
//     currentHint = currentHint + 1;
// }
// void HudSystem.lastHintButtonClicked(){
//     currentHint = currentHint - 1;
// }
// void HudSystem.saveButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     Engine.save("quick.sav");
//     print("Game Saved");
//     //Because using update load button here doesn't seem to work unless you press save twice
//     this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").enable();
// }
// void HudSystem.loadButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     Engine.load("quick.sav");
//     print("Game loaded");
//     this.rootGUIWindow.getChild("PauseMenu").hide();
//     this.menuOpen = false;
// }

// void HudSystem.menuButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     print("played sound");
//     this.rootGUIWindow.getChild("PauseMenu").show();
//     this.rootGUIWindow.getChild("PauseMenu").moveToFront();
//     this.updateLoadButton();
//     Engine.pauseGame();
//     this.menuOpen = true;
// }

// void HudSystem.resumeButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     print("played sound");
//     this.rootGUIWindow.getChild("PauseMenu").hide();
//     this.updateLoadButton();
//     Engine.resumeGame();
//     this.menuOpen = false;
// }


// void HudSystem.toggleCompoundPanel(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     if(this.compoundsOpen){
//         this.rootGUIWindow.getChild("CompoundPanel").hide();
//         this.rootGUIWindow.getChild("CompoundExpandButton").getChild("CompoundExpandIcon").hide();
//         this.rootGUIWindow.getChild("CompoundExpandButton").getChild("CompoundContractIcon").show();
//         this.compoundsOpen = false;
//     } else {
//         this.rootGUIWindow.getChild("CompoundPanel").show();
//         this.rootGUIWindow.getChild("CompoundExpandButton").getChild("CompoundExpandIcon").show();
//         this.rootGUIWindow.getChild("CompoundExpandButton").getChild("CompoundContractIcon").hide();
//         this.compoundsOpen = true;
//     }
// }






// void HudSystem.helpButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     this.rootGUIWindow.getChild("PauseMenu").getChild("HelpPanel").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("CloseHelpButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("ResumeButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("QuicksaveButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("SaveGameButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("StatsButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("HelpButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("OptionsButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("MainMenuButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("QuitButton").hide();
//     this.helpOpen = not this.helpOpen;
// }


// void HudSystem.closeHelpButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     this.rootGUIWindow.getChild("PauseMenu").getChild("HelpPanel").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("CloseHelpButton").hide();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("ResumeButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("QuicksaveButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("SaveGameButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("StatsButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("HelpButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("OptionsButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("MainMenuButton").show();
//     this.rootGUIWindow.getChild("PauseMenu").getChild("QuitButton").show();
//     this.helpOpen = not this.helpOpen;
// }

// void HudSystem.menuMainMenuClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     g_luaEngine.setCurrentGameState(GameState.MAIN_MENU);
// }


// void HudSystem.editorButtonClicked(){
//     auto player = Entity("player", this.gameState.wrapper);
//     // Return the first cell to its normal, non duplicated cell arangement.
//     SpeciesSystem.restoreOrganelleLayout(player, MicrobeSystem.getSpeciesComponent(player));

//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     this.editorButton.disable();
//     b3 = false;
//     t3 = 0;
//     g_luaEngine.setCurrentGameState(GameState.MICROBE_EDITOR);
// }

// // //[[
// // void HudSystem.returnButtonClicked(){
// //     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
// //         //Engine.currentGameState().rootGUIWindow().getChild("MenuPanel").hide()
// //         if(Engine.currentGameState().name() == "microbe"){
// //             Engine.currentGameState().rootGUIWindow().getChild("HelpPanel").hide();
// //                 Engine.currentGameState().rootGUIWindow().getChild("MessagePanel").hide();
// //                 Engine.currentGameState().rootGUIWindow().getChild("ReproductionPanel").hide();
// //                 Engine.resumeGame();
// //             else if(Engine.currentGameState().name() == "microbe_editor"){
// //                 Engine.currentGameState().rootGUIWindow().getChild("SaveLoadPanel").hide()
// //                     }
// //         } //]]

// void quitButtonClicked(){
//     getComponent("gui_sounds", this.gameState, SoundSourceComponent).playSound("button-hover-click");
//     Engine.quit();
// }
