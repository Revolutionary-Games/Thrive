// Planet editor GUI scripts
import * as common from "./gui_common.mjs";

// Import * as main_menu from "./main_menu.mjs";

let freebuild = false;

function updatePlanetValues(data){
    //add the star GetVariables
    document.getElementById("starMassSlider").value = data.orbitingBody.mass;
    document.getElementById("starMassValueBox").innerHTML = "Star Mass <br>" + data.orbitingBody.mass;
    document.getElementById("starRadiusValueBox").innerHTML = "Star Radius <br>" + data.orbitingBody.radius;
    document.getElementById("starGravitationalParameterValueBox").innerHTML = "Star Gravitational Parameter <br>" + data.orbitingBody.gravitationalParameter;
    document.getElementById("starLifespanValueBox").innerHTML = "Star Lifespan <br>" + data.orbitingBody.lifeSpan;
    document.getElementById("starTemperatureValueBox").innerHTML = "Star Temperature <br>" + data.orbitingBody.temperature;

    drawGraph(document.getElementById("stellarSpectrumGraph"), data.orbitingBody.stellarSpectrum);

    //add the planet variables
    document.getElementById("massSlider").value = data.mass;
    document.getElementById("planetMassValueBox").innerHTML = "Planet Mass <br>" + data.mass;
    document.getElementById("radiusSlider").value = data.radius;
    document.getElementById("planetRadiusValueBox").innerHTML = "Planet Radius <br>" + data.radius;
    document.getElementById("planetOceanMassValueBox").innerHTML = "Ocean Mass <br>" + data.oceanMass;
    document.getElementById("planetLithosphereMassValueBox").innerHTML = "Lithosphere Mass <br>" + data.lithosphereMass;
    document.getElementById("planetAtmosphereMassValueBox").innerHTML = "Atmosphere Mass <br>" + data.atmosphereMass;
    document.getElementById("planetAtmosphereWaterValueBox").innerHTML = "Atmosphere Water <br>" + data.atmosphereWater;
    document.getElementById("planetAtmosphereOxygenValueBox").innerHTML = "Atmosphere Oxygen <br>" + data.atmosphereOxygen;
    document.getElementById("planetAtmosphereNitrogenValueBox").innerHTML = "Atmosphere Nitrogen <br>" + data.atmosphereNitrogen;
    document.getElementById("planetCarbonDioxideValueBox").innerHTML = "Atmosphere CarbonDioxide <br>" + data.atmosphereCarbonDioxide;
    document.getElementById("planetTemperatureValueBox").innerHTML = "Planet Temperature <br>" + data.planetTemperature;

    //drawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("terrestrialSpectrumGraph"), data.terrestrialSpectrum);

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

//functions for graph drawing
//draw a line in the graph box
function drawLine(graph, x1,y1,x2,y2, stroke, strokeWidth)
{
	graph.innerHTML += "<line x1=\"" + x1 + "\" y1=\"" + y1 + "\" x2=\"" + x2 + "\" y2=\"" + y2 + "\" style=\"stroke:" + stroke + ";stroke-width:" + strokeWidth + "\" />";
}

//draw the graph
function drawGraph(graph, data)
{
    //check if there is good data
    if (data === undefined || data.length == 0) {
        return;
    }
	//get the size of the container
	var positionInfo = graph.getBoundingClientRect();
	var height = positionInfo.height;
	var width = positionInfo.width;
	//work out the bounds of the data
	var xRange = data.length;
	var yMax = Math.max(...data);
	var yMin = Math.min(...data);
	var yRange = yMax - yMin;
	//draw the axes
	var offset = 10; // padding for the x and y axis
	drawLine(graph,offset,offset,offset,height - offset,"rgb(0,0,200)", "3");
	drawLine(graph,offset,height - offset,width - offset,height - offset,"rgb(0,0,200)", "3");
	//draw the points of the graph
    var newX = 0;
    var newY = 0;
	var oldX = offset;
	var oldY = height - offset;
	var xStep = (width - 2*offset)/xRange;
	var yStep = (height - 2*offset)/yRange;
	for (var i in data)
	{
		newX = oldX + xStep;
		newY = (height - offset) - yStep*data[i];
		drawLine(graph,oldX,oldY,newX,newY,"rgb(200,10,50)", "3");
		oldX = newX;
		oldY = newY;
	}
}
