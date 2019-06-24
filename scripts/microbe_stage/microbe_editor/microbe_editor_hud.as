#include "microbe_editor.as"


const array<string> MICROBE_EDITOR_AMBIENT_TRACKS = {
    "microbe-editor-theme-1","microbe-editor-theme-2", "microbe-editor-theme-3",
    "microbe-editor-theme-4", "microbe-editor-theme-5"
};

const int BASE_MUTATION_POINTS = 100;


class MicrobeEditorHudSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this._world = cast<MicrobeEditorWorld>(w);

        assert(this.world !is null, "MicrobeEditorHudSystem didn't get proper world");

        @editor = MicrobeEditor(this);


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
        if (audio !is null){
            if(audio.HasInternalSource()){
            audio.Get().setVolume(0.2);
            }
            else {
            LOG_ERROR("Failed to create editor music internal source");
            }
        }
        else {
            //LOG_ERROR("Failed to create editor music sound source");
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
            if (@ambienceSounds !is null)
                {
                ambienceSounds.Get().play();
                }
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

        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this
        for(uint i = 0; i < hoverHex.length(); ++i){

            auto node = world.GetComponent_RenderNode(hoverHex[i]);
            node.Node.setPosition(Float3(0, 0, 0));

            bs::Quaternion rot(0.40118, 0.791809, 0.431951, 0.0381477);

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
        for(int i = 0; i < 200; ++i){

            ObjectID hex = world.CreateEntity();
            auto node = world.Create_RenderNode(hex);
            world.Create_Model(hex, "hex.mesh",
                getBasicMaterialWithTexture("single_hex.png"));
            node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
            node.Marked = true;
            node.Node.setPosition(bs::Vector3(0, 0, 0));
            hoverHex.insertLast(hex);
        }

        for(int i = 0; i < 6; ++i){
            ObjectID hex = world.CreateEntity();
            auto node = world.Create_RenderNode(hex);
            node.Scale = Float3(HEX_SIZE, HEX_SIZE, HEX_SIZE);
            node.Marked = true;
            node.Node.setPosition(bs::Vector3(0, 0, 0));
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

        vars.AddValue(ScriptSafeVariableBlock("mutationPoints", editor.getMutationPoints()));
        vars.AddValue(ScriptSafeVariableBlock("maxMutationPoints", BASE_MUTATION_POINTS));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    void updateSize()
    {
        GenericEvent@ event = GenericEvent("SizeUpdated");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("size", editor.getActualMicrobeSize()));

        GetEngine().GetEventHandler().CallEvent(event);
    }


    void updateGeneration()
    {
        GenericEvent@ event = GenericEvent("GenerationUpdated");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("generation", editor.getMicrobeGeneration()));

        GetEngine().GetEventHandler().CallEvent(event);
    }

    void updateSpeed()
        {
        // Number of Flagella / total number of organelles
        GenericEvent@ event = GenericEvent("SpeedUpdated");
        NamedVars@ vars = event.GetNamedVars();

        vars.AddValue(ScriptSafeVariableBlock("speed", editor.getMicrobeSpeed()));
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
}
