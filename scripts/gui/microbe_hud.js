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
            document.getElementById("microbeHUDPlayerHitpoints").textContent = vars.hitpoints;
            document.getElementById("microbeHUDPlayerMaxHitpoints").textContent =
                vars.maxHitpoints;

            document.getElementById("microbeHUDPlayerATP").textContent =
                vars.compoundATP;
            
        });
    }
    
    microbeHudSetupRan = true;
}
