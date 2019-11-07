// JavaScript code to handle updating all the microbe stage stuff

import * as common from "./gui_common.mjs";
import * as main_menu from "./main_menu.mjs";
import {doEnterMicrobeEditor} from "./microbe_editor.mjs";


let microbeHudSetupRan = false;

let readyToEdit = false;

let wonOnce = false;

// Variable to show data useful during develop
const showMouseCoordinates = false;

// For toggling paused with the pause button
let paused = false;

//! Registers all the stuff for this to work.
//! This makes sure it does something only once
export function runMicrobeHUDSetup(){

    if(microbeHudSetupRan)
        return;

    document.getElementById("pauseButtonBottom").addEventListener("click",
        onPauseButtonClicked, true);

    document.getElementById("microbeToEditorButton").addEventListener("click",
        onEditorButtonClicked, true);

    // Compound Panel
    document.getElementById("compoundsButton").addEventListener("click",
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

    // Compounds eompress Panel button
    const compressPanels = document.getElementsByClassName("compressPanel");

    for (const element of compressPanels) {

        const panelToChange = document.getElementById(element.getAttribute("data-parentId"));
        element.addEventListener("click",
            () => {
                if(!panelToChange.classList.contains("Compress")) {

                    // Determine different values because right now
                    // The two panels are  different in layout
                    if(panelToChange.id == "compoundsPanel") {
                        onCompressPanelClicked(panelToChange, {
                            bar: "Bar",
                            title: "BarTitle",
                            value: "BarValue",
                            height: 92,
                            width: 150,
                            background:
                                "url('../../Textures/gui/bevel/CompoundPanelCompress.png')",
                            leftMargin: -25,
                            valueLeft: -30
                        });
                    } else {
                        onCompressPanelClicked(panelToChange, {
                            bar: "EnvironmentBar",
                            title: "EnvironmentBarTitle",
                            value: "EnvironmentBarValue",
                            height: 58,
                            width: 100,
                            background:
                                "url('../../Textures/gui/bevel/environmentPanelCompress.png')",
                            leftMargin: 5,
                            valueLeft: 15
                        });
                    }
                    panelToChange.classList.add("Compress");
                    panelToChange.classList.remove("Expand");
                }
            }, true);
    }

    // Compounds expand Panel button
    const expandPanels = document.getElementsByClassName("expandPanel");

    for (const element of expandPanels) {
        const panelToChange = document.getElementById(element.getAttribute("data-parentId"));
        element.addEventListener("click",
            () => {
                if(!panelToChange.classList.contains("Expand")) {

                    // Determine different values because right now
                    // The two panels are  different in layout
                    if(panelToChange.id == "compoundsPanel") {
                        onExpandPanelClicked(panelToChange, {
                            bar: "Bar",
                            title: "BarTitle",
                            value: "BarValue",
                            height: 92,
                            width: 150,
                            background:
                                "url('../../Textures/gui/bevel/CompoundPanelExpand.png')",
                            leftMargin: 20,
                            valueLeft: 120
                        });
                    } else {
                        onExpandPanelClicked(panelToChange, {
                            bar: "EnvironmentBar",
                            title: "EnvironmentBarTitle",
                            value: "EnvironmentBarValue",
                            height: 58,
                            width: 100,
                            background:
                                "url('../../Textures/gui/bevel/environmentPanelExpand.png')",
                            leftMargin: 20,
                            valueLeft: 100
                        });
                    }
                    panelToChange.classList.add("Expand");
                    panelToChange.classList.remove("Compress");
                }
            }, true);
    }

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

        // Event for patch details
        Leviathan.OnGeneric("UpdatePatchDetails", (event, vars) => {

            // Apply the new values
            updatePatchInfo(vars.patchName);
        });

        // Event for checking extinction
        Leviathan.OnGeneric("CheckExtinction", (event, vars) => {
            checkExtinction(vars.population);
        });

        // Event for updating o2 and c02 numbers
        Leviathan.OnGeneric("UpdateDissolvedGasses", (event, vars) => {
            updateEnvironmentalCompounds(vars.oxygenPercent, vars.co2Percent, vars.n2Percent,
                vars.sunlightPercent);
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


        // Event for updating player pecies name
        Leviathan.OnGeneric("updateSpeciesName", (event, vars) => {

            // Apply the new species name
            updateSpeciesName(vars.speciesName);
        });

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
            reproductionProgress: common.randomBetween(0, 100) / 100.0,
            reproductionAmmoniaFraction: common.randomBetween(0, 100) / 100.0,
            reproductionPhosphatesFraction: common.randomBetween(0, 100) / 100.0,
        });

        // Pseudo population code
        updatePopulation(common.randomBetween(0, 50));

        // Put some hover stuff
        updateHoverInfo({
            mousePos: "[0, 0, 0]",
            ammonia0: "Ammonia: 12.2",
        });

        updatePatchInfo("Browser patch");

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

function updatePatchInfo(patchName){
    document.getElementById("infoPatch").textContent = "Patch: " + patchName;
}

//! Enables the editor button
export function onReadyToEnterEditor(){

    readyToEdit = true;
    document.getElementById("microbeToEditorButton").classList.remove("DisabledButton");
    document.getElementById("microbeToEditorButton").style.zIndex = "1";
    document.getElementById("microbeToEditorButton").classList.add("pulseEditor");
}


//! Disabled the editor button
export function onResetEditor(){

    // Disable and remove animation
    document.getElementById("microbeToEditorButton").classList.add("DisabledButton");
    document.getElementById("microbeToEditorButton").classList.remove("pulseEditor");
    document.getElementById("microbeToEditorButton").style.zIndex = "-1";
    readyToEdit = false;
}


function onPauseButtonClicked(){
    paused = !paused;
    Thrive.pause(paused);
    document.getElementById("pauseButtonBottom").classList.toggle("paused");
}

function onCompoundPanelClicked() {
    common.playButtonPressSound();
    document.getElementById("compoundsPanel").style.transition = "0s";

    $("#environmentPanel").animate({"width": "toggle"});
    $("#compoundsPanel").animate({"width": "toggle"});
    $("#agentsPanel").animate({"width": "toggle"});

    document.getElementById("compoundsButton").classList.toggle("active");
    document.getElementById("compoundsButton").classList.toggle("inactive");
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
    document.getElementById("mainMenuButton").classList.add("MainMenuActive");
    document.getElementById("mainMenuButton").classList.remove("MainMenuNormal");
    const pause = document.getElementById("pauseOverlay");
    pause.style.display = "block";
    const help = document.getElementById("helpText");
    help.style.display = "none";
    Thrive.pause(true);
}

function onResumeClicked(){

    common.playButtonPressSound();
    document.getElementById("mainMenuButton").classList.remove("MainMenuActive");
    document.getElementById("mainMenuButton").classList.add("MainMenuNormal");
    const pause = document.getElementById("pauseOverlay");
    pause.style.display = "none";

    // Use paused here so the game won't be unpaused when also paused by the pause button.
    Thrive.pause(paused);
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
    document.getElementById("microbeToEditorButton").style.zIndex = "-1";
    document.getElementById("microbeToEditorButton").classList.remove("pulseEditor");
    readyToEdit = false;

    return true;
}

//! Exit to main menu clicked
function onExitToMenuClicked() {
    if(common.isInEngine()){
        document.getElementById("extinctionTitle").style.display = "none";
        document.getElementById("extinctionBody").style.display = "none";
        document.getElementById("extinctionContainer").style.display = "none";
        document.getElementById("microbeToEditorButton").classList.add("DisabledButton");
        document.getElementById("microbeToEditorButton").classList.remove("pulse");
        document.getElementById("mainMenuButton").classList.remove("MainMenuActive");
        document.getElementById("mainMenuButton").classList.add("MainMenuNormal");
        document.getElementById("pauseButtonBottom").classList.remove("paused");

        readyToEdit = false;
        hideWinText();

        // Gotta reset this
        wonOnce = false;
        paused = false;
        Thrive.exitToMenuClicked();

    } else {
        main_menu.doExitToMenu();
    }
}


function updateSpeciesName(speciesName) {
    document.getElementById("speciesName").innerHTML = speciesName;
}


//! Updates the mouse hover box with stuff
function updateHoverInfo(vars){

    const panel = document.getElementById("mouseHoverPanel");
    common.clearChildren(panel);

    if(showMouseCoordinates) {
        panel.appendChild(document.createTextNode("Stuff at " + vars.mousePos + ":"));
        panel.appendChild(document.createElement("br"));
    }

    const mainContent = document.createElement("div");
    mainContent.style.width = "100%";
    mainContent.style.height = "15px";
    mainContent.style.color = "white";
    mainContent.style.textAlign = "center";
    mainContent.innerHTML = "Hello";

    if(vars.noCompounds){

        mainContent.innerHTML = "Nothing to eat here";
        panel.appendChild(mainContent);
    } else {

        if(!vars.compounds){
            mainContent.innerHTML = "Error reading compounds Data";
            return;
        }
        let objCompoundsData = null;


        // Compounds data are store in json format, so we need parse it
        if(typeof vars.compounds === "string" || vars.compounds instanceof String){
            try{
                objCompoundsData = JSON.parse(vars.compounds);
            } catch(err){
                mainContent.innerHTML = "invalid json for mouseHover info: " + err;
                return;
            }
        } else {
            objCompoundsData = vars.compounds;
        }

        mainContent.innerHTML = "At cursor:";
        panel.appendChild(mainContent);

        const title = document.createElement("p");
        title.style.fontSize = "12pt";
        title.style.marginTop = "0";
        title.style.marginBottom = "5px";
        const titleText = document.createTextNode("Compounds: ");
        title.appendChild(titleText);
        panel.appendChild(title);


        // Create for each compound the information in GUI
        objCompoundsData.forEach(function(compoundData){
            // Line breaks between elements
            panel.appendChild(document.createElement("br"));
            const img = document.createElement("img");
            let src = "../../Textures/gui/bevel/";

            src = src + compoundData.name.replace(/\s/g, "") + ".png";

            const par = document.createElement("p");

            par.style.marginBottom = "0";
            par.style.paddingBottom = "10px";
            par.style.marginTop = "0";
            img.setAttribute("src", src);
            img.style.verticalAlign = "text-bottom";
            img.setAttribute("width", "25");
            img.setAttribute("height", "25");
            par.appendChild(img);
            par.appendChild(document.createTextNode("  " + compoundData.name + ": "));
            const parText = document.createTextNode("" + compoundData.quantity.toFixed(2));
            par.appendChild(parText);
            panel.appendChild(par);
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
function updateEnvironmentalCompounds(oxygen, c02, n2, light){
    document.getElementById("oxygenPercent").innerHTML = oxygen + "%";
    document.getElementById("carbonDioxidePercent").innerHTML = c02 + "%";
    document.getElementById("nitrogenPercent").innerHTML = n2 + "%";
    document.getElementById("sunlightPercent").innerHTML = light + "%";
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
    if(generation >= 20 && population >= 300 && wonOnce == false){
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

function onCompressPanelClicked(panelToChange, dataToChange) {

    $("#Panels").animate({height: $("#Panels").height() - dataToChange.height + "px"}, 300);

    panelToChange.style.backgroundImage = dataToChange.background;
    panelToChange.style.height = panelToChange.offsetHeight - dataToChange.height + "px";

    // Change buttons status
    panelToChange.querySelector(".compressPanel").style.backgroundImage =
        "url('../../Textures/gui/bevel/compressPanelActive.png')";
    panelToChange.querySelector(".expandPanel").style.backgroundImage =
        "url('../../Textures/gui/bevel/expandPanel.png')";

    //! ROWS
    const rows = panelToChange.querySelectorAll(".row");

    for(const row of rows) {
        const bars = row.getElementsByClassName(dataToChange.bar);
        const title = row.getElementsByClassName(dataToChange.title);
        const barValues = row.getElementsByClassName(dataToChange.value);

        for (const bar of bars) {

            bar.style.display = "inline-block";
            bar.style.width = bar.offsetWidth - dataToChange.width + "px";
            bar.style.marginLeft = dataToChange.leftMargin + "px";
            bar.style.marginBottom = "0px";
            bar.style.marginTop = "6px";
        }

        for (const tit of title) {
            tit.style.visibility = "hidden";
        }

        for (const barValue of barValues) {
            barValue.style.left = dataToChange.valueLeft + "px";
        }
    }
}

//! Expand panel function
function onExpandPanelClicked(panelToChange, dataToChange) {


    $("#Panels").animate({height: $("#Panels").height() + dataToChange.height + "px"}, 300);

    // Change buttons status
    panelToChange.querySelector(".compressPanel").style.backgroundImage =
        "url('../../Textures/gui/bevel/compressPanel.png')";
    panelToChange.querySelector(".expandPanel").style.backgroundImage =
        "url('../../Textures/gui/bevel/expandPanelActive.png')";

    panelToChange.style.backgroundImage = dataToChange.background;
    panelToChange.style.height = panelToChange.offsetHeight + dataToChange.height + "px";

    const rows = panelToChange.querySelectorAll(".row");

    for(const row of rows) {
        const bars = row.getElementsByClassName(dataToChange.bar);
        const title = row.getElementsByClassName(dataToChange.title);
        const barValues = row.getElementsByClassName(dataToChange.value);

        for (const bar of bars) {
            bar.style.display = "block";
            bar.style.marginBottom = "4px";
            bar.style.marginTop = "6px";
            bar.style.width = bar.offsetWidth + dataToChange.width + "px";
            bar.style.marginLeft = dataToChange.leftMargin + "px";
        }

        for (const tit of title) {
            tit.style.visibility = "visible";
        }

        for (const barValue of barValues) {
            barValue.style.left = dataToChange.valueLeft + "px";
        }
    }
}

//! Updates the GUI bars
//! values needs to be an object with properties set with values for everything
function updateMicrobeHUDBars(values){
    // The bars
    document.getElementById("microbeHUDPlayerHitpoints").textContent =
        values.hitpoints;
    document.getElementById("microbeHUDPlayerMaxHitpoints").textContent =
        values.maxHitpoints;

    document.getElementById("microbeHUDPlayerATPCompound").textContent =
         values.compoundATP.toFixed(1);
    document.getElementById("microbeHUDPlayerATPMax").textContent =
         values.ATPMax;

    const valueAtp = common.barHelper(values.compoundATP, values.ATPMax).replace("%", "");
    const valueHp = common.barHelper(values.hitpoints, values.maxHitpoints).replace("%", "");

    const circles = document.querySelectorAll("#circleBars");

    // Instead of using totalProgress var, two hardCoded value are used
    // They are in thrive_gui.html at line 117 and 134.
    // two loops could be used but this need draw two differents svg for each circle

    for(const circle of circles) {

        let progress = 100 - valueAtp;

        if(valueAtp < 2.5) {
            circle.querySelector("#shapeAtp").style["stroke-dashoffset"] = 192.042;
        } else {
            circle.querySelector("#shapeAtp").style["stroke-dashoffset"] =
                192.042 * progress / 100;
        }

        progress = 100 - valueHp;
        circle.querySelector("#shapeHp").style["stroke-dashoffset"] = 231.13 * progress / 100;
        circle.querySelector("#shapeAmmonia").style["stroke-dashoffset"] =
            191.673 * values.reproductionAmmoniaFraction;
        circle.querySelector("#shapePhosphate").style["stroke-dashoffset"] =
            -191.673 * values.reproductionPhosphatesFraction;
    }

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
