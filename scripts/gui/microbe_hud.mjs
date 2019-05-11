// JavaScript code to handle updating all the microbe stage stuff

import * as common from "./gui_common.mjs";
import * as main_menu from "./main_menu.mjs";
import {doEnterMicrobeEditor} from "./microbe_editor.mjs";


let microbeHudSetupRan = false;

let readyToEdit = false;

let wonOnce = false;

//! Registers all the stuff for this to work.
//! This makes sure it does something only once
export function runMicrobeHUDSetup(){

    if(microbeHudSetupRan)
        return;

    document.getElementById("microbeToEditorButton").addEventListener("click",
        onEditorButtonClicked, true);

    // Compound Panel
    document.getElementById("compoundExpand").addEventListener("click",
        onCompoundPanelClicked, true);

    // Pause Menu Clicked
    document.getElementById("mainMenuButton").addEventListener("click", onMenuClicked, true);

    // Pause Menu closed
    document.getElementById("resumeButton").addEventListener("click", onResumeClicked, true);

    // Quit Button Clicked
    document.getElementById("quitButtonHud").addEventListener("click", quitGameHud, true);

    // Main-Menu Button (Inside pause menu) Clicked
    document.getElementById("exitToMenuButton").addEventListener("click",
        onExitToMenuClicked, true);

    // Help Button Clicked
    document.getElementById("helpButton").addEventListener("click", openHelp, true);

    // Close Help Button Clicked
    document.getElementById("closeHelp").addEventListener("click", closeHelp, true);

    // Editor button is initially disabled
    document.getElementById("microbeToEditorButton").classList.add("DisabledButton");

    if(common.isInEngine()){

        // Register for the microbe stage events
        Leviathan.OnGeneric("PlayerCompoundAmounts", (event, vars) => {

            // Apply the new values
            updateMicrobeHUDBars(vars);
        });

        // Event for population changes
        Leviathan.OnGeneric("PopulationChange", (event, vars) => {

            // Apply the new values
            updatePopulation(vars.populationAmount);
        });

        // Event for checking extinction
        Leviathan.OnGeneric("CheckExtinction", (event, vars) => {
            checkExtinction(vars.population);
        });

        // Event for updating o2 and c02 numbers
        Leviathan.OnGeneric("UpdateDissolvedGasses", (event, vars) => {
            updateDissolvedGasses(vars.oxygenPercent, vars.co2Percent, vars.n2Percent);
        });

        // Event for checking win conditions
        Leviathan.OnGeneric("CheckWin", (event, vars) => {
            checkGeneration(vars.generation, vars.population);
        });

        // Event for receiving data about stuff we are hovering over
        Leviathan.OnGeneric("PlayerMouseHover", (event, vars) => {

            // Apply the new values
            updateHoverInfo(vars);
        });

        // Event for entering the editor
        Leviathan.OnGeneric("MicrobeEditorEntered", doEnterMicrobeEditor);

        // Event that enables the editor button
        Leviathan.OnGeneric("PlayerReadyToEnterEditor", onReadyToEnterEditor);

        // Event that disabled the editor button
        Leviathan.OnGeneric("PlayerDiedBeforeEnter", onResetEditor);

        // Add listner for sucide button
        document.getElementById("suicideButton").addEventListener("click",
            killPlayerCell, true);

    } else {

        // Update random values to make it prettier to look at
        const hp = common.randomBetween(10, 50);
        const ammonia = common.randomBetween(0, 50);
        const glucose = common.randomBetween(10, 50);
        const oxytoxy = common.randomBetween(0, 10);
        const phosphate = common.randomBetween(0, 50);
        const hydrogenSulfide = common.randomBetween(0, 50);
        const iron = common.randomBetween(0, 50);
        updateMicrobeHUDBars({
            hitpoints: common.randomBetween(1, hp),
            maxHitpoints: hp,
            compoundATP: common.randomBetween(10, 100),
            ATPMax: 100,
            compoundAmmonia: common.randomBetween(0, ammonia),
            AmmoniaMax: ammonia,
            compoundGlucose: common.randomBetween(0, glucose),
            GlucoseMax: glucose,
            compoundOxytoxy: common.randomBetween(0, oxytoxy),
            OxytoxyMax: oxytoxy,
            compoundPhosphate: common.randomBetween(0, phosphate),
            PhosphateMax: phosphate,
            compoundHydrogenSulfide: common.randomBetween(0, hydrogenSulfide),
            HydrogenSulfideMax: hydrogenSulfide,
            compoundIron: common.randomBetween(0, iron),
            IronMax: iron,
        });

        // Pseudo population code
        updatePopulation(common.randomBetween(0, 50));

        // Put some hover stuff
        updateHoverInfo({
            mousePos: "[0, 0, 0]",
            ammonia0: "Ammonia: 12.2",
        });

        onReadyToEnterEditor();
    }

    microbeHudSetupRan = true;
}

