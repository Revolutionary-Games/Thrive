// JavaScript code to handle updating all the microbe stage stuff
"use strict";

let microbeHudSetupRan = false;

let readyToEdit = false;

//! Registers all the stuff for this to work. For performance reasons
//! this should only be called
function runMicrobeHUDSetup(){

    if(microbeHudSetupRan)
        return;

    document.getElementById("microbeToEditorButton").addEventListener(
        "click", onEditorButtonClicked, true);

    if(isInEngine()){

        // Register for the microbe stage events
        Leviathan.OnGeneric("PlayerCompoundAmounts", (event, vars) => {

            // Apply the new values
            updateMicrobeHUDBars(vars);
        });
    } else {

        // Update random values to make it prettier to look at
        let hp = randomBetween(10, 50);
        let ammonia = randomBetween(0, 50);
        let glucose = randomBetween(10, 50);
        let oxytoxy = randomBetween(0, 10);
        let phosphate = randomBetween(0, 50);
        let hydrogenSulfide = randomBetween(0, 50);
        updateMicrobeHUDBars({
            hitpoints: randomBetween(1, hp),
            maxHitpoints: hp,
            compoundATP: randomBetween(10, 100),
            ATPMax: 100,
            compoundAmmonia: randomBetween(0, ammonia),
            AmmoniaMax: ammonia,
            compoundGlucose: randomBetween(0, glucose),
            GlucoseMax: glucose,
            compoundOxytoxy: randomBetween(0, oxytoxy),
            OxytoxyMax: oxytoxy,
            compoundPhosphate: randomBetween(0, phosphate),
            PhosphateMax: phosphate,
            compoundHydrogenSulfide: randomBetween(0, hydrogenSulfide),
            HydrogenSulfideMax: hydrogenSulfide,
        });

        onReadyToEnterEditor();
    }
    
    microbeHudSetupRan = true;
}

//! Enables the editor button
function onReadyToEnterEditor(){
    
    readyToEdit = true;
    document.getElementById("microbeToEditorButton").classList.remove("DisabledButton");
}

function onEditorButtonClicked(event){
    
    if(!readyToEdit)
        return false;
    
    event.stopPropagation();
    playButtonPressSound();
    
    // Fire event
    if(isInEngine()){

        // Fire an event to tell the game to swap to the editor. It
        // will notify us when it is done
        
    } else {

        // Swap GUI for previewing
        doEnterMicrobeEditor();
    }
    
    // Disable
    document.getElementById("microbeToEditorButton").classList.add("DisabledButton");
    readyToEdit = false;
}

//! Updates the GUI bars
//! values needs to be an object with properties set with values for everything
function updateMicrobeHUDBars(values){
    document.getElementById("microbeHUDPlayerHitpoints").textContent =
        values.hitpoints;
    document.getElementById("microbeHUDPlayerMaxHitpoints").textContent =
        values.maxHitpoints;
    document.getElementById("microbeHUDPlayerHitpointsBar").style.width =
        barHelper(values.hitpoints, values.maxHitpoints);

    // TODO: remove this debug code
    document.getElementById("microbeHUDPlayerATP").textContent =
        values.compoundATP.toFixed(1);

    // The bars
    // document.getElementById("microbeHUDPlayerATPCompound").textContent =
    //     values.compoundATP;
    document.getElementById("microbeHUDPlayerATPMax").textContent =
        values.ATPMax;
    document.getElementById("microbeHUDPlayerATPBar").style.width =
        barHelper(values.compoundATP, values.ATPMax);

    document.getElementById("microbeHUDPlayerAmmonia").textContent =
        values.compoundAmmonia.toFixed(1);
    document.getElementById("microbeHUDPlayerAmmoniaMax").textContent =
        values.AmmoniaMax;
    document.getElementById("microbeHUDPlayerAmmoniaBar").style.width =
        barHelper(values.compoundAmmonia, values.AmmoniaMax);

    document.getElementById("microbeHUDPlayerPhosphates").textContent =
        values.compoundPhosphate.toFixed(1);
    document.getElementById("microbeHUDPlayerPhosphatesMax").textContent =
        values.PhosphateMax;
    document.getElementById("microbeHUDPlayerPhosphatesBar").style.width =
        barHelper(values.compoundPhosphate, values.PhosphateMax);
        
    document.getElementById("microbeHUDPlayerGlucose").textContent =
        values.compoundGlucose.toFixed(1);
    document.getElementById("microbeHUDPlayerGlucoseMax").textContent =
        values.GlucoseMax;
    document.getElementById("microbeHUDPlayerGlucoseBar").style.width =
        barHelper(values.compoundGlucose, values.GlucoseMax);

    document.getElementById("microbeHUDPlayerOxytoxy").textContent =
        values.compoundOxytoxy.toFixed(1);
    document.getElementById("microbeHUDPlayerOxytoxyMax").textContent =
        values.OxytoxyMax;
    document.getElementById("microbeHUDPlayerOxytoxyBar").style.width =
        barHelper(values.compoundOxytoxy, values.OxytoxyMax);
        
    document.getElementById("microbeHUDPlayerHydrogenSulfide").textContent =
        values.compoundHydrogenSulfide.toFixed(1);
    document.getElementById("microbeHUDPlayerHydrogenSulfideMax").textContent =
        values.HydrogenSulfideMax;
    document.getElementById("microbeHUDPlayerHydrogenSulfideBar").style.width =
        barHelper(values.compoundHydrogenSulfide, values.HydrogenSulfideMax);
        
}
