// Microbe editor GUI scripts


import * as common from "./gui_common.mjs";
import * as main_menu from "./main_menu.mjs";
import * as microbe_hud from "./microbe_hud.mjs";

let readyToFinishEdit = false;
let symmetry = 0;
let currentTab = null;

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
        element: document.getElementById("addChemoSynthisizingProteins"),
        organelle: "chemoSynthisizingProteins"
    },
    {
        element: document.getElementById("addRusticyanin"),
        organelle: "rusticyanin"
    },
    {
        element: document.getElementById("addFlagellum"),
        organelle: "flagellum"
    },
    {
        element: document.getElementById("addMetabolosome"),
        organelle: "metabolosome"
    },
    {
        element: document.getElementById("addChromatophor"),
        organelle: "chromatophors"
    },
    {
        element: document.getElementById("addNitrogenase"),
        organelle: "nitrogenase"
    },
    {
        element: document.getElementById("addToxinProtein"),
        organelle: "oxytoxyProteins"
    },
    {
        element: document.getElementById("addNucleus"),
        organelle: "nucleus"
    }

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

    // Quit Button Clicked
    document.getElementById("exitToMenuButtonEditor").addEventListener("click",
        onExitToMenuClickedEditor, true);

    // Help Button Clicked
    document.getElementById("helpButtonEditor").addEventListener("click",
        openHelpEditor, true);

    // Close Help Button Clicked
    document.getElementById("closeHelpEditor").addEventListener("click",
        closeHelpEditor, true);

    // Finish button clicked
    document.getElementById("microbeEditorFinishButton").addEventListener("click",
        onFinishButtonClicked, true);

    // Symmetry Button Clicked
    document.getElementById("SymmetryButton").addEventListener("click",
        onSymmetryClicked, true);

    // New Cell Button Clicked
    document.getElementById("newButton").addEventListener("click",
        OnNewCellClicked, true);

    // Undo Button Clicked
    document.getElementById("Undo").addEventListener("click",
        onUndoClicked, true);

    // Redo Button Clicked
    document.getElementById("Redo").addEventListener("click",
        onRedoClicked, true);

    document.getElementById("editorNextPageButton").addEventListener("click",
        onNextTabClicked, true);


    document.getElementById("editorTabReportButton").addEventListener("click",
        () => {
            selectEditorTab("report");
        }, true);

    document.getElementById("editorTabMapButton").addEventListener("click",
        () => {
            selectEditorTab("map");
        }, true);

    document.getElementById("editorTabCellButton").addEventListener("click",
        () => {
            selectEditorTab("cell");
        }, true);

    // All of the organelle buttons
    for(const element of organelleSelectionElements){

        element.element.addEventListener("click", (event) => {
            event.stopPropagation();
            if(!element.element.classList.contains("DisabledButton")) {
                onSelectNewOrganelle(element.organelle);
            }
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

        // Event for size update
        Leviathan.OnGeneric("SizeUpdated", (event, vars) => {
            // Apply the new values
            updateSize(vars.size);
        });

        // Event for Generation update
        Leviathan.OnGeneric("GenerationUpdated", (event, vars) => {
            // Apply the new values
            updateGeneration(vars.generation);
        });

        // Event for speed update
        Leviathan.OnGeneric("SpeedUpdated", (event, vars) => {
            // Apply the new values
            updateSpeed(vars.speed);
        });

        // Event for undo setting
        Leviathan.OnGeneric("EditorUndoButtonStatus", (event, vars) => {
            // Apply the new values
            setUndo(vars.enabled);
        });

        // Event for redo setting
        Leviathan.OnGeneric("EditorRedoButtonStatus", (event, vars) => {
            // Apply the new values
            setRedo(vars.enabled);
        });

        // Event for detecting the active organelle
        Leviathan.OnGeneric("MicrobeEditorOrganelleSelected", (event, vars) => {
            updateSelectedOrganelle(vars.organelle);
        });

        // Event for restoring the microbe GUI
        Leviathan.OnGeneric("MicrobeEditorExited", doExitMicrobeEditor);

        // Event for update buttons depending on presence or not of nucleus
        Leviathan.OnGeneric("MicrobeEditorNucleusIsPresent", (event, vars) => {
            updateGuiButtons(vars.nucleus);
        });

    } else {
        updateSelectedOrganelle("cytoplasm");
    }
}

//! Called to enter the editor view
export function doEnterMicrobeEditor(){

    document.getElementById("topLevelMicrobeStage").style.display = "none";
    document.getElementById("topLevelMicrobeEditor").style.display = "block";

    // Select the default tab
    selectEditorTab("report");
}

function selectEditorTab(tab){
    // Hide all
    document.getElementById("topLevelMicrobeEditorCellEditor").style.display = "none";
    document.getElementById("topLevelMicrobeEditorPatchReport").style.display = "none";
    document.getElementById("topLevelMicrobeEditorPatchMap").style.display = "none";
    document.getElementById("editorNextPageButton").style.display = "none";

    document.getElementById("editorTabReportButton").classList.remove("Active");
    document.getElementById("editorTabMapButton").classList.remove("Active");
    document.getElementById("editorTabCellButton").classList.remove("Active");

    currentTab = tab;
    if(common.isInEngine()){
        Leviathan.CallGenericEvent("MicrobeEditorSelectedTab", {tab: tab});
    }

    // Show selected
    if(tab == "report"){
        document.getElementById("topLevelMicrobeEditorPatchReport").style.display = "block";
        document.getElementById("editorNextPageButton").style.display = "block";
        document.getElementById("editorTabReportButton").classList.add("Active");
    } else if(tab == "map"){
        document.getElementById("topLevelMicrobeEditorPatchMap").style.display = "block";
        document.getElementById("editorNextPageButton").style.display = "block";
        document.getElementById("editorTabMapButton").classList.add("Active");
    } else if(tab == "cell"){
        document.getElementById("topLevelMicrobeEditorCellEditor").style.display = "block";
        document.getElementById("editorTabCellButton").classList.add("Active");

        if(!readyToFinishEdit){
            window.setTimeout(() => {
                // Enable finish button
                onFinishButtonEnable();
            }, 500);
        }

    } else {
        throw "invalid tab";
    }
}

function onNextTabClicked(){
    if(currentTab == "report"){
        selectEditorTab("map");
    } else if(currentTab == "map"){
        selectEditorTab("cell");
    } else {
        selectEditorTab("cell");
    }
}

// Undo
function setUndo(enabled){
    if (enabled) {
        document.getElementById("Undo").classList.remove("DisabledButton");
    } else {
        document.getElementById("Undo").classList.add("DisabledButton");
    }
}

// Redo
function setRedo(enabled){
    if (enabled) {
        document.getElementById("Redo").classList.remove("DisabledButton");
    } else {
        document.getElementById("Redo").classList.add("DisabledButton");
    }
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

//! Updates size points in GUI
function updateSize(size){
    document.getElementById("sizeLabel").textContent =
    size + " / Osmoregulation Cost: (" + size + ") ATP/s";
}

//! Updates generation points in GUI
function updateGeneration(generation){
    document.getElementById("generationLabel").textContent =
    generation;
}

//! Updates buttons status depending on presence of nucleus in GUI
function updateGuiButtons(isNucleusPresent){

    if(!isNucleusPresent &&
        !document.getElementById("addMitochondrion").classList.contains("DisabledButton")) {

        document.getElementById("addNucleus").classList.remove("DisabledButton");
        document.getElementById("addMitochondrion").classList.add("DisabledButton");
        document.getElementById("addChloroplast").classList.add("DisabledButton");
        document.getElementById("addChemoplast").classList.add("DisabledButton");
        document.getElementById("addNitrogenFixingPlastid").classList.add("DisabledButton");
        document.getElementById("addVacuole").classList.add("DisabledButton");
        document.getElementById("addToxinVacuole").classList.add("DisabledButton");

    } else if(isNucleusPresent &&
        document.getElementById("addMitochondrion").classList.contains("DisabledButton")) {

        document.getElementById("addNucleus").classList.add("DisabledButton");
        document.getElementById("addMitochondrion").classList.remove("DisabledButton");
        document.getElementById("addChloroplast").classList.remove("DisabledButton");
        document.getElementById("addChemoplast").classList.remove("DisabledButton");
        document.getElementById("addNitrogenFixingPlastid").classList.remove("DisabledButton");
        document.getElementById("addVacuole").classList.remove("DisabledButton");
        document.getElementById("addToxinVacuole").classList.remove("DisabledButton");
    }
}

//! Updates generation points in GUI
function updateSpeed(speed){
    document.getElementById("speedLabel").textContent =
    speed.toFixed(2);
}



function onResumeClickedEditor(){

    common.playButtonPressSound();
    const pause = document.getElementById("pauseOverlayEditor");
    pause.style.display = "none";
}

function onExitToMenuClickedEditor(){
    document.getElementById("pauseOverlayEditor").style.display = "none";
    if(common.isInEngine()){
        Thrive.exitToMenuClicked();
    } else {
        main_menu.doExitToMenu();
    }
}

function openHelpEditor(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "none";

    const help = document.getElementById("helpTextEditor");
    help.style.display = "block";

    // Easter egg code, shows a small message saying something from the
    // List of messages when you open up the help menu
    const message = [
        "Fun Fact, The Didinium  and Paramecium are a textbook example of a " +
            "predator prey relationship" +
            " that has been studied for decades, now are you the Didinium, or the " +
            "Paramecium? Predator, or Prey?",
        "Heres a tip, toxins can be used to knock other toxins away from you " +
            "if you are quick enough.",
        "Heres a tip, Osmoregulation costs 1 ATP per second per hex your cell has, " +
            " each empty hex of cytoplasm generates 5 ATP per second aswell," +
            "which means if you are losing ATP due to osmoregulation just add a couple" +
            " empty hexes cytoplasm or remove some organelles.",
        "Fun Fact, In real life prokaryotes have something called Biocompartments " +
        "which act like organelles, and are in fact called Polyhedral organelles",
        "Fun Fact, The metabolosome is what is called a Polyhedral organelle",
        "Heres a Tip, Chromatophores generate 1/3rd the glucose of a chloroplast",
        "Heres a Tip, You generate exactly 2 glucose per second per chemoplast," +
            "as long as you have at least 1 hydrogen sulfide to convert.",
        "Thrive is meant as a simulation of an alien planet, therefore it makes " +
            "sense that most creatures you find will be related to one " +
        "or two other species due to evolution happening around you, see if you can " +
        "identify them!",
        "One of the first playable game-play prototypes was made by our awesome programmer," +
        " untrustedlife!",
        "Fun Fact, The Didinium  and Paramecium are a textbook example of a " +
            "predator prey relationship" +
            " that has been studied for decades, now are you the Didinium, or the " +
            "Paramecium? Predator, or Prey?",
        "Heres a tip, toxins can be used to knock other toxins away from you " +
            "if you are quick enough.",
        "Heres a tip, sometimes its best just to run away from other cells.",
        "Heres a tip, if a cell is about half your size, thats when you can engulf them.",
        "Heres a tip, Bacteria can be stronger then they appear, they may look " +
            "small, but some of them can burrow into you and kill you that way!",
        "Heres a tip, You can hunt other species to extinction if you arent careful " +
            "enough, they can also be hunted to extinction by other species.",
        "Heres a tip, Every 5 minutes an Auto-evo step happens, if you dont evolve " +
            "fast enough you may be out-competed.",
        "Heres a tip, If you mouse over a cloud a box will pop up on the top left " +
            "of your screen that tells you exactly whats there.",
        "WIGGLY THINGS!!",
        "Smeltal the meltal.",
        "Those blue cells though.",
        "Fun Fact, The thrive team does podcasts every so often, you should check them out!",
        "Heres a tip, Biomes are more then just differnet backgrounds, " +
            "the compounds in, different biomes sometimes spawn at different rates.",
        "Heres a tip, The more flagella you have, the faster you go, " +
            "vroom vroom, but it also costs more ATP",
        "Heres a tip, you can en[g]ulf chunks iron or otherwise.",
        "Heres a tip, prepare before adding a nucleus." +
        " those things are expensive! In upkeep and up front cost.",
        "Fun Fact, Did you know that there are over 8000 species of ciliate on planet earth?",
        "Fun Fact, The Stentor is a ciliate that can stretch itself and catch prey " +
            "in a kind of trumpet like mouth that draws prey in by generating " +
            "water currents with cilia.",
        "Fun Fact, The Didinum is a ciliate that hunts paramecia.",
        "Fun Fact, The Ameoba hunts and catches prey with 'legs' made of " +
            "cytoplasm called pseudopods, eventually we want those in thrive.",
        "Heres a tip, Watch out for larger cells and large bacteria, " +
            "it's not fun to be digested,  and they will eat you.",
        "Heres a tip, Osmoregulation costs 1 ATP per second per hex, " +
            " each empty hex of cytoplasm generates 5 ATP per second aswell," +
            "which means if you are losing ATP due to osmoregulation just add a couple" +
            " empty hexes cytoplasm or remove some organelles",
        "Fun Fact, Thrive is meant as a simulation of an alien planet, therefore it makes" +
            "sense that most creatures you find will be related to one " +
        "or two other species due to evolution happening around you, see if" +
        " you can identify them!",
        "Heres a tip, if your cell is 150 hexes, you can engulf the large iron chunks."
    ];


    const tipEasterEggChance = common.randomBetween(0, 5);
    const messageNum = common.randomBetween(0, message.length - 1);

    if (tipEasterEggChance > 1) {
        document.getElementById("tipMsgEditor").style.display = "unset";
        document.getElementById("tipMsgEditor").textContent = message[messageNum];
        setTimeout(hideTipMsg, 10000);
    }

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

    // To reset the symmetry button properly when you exit
    symmetry = 0;
    document.getElementById("SymmetryIcon").style.backgroundImage = "url()";
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

function hideTipMsg() {
    document.getElementById("tipMsgEditor").style.display = "none";
}

function onSymmetryClicked(event){
    common.playButtonPressSound();
    if (symmetry == 3) {
        document.getElementById("SymmetryIcon").style.backgroundImage = "url()";
        symmetry = 0;
    } else if (symmetry == 0) {
        document.getElementById("SymmetryIcon").style.backgroundImage = "url(../../Textures" +
        "/gui/bevel/2xSymmetry.png)";
        symmetry = 1;
    } else if (symmetry == 1) {
        document.getElementById("SymmetryIcon").style.backgroundImage = "url(../../Textures" +
        "/gui/bevel/4xSymmetry.png)";
        symmetry = 2;
    } else if (symmetry == 2) {
        document.getElementById("SymmetryIcon").style.backgroundImage = "url(../../Textures" +
        "/gui/bevel/6xSymmetry.png)";
        symmetry = 3;
    }

    // I should make teh editor and the javascript use the same exact variable
    Leviathan.CallGenericEvent("SymmetryClicked", {symmetry: symmetry});

    event.stopPropagation();
}

function OnNewCellClicked(event){
    common.playButtonPressSound();
    Leviathan.CallGenericEvent("NewCellClicked", {});
    event.stopPropagation();
}

function onRedoClicked(event){
    common.playButtonPressSound();
    Leviathan.CallGenericEvent("RedoClicked", {});
    event.stopPropagation();
}

function onUndoClicked(event){
    common.playButtonPressSound();
    Leviathan.CallGenericEvent("UndoClicked", {});
    event.stopPropagation();
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
