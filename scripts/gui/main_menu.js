// Main menu scripts are here
"use strict";


//! Setup callbacks for buttons
function runMenuSetup(){

    document.getElementById("quitButton").addEventListener("click", quitGame, true);

    // Version number
    if(isInEngine()){
        Thrive.getVersion((result) => {

            document.getElementById("versionNumber").textContent = result;
            
        }, () => {});
    } else {
        document.getElementById("versionNumber").textContent = "Thrive GUI in browser";
    }
}

function quitGame(){
    requireEngine();
    Leviathan.Quit();
}
    
