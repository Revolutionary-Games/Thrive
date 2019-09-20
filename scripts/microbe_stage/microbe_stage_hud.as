
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

        this.ironId = SimulationParameters::compoundRegistry().getTypeId("iron");
        this.ironVolume = SimulationParameters::compoundRegistry().getTypeData(
            this.ironId).volume;

    }

    void handleAmbientSound()
    {
        //randomize ambient sounds out of all available sounds
        // The isPlaying check will start a new track when the previous ends
        if (@ambienceSounds is null || !ambienceSounds.IsPlaying())
        {
            @ambienceSounds = _playRandomMicrobeMusic();
        }

        //play ambient track alongside music and loop it (its meant to be played alongside)
        if (@ambientTrack is null || !ambientTrack.IsPlaying())
        {
            @ambientTrack = _playRandomMicrobeAmbience();
        }
    }

    void Release(){

    }

    void Run(){

        ObjectID player = GetThriveGame().playerData().activeCreature();

        //since this is ran every step this is a good place to do music code
        handleAmbientSound();

        // Update player stats if there is a cell currently
        if(player != NULL_OBJECT){

            auto bag = World.GetComponent_CompoundBagComponent(player);

            Species@ playerSpecies = MicrobeOperations::getSpecies(
                GetThriveGame().getCellStage(), GetThriveGame().playerData().activeCreature());

            MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
                World.GetScriptComponentHolder("MicrobeComponent").Find(player));

            if(microbeComponent is null){

                return;
            }

            GenericEvent@ event = GenericEvent("PlayerCompoundAmounts");
            GenericEvent@ changePopulation = GenericEvent("PopulationChange");
            NamedVars@ vars = event.GetNamedVars();
            NamedVars@ populationVars = changePopulation.GetNamedVars();

            // Write data
            vars.AddValue(ScriptSafeVariableBlock("hitpoints",
                    int(microbeComponent.hitpoints)));
            vars.AddValue(ScriptSafeVariableBlock("maxHitpoints",
                    int(microbeComponent.maxHitpoints)));
            populationVars.AddValue(ScriptSafeVariableBlock("populationAmount",
                    playerSpecies.population));

            {
                // Get player reproduction progress
                dictionary gatheredCompounds;
                dictionary totalNeededCompounds;
                const auto totalProgress = MicrobeOperations::calculateReproductionProgress(
                    microbeComponent, gatheredCompounds, totalNeededCompounds, bag);

                float fractionOfAmmonia = 0;
                float fractionOfPhosphates = 0;

                float gatheredAmmonia, neededAmmonia, gatheredPhosphates, neededPhosphates;

                if(gatheredCompounds.get("ammonia", gatheredAmmonia) &&
                    totalNeededCompounds.get("ammonia", neededAmmonia))
                {
                    fractionOfAmmonia = gatheredAmmonia / neededAmmonia;
                } else {
                    LOG_WARNING("can't get reproduction ammonia progress");
                }

                if(gatheredCompounds.get("phosphates", gatheredPhosphates) &&
                    totalNeededCompounds.get("phosphates", neededPhosphates))
                {
                    fractionOfPhosphates = gatheredPhosphates / neededPhosphates;
                } else {
                    LOG_WARNING("can't get reproduction phosphates progress");
                }

                LOG_WRITE("total progress: " + totalProgress + " ammonia: " +
                    fractionOfAmmonia + " phosphates: " + fractionOfPhosphates);
            }

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

                const auto ironAmount = bag.getCompoundAmount(ironId);
                const auto maxIron = microbeComponent.capacity;

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

                vars.AddValue(ScriptSafeVariableBlock("compoundIron", ironAmount));
                vars.AddValue(ScriptSafeVariableBlock("IronMax", maxIron));
            }

            // Fire it off so that the GUI scripts will get it and update the GUI state
            GetEngine().GetEventHandler().CallEvent(event);
            GetEngine().GetEventHandler().CallEvent(changePopulation);
        }
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
            ambientTrack.Pause();

        if(ambienceSounds !is null)
            ambienceSounds.Pause();
    }

    //! This resumes sound when the cell stage world is active again
    void Resume(){

        LOG_INFO("Resuming microbe stage background sounds");

        if(ambientTrack !is null)
            ambientTrack.Resume();

        if(ambienceSounds !is null)
            ambienceSounds.Resume();

        // This is called when you come back from teh editor, so set reproduction to false
        reproductionDialogOpened=false;
    }


    void updateLoadButton(){

        if(FileSystem::FileExists("quick.sav")){
            //this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").enable();
        } else {
            //this.rootGUIWindow.getChild("PauseMenu").getChild("LoadGameButton").disable();
        }
    }

    void showReproductionDialog(){
        // print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
        //global_activeMicrobeStageHudSystem.rootGUIWindow.getChild("ReproductionPanel").show()
        if(reproductionDialogOpened == false){

            reproductionDialogOpened = true;

            GetEngine().GetSoundDevice().Play2DSoundEffect(
                "Data/Sound/soundeffects/microbe-pickup-organelle.ogg");
            LOG_INFO("Ready to reproduce!");
            GenericEvent@ event = GenericEvent("PlayerReadyToEnterEditor");
            GetEngine().GetEventHandler().CallEvent(event);
        }
    }

    void hideReproductionDialog(){
         reproductionDialogOpened = false;
         GenericEvent@ event = GenericEvent("PlayerDiedBeforeEnter");
         GetEngine().GetEventHandler().CallEvent(event);
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
                    MICROBE_MUSIC_TRACKS.length() - 1)] + ".ogg", false);
        if (audio !is null){
            audio.SetVolume(0.8);
        } else {
            LOG_ERROR("Failed to create ambiance music source");
        }
        return audio;
    }

    private AudioSource@ _playRandomMicrobeAmbience(){
        string track = MICROBE_AMBIENT_TRACKS[GetEngine().GetRandom().GetNumber(0,
                MICROBE_AMBIENT_TRACKS.length() - 1)] + ".ogg";
        AudioSource@ audio = GetEngine().GetSoundDevice().Play2DSound(
            "Data/Sound/soundeffects/" + track, false);

        if (audio !is null){
            audio.SetVolume(0.2);

            if (track == "microbe-ambience2.ogg") {
                audio.SetVolume(0.05);
            }
        }
        else {
            LOG_ERROR("Failed to create ambiance sound source");
        }

        return audio;
    }

    private CellStageWorld@ World;


    int t1 = 0;
    int t2 = 0;
    int t3 = 0;
    //TODO: These need more informative names
    bool chloroplastNotificationOpened = false;
    bool toxinNotificationEnabled = false;
    bool reproductionDialogOpened = false;
    // suicideButton setting up
    // really creative naming scheme
    // TODO: Why? Just Why? Ill be honest im not even sure what these are supposed to do
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

    CompoundId ironId;
    float ironVolume;

}