// Quit Button
function quitGameHud(){

    common.playButtonPressSound();
    common.requireEngine();
    Leviathan.Quit();
}

//! Enables the editor button
export function onReadyToEnterEditor(){

    readyToEdit = true;
    document.getElementById("microbeToEditorButton").classList.remove("DisabledButton");
}


//! Disabled the editor button
export function onResetEditor(){

    // Disable
    document.getElementById("microbeToEditorButton").classList.add("DisabledButton");
    readyToEdit = false;
}


function onCompoundPanelClicked() {
    common.playButtonPressSound();

    $("#compoundsPanel").slideToggle(400, "swing", function(){

        const visible = $(this).is(":visible");

        // TODO: could just animate this image to rotate
        document.getElementById("compoundExpandIcon").style.backgroundImage = visible ?
            "url(../../Textures/gui/bevel/ExpandDownIcon.png)" :
            "url(../../Textures/gui/bevel/ExpandUpIcon.png)";
    });


}

function openHelp(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenu");
    pause.style.display = "none";

    const help = document.getElementById("helpText");
    help.style.display = "block";

    // Easter egg code, shows a small message saying something from the
    // List of messages when you open up the help menu
    // TODO: Can we perhaps move this to json?
    const message = [
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
        "Heres a tip, if your cell is 150 hexes, you can engulf the large iron chunks.",
        "Fun Fact, One of the first playable game-play prototypes was made " +
        "by our awesome programmer, untrustedlife!"
    ];


    const tipEasterEggChance = common.randomBetween(0, 5);
    const messageNum = common.randomBetween(0, message.length - 1);

    if (tipEasterEggChance > 1) {
        document.getElementById("tipMsg").style.display = "unset";
        document.getElementById("tipMsg").textContent = message[messageNum];
        setTimeout(hideTipMsg, 10000);
    }

}

function closeHelp(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenu");
    pause.style.display = "block";

    const help = document.getElementById("helpText");
    help.style.display = "none";
}

function hideTipMsg() {
    document.getElementById("tipMsg").style.display = "none";
}

function onMenuClicked(){

    common.playButtonPressSound();
    const pause = document.getElementById("pauseOverlay");
    pause.style.display = "block";
    const help = document.getElementById("helpText");
    help.style.display = "none";
}

function onResumeClicked(){

    common.playButtonPressSound();
    const pause = document.getElementById("pauseOverlay");
    pause.style.display = "none";
}

function killPlayerCell(){

    common.playButtonPressSound();
    Thrive.killPlayerCellClicked();

    // Easter egg code, shows a small message saying something from the
    // List of messages when you kill yourself
    const message = [
        "Do you want to go extinct?", "Darwin Award?", "Why? :(",
        "How could you do this to me?", "Thats not quite, 'Thriving'", "B..ut...why?",
        "Microbes may not have a nervous system, but thats still not very nice!",
        "And so you explode in a bubble of organic chemicals, never to evolve, " +
                   "never to thrive...",
        "Did you know there is in fact such a thing as 'programmed cell death', " +
                   "its called apoptosis."
    ];

    const deathEasterEggChance = common.randomBetween(0, 10);
    const messageNum = common.randomBetween(0, message.length - 1);

    if (deathEasterEggChance == 0) {
        document.getElementById("suicideMsg").style.display = "unset";
        document.getElementById("suicideMsg").textContent = message[messageNum];
        setTimeout(hideSuicideMsg, 5000);
    }
}

