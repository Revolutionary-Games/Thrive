// TODO: update this
/*
Organelle atributes:
    mass:   How heavy an organelle is. Affects speed, mostly.

    components: A table with the components an organelle has, plus
                the arguments the component needs to initialize.
                Refer to the particular component's lua file for
                more information.

    mpCost: The cost (in mutation points) an organelle costs in the
            microbe editor.

    mesh:   The name of the mesh file of the organelle.
            It has to be in the models folder.

    hexes:  A table of the hexes that the organelle occupies.
            Each hex it's represented by a table that looks like this:
                {["q"]=q,   ["r"]=r}
            where q and r are the hex position in axial coordinates.

    gene:   The letter that will be used by the auto-evo system to
            identify this organelle.

    chanceToCreate: The (relative) chance this organelle will appear in a randomly
                    generated or mutated microbe (to do roulette selection).

    prokaryoteChance: The (relative) chance this organelle will appear in a randomly
                    generated or mutated prokaryotes (to do roulette selection).

    processes:  A table with all the processes this organelle does,
                and the capacity of the process (the amount of
                process that can be made in one second).
                TODO: put it in the procesOrganelle component?

    composition:    A table with the compounds that compost the organelle.
                    They are needed in order to split the organelle, and a
                    percentage of them is released upon death of the microbe.
*/
#include "organelle.as"
#include "organelle_component.as"

#include "process_table.as"

// For AxialCoordinates
#include "configs.as"

// Need to include all the used organelle types for the factory functions
#include "organelle_components/nucleus_organelle.as"
#include "organelle_components/storage_organelle.as"
#include "organelle_components/processor_organelle.as"
#include "organelle_components/agent_vacuole.as"
#include "organelle_components/movement_organelle.as"

//! Factory typedef for OrganelleComponent
funcdef OrganelleComponent@ OrganelleComponentFactoryFunc();

class OrganelleComponentFactory{

    OrganelleComponentFactory(OrganelleComponentFactoryFunc@ f, const string &in name){

        @factory = f;
        this.name = name;
    }

    OrganelleComponentFactoryFunc@ factory;
    string name;
}

//! This replaced the old tables that specified things for cells.
//! This is clearer as to what are valid properties
class OrganelleParameters{

    //! It might be an AngelScript bug that requires this to be specified.
    //! \todo try removing at some point
    OrganelleParameters(){
        this.name = "invalid";
    }

    OrganelleParameters(const string &in name){

        this.name = name;
    }
    float mass = 0;
    string name;
    string gene = "INVALID";
    string mesh;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float chanceToCreate = 0.0;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float prokaryoteChance = 0.0;

    //! The factories for the components that define what this organelle does
    array<OrganelleComponentFactory@> components;

    array<TweakedProcess@> processes;

    array<Int2> hexes;

    //! The initial amount of compounds this organelle consists of
    dictionary initialComposition;

    //! Cost in mutation points
    int mpCost = 0;
}

//! Cache the result if called multiple times in quick succession
Organelle@ getOrganelleDefinition(const string &in name){

    Organelle@ organelle = cast<Organelle@>(_mainOrganelleTable[name]);

    if(organelle is null){

        LOG_ERROR("getOrganelleDefinition: no organelle named '" + name + "'");
        PrintCallStack();
    }

    return organelle;
}

// ------------------------------------ //
// Private part starts here don't directly call or read these things from any other file
// Only thing you'll need to modify is the "Main organelle table" below

// Don't touch this from anywhere except setupOrganelles
// use getOrganelleDefinition for accessing
dictionary _mainOrganelleTable;

// Factory functions for all the organelle components

OrganelleComponent@ makeNucleusOrganelle(){
    return NucleusOrganelle();
}

OrganelleComponentFactory@ nucleusComponentFactory = OrganelleComponentFactory(
    @makeNucleusOrganelle, "NucleusOrganelle"
);

class StorageOrganelleFactory{

    StorageOrganelleFactory(float capacity){

        this.capacity = capacity;
    }

    OrganelleComponent@ makeStorageOrganelle(){
        return StorageOrganelle(capacity);
    }

    float capacity;
}

OrganelleComponentFactory@ storageOrganelleFactory(float capacity){

    auto factory = StorageOrganelleFactory(capacity);
    return OrganelleComponentFactory(
        OrganelleComponentFactoryFunc(factory.makeStorageOrganelle), "StorageOrganelle");
}

