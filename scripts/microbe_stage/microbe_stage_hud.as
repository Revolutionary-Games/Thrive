
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
    //! This stops sound while the cell stage world isn't active
    void Suspend()
    {
        LOG_INFO("Suspending microbe stage background sounds");

        // Pause to allow resuming
        if(ambientTrack !is null)
            ambientTrack.Pause();

        if(ambienceSounds !is null)
            ambienceSounds.Pause();
    }

    //! This resumes sound when the cell stage world is active again
    void Resume()
    {
        LOG_INFO("Resuming microbe stage background sounds");

        if(ambientTrack !is null)
            ambientTrack.Resume();

        if(ambienceSounds !is null)
            ambienceSounds.Resume();
    }
}