function hideSuicideMsg() {
    document.getElementById("suicideMsg").style.display = "none";
}

function onEditorButtonClicked(event){

    if(!readyToEdit)
        return false;

    event.stopPropagation();
    common.playButtonPressSound();

    // Fire event
    if(common.isInEngine()){

        // Call a function to tell the game to swap to the editor. It
        // Will notify us when it is done
        Thrive.editorButtonClicked();

    } else {

        // Swap GUI for previewing
        doEnterMicrobeEditor();
    }

    // Disable
    document.getElementById("microbeToEditorButton").classList.add("DisabledButton");
    readyToEdit = false;

    return true;
}

//! Exit to main menu clicked
function onExitToMenuClicked() {
    if(common.isInEngine()){
        document.getElementById("extinctionTitle").style.display = "none";
        document.getElementById("extinctionBody").style.display = "none";
        document.getElementById("extinctionContainer").style.display = "none";
        hideWinText();

        // Gotta reset this
        wonOnce = false;
        Thrive.exitToMenuClicked();

    } else {
        main_menu.doExitToMenu();
    }
}

//! Updates the mouse hover box with stuff
function updateHoverInfo(vars){

    const panel = document.getElementById("mouseHoverPanel");
    common.clearChildren(panel);

    panel.appendChild(document.createTextNode("Stuff at " + vars.mousePos + ":"));

    if(vars.noCompounds){

        panel.appendChild(document.createElement("br"));
        panel.appendChild(document.createTextNode("Nothing to eat here"));

    } else {

        common.getKeys(vars).forEach(function(key){

            // Skip things that are handled elsewhere
            if(key == "mousePos" || key == "hoveredCells")
                return;

            // Line breaks between elements
            panel.appendChild(document.createElement("br"));

            // Debug print version
            // Panel.appendChild(document.createTextNode(key + ": " + vars[key]));
            panel.appendChild(document.createTextNode(vars[key]));
        });
    }

    if(vars.hoveredCells){

        // When there is a single cell under the mouse this isn't an array
        if(Array.isArray(vars.hoveredCells)){
            for(const species of vars.hoveredCells){

                panel.appendChild(document.createElement("br"));
                panel.appendChild(document.createTextNode("Cell of species " + species));
            }
        } else {
            panel.appendChild(document.createElement("br"));
            panel.appendChild(document.createTextNode("Cell of species " + vars.hoveredCells));
        }
    }

    // Last line break needs to be skipped to avoid an excess empty line
}

//! Updates population bar in GUI
function updatePopulation(population){
    document.getElementById("populationCount").textContent =
    population;
}

// Update dissolved gasses
function updateDissolvedGasses(oxygen, c02, n2){
    document.getElementById("oxygenPercent").innerHTML =
    "O<sub>2</sub>" + ": " + oxygen + "%";
    document.getElementById("carbonDioxidePercent").innerHTML =
    "CO<sub>2</sub>" + ": " + c02 + "%";
    document.getElementById("nitrogenPercent").innerHTML =
    "N<sub>2</sub>" + ": " + n2 + "%";
}


//! Checks if the player is extinct
function checkExtinction(population){
    if(population <= 0){
        document.getElementById("extinctionTitle").style.display = "inline-block";
        document.getElementById("extinctionBody").style.display = "inline-block";
        document.getElementById("extinctionContainer").style.display = "inline-block";
    }else{
        document.getElementById("extinctionTitle").style.display = "none";
        document.getElementById("extinctionBody").style.display = "none";
        document.getElementById("extinctionContainer").style.display = "none";
    }
}

