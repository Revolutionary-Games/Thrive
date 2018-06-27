// Microbe editor GUI scripts
"use strict";

let microbeEditorSetup = false;
let readyToFinishEdit = false;

//! Called to enter the editor view
function doEnterMicrobeEditor(){

    document.getElementById("topLevelMicrobeStage").style.display = "none";
    document.getElementById("topLevelMicrobeEditor").style.display = "block";
    // Pause Menu Clicked
    document.getElementById("mainMenuButtonEditor").addEventListener(
        "click", onMenuClickedEditor, true);

    // Pause Menu closed
    document.getElementById("resumeButtonEditor").addEventListener(
        "click", onResumeClickedEditor, true);
        
    // Quit Button Clicked
    document.getElementById("quitButtonEditor").addEventListener(
        "click", quitGameEditor, true);
        
    // Help Button Clicked
    document.getElementById("helpButtonEditor").addEventListener(
        "click", openHelpEditor, true);
        
    // Close Help Button Clicked
    document.getElementById("closeHelpEditor").addEventListener(
        "click", closeHelpEditor, true);
        
    window.setTimeout(() => {
        // Enable finish button
        onFinishButtonEnable();
    }, 500);

    // Do setup
    if(!microbeEditorSetup){

        document.getElementById("microbeEditorFinishButton").addEventListener(
            "click", onFinishButtonClicked, true);

        if(isInEngine()){

            // Event for restoring the microbe GUI
            Leviathan.OnGeneric("MicrobeEditorExited", doExitMicrobeEditor);
        }

        microbeEditorSetup = true;
    }
}

function onResumeClickedEditor(event){

    playButtonPressSound();
    let pause = document.getElementById("pauseOverlayEditor");
    pause.style.display = "none";
}

function openHelpEditor(event){

    playButtonPressSound();

    let pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "none";
    
    let help = document.getElementById("helpTextEditor");
    help.style.display = "block";
    
}

function closeHelpEditor(event){

    playButtonPressSound();
    
    let pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "block";
    
    let help = document.getElementById("helpTextEditor");
    help.style.display = "none";
    
}

function onMenuClickedEditor(event){

    playButtonPressSound();
    let pause = document.getElementById("pauseOverlayEditor");
    pause.style.display = "block";
     let help = document.getElementById("helpTextEditor");
    help.style.display = "none";
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

function quitGameEditor(){
    
    playButtonPressSound();
    requireEngine();
    Leviathan.Quit();
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
        Thrive.finishEditingClicked();
        
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
