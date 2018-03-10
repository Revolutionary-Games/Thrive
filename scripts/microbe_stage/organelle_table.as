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
#include "nucleus_organelle.as"
#include "storage_organelle.as"
#include "processor_organelle.as"
#include "agent_vacuole.as"
#include "movement_organelle.as"

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

    OrganelleParameters(const string &in name){

        this.name = name;
    }
    float mass = 0;
    string name;
    string gene = "INVALID";
    string mesh;
    
    //! Chance of randomly generating this (used by procedural_microbes.as)
    float chanceToCreate = 0.0;

    //! The factories for the components that define what this organelle does
    array<OrganelleComponentFactory@> components;

    array<TweakedProcess@> processes;

    array<Int2> hexes;

    //! The initial amount of compounds this organelle consists of
    dictionary initialComposition;

    //! Cost in mutation points
    int mpCost = 0;
}

//! Cache the result if called multiple times for the same world
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
    nucleusParameters.mpCost = 0; //it's not supossed to be purchased.
    nucleusParameters.initialComposition = {
        {"aminoacids", 4}
    };
    nucleusParameters.components = {
        nucleusComponentFactory
    };
    nucleusParameters.processes = {
        TweakedProcess("fattyAcidSynthesis", 0.2),
        TweakedProcess("aminoAcidSynthesis", 0.2)
    };
    nucleusParameters.hexes = {
        Int2(0, 0),
        Int2(1, 0),
        Int2(0, 1),
        Int2(0, -1),
        Int2(1, -1),
        Int2(-1, 1),
        Int2(-1, 0),
        Int2(-1, -1),
        Int2(0, -2),
        Int2(1, -2)
    };

    _addOrganelleToTable(Organelle(nucleusParameters));


    // ------------------------------------ //
    // Cytoplasm
    auto cytoplasmParameters = OrganelleParameters("cytoplasm");
    
    cytoplasmParameters.mass = 0.1;
    cytoplasmParameters.gene = "Y";
    cytoplasmParameters.mesh = ""; //it's an empty hex
    cytoplasmParameters.chanceToCreate = 1; 
    cytoplasmParameters.mpCost = 5;
    cytoplasmParameters.initialComposition = {
        {"aminoacids", 3},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    cytoplasmParameters.components = {
        storageOrganelleFactory(10.0f)
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
    chloroplastParameters.chanceToCreate = 2;
    chloroplastParameters.mpCost = 20;
    chloroplastParameters.initialComposition = {
        {"aminoacids", 4},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    chloroplastParameters.components = {
        processorOrganelleFactory(1.0)
    };
    chloroplastParameters.processes = {
        TweakedProcess("photosynthesis", 0.2)
    };
    chloroplastParameters.hexes = {
        Int2(0, 0),
        Int2(1, 0),
        Int2(0, 1)
    };

    _addOrganelleToTable(Organelle(chloroplastParameters));

    // ------------------------------------ //
    // Oxytoxy
    auto oxytoxyParameters = OrganelleParameters("oxytoxy");
    
    oxytoxyParameters.mass = 0.3;
    oxytoxyParameters.gene = "T";
    oxytoxyParameters.mesh = "oxytoxy.mesh";
    oxytoxyParameters.chanceToCreate = 1;
    oxytoxyParameters.mpCost = 40;
    oxytoxyParameters.initialComposition = {
        {"aminoacids", 4},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    oxytoxyParameters.components = {
        agentVacuoleFactory("oxytoxy", "oxytoxySynthesis")
    };
    oxytoxyParameters.processes = {
        TweakedProcess("oxytoxySynthesis", 0.05)
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
    mitochondrionParameters.mpCost = 20;
    mitochondrionParameters.initialComposition = {
        {"aminoacids", 4},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    mitochondrionParameters.components = {
        processorOrganelleFactory(1.0f)
    };
    mitochondrionParameters.processes = {
        TweakedProcess("respiration", 0.07)
    };
    mitochondrionParameters.hexes = {
        Int2(0, 0),
        Int2(0, 1)
    };

    _addOrganelleToTable(Organelle(mitochondrionParameters));
    
    // ------------------------------------ //
    // Vacuole
    auto vacuoleParameters = OrganelleParameters("vacuole");
    
    vacuoleParameters.mass = 0.4;
    vacuoleParameters.gene = "V";
    vacuoleParameters.mesh = "vacuole.mesh";
    vacuoleParameters.chanceToCreate = 3;
    vacuoleParameters.mpCost = 15;
    vacuoleParameters.initialComposition = {
        {"aminoacids", 4},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    vacuoleParameters.components = {
        storageOrganelleFactory(100.0f)
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
    flagellumParameters.chanceToCreate = 3;
    flagellumParameters.mpCost = 25;
    flagellumParameters.initialComposition = {
        {"aminoacids", 4},
        {"glucose", 2}
        // fattyacids = 0 :/
    };
    flagellumParameters.components = {
        movementOrganelleFactory(20, 300)
    };
    flagellumParameters.hexes = {
        Int2(0, 0)
    };

    _addOrganelleToTable(Organelle(flagellumParameters));
    
    // ------------------------------------ //
    // Setup the organelle letters
    setupOrganelleLetters();
}

void _addOrganelleToTable(Organelle@ organelle){

    _mainOrganelleTable[organelle.name] = @organelle;
}