class ProcessorOrganelleFactory{

    ProcessorOrganelleFactory(float colourChangeFactor){

        this.colourChangeFactor = colourChangeFactor;
    }

    OrganelleComponent@ makeProcessorOrganelle(){
        return ProcessorOrganelle(colourChangeFactor);
    }

    float colourChangeFactor;
}

OrganelleComponentFactory@ processorOrganelleFactory(float colourChangeFactor){

    auto factory = ProcessorOrganelleFactory(colourChangeFactor);
    return OrganelleComponentFactory(
        OrganelleComponentFactoryFunc(factory.makeProcessorOrganelle), "ProcessorOrganelle");
}


class AgentVacuoleFactory{

    AgentVacuoleFactory(const string &in compound, const string &in process){

        this.compound = compound;
        this.process = process;
    }

    OrganelleComponent@ makeAgentVacuole(){
        return AgentVacuole(compound, process);
    }

    string compound;
    string process;
}

OrganelleComponentFactory@ agentVacuoleFactory(const string &in compound,
    const string &in process
) {
    auto factory = AgentVacuoleFactory(compound, process);
    return OrganelleComponentFactory(
        OrganelleComponentFactoryFunc(factory.makeAgentVacuole),
        "AgentVacuole");
}


class MovementOrganelleFactory{

    MovementOrganelleFactory(float momentum, float torque){

        this.momentum = momentum;
        this.torque = torque;
    }

    OrganelleComponent@ makeMovementOrganelle(){
        return MovementOrganelle(momentum, torque);
    }

    float momentum;
    float torque;
}

OrganelleComponentFactory@ movementOrganelleFactory(float momentum, float torque){

    auto factory = MovementOrganelleFactory(momentum, torque);
    return OrganelleComponentFactory(
        OrganelleComponentFactoryFunc(factory.makeMovementOrganelle),
        "MovementOrganelle");
}


