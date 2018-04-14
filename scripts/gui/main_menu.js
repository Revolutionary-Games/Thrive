// Main menu scripts are here
"use strict";


//! Setup callbacks for buttons
function runMenuSetup(){

    document.getElementById("quitButton").addEventListener("click", quitGame, true);
    document.getElementById("newGameButton").addEventListener("click", newGame, true);

    

    // Version number
    if(isInEngine()){
        Thrive.getVersion((result) => {

            document.getElementById("versionNumber").textContent = result;
            
        }, () => {});

        // TODO: play intro video (this could theoretically work in a
        // browser but would be a bit annoying to work on)
        
    } else {
        document.getElementById("versionNumber").textContent = "Thrive GUI in browser";

        // Background to be black to fix the white text and cursor not showing up well
        document.getElementsByTagName("body")[0].style.background = "black";
    }
}

function quitGame(){
    requireEngine();
    Leviathan.Quit();
}

function newGame(){

    if(isInEngine()){
    
        // TODO: show intro
        Thrive.start();
        
    } else {

        // Show the microbe GUI anyway for testing purposes
    }

    // Hide main menu
    // If this is ever restored this needs to be set to "flex"
    document.getElementById("topLevelMenuContainer").style.display = "none";

    // And show microbe gui
    document.getElementById("topLevelMicrobeStage").style.display = "block";
}
    
