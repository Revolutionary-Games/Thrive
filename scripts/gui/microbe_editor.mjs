// Microbe editor GUI scripts


import * as common from "./gui_common.mjs";
import * as microbe_hud from "./microbe_hud.mjs";

let readyToFinishEdit = false;

//! These are all the organelle selection buttons
const organelleSelectionElements = [
    {
        element: document.getElementById("addCytoplasm"),
        organelle: "cytoplasm"
    },
    {
        element: document.getElementById("addMitochondrion"),
        organelle: "mitochondrion"
    },
    {
        element: document.getElementById("addChloroplast"),
        organelle: "chloroplast"
    },

    // {
    //     element: document.getElementById("addThermoplast"),
    //     organelle: "thermoplast"
    // },
    {
        element: document.getElementById("addVacuole"),
        organelle: "vacuole"
    },
    {
        element: document.getElementById("addToxinVacuole"),
        organelle: "oxytoxy"
    },

    // {
    //     element: document.getElementById("addBioluminescent"),
    //     organelle: "bioluminescent"
    // },
    {
        element: document.getElementById("addChemoplast"),
        organelle: "chemoplast"
    },
    {
        element: document.getElementById("addNitrogenFixingPlastid"),
        organelle: "nitrogenfixingplastid"
    },
    {
        element: document.getElementById("addFlagellum"),
        organelle: "flagellum"
    },

    // AddPilus
    // addCilia
];

//! Selected organelle label
const selectedOrganelleListItem = document.createElement("div");
selectedOrganelleListItem.classList.add("OrganelleSelectedText");
selectedOrganelleListItem.appendChild(document.createTextNode("Selected"));

//! Setup for editor callbacks
export function setupMicrobeEditor(){
    // Pause Menu Clicked
    document.getElementById("mainMenuButtonEditor").addEventListener("click",
        onMenuClickedEditor, true);

    // Pause Menu closed
    document.getElementById("resumeButtonEditor").addEventListener("click",
        onResumeClickedEditor, true);

    // Quit Button Clicked
    document.getElementById("quitButtonEditor").addEventListener("click",
        quitGameEditor, true);

    // Help Button Clicked
    document.getElementById("helpButtonEditor").addEventListener("click",
        openHelpEditor, true);

    // Close Help Button Clicked
    document.getElementById("closeHelpEditor").addEventListener("click",
        closeHelpEditor, true);

    document.getElementById("microbeEditorFinishButton").addEventListener("click",
        onFinishButtonClicked, true);

    // All of the organelle buttons
    for(const element of organelleSelectionElements){

        element.element.addEventListener("click", (event) => {
            event.stopPropagation();
            onSelectNewOrganelle(element.organelle);
        }, true);
    }

    if(common.isInEngine()){

        // The editor area was clicked, do send press to AngelScript
        document.getElementById("microbeEditorClickDetector").addEventListener("click",
            (event) => {
                event.stopPropagation();
                Leviathan.CallGenericEvent("MicrobeEditorClicked", {secondary: false});
                return true;
            }, false);

        document.getElementById("microbeEditorClickDetector").
            addEventListener("contextmenu",
                (event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    Leviathan.CallGenericEvent("MicrobeEditorClicked", {secondary: true});
                    return true;
                }, false);

        // Event for mutation point amount
        Leviathan.OnGeneric("MutationPointsUpdated", (event, vars) => {
            // Apply the new values
            updateMutationPoints(vars.mutationPoints, vars.maxMutationPoints);
        });

        // Event for detecting the active organelle
        Leviathan.OnGeneric("MicrobeEditorOrganelleSelected", (event, vars) => {
            updateSelectedOrganelle(vars.organelle);
        });

        // Event for restoring the microbe GUI
        Leviathan.OnGeneric("MicrobeEditorExited", doExitMicrobeEditor);

    } else {
        updateSelectedOrganelle("cytoplasm");
    }
}

//! Called to enter the editor view
export function doEnterMicrobeEditor(){

    document.getElementById("topLevelMicrobeStage").style.display = "none";
    document.getElementById("topLevelMicrobeEditor").style.display = "block";

    window.setTimeout(() => {
        // Enable finish button
        onFinishButtonEnable();
    }, 500);
}

//! Sends organelle selection to the Game
function onSelectNewOrganelle(organelle){

    if(common.isInEngine()){

        Leviathan.CallGenericEvent("MicrobeEditorOrganelleSelected", {organelle: organelle});

    } else {

        updateSelectedOrganelle(organelle);
    }
}

//! Updates the GUI buttons based on selected organelle
function updateSelectedOrganelle(organelle){

    // Remove the selected text from existing ones
    for(const element of organelleSelectionElements){

        if(element.element.contains(selectedOrganelleListItem)){
            element.element.removeChild(selectedOrganelleListItem);
            break;
        }
    }

    // Make all buttons unselected except the one that is now selected
    for(const element of organelleSelectionElements){

        if(element.organelle === organelle){
            element.element.classList.add("Selected");
            element.element.prepend(selectedOrganelleListItem);
        } else {
            element.element.classList.remove("Selected");
        }
    }
}

//! Updates mutation points in GUI
function updateMutationPoints(mutationPoints, maxMutationPoints){
    document.getElementById("microbeHUDPlayerMutationPoints").textContent =
    mutationPoints + "/";
    document.getElementById("microbeHUDPlayerMaxMutationPoints").textContent =
    maxMutationPoints;
    document.getElementById("microbeHUDPlayerMutationPointsBar").style.width =
         common.barHelper(mutationPoints, maxMutationPoints);
}

function onResumeClickedEditor(){

    common.playButtonPressSound();
    const pause = document.getElementById("pauseOverlayEditor");
    pause.style.display = "none";
}

function openHelpEditor(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "none";

    const help = document.getElementById("helpTextEditor");
    help.style.display = "block";

}

function closeHelpEditor(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "block";

    const help = document.getElementById("helpTextEditor");
    help.style.display = "none";

}

function onMenuClickedEditor(){

    common.playButtonPressSound();
    const pause = document.getElementById("pauseOverlayEditor");
    pause.style.display = "block";
    const help = document.getElementById("helpTextEditor");
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

    common.playButtonPressSound();
    common.requireEngine();
    Leviathan.Quit();
}

function onFinishButtonClicked(event){

    if(!readyToFinishEdit)
        return false;

    event.stopPropagation();
    common.playButtonPressSound();

    // Fire event
    if(common.isInEngine()){

        // Fire an event to tell the game to back to the stage. It
        // Will notify us when it is done
        Thrive.finishEditingClicked();

    } else {

        // Swap GUI for previewing
        doExitMicrobeEditor();

        // And re-enable the button
        window.setTimeout(() => {
            microbe_hud.onReadyToEnterEditor();
        }, 500);
    }

    // Disable
    document.getElementById("microbeEditorFinishButton").classList.add("DisabledButton");
    readyToFinishEdit = false;

    return true;
}
