// Planet editor GUI scripts
import * as common from "./gui_common.mjs";

// Import * as main_menu from "./main_menu.mjs";

let freebuild = false;

function updatePlanetValues(data){
    // Add the star GetVariables
    document.getElementById("starMassSlider").value = data.orbitingBody.mass;
    document.getElementById("starMassValueBox").innerHTML =
        "Star Mass <br>" + scienceNumber(data.orbitingBody.mass) + " kg.";
    document.getElementById("starRadiusValueBox").innerHTML =
        "Star Radius <br>" + scienceNumber(data.orbitingBody.radius) + " meters.";
    document.getElementById("starGravitationalParameterValueBox").innerHTML =
        "Star Gravitational Parameter <br>" +
        scienceNumber(data.orbitingBody.gravitationalParameter);
    document.getElementById("starLifespanValueBox").innerHTML =
        "Star Lifespan <br>" + scienceNumber(data.orbitingBody.lifeSpan) + " earth years.";
    document.getElementById("starTemperatureValueBox").innerHTML =
        "Star Temperature <br>" + scienceNumber(data.orbitingBody.temperature) + " kelvin.";

    drawGraph(document.getElementById("stellarSpectrumGraph"),
        data.orbitingBody.stellarSpectrum);

    // Add the planet variables
    document.getElementById("planetMassSlider").value = data.mass;
    document.getElementById("planetMassValueBox").innerHTML =
        "Planet Mass <br>" + scienceNumber(data.mass) + " kg.";
    document.getElementById("planetRadiusValueBox").innerHTML =
        "Planet Radius <br>" + scienceNumber(data.radius) + " meters";
    document.getElementById("planetOceanMassValueBox").innerHTML =
        "Ocean Mass <br>" + scienceNumber(data.oceanMass) + " kg.";
    document.getElementById("planetLithosphereMassValueBox").innerHTML =
        "Lithosphere Mass <br>" + scienceNumber(data.lithosphereMass) + " kg.";
    document.getElementById("planetAtmosphereMassValueBox").innerHTML =
        "Atmosphere Mass <br>" + scienceNumber(data.atmosphereMass) + " kg.";

    const oxygenPercentage = parseInt(100 * data.atmosphereOxygen / data.atmosphereMass);
    const carbonDioxidePercentage =
        parseInt(100 * data.atmosphereCarbonDioxide / data.atmosphereMass);

    // UNUSED: const waterPercentage =
    // parseInt(100 * data.atmosphereWater / data.atmosphereMass);
    const nitrogenPercentage = parseInt(100 * data.atmosphereNitrogen / data.atmosphereMass);
    document.getElementById("planetOxygenPercentageValueBox").innerHTML =
        "Percentage of Oxygen in Atmosphere <br>" + oxygenPercentage + "%.";
    document.getElementById("planetCarbonDioxidePercentageValueBox").innerHTML =
        "Percentage of Carbon Dioxide in Atmosphere <br>" + carbonDioxidePercentage + " %.";
    document.getElementById("planetNitrogenPercentageValueBox").innerHTML =
        "Percentage of Nitrogen in Atmosphere <br>" + nitrogenPercentage + " %.";
    document.getElementById("planetAtmosphereOxygenSlider").value = oxygenPercentage;
    document.getElementById("planetAtmosphereCarbonDioxideSlider").value =
        carbonDioxidePercentage;

    document.getElementById("planetAtmosphereWaterValueBox").innerHTML =
        "Mass of Water in Atmosphere <br>" + scienceNumber(data.atmosphereWater) + " kg.";
    document.getElementById("planetAtmosphereOxygenValueBox").innerHTML =
        "Mass of Oxygen in Atmosphere <br>" + scienceNumber(data.atmosphereOxygen) + " kg.";
    document.getElementById("planetAtmosphereNitrogenValueBox").innerHTML =
        "Mass of Nitrogen in Atmosphere <br>" + scienceNumber(data.atmosphereNitrogen) +
        " kg.";
    document.getElementById("planetCarbonDioxideValueBox").innerHTML =
        "Mass of Carbon Dioxide in Atmosphere <br>" +
        scienceNumber(data.atmosphereCarbonDioxide) + " kg.";

    drawGraph(document.getElementById("habitabilityGraph"),
        data.orbitingBody.habitabilityScore);
    drawPointOnGraph(document.getElementById("habitabilityGraph"),
        data.orbitalRadiusGraphFraction);
    document.getElementById("planetHabitabilityValueBox").innerHTML =
        "Habitability Score <br>" + data.habitability + "%.";

    document.getElementById("planetOrbitalRadiusSlider").value = data.orbit.radius;
    document.getElementById("planetOrbitalRadiusValueBox").innerHTML =
        "Orbital Radius <br>" + scienceNumber(data.orbit.radius) + " meters.";
    document.getElementById("planetOrbitalPeriodValueBox").innerHTML =
        "Orbital Period <br>" + scienceNumber(data.orbit.period) + " earth years.";

    document.getElementById("planetTemperatureValueBox").innerHTML =
        "Planet Average Temperature <br>" + data.planetTemperature.toPrecision(3) + " kelvin.";

    // DrawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("atmosphericFilterGraph"), data.atmosphericFilter);
    drawGraph(document.getElementById("terrestrialSpectrumGraph"), data.terrestrialSpectrum);

}

