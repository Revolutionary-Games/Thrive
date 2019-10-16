// Planet editor GUI scripts
import * as common from "./gui_common.mjs";
import * as main_menu from "./main_menu.mjs";

let freebuild = false;

export function setupPlanetEditor(freebuild){
    document.getElementById("massSlider").addEventListener("input",
        onMassInput, true);
    
    document.getElementById("radiusSlider").addEventListener("input",
		onRadiusInput, true);

    document.getElementById("planetEditorBack").addEventListener("click",
        Thrive.exitToMenuClicked, true);
    
    document.getElementById("planetEditorStartGame").addEventListener("click",
		startGame, true);

    Leviathan.OnGeneric("PlanetEditorPlanetModified", (event, vars) => {
        let data = JSON.parse(vars.data);
        document.getElementById("massSlider").value = data.mass;
        document.getElementById("radiusSlider").value = data.radius;
    });

    freebuild = freebuild;
}

function onMassInput(event){
    Thrive.editPlanet("mass", parseFloat(event.target.value));
}

function onRadiusInput(event){
    Thrive.editPlanet("radius", parseFloat(event.target.value));
}

function startGame(){
    if(common.isInEngine()){
        Leviathan.PlayCutscene("Data/Videos/MicrobeIntro.mkv", onMicrobeIntroEnded,
            onMicrobeIntroEnded);
        Leviathan.CallGenericEvent("UpdateLoadingScreen", {show: true, status: "Loading Microbe Stage", message: ""});
    } else {
        onMicrobeIntroEnded();
    }
}

function onMicrobeIntroEnded(error){

    if(error)
        console.error("failed to play microbe intro video: " + error);

    //menuAlreadySkipped = true;

    if(common.isInEngine()){

        Leviathan.CallGenericEvent("UpdateLoadingScreen", {show: false});

        // Make sure no video is playing in case we did an immediate start
        Leviathan.CancelCutscene();
        Thrive.start();

    } else {

        // Show the microbe GUI anyway for testing purposes
    }

    switchToMicrobeHUD();
}

function switchToMicrobeHUD(){

    // Stop menu music
    if(jams){

        jams.Pause();
    }

    // Hide main menu
    // If this is ever restored this needs to be set to "flex"
    document.getElementById("topLevelMenuContainer").style.display = "none";

    // And show microbe gui
    document.getElementById("topLevelMicrobeStage").style.display = "block";
}
