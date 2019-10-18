// Planet editor GUI scripts
import * as common from "./gui_common.mjs";

// Import * as main_menu from "./main_menu.mjs";

let freebuild = false;

function updatePlanetValues(data){
    //add the star GetVariables
    document.getElementById("starMassSlider").value = data.orbitingBody.mass;
    document.getElementById("starMassValueBox").innerHTML = "Star Mass <br>" + data.orbitingBody.mass;
    //add the planet variables
    document.getElementById("massSlider").value = data.mass;
    document.getElementById("planetMassValueBox").innerHTML = "Planet Mass <br>" + data.mass;
    document.getElementById("radiusSlider").value = data.radius;
    document.getElementById("planetRadiusValueBox").innerHTML = "Planet Radius <br>" + data.radius;
}

export function setupPlanetEditor(fromFreebuild){
    document.getElementById("starMassSlider").addEventListener("input",
        onStarMassInput, true);

    document.getElementById("massSlider").addEventListener("input",
        onMassInput, true);

    document.getElementById("radiusSlider").addEventListener("input",
        onRadiusInput, true);

    document.getElementById("planetEditorBack").addEventListener("click",
        Thrive.exitToMenuClicked, true);

    document.getElementById("planetEditorStartGame").addEventListener("click",
        startGame, true);

    Leviathan.OnGeneric("PlanetEditorPlanetModified", (event, vars) => {
        const data = JSON.parse(vars.data);
        updatePlanetValues(data);
    });

    document.addEventListener("keydown", (event) => {
        if(event.key === "Escape"){

            event.stopPropagation();
            onEscapePressed();
        }
    }, true);

    freebuild = fromFreebuild;
}

function onStarMassInput(event){
    Thrive.editPlanet("starMass", parseFloat(event.target.value));
}

function onMassInput(event){
    Thrive.editPlanet("mass", parseFloat(event.target.value));
}

function onRadiusInput(event){
    Thrive.editPlanet("radius", parseFloat(event.target.value));
}

function startGame(){
    if(common.isInEngine()){
        if(freebuild){
            onMicrobeIntroEnded();
        } else {
            Leviathan.PlayCutscene("Data/Videos/MicrobeIntro.mkv", onMicrobeIntroEnded,
                onMicrobeIntroEnded);
        }
        Leviathan.CallGenericEvent("UpdateLoadingScreen", {show: true, status: "Loading Microbe Stage", message: ""});
    } else {
        onMicrobeIntroEnded();
    }
}

//! Handles pressing Escape in the GUI (this will unpause the game,
//! pausing is initiated from c++ key listener)
function onEscapePressed() {
    // TODO: move this to the cutscene player
    Leviathan.CancelCutscene();
}

function onMicrobeIntroEnded(error){

    if(error)
        console.error("failed to play microbe intro video: " + error);

    // MenuAlreadySkipped = true;

    if(common.isInEngine()){

        Leviathan.CallGenericEvent("UpdateLoadingScreen", {show: false});

        // Make sure no video is playing in case we did an immediate start
        Leviathan.CancelCutscene();
        Thrive.start();

        if(freebuild){
            Thrive.freebuildEditorButtonClicked();
        }

    } else {

        // Show the microbe GUI anyway for testing purposes
    }

    switchToMicrobeHUD();
}

function switchToMicrobeHUD(){
    // Hide planet editor
    // If this is ever restored this needs to be set to "flex"
    document.getElementById("topLevelPlanetEditor").style.display = "none";

    // And show microbe gui
    document.getElementById("topLevelMicrobeStage").style.display = "block";
}