export function setupPlanetEditor(fromFreebuild){
    document.getElementById("starMassSlider").addEventListener("input",
        onStarMassInput, true);

    document.getElementById("starMassSetSolButton").addEventListener("click",
        onStarSetSolInput, true);

    document.getElementById("planetMassSlider").addEventListener("input",
        onPlanetMassInput, true);

    document.getElementById("planetMassSetEarthButton").addEventListener("click",
        onPlanetSetEarthInput, true);

    document.getElementById("planetAtmosphereOxygenSlider").addEventListener("input",
        onPlanetSetOxygenInput, true);

    document.getElementById("planetAtmosphereCarbonDioxideSlider").addEventListener("input",
        onPlanetSetCarbonDioxideInput, true);

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
    Thrive.editPlanet("onStarMassInput", parseFloat(event.target.value));
}

function onStarSetSolInput(event){
    Thrive.editPlanet("onStarSetSolInput", parseFloat(event.target.value));
}

function onPlanetMassInput(event){
    Thrive.editPlanet("onPlanetMassInput", parseFloat(event.target.value));
}

function onPlanetSetOxygenInput(event){
    Thrive.editPlanet("onPlanetSetOxygenInput", parseFloat(0.01 * event.target.value));
}

function onPlanetSetCarbonDioxideInput(event){
    Thrive.editPlanet("onPlanetSetCarbonDioxideInput", parseFloat(0.01 * event.target.value));
}

function onPlanetSetEarthInput(event){
    Thrive.editPlanet("onPlanetSetEarthInput", parseFloat(event.target.value));
}

function onPlanetOrbitalRadiusInput(event){
    Thrive.editPlanet("onPlanetOrbitalRadiusInput", parseFloat(event.target.value));
}

function startGame(){
    if(common.isInEngine()){
        if(freebuild){
            onMicrobeIntroEnded();
        } else {
            Leviathan.PlayCutscene("Data/Videos/MicrobeIntro.mkv", onMicrobeIntroEnded,
                onMicrobeIntroEnded);
        }
        Leviathan.CallGenericEvent("UpdateLoadingScreen",
            {show: true, status: "Loading Microbe Stage", message: ""});
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

// Functions for graph drawing
// draw a line in the graph box
function drawLine(graph, x1, y1, x2, y2, stroke, strokeWidth) {
    graph.innerHTML += "<line x1=\"" + x1 + "\" y1=\"" + y1 + "\" x2=\"" + x2 + "\" y2=\"" +
        y2 + "\" style=\"stroke:" + stroke + ";stroke-width:" + strokeWidth + "\" />";
}

// Draw circle in the graph HelpBox
function drawCircle(graph, x, y, r, stroke, strokeWidth, fill) {
    graph.innerHTML += "<circle cx=\"" + x + "\" cy=\"" + y + "\" r=\"" + r + "\" stroke=\"" +
        stroke + "\" stroke-width=\"" + strokeWidth + "\" fill=\"" + fill + "\" />";
}

// Padding for the x and y axis which is used by drawGraph and drawPoint
const offset = 10;


// Draw the graph
function drawGraph(graph, data) {
    // Check if there is good data
    if (data === undefined || data.length == 0) {
        return;
    }
    graph.innerHTML = "";

    // Get the size of the container
    const positionInfo = graph.getBoundingClientRect();
    const height = positionInfo.height;
    const width = positionInfo.width;

    // Work out the bounds of the data
    const xRange = data.length;
    const yMax = Math.max(...data);
    const yMin = Math.min(...data);
    const yRange = Math.max(1, yMax - yMin);

    // Draw the axes
    const offset = 10; // Padding for the x and y axis
    drawLine(graph, offset, offset, offset, height - offset, "rgb(0,0,200)", "3");
    drawLine(graph, offset, height - offset, width - offset, height - offset,
        "rgb(0,0,200)", "3");

    // Draw the points of the graph
    let newX = 0;
    let newY = 0;
    let oldX = offset;
    let oldY = height - offset;
    const xStep = (width - 2 * offset) / xRange;
    const yStep = (height - 2 * offset) / yRange;

    for (const i in data) {
        newX = oldX + xStep;
        newY = (height - offset) - yStep * data[i];
        drawLine(graph, oldX, oldY, newX, newY, "rgb(200,10,50)", "3");
        oldX = newX;
        oldY = newY;
    }
}

function drawPointOnGraph(graph, point) {
    // Get the size of the container
    const positionInfo = graph.getBoundingClientRect();
    const height = positionInfo.height;
    const width = positionInfo.width;
    const x = offset + (width - 2 * offset) * point;
    const y = height - offset;
    const r = 10;
    drawCircle(graph, x, y, r, "green", 3, "green");
}

function scienceNumber(a) {
    const rounded = parseFloat(a.toPrecision(3));

    return rounded.toExponential();
}
