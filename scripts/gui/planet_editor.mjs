// Planet editor GUI scripts
import * as common from "./gui_common.mjs";

// Import * as main_menu from "./main_menu.mjs";

let freebuild = false;

function updatePlanetValues(data){
    //add the star GetVariables
    document.getElementById("starMassSlider").value = data.orbitingBody.mass;
    document.getElementById("starMassValueBox").innerHTML = "Star Mass <br>" + scienceNumber(data.orbitingBody.mass) + " kg.";
    document.getElementById("starRadiusValueBox").innerHTML = "Star Radius <br>" + scienceNumber(data.orbitingBody.radius) + " meters.";
    document.getElementById("starGravitationalParameterValueBox").innerHTML = "Star Gravitational Parameter <br>" + scienceNumber(data.orbitingBody.gravitationalParameter);
    document.getElementById("starLifespanValueBox").innerHTML = "Star Lifespan <br>" + scienceNumber(data.orbitingBody.lifeSpan) + " earth years.";
    document.getElementById("starTemperatureValueBox").innerHTML = "Star Temperature <br>" + scienceNumber(data.orbitingBody.temperature) + " kelvin.";

    drawGraph(document.getElementById("stellarSpectrumGraph"), data.orbitingBody.stellarSpectrum);

    //add the planet variables
    document.getElementById("planetMassSlider").value = data.mass;
    document.getElementById("planetMassValueBox").innerHTML = "Planet Mass <br>" + scienceNumber(data.mass) + " kg.";
    document.getElementById("planetRadiusValueBox").innerHTML = "Planet Radius <br>" + scienceNumber(data.radius) + " meters";
    document.getElementById("planetOceanMassValueBox").innerHTML = "Ocean Mass <br>" + scienceNumber(data.oceanMass) + " kg.";
    document.getElementById("planetLithosphereMassValueBox").innerHTML = "Lithosphere Mass <br>" + scienceNumber(data.lithosphereMass) + " kg.";
    document.getElementById("planetAtmosphereMassValueBox").innerHTML = "Atmosphere Mass <br>" + scienceNumber(data.atmosphereMass) + " kg.";
    document.getElementById("planetAtmosphereWaterValueBox").innerHTML = "Atmosphere Water <br>" + scienceNumber(data.atmosphereWater) + " kg.";
    document.getElementById("planetAtmosphereOxygenValueBox").innerHTML = "Atmosphere Oxygen <br>" + scienceNumber(data.atmosphereOxygen) + " kg.";
    document.getElementById("planetAtmosphereNitrogenValueBox").innerHTML = "Atmosphere Nitrogen <br>" + scienceNumber(data.atmosphereNitrogen) + " kg.";
    document.getElementById("planetCarbonDioxideValueBox").innerHTML = "Atmosphere CarbonDioxide <br>" + scienceNumber(data.atmosphereCarbonDioxide) + " kg.";

    drawGraph(document.getElementById("habitabilityGraph"), data.orbitingBody.habitabilityScore);
    drawPointOnGraph(document.getElementById("habitabilityGraph"), data.orbitalRadiusGraphFraction)
    document.getElementById("planetHabitabilityValueBox").innerHTML = "Habitability <br>" + data.habitability + "%.";

    document.getElementById("planetOrbitalRadiusSlider").value = data.orbit.radius;
    document.getElementById("planetOrbitalRadiusValueBox").innerHTML = "Orbital Radius <br>" + scienceNumber(data.orbit.radius) + " meters.";
    document.getElementById("planetOrbitalPeriodValueBox").innerHTML = "Orbital Period <br>" + scienceNumber(data.orbit.period) + " earth years.";

    document.getElementById("planetTemperatureValueBox").innerHTML = "Planet Average Temperature <br>" + data.planetTemperature.toPrecision(3) + " kelvin.";

    //drawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("terrestrialSpectrumGraph"), data.terrestrialSpectrum);

}

export function setupPlanetEditor(fromFreebuild){
    document.getElementById("starMassSlider").addEventListener("input",
        onStarMassInput, true);

    document.getElementById("planetMassSlider").addEventListener("input",
        onPlanetMassInput, true);

    document.getElementById("planetOrbitalRadiusSlider").addEventListener("input",
            onPlanetOrbitalRadiusInput, true);

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

function onPlanetMassInput(event){
    Thrive.editPlanet("planetMass", parseFloat(event.target.value));
}

function onPlanetOrbitalRadiusInput(event){
    Thrive.editPlanet("planetOrbitalRadius", parseFloat(event.target.value));
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
//draw circle in the graph HelpBox
function drawCircle(graph, x, y, r, stroke, strokeWidth, fill)
{
	graph.innerHTML += "<circle cx=\"" + x + "\" cy=\"" + y + "\" r=\"" + r + "\" stroke=\"" + stroke + "\" stroke-width=\"" + strokeWidth + "\" fill=\"" + fill + "\" />";
}
// padding for the x and y axis which is used by drawGraph and drawPoint
var offset = 10;
//draw the graph
function drawGraph(graph, data)
{
    //check if there is good data
    if (data === undefined || data.length == 0) {
        return;
    }
    graph.innerHTML = "";
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

function drawPointOnGraph(graph, point)
{
    //get the size of the container
    var positionInfo = graph.getBoundingClientRect();
    var height = positionInfo.height;
    var width = positionInfo.width;
    var x = offset + (width - 2*offset)*point;
    var y = height - offset;
    var r = 10;
    drawCircle(graph, x, y, r, "green", 3, "green")
}

function scienceNumber(a)
{
    var rounded = parseFloat(a.toPrecision(3));
    return rounded.toExponential();
}
