// Main menu scripts are here
"use strict";

let jams = null;

// Pauses the menu music instantly (used for instant start)
let menuAlreadySkipped = false;

//! Setup callbacks for buttons
function runMenuSetup(){

    document.getElementById("quitButton").addEventListener("click", (event) => {
        event.stopPropagation();
        playButtonPressSound();
        quitGame();
    }, true);
    document.getElementById("newGameButton").addEventListener("click", (event) => {
        event.stopPropagation();
        playButtonPressSound();        
        newGame();
    }, true);

    document.addEventListener("keydown", (event) => {
        if(event.key === "Escape"){

            event.stopPropagation();
            onEscapePressed();
            return;
        }
    }, true);
    
    // Some setup cannot be ran when previewing in a browser
    if(isInEngine()){
        
        // Version number
        Thrive.getVersion((result) => {

            document.getElementById("versionNumber").textContent = result;
            
        }, () => {});

        // Detect return to menu
        Leviathan.OnGeneric("ExitedToMenu", (event, vars) => {
            doExitToMenu();
        });        
        

        // (this would theoretically work in a browser but would be a bit annoying to work on)
        // Start intro video
        playVideo("../../Videos/intro.mkv", onIntroEnded);
        
    } else {
        document.getElementById("versionNumber").textContent = "Thrive GUI in browser";

        // Background to be black to fix the white text and cursor not showing up well
        document.getElementsByTagName("body")[0].style.background = "black";

        // playVideo("../../assets/videos/intro.mkv", onIntroEnded);
    }
    
    //
    // Use these to immediately test some specific menu
    //
    // onMicrobeIntroEnded();
    // doEnterMicrobeEditor();
}

function startMenuMusic(restart = true)
{
    if(jams && restart){

        // Stop already playing
        jams.Stop();
        jams = null;
    }

    if(!jams){
        
        let startPaused = Boolean(menuAlreadySkipped);
        
        // Start the menu music
        Leviathan.Play2DSound("Data/Sound/main-menu-theme-2.ogg", true, startPaused,
                              (source) => {
                                  jams = source;
                              });
    } else {
        
        jams.Play2D();
    }
}

//! Handles pressing Escape in the GUI (this will skip videos and
//! unpause, pausing is initiated from c++ key listener)
function onEscapePressed(){

    if(!document.getElementById("videoPlayersVideo").ended)
        stopVideo();
}

function onIntroEnded()
{
    if(isInEngine()){

        startMenuMusic();
    }
}

function quitGame(){
    requireEngine();
    Leviathan.Quit();
}

function newGame(){

    if(isInEngine()){
        playVideo("../../Videos/MicrobeIntro.mkv", onMicrobeIntroEnded);
    } else {
        onMicrobeIntroEnded();
    }
}

function onMicrobeIntroEnded(){

    menuAlreadySkipped = true;

    if(jams){

        jams.Pause();
    }

    if(isInEngine()){

        // Make sure no video is playing in case we did an immediate start
        if(!document.getElementById("videoPlayersVideo").ended)
            stopVideo();
    
        Thrive.start();
        
    } else {

        // Show the microbe GUI anyway for testing purposes
    }

    // Hide main menu
    // If this is ever restored this needs to be set to "flex"
    document.getElementById("topLevelMenuContainer").style.display = "none";

    // And show microbe gui
    document.getElementById("topLevelMicrobeStage").style.display = "block";
    runMicrobeHUDSetup();
}

//! Called once C++ has finished exiting to menu
function doExitToMenu()
{
    document.getElementById("topLevelMenuContainer").style.display = '';
    document.getElementById("topLevelMicrobeStage").style.display = 'none';
    document.getElementById("pauseOverlay").style.display = 'none';

    startMenuMusic(false);
}
    
