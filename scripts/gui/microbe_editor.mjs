
function onRigidityChanged(event){
    if (common.isInEngine()) {
        Leviathan.CallGenericEvent("MicrobeEditorRigidityChanged",
            {rigidity: parseInt(event.target.value) * 2.0 / 10 - 1});
    }
}

function openHelpEditor(){

    common.playButtonPressSound();

    const pause = document.getElementById("pauseMenuEditor");
    pause.style.display = "none";

    const help = document.getElementById("helpTextEditor");
    help.style.display = "block";

    // Easter egg code, shows a small message saying something from the
    // List of messages when you open up the help menu
    const message = [
        "Fun Fact, The Didinium  and Paramecium are a textbook example of a " +
            "predator prey relationship" +
            " that has been studied for decades, now are you the Didinium, or the " +
            "Paramecium? Predator, or Prey?",
        "Heres a tip, toxins can be used to knock other toxins away from you " +
            "if you are quick enough.",
        "Heres a tip, Osmoregulation costs 1 ATP per second per hex your cell has, " +
            " each empty hex of cytoplasm generates 5 ATP per second aswell," +
            "which means if you are losing ATP due to osmoregulation just add a couple" +
            " empty hexes cytoplasm or remove some organelles.",
        "Fun Fact, In real life prokaryotes have something called Biocompartments " +
        "which act like organelles, and are in fact called Polyhedral organelles",
        "Fun Fact, The metabolosome is what is called a Polyhedral organelle",
        "Heres a Tip, Chromatophores generate 1/3rd the glucose of a chloroplast",
        "Heres a Tip, You generate exactly 2 glucose per second per chemoplast," +
            "as long as you have at least 1 hydrogen sulfide to convert.",
        "Thrive is meant as a simulation of an alien planet, therefore it makes " +
            "sense that most creatures you find will be related to one " +
        "or two other species due to evolution happening around you, see if you can " +
        "identify them!",
        "One of the first playable game-play prototypes was made by our awesome programmer," +
        " untrustedlife!",
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
        "Heres a tip, if your cell is 150 hexes, you can engulf the large iron chunks."
    ];


    const tipEasterEggChance = common.randomBetween(0, 5);
    const messageNum = common.randomBetween(0, message.length - 1);

    if (tipEasterEggChance > 1) {
        document.getElementById("tipMsgEditor").style.display = "unset";
        document.getElementById("tipMsgEditor").textContent = message[messageNum];
        setTimeout(hideTipMsg, 10000);
    }

}


function updateSelectedPatchData(patch){
    selectedPatch = patch;

    // Reset species shown
    document.getElementById("speciesInPatch").textContent = "";

    if(!selectedPatch){
        document.getElementById("noPatchSelectedText").style.display = "inline-block";
        document.getElementById("patchInfoBox").style.display = "none";
        document.getElementById("moveToPatchButton").classList.add("Disabled");
        return;
    }

    // Get chunck values of patch
    const chunk = getPatchChunkTotalCompoundAmounts(patch);

    document.getElementById("noPatchSelectedText").style.display = "none";
    document.getElementById("patchInfoBox").style.display = "block";

    document.getElementById("editorSelectedPatchName").textContent = patch.name;

    if(patchMoveAllowed(selectedPatch.id)){
        document.getElementById("moveToPatchButton").classList.remove("Disabled");
    } else {
        document.getElementById("moveToPatchButton").classList.add("Disabled");
    }

    // Reset all arrows and text when we select a patch
    // Is called when we move on another patch or we select one
    if(currentPatchId == selectedPatch.id){
        document.getElementById("editorSelectedPatchName").textContent = patch.name;
        document.getElementById("editorSelectedPatchSituation").textContent =
            "You are currently in this patch";


        // Reset all box that show up or down arrow on selected patch
        document.getElementById("microbeHUDPatchTemperatureSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchPressureSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchLightSituation").style.backgroundImage =
        "none";

        document.getElementById("microbeHUDPatchOxygenSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchNitrogenSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchCO2Situation").style.backgroundImage =
        "none";

        document.getElementById("microbeHUDPatchGlucoseSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchPhosphateSituation").style.backgroundImage =
        "none";
        document.getElementById("microbeHUDPatchHydrogenSulfideSituation").
            style.backgroundImage = "none";
        document.getElementById("microbeHUDPatchAmmoniaSituation").
            style.backgroundImage = "none";
        document.getElementById("microbeHUDPatchIronSituation").style.backgroundImage = "none";

    } else {
        document.getElementById("editorSelectedPatchSituation").textContent = "";
        updateConditionDifferencesBetweenPatches(patch, patchData.patches[currentPatchId]);
    }


    // Biome name
    document.getElementById("patchName").textContent = "Biome: " + patch.biome.name;

    // Set all environment data from objects received
    document.getElementById("microbeHUDPatchTemperature").textContent =
        patch.biome.temperature;

    document.getElementById("microbeHUDPatchLight").textContent =
        (patch.biome.compounds.sunlight.dissolved * 100) + "%";
    document.getElementById("microbeHUDPatchOxygen").textContent =
        (patch.biome.compounds.oxygen.dissolved * 100) + "%";
    document.getElementById("microbeHUDPatchNitrogen").textContent =
        (patch.biome.compounds.nitrogen.dissolved * 100) + "%";
    document.getElementById("microbeHUDPatchCO2").textContent =
        (patch.biome.compounds.carbondioxide.dissolved * 100) + "%";

    document.getElementById("microbeHUDPatchHydrogenSulfide").textContent =
     (patch.biome.compounds.hydrogensulfide.density *
     patch.biome.compounds.hydrogensulfide.amount + chunk.hydrogensulfide).toFixed(3) + "%";
    document.getElementById("microbeHUDPatchAmmonia").textContent =
     (patch.biome.compounds.ammonia.density *
     patch.biome.compounds.ammonia.amount + chunk.ammonia).toFixed(3) + "%";
    document.getElementById("microbeHUDPatchGlucose").textContent =
     (patch.biome.compounds.glucose.density *
     patch.biome.compounds.glucose.amount + chunk.glucose).toFixed(3) + "%";
    document.getElementById("microbeHUDPatchPhosphate").textContent =
     (patch.biome.compounds.phosphates.density *
    patch.biome.compounds.phosphates.amount + chunk.phosphates).toFixed(3) + "%";

    document.getElementById("microbeHUDPatchIron").textContent = chunk.iron.toFixed(3) + "%";

    for(const species of patch.species){
        const name = species.species.genus + " " + species.species.epithet;

        const par = document.createElement("p");
        par.textContent = name + " with population: " + species.population;
        document.getElementById("speciesInPatch").appendChild(par);
    }
}