// ------------------------------------ //
// Sets up the organelle table
void setupOrganelles(){

    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    _mainOrganelleTable = dictionary();

    //
    //
    // ------------------------------------ //
    // Main organelle table is now here
    //

    // ------------------------------------ //
    // Nucleus
    auto nucleusParameters = OrganelleParameters("nucleus");

    nucleusParameters.mass = 0.7;
    nucleusParameters.gene = "N";
    nucleusParameters.mesh = "nucleus.mesh";
    nucleusParameters.chanceToCreate = 0; // Not randomly generated.
    nucleusParameters.prokaryoteChance = 0; // Not randomly generated.
    nucleusParameters.mpCost = 100; //big evolution moment.
    nucleusParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    nucleusParameters.components = {
        nucleusComponentFactory,
    // Same storage as vacuole
    storageOrganelleFactory(15.0f)
    };
    nucleusParameters.processes = {
    };
    nucleusParameters.hexes = {
        Int2(0, 0),
        Int2(1, 0),
        Int2(0, 1),
        Int2(0, -1),
        Int2(1, -1),
        Int2(-1, 1),
        Int2(-1, 0),
        Int2(1, 1),
        Int2(0, 2),
        Int2(-1, 2)
    };

    _addOrganelleToTable(Organelle(nucleusParameters));


    // ------------------------------------ //
    // Cytoplasm
    auto cytoplasmParameters = OrganelleParameters("cytoplasm");

    cytoplasmParameters.mass = 0.1;
    cytoplasmParameters.gene = "Y";
    cytoplasmParameters.mesh = ""; //it's an empty hex
    cytoplasmParameters.chanceToCreate = 1;
    cytoplasmParameters.prokaryoteChance = 1;
    cytoplasmParameters.mpCost = 30;
    cytoplasmParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    cytoplasmParameters.components = {
        processorOrganelleFactory(1.0),
        storageOrganelleFactory(4.0f)
    };
    cytoplasmParameters.processes = {
        TweakedProcess("glycolosis", 1)
    };
    cytoplasmParameters.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(cytoplasmParameters));

    // ------------------------------------ //
    // Chloroplast
    auto chloroplastParameters = OrganelleParameters("chloroplast");

    chloroplastParameters.mass = 0.4;
    chloroplastParameters.gene = "H";
    chloroplastParameters.mesh = "chloroplast.mesh";
    chloroplastParameters.chanceToCreate = 1;
    chloroplastParameters.prokaryoteChance = 0;
    chloroplastParameters.mpCost = 55;
    chloroplastParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    chloroplastParameters.components = {
        processorOrganelleFactory(1.0),
    //chloroplast takes 3 hexes, so allowed storage of 1 cytoplasm
    storageOrganelleFactory(2.0f)
    };
    chloroplastParameters.processes = {
        TweakedProcess("photosynthesis", 1)
    };
    chloroplastParameters.hexes = {
        Int2(0, 0),
        Int2(0, -1),
        Int2(1, -1)
    };

    _addOrganelleToTable(Organelle(chloroplastParameters));

    // ------------------------------------ //
    // Oxytoxy
    auto oxytoxyParameters = OrganelleParameters("oxytoxy");

    oxytoxyParameters.mass = 0.3;
    oxytoxyParameters.gene = "T";
    oxytoxyParameters.mesh = "oxytoxy.mesh";
    oxytoxyParameters.chanceToCreate = 1;
    oxytoxyParameters.prokaryoteChance = 0;
    oxytoxyParameters.mpCost = 70;
    oxytoxyParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    oxytoxyParameters.components = {
    //this can't hold since it is a vacuole
        agentVacuoleFactory("oxytoxy", "oxytoxySynthesis"),
        storageOrganelleFactory(5.0f)
    };
    oxytoxyParameters.processes = {
        TweakedProcess("oxytoxySynthesis", 1)
    };
    oxytoxyParameters.hexes = {
        Int2(0, 0)
    };

    _addOrganelleToTable(Organelle(oxytoxyParameters));


    // ------------------------------------ //
    // Mitochondrion
    auto mitochondrionParameters = OrganelleParameters("mitochondrion");

    mitochondrionParameters.mass = 0.3;
    mitochondrionParameters.gene = "M";
    mitochondrionParameters.mesh = "mitochondrion.mesh";
    mitochondrionParameters.chanceToCreate = 3;
    mitochondrionParameters.prokaryoteChance = 0;
    mitochondrionParameters.mpCost = 45;
    mitochondrionParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    mitochondrionParameters.components = {
        processorOrganelleFactory(1.0f),
        // Mitochondria takes 2 hexes, so allowed storage of 1 cytooplasm
        storageOrganelleFactory(2.0f)
    };
    mitochondrionParameters.processes = {
        TweakedProcess("respiration", 1)
    };
    mitochondrionParameters.hexes = {
        Int2(0, 0),
        Int2(0, -1)
    };

    _addOrganelleToTable(Organelle(mitochondrionParameters));

    // ------------------------------------ //
    // Vacuole
    auto vacuoleParameters = OrganelleParameters("vacuole");

    vacuoleParameters.mass = 0.4;
    vacuoleParameters.gene = "V";
    vacuoleParameters.mesh = "vacuole.mesh";
    vacuoleParameters.chanceToCreate = 3;
    vacuoleParameters.prokaryoteChance = 0;
    vacuoleParameters.mpCost = 50;
    vacuoleParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    vacuoleParameters.components = {
        storageOrganelleFactory(15.0f)
    };

    vacuoleParameters.processes = {
    };

    vacuoleParameters.hexes = {
        Int2(0, 0)
    };

    _addOrganelleToTable(Organelle(vacuoleParameters));

    // ------------------------------------ //
    // Flagellum
    auto flagellumParameters = OrganelleParameters("flagellum");

    flagellumParameters.mass = 0.3;
    flagellumParameters.gene = "F";
    flagellumParameters.mesh = "flagellum.mesh";
    flagellumParameters.chanceToCreate = 6;
    flagellumParameters.prokaryoteChance = 2;
    flagellumParameters.mpCost = 55;
    flagellumParameters.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    flagellumParameters.components = {
        movementOrganelleFactory(20, 300),
    // Flagella takes 1 hex, so allowed storage of 1 cytooplasm
    storageOrganelleFactory(1.0f)
    };

    flagellumParameters.processes = {
    };

    flagellumParameters.hexes = {
        Int2(0, 0)
    };

    _addOrganelleToTable(Organelle(flagellumParameters));


    // Chemoplast
    auto chemoplast = OrganelleParameters("chemoplast");

    chemoplast.mass = 0.1;
    chemoplast.gene = "C";
    //TODO: They need their model
    chemoplast.mesh = "chemoplast.mesh";
    chemoplast.chanceToCreate = 1;
    chemoplast.prokaryoteChance = 0;
    chemoplast.mpCost = 45;
    chemoplast.initialComposition = {
        {"phosphates", 5},
        {"ammonia", 5}
    };
    chemoplast.components = {
        processorOrganelleFactory(1.0f),
    // Chemoplast takes 2 hexes, so allowed storage of 1 cytooplasm
    storageOrganelleFactory(2.0f)
    };
    chemoplast.processes = {
          TweakedProcess("chemoSynthesis", 1)
    };
    chemoplast.hexes = {
        Int2(0, 0),
    Int2(0, -1)
    };

    _addOrganelleToTable(Organelle(chemoplast));

    // Nitrogen Fixing Plastid
    auto nitrogenPlastid = OrganelleParameters("nitrogenfixingplastid");

    nitrogenPlastid.mass = 0.1;
    nitrogenPlastid.gene = "I";
    //TODO: They need their model
    nitrogenPlastid.mesh = "nitrogenplastid.mesh";
    nitrogenPlastid.chanceToCreate = 1;
    nitrogenPlastid.prokaryoteChance = 0;
    nitrogenPlastid.mpCost = 50;
    nitrogenPlastid.initialComposition = {
        {"phosphates", 2},
        {"ammonia", 2}
    };
    nitrogenPlastid.components = {
        processorOrganelleFactory(1.0f),
        // nitrogenPlastid takes 2 hexes, so allowed storage of 1 cytooplasm
        storageOrganelleFactory(2.0f)
    };
    nitrogenPlastid.processes = {
          TweakedProcess("nitrogenFixing", 1)
    };
    nitrogenPlastid.hexes = {
        Int2(0, 0),
        Int2(0, -1)
    };

    _addOrganelleToTable(Organelle(nitrogenPlastid));
    // Prokaryotic Organelles (all meshes are placeholders)//

    // ------------------------------------ //
    // Respiratory Protein
    auto respiratoryProtein = OrganelleParameters("metabolosome");

    respiratoryProtein.mass = 0.1;
    respiratoryProtein.gene = "m";
    respiratoryProtein.mesh = "metabolosome.mesh";
    respiratoryProtein.chanceToCreate = 0.5f;
    respiratoryProtein.prokaryoteChance = 1;
    respiratoryProtein.mpCost = 45;
    respiratoryProtein.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    respiratoryProtein.components = {
        processorOrganelleFactory(1.0f),
        storageOrganelleFactory(1.0f)
    };
    respiratoryProtein.processes = {
        TweakedProcess("protein_respiration", 1)
    };
    respiratoryProtein.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(respiratoryProtein));

    // chromatophors
    auto photosyntheticProtein = OrganelleParameters("chromatophors");

    photosyntheticProtein.mass = 0.1;
    photosyntheticProtein.gene = "h";
    photosyntheticProtein.mesh = "chromatophores.mesh";
    photosyntheticProtein.chanceToCreate = 0.5f;
    photosyntheticProtein.prokaryoteChance = 1;
    photosyntheticProtein.mpCost = 55;
    photosyntheticProtein.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    photosyntheticProtein.components = {
        processorOrganelleFactory(1.0f),
        storageOrganelleFactory(1.0f)
    };
    photosyntheticProtein.processes = {
      TweakedProcess("chromatophore_photosynthesis", 1),
      TweakedProcess("glycolosis", 1)
    };
    photosyntheticProtein.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(photosyntheticProtein));

    // Oxytoxy Protein
    auto oxytoxyProtein = OrganelleParameters("oxytoxyProteins");

    oxytoxyProtein.mass = 0.1;
    oxytoxyProtein.gene = "t";
    oxytoxyProtein.mesh = "oxytoxy.mesh";
    oxytoxyProtein.chanceToCreate = 0;
    oxytoxyProtein.prokaryoteChance = 1;
    oxytoxyProtein.mpCost = 15;
    oxytoxyProtein.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    oxytoxyProtein.components = {
        agentVacuoleFactory("oxytoxy", "oxytoxySynthesis"),
        storageOrganelleFactory(2.0f),
        processorOrganelleFactory(1.0f)
    };
    oxytoxyProtein.processes = {
     TweakedProcess("oxytoxySynthesis", 1),
     TweakedProcess("glycolosis", 1)
    };
    oxytoxyProtein.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(oxytoxyProtein));


    // chemoSynthisizingProtein
    auto chemoSynthisizingProtein = OrganelleParameters("chemoSynthisizingProteins");

    chemoSynthisizingProtein.mass = 0.1;
    chemoSynthisizingProtein.gene = "c";
    chemoSynthisizingProtein.mesh = "chemoproteins.mesh";
    chemoSynthisizingProtein.chanceToCreate = 0.5f;
    chemoSynthisizingProtein.prokaryoteChance = 1;
    chemoSynthisizingProtein.mpCost = 45;
    chemoSynthisizingProtein.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    chemoSynthisizingProtein.components = {
        processorOrganelleFactory(1.0f),
        storageOrganelleFactory(1.0f)
    };
    chemoSynthisizingProtein.processes = {
      TweakedProcess("bacterial_ChemoSynthesis", 1),
      TweakedProcess("glycolosis", 1)
    };
    chemoSynthisizingProtein.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(chemoSynthisizingProtein));

    // Bacterial cytoplasm equivilent (so they dont die immediately) (just a stopgap measure for now, though it is real)
    auto protoplasmParameters = OrganelleParameters("protoplasm");

    protoplasmParameters.mass = 0.1;
    protoplasmParameters.gene = "y";
    protoplasmParameters.mesh = ""; //it's an empty hex
    protoplasmParameters.chanceToCreate = 0;
    protoplasmParameters.prokaryoteChance = 1;
    protoplasmParameters.mpCost = 55;
    protoplasmParameters.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    protoplasmParameters.components = {
        processorOrganelleFactory(1.0),
        storageOrganelleFactory(15.0f)
    };
    protoplasmParameters.processes = {
        TweakedProcess("glycolosis", 1)
    };
    protoplasmParameters.hexes = {
        Int2(0, 0),
    };
    _addOrganelleToTable(Organelle(protoplasmParameters));

    // Nitrogenase
    auto nitrogenFixationProtein = OrganelleParameters("nitrogenase");

    nitrogenFixationProtein.mass = 0.1;
    nitrogenFixationProtein.gene = "i";
    nitrogenFixationProtein.mesh = "nitrogenase.mesh";
    nitrogenFixationProtein.chanceToCreate = 1;
    nitrogenFixationProtein.prokaryoteChance = 1;
    nitrogenFixationProtein.mpCost = 55;
    nitrogenFixationProtein.initialComposition = {
        {"phosphates", 1},
        {"ammonia",1}
    };
    nitrogenFixationProtein.components = {
        processorOrganelleFactory(1.0f),
        storageOrganelleFactory(2.0f)
    };
    nitrogenFixationProtein.processes = {
      TweakedProcess("nitrogenaseReaction", 1),
      TweakedProcess("glycolosis", 1)
    };
    nitrogenFixationProtein.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(nitrogenFixationProtein));

    // Rusticyanin
    auto rusticyanin = OrganelleParameters("rusticyanin");

    rusticyanin.mass = 0.1;
    rusticyanin.gene = "f";
    rusticyanin.mesh = "chemoproteins.mesh";
    rusticyanin.chanceToCreate = 0.5f;
    rusticyanin.prokaryoteChance = 1;
    rusticyanin.mpCost = 45;
    rusticyanin.initialComposition = {
        {"phosphates", 1},
        {"ammonia", 1}
    };
    rusticyanin.components = {
        processorOrganelleFactory(1.0f),
        storageOrganelleFactory(1.0f)
    };
    rusticyanin.processes = {
      TweakedProcess("iron_chemolithoautotrophy", 2)
    };
    rusticyanin.hexes = {
        Int2(0, 0),
    };

    _addOrganelleToTable(Organelle(rusticyanin));
    // ------------------------------------ //
    // Setup the organelle letters
    setupOrganelleLetters();
}

void _addOrganelleToTable(Organelle@ organelle){

    _mainOrganelleTable[organelle.name] = @organelle;
}