// ------------------------------------ //
// Wrappers for calling GUI update things from random places

void showReproductionDialog(GameWorld@ world){
    cast<MicrobeStageHudSystem@>(world.GetScriptSystem("MicrobeStageHudSystem")).
        showReproductionDialog();
}

void hideReproductionDialog(GameWorld@ world){
    cast<MicrobeStageHudSystem@>(world.GetScriptSystem("MicrobeStageHudSystem")).
        hideReproductionDialog();
}

void showMessage(const string &in msg){
    LOG_INFO(msg + " (note, in-game messages currently disabled)");
    //auto messagePanel = Engine.currentGameState().rootGUIWindow().getChild("MessagePanel")
    //messagePanel.getChild("MessageLabel").setText(msg)
    //messagePanel.show()
}

// ------------------------------------ //
// GUI action callbacks

// Targets player cell and kills it (For suicide button)
void killPlayerCellClicked(CellStageWorld@ world)
{
    auto playerEntity = GetThriveGame().playerData().activeCreature();
    //kill it hard
    MicrobeOperations::damage(world, playerEntity, 9999.0f, "suicide");
}

// This is called from c++ system PlayerMicrobeControlSystem
void applyCellMovementControl(CellStageWorld@ world, ObjectID entity,
    const Float3 &in movement, const Float3 &in lookPosition)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));

    if(microbeComponent is null){
        return;
    }

    if(!microbeComponent.dead){
        microbeComponent.facingTargetPoint = lookPosition;
        microbeComponent.movementDirection = movement;
    }
}

// Activate Engulf Mode
void applyEngulfMode(CellStageWorld@ world, ObjectID entity)
{
    MicrobeOperations::toggleEngulfMode(world, entity);
}

// Player shoot toxin
void playerShootToxin(CellStageWorld@ world, ObjectID entity)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(entity));
    CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
    MicrobeOperations::emitAgent(world, entity, oxytoxyId, 10.0f, 400*10.0f);
}

