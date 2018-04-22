// JavaScript code to handle updating all the microbe stage stuff

let microbeHudSetupRan = false;

// Registers all the stuff for this to work. For performance reasons
// this should only be called
function runMicrobeHUDSetup(){

    if(microbeHudSetupRan)
        return;

    if(isInEngine()){

        // Register for the microbe stage events
        Leviathan.OnGeneric("PlayerCompoundAmounts", (event, vars) => {

            // Apply the new values
            document.getElementById("microbeHUDPlayerHitpoints").textContent =
                vars.hitpoints;
            document.getElementById("microbeHUDPlayerMaxHitpoints").textContent =
                vars.maxHitpoints;

            document.getElementById("microbeHUDPlayerATP").textContent =
                vars.compoundATP;

            // The bars
            document.getElementById("microbeHUDPlayerATPCompound").textContent =
                vars.compoundATP;
            document.getElementById("microbeHUDPlayerATPMax").textContent =
                vars.ATPMax;

            document.getElementById("microbeHUDPlayerAminoacids").textContent =
                vars.compoundAminoacids;
            document.getElementById("microbeHUDPlayerAminoacidsMax").textContent =
                vars.AminoacidsMax;

            document.getElementById("microbeHUDPlayerAmmonia").textContent =
                vars.compoundAmmonia;
            document.getElementById("microbeHUDPlayerAmmoniaMax").textContent =
                vars.AmmoniaMax;

            document.getElementById("microbeHUDPlayerGlucose").textContent =
                vars.compoundGlucose;
            document.getElementById("microbeHUDPlayerGlucoseMax").textContent =
                vars.GlucoseMax;

            document.getElementById("microbeHUDPlayerFattyacids").textContent =
                vars.compoundFattyacids;
            document.getElementById("microbeHUDPlayerFattyacidsMax").textContent =
                vars.FattyacidsMax;

            document.getElementById("microbeHUDPlayerOxytoxy").textContent =
                vars.compoundOxytoxy;
            document.getElementById("microbeHUDPlayerOxytoxyMax").textContent =
                vars.OxytoxyMax;
        });
    }
    
    microbeHudSetupRan = true;
}
