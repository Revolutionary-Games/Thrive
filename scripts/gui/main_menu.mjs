// Main menu scripts are here


import * as common from "./gui_common.mjs";
import * as microbe_hud from "./microbe_hud.mjs";

let jams = null;

// Pauses the menu music instantly (used for instant start)
let menuAlreadySkipped = false;

//! Setup callbacks for buttons
export function runMenuSetup(){

    document.getElementById("quitButton").addEventListener("click", (event) => {
        event.stopPropagation();
        common.playButtonPressSound();
        quitGame();
    }, true);
    document.getElementById("newGameButton").addEventListener("click", (event) => {
        event.stopPropagation();
        common.playButtonPressSound();
        newGame();
    }, true);

    document.addEventListener("keydown", (event) => {
        if(event.key === "Escape"){

            event.stopPropagation();
            onEscapePressed();

        }
    }, true);

    // Some setup cannot be ran when previewing in a browser
    if(common.isInEngine()){

        // Version number
        Thrive.getVersion((result) => {

            document.getElementById("versionNumber").textContent = result;

        }, () => {});

        // Detect return to menu
        Leviathan.OnGeneric("ExitedToMenu", () => {
            doExitToMenu();
        });

        // Start intro video
        Leviathan.PlayCutscene("Data/Videos/intro.mkv", onIntroEnded, onIntroEnded);

    } else {
        document.getElementById("versionNumber").textContent = "Thrive GUI in browser";

        // Background to be black to fix the white text and cursor not showing up well
        document.getElementsByTagName("body")[0].style.background = "black";

        // (this would theoretically work in a browser but would be a bit annoying to work on)
        // common.playVideo("../../Videos/intro.mkv", onIntroEnded);
    }

    //
    // Use these to immediately test some specific menu
    //

    // eslint off

    // onMicrobeIntroEnded();
    // doEnterMicrobeEditor();
    // onIntroEnded();

    // eslint on
}

function startMenuMusic(restart = true) {
    if(!common.isInEngine())
        return;

    if(jams && restart){

        // Stop already playing
        jams.Stop();
        jams = null;
    }

    if(!jams){

        const startPaused = Boolean(menuAlreadySkipped);

        // Start the menu music
        Leviathan.Play2DSound("Data/Sound/main-menu-theme-2.ogg", true, startPaused,
            (source) => {
                jams = source;
            });
    } else {

        jams.Play2D();
    }
}

//! Handles pressing Escape in the GUI (this will unpause the game,
//! pausing is initiated from c++ key listener)
function onEscapePressed() {
    // TODO: move this to the cutscene player
    Leviathan.CancelCutscene();
}

function onIntroEnded(error) {

    if(error)
        console.error("failed to play intro video: " + error);

    if(common.isInEngine()){

        startMenuMusic();
    }
}

function quitGame(){
    common.requireEngine();
    Leviathan.Quit();
}

function newGame(){

    if(common.isInEngine()){
        Leviathan.PlayCutscene("Data/Videos/MicrobeIntro.mkv", onMicrobeIntroEnded,
            onMicrobeIntroEnded);
    } else {
        onMicrobeIntroEnded();
    }
}

function onMicrobeIntroEnded(error){

    if(error)
        console.error("failed to play microbe intro video: " + error);

    menuAlreadySkipped = true;

    if(jams){

        jams.Pause();
    }

    if(common.isInEngine()){

        // Make sure no video is playing in case we did an immediate start
        Leviathan.CancelCutscene();

        Thrive.start();

    } else {

        // Show the microbe GUI anyway for testing purposes
    }

    // Hide main menu
    // If this is ever restored this needs to be set to "flex"
    document.getElementById("topLevelMenuContainer").style.display = "none";

    // And show microbe gui
    document.getElementById("topLevelMicrobeStage").style.display = "block";
    microbe_hud.runMicrobeHUDSetup();
}

//! Called once C++ has finished exiting to menu
export function doExitToMenu() {
    document.getElementById("topLevelMenuContainer").style.display = "";
    document.getElementById("topLevelMicrobeStage").style.display = "none";
    document.getElementById("pauseOverlay").style.display = "none";

    startMenuMusic(false);
}

