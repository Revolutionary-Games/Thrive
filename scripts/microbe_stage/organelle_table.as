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

    texture: The name of the texture file to use

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
#include "organelle_components/pilus.as"

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

//! Cache the result if called multiple times in quick succession
Organelle@ getOrganelleDefinition(const string &in name){

    Organelle@ organelle = cast<Organelle@>(_organelleTable[name]);

    if(organelle is null){

        LOG_ERROR("getOrganelleDefinition: no organelle named '" + name + "'");
        PrintCallStack();
    }

    return organelle;
}

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


class PilusOrganelleFactory{

    PilusOrganelleFactory()
    {
    }

    OrganelleComponent@ makePilusOrganelle(){
        return Pilus();
    }
}

OrganelleComponentFactory@ pilusOrganelleFactory()
{
    auto factory = PilusOrganelleFactory();
    return OrganelleComponentFactory(
        OrganelleComponentFactoryFunc(factory.makePilusOrganelle),
        "Pilus");
}

//! This replaced the old tables that specified things for cells.
//! This is clearer as to what are valid properties
class OrganelleParameters{

    //! It might be an AngelScript bug that requires this to be specified.
    //! \todo try removing at some point
    OrganelleParameters(){
        this.name = "invalid";
    }

    OrganelleParameters(const OrganelleType@ type){
        this.name = type.name;
        this.gene = type.gene;
        this.mesh = type.mesh;
        this.texture = type.texture;
        this.mass = type.mass;
        this.chanceToCreate = type.chanceToCreate;
        this.prokaryoteChance = type.prokaryoteChance;

        // Components
        for(uint i = 0; i < type.getComponentKeys().length(); i++){
            string key = type.getComponentKeys()[i];

            if(key == "nucleus"){
                this.components.insertLast(nucleusComponentFactory);
            } else if(key == "storage"){
                this.components.insertLast(storageOrganelleFactory(type.getComponentParameterAsDouble(key, "capacity")));
            } else if(key == "processor"){
                this.components.insertLast(processorOrganelleFactory(type.getComponentParameterAsDouble(key, "colourChangeFactor")));
            } else if(key == "agentVacuole"){
                this.components.insertLast(agentVacuoleFactory(type.getComponentParameterAsString(key, "compound"), type.getComponentParameterAsString(key, "process")));
            } else if(key == "movement"){
                this.components.insertLast(movementOrganelleFactory(type.getComponentParameterAsDouble(key, "momentum"), type.getComponentParameterAsDouble(key, "torque")));
            } else if(key == "pilus"){
                this.components.insertLast(pilusOrganelleFactory());
            }
        }

        // Processes
        for(uint i = 0; i < type.getProcessKeys().length(); i++){
            string key = type.getProcessKeys()[i];
            this.processes.insertLast(TweakedProcess(key, type.getProcessTweakRate(key)));
        }

        this.hexes = type.getHexes();

        // Initial composition
        for(uint i = 0; i < type.getInitialCompositionKeys().length(); i++){
            string key = type.getInitialCompositionKeys()[i];
            this.initialComposition[key] = type.getInitialComposition(key);
        }

        this.mpCost = type.mpCost;
    }

    string name;
    string gene = "INVALID";
    string mesh;
    string texture = "UNSET TEXTURE";
    float mass = 0;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float chanceToCreate = 0.0;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float prokaryoteChance = 0.0;

    //! The factories for the components that define what this organelle does
    array<OrganelleComponentFactory@> components = {};

    array<TweakedProcess@> processes = {};

    array<Int2> hexes = {};

    //! The initial amount of compounds this organelle consists of
    dictionary initialComposition = {};

    //! Cost in mutation points
    int mpCost = 0;
}

dictionary _organelleTable;

//! Sets up Organelles for use
void setupOrganelles(){

    uint64 count = SimulationParameters::organelleRegistry().getSize();
    for(uint64 organelleId = 0; organelleId < count; ++organelleId){

        const auto name = SimulationParameters::organelleRegistry().
            getInternalName(organelleId);

        OrganelleParameters@ organelleParameters = OrganelleParameters(SimulationParameters::organelleRegistry().
            getTypeData(organelleId));

        // The handle needs to be used here to properly give the dictionary the handle value
        @_organelleTable[name] = Organelle(organelleParameters);

        // This is just a sanity check
        // But keep this code for reference
        const Organelle@ organelle;
        if(!_organelleTable.get(name, @organelle) || organelle is null){
            assert(false, "Logic for building _organelleTable broke");
        }
    }

    // Uncomment to print organelle table for debugging
    printOrganelleTable();
}

void printOrganelleTable(){
    LOG_INFO("Registered organelles:");
    auto keys = _organelleTable.getKeys();
    for(uint i = 0; i < keys.length(); ++i){

        LOG_WRITE(keys[i]);
    }
    LOG_WRITE("");
}


