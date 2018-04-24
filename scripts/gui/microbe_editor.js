// Microbe editor GUI scripts
"use strict";

let microbeEditorSetup = false;
let readyToFinishEdit = false;

//! Called to enter the editor view
function doEnterMicrobeEditor(){

    document.getElementById("topLevelMicrobeStage").style.display = "none";
    document.getElementById("topLevelMicrobeEditor").style.display = "block";

    window.setTimeout(() => {
        // Enable finish button
        onFinishButtonEnable();
    }, 500);

    // Do setup
    if(!microbeEditorSetup){

        document.getElementById("microbeEditorFinishButton").addEventListener(
        "click", onFinishButtonClicked, true);
        

        microbeEditorSetup = true;
    }
}

//! Called to exit the editor
function doExitMicrobeEditor(){
    document.getElementById("topLevelMicrobeStage").style.display = "block";
    document.getElementById("topLevelMicrobeEditor").style.display = "none";
}

function onFinishButtonEnable(){

    readyToFinishEdit = true;
    document.getElementById("microbeEditorFinishButton").classList.remove("DisabledButton");
}

function onFinishButtonClicked(event){
    
    if(!readyToFinishEdit)
        return false;
    
    event.stopPropagation();
    playButtonPressSound();
    
    // Fire event
    if(isInEngine()){

        // Fire an event to tell the game to back to the stage. It
        // will notify us when it is done
        
    } else {

        // Swap GUI for previewing
        doExitMicrobeEditor();

        // And re-enable the button
        window.setTimeout(() => {
            onReadyToEnterEditor();
        }, 500);
    }
    
    // Disable
    document.getElementById("microbeEditorFinishButton").classList.add("DisabledButton");
    readyToFinishEdit = false;
}