function checkGeneration (generation, population){
    if(generation >= 20 && population >= 400 && wonOnce == false){
        document.getElementById("winTitle").style.display = "inline-block";
        document.getElementById("winBody").style.display = "inline-block";
        document.getElementById("winContainer").style.display = "inline-block";
        wonOnce = true;
        setTimeout(hideWinText, 14000);
    }
}

//! Supplementry function for checkGeneration that hides the wintext
function hideWinText(){
    document.getElementById("winTitle").style.display = "none";
    document.getElementById("winBody").style.display = "none";
    document.getElementById("winContainer").style.display = "none";
}

//! Updates the GUI bars
//! values needs to be an object with properties set with values for everything
function updateMicrobeHUDBars(values){
    document.getElementById("microbeHUDPlayerHitpoints").textContent =
        values.hitpoints;
    document.getElementById("microbeHUDPlayerMaxHitpoints").textContent =
        values.maxHitpoints;
    document.getElementById("microbeHUDPlayerHitpointsBar").style.width =
        common.barHelper(values.hitpoints, values.maxHitpoints);

    // TODO: remove this debug code
    document.getElementById("microbeHUDPlayerATP").textContent =
        values.compoundATP.toFixed(1);

    // The bars
    document.getElementById("microbeHUDPlayerATPCompound").textContent =
         values.compoundATP.toFixed(1);
    document.getElementById("microbeHUDPlayerATPMax").textContent =
         values.ATPMax;
    document.getElementById("microbeHUDPlayerATPBar").style.width =
         common.barHelper(values.compoundATP, values.ATPMax);

    document.getElementById("microbeHUDPlayerAmmonia").textContent =
        values.compoundAmmonia.toFixed(1);
    document.getElementById("microbeHUDPlayerAmmoniaMax").textContent =
        values.AmmoniaMax;
    document.getElementById("microbeHUDPlayerAmmoniaBar").style.width =
        common.barHelper(values.compoundAmmonia, values.AmmoniaMax);

    document.getElementById("microbeHUDPlayerPhosphates").textContent =
        values.compoundPhosphate.toFixed(1);
    document.getElementById("microbeHUDPlayerPhosphatesMax").textContent =
        values.PhosphateMax;
    document.getElementById("microbeHUDPlayerPhosphatesBar").style.width =
        common.barHelper(values.compoundPhosphate, values.PhosphateMax);

    document.getElementById("microbeHUDPlayerGlucose").textContent =
        values.compoundGlucose.toFixed(1);
    document.getElementById("microbeHUDPlayerGlucoseMax").textContent =
        values.GlucoseMax;
    document.getElementById("microbeHUDPlayerGlucoseBar").style.width =
        common.barHelper(values.compoundGlucose, values.GlucoseMax);

    document.getElementById("microbeHUDPlayerOxytoxy").textContent =
        values.compoundOxytoxy.toFixed(1);
    document.getElementById("microbeHUDPlayerOxytoxyMax").textContent =
        values.OxytoxyMax;
    document.getElementById("microbeHUDPlayerOxytoxyBar").style.width =
        common.barHelper(values.compoundOxytoxy, values.OxytoxyMax);

    document.getElementById("microbeHUDPlayerHydrogenSulfide").textContent =
        values.compoundHydrogenSulfide.toFixed(1);
    document.getElementById("microbeHUDPlayerHydrogenSulfideMax").textContent =
        values.HydrogenSulfideMax;
    document.getElementById("microbeHUDPlayerHydrogenSulfideBar").style.width =
        common.barHelper(values.compoundHydrogenSulfide, values.HydrogenSulfideMax);

    document.getElementById("microbeHUDPlayerIron").textContent =
        values.compoundIron.toFixed(1);
    document.getElementById("microbeHUDPlayerIronMax").textContent =
        values.IronMax;
    document.getElementById("microbeHUDPlayerIronBar").style.width =
        common.barHelper(values.compoundIron, values.IronMax);

}
