// Main menu scripts are here
"use strict";


//! Setup callbacks for buttons
function runMenuSetup(){

    document.getElementById("quitButton").addEventListener("click", quitGame, true);
}

function quitGame(){
    requireEngine();
    Leviathan.Quit();
}
    
