#include "organelle_table.as"
#include "microbe_operations.as"
#include "procedural_microbes.as"

float randomColourChannel(){
    return GetEngine().GetRandom().GetNumber(MIN_COLOR, MAX_COLOR);
}

Float4 randomColour(float opaqueness = 1.0f){
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}


const dictionary DEFAULT_INITIAL_COMPOUNDS =
    {
        {"atp", InitialCompound(60, 10)},
        {"glucose", InitialCompound(10)},
        {"reproductase", InitialCompound(0, 8)},
        {"oxygen", InitialCompound(10)},
        {"oxytoxy", InitialCompound(1)}
    };

string randomSpeciesName(){
    return "Species_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
    //gotta use the latin names (But they aren't used?)
}

////////////////////////////////////////////////////////////////////////////////
// Species class
//
// Class for representing an individual species (This is stored in the world's
// SpeciesSystem instance)
////////////////////////////////////////////////////////////////////////////////
//! \todo This should be moved into the SpeciesComponent class to simplify things
class Species{

    //! Constructor for automatically creating a random species
    Species(CellStageWorld@ world){

        name = randomSpeciesName();
        
        auto stringSize = GetEngine().GetRandom().GetNumber(MIN_INITIAL_LENGTH,
            MAX_INITIAL_LENGTH);

        //it should always have a nucleus and a cytoplasm.
        stringCode = getOrganelleDefinition("nucleus").gene +
            getOrganelleDefinition("cytoplasm").gene;
        
        for(int i = 0; i < stringSize; ++i){
            this.stringCode += getRandomLetter();
        }
        
        commonConstructor(world);

        colour = randomColour();

        this.setupSpawn(world);
    }

    ~Species(){

        if(templateEntity != NULL_OBJECT){

            LOG_ERROR("Species object not extinguish()ed before destructor, doing that now");
            extinguish();
        }
    }

    // Creates a mutated version of the species and reduces the species population by half
    Species(Species@ parent, CellStageWorld@ world){
        name = randomSpeciesName();
        this.colour = randomColour();
        this.population = int(floor(parent.population / 2.f));
        parent.population = int(ceil(parent.population / 2.f));
        this.stringCode = Species::mutate(parent.stringCode);

        commonConstructor(world);

        this.setupSpawn(world);
    }

    private void commonConstructor(CellStageWorld@ world){

        @forWorld = world;
        
        auto organelles = positionOrganelles(stringCode);
        
        templateEntity = Species::createSpecies(forWorld, this.name, organelles, this.colour,
            DEFAULT_INITIAL_COMPOUNDS);
    }
    
    //delete a species
    void extinguish(){
        if(forWorld !is null){
            LOG_INFO("Species " + name + " has been extinguished");
            forWorld.GetSpawnSystem().removeSpawnType(this.id);
            //this.template.destroy() //game crashes if i do that.
            // Let's hope this doesn't crash then
            if(templateEntity != NULL_OBJECT){
                forWorld.DestroyEntity(templateEntity);
                templateEntity = NULL_OBJECT;
            }

            @forWorld = null;
        }
    }

    ObjectID factorySpawn(CellStageWorld@ world, Float3 pos){

        LOG_INFO("New member of species spawned: " + this.name);
        return MicrobeOperations::spawnMicrobe(world, pos, this.name,
            // ai controlled
            true,
            // No individual name (could be good for debugging)
            "");
    }
    
    //sets up the spawn of the species
    // This may only be called once. Otherwise old spawn types are left active
    void setupSpawn(CellStageWorld@ world){

        assert(world is forWorld, "Wrong world passed to setupSpawn");
        
        spawningEnabled = true;
        
        SpawnFactoryFunc@ factory = SpawnFactoryFunc(this.factorySpawn);

        // And register new
        LOG_INFO("Registering species to spawn: " + name);
        this.id = forWorld.GetSpawnSystem().addSpawnType(
            factory, DEFAULT_SPAWN_DENSITY, //spawnDensity should depend on population
            MICROBE_SPAWN_RADIUS);
    }


    //updates the population count of the species
    void updatePopulation(){
        //TODO:
        //fill me
        //with code
        this.population += GetEngine().GetRandom().GetNumber(-200, 200);
    }

    string name;
    string stringCode;
    int population = INITIAL_POPULATION;
    Float4 colour = randomColour();

    //! The species entity that has this species' SpeciesComponent
    ObjectID templateEntity = NULL_OBJECT;

    SpawnerTypeId id;
    bool spawningEnabled = false;
    CellStageWorld@ forWorld;
}
////////////////////////////////////////////////////////////////////////////////
// SpeciesSystem
//
// System for estimating and simulating population count for various species
////////////////////////////////////////////////////////////////////////////////

//How big is a newly created species's population.
const auto INITIAL_POPULATION = 2000;

//how much time does it take for the simulation to update.
const auto SPECIES_SIM_INTERVAL = 20000;

//if a specie's population goes below this it goes extinct.
const auto MIN_POP_SIZE = 100;

//if a specie's population goes above this it gets split in half and a
//new mutated species apears.
const auto MAX_POP_SIZE = 5000;

//the amount of species at the start of the microbe stage (not counting Default/Player)
const auto INITIAL_SPECIES = 7;

//if there are more species than this then all species get their population reduced by half
const auto MAX_SPECIES = 15;

//if there are less species than this create new ones.
const auto MIN_SPECIES = 3;

//! Updates the species's population and creates new ones. And keeps track of Species objects
class SpeciesSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "SpeciesSystem expected CellStageWorld");

        // This was commented out in the Lua version... TODO: check that this works
		//can confirtm, it crashes here - Untrustedlife
        // This is needed to actually have AI species in the world
        for(int i = 0; i < INITIAL_SPECIES; ++i){

            createSpecies();
        }
    }

    void Release(){
        // Destroy all species to stop complaints that they aren't extinguished
        resetAutoEvo();
    }
    
    void Run(){
        while(this.timeSinceLastCycle > SPECIES_SIM_INTERVAL){

            this.timeSinceLastCycle -= SPECIES_SIM_INTERVAL;
            
            //update population numbers and split/extinct species as needed
            auto numberOfSpecies = species.length();
            for(uint i = 0; i < numberOfSpecies; ++i){
                //traversing the population backwards to avoid
                //"chopping down the branch i'm sitting in"
                auto index = numberOfSpecies - i;   

                auto currentSpecies = species[index];
                currentSpecies.updatePopulation();
                auto population = currentSpecies.population;

                //reproduction/mutation
                if(population > MAX_POP_SIZE){

                    auto newSpecies = Species(currentSpecies, world);
                    species.insertLast(newSpecies);

                    LOG_INFO("Species " + currentSpecies.name + " split off a child species:" +
                        newSpecies.name);
                }

                //extinction
                if(population < MIN_POP_SIZE){

                    LOG_INFO("Species " + currentSpecies.name + " went extinct");
                    currentSpecies.extinguish();
                    species.removeAt(index);
                }
            }

            //new species
            while(species.length() < MIN_SPECIES){
                LOG_INFO("Creating new species as there's too few");
                createSpecies();
            }

            //mass extinction
            if(species.length() > MAX_SPECIES){

                LOG_INFO("Mass extinction time");
                doMassExtinction();
            }
        }
    }

    void Clear(){}

    void CreateAndDestroyNodes(){}


    void resetAutoEvo(){

        for(uint i = 0; i < species.length(); ++i){

            species[i].extinguish();
        }

        species.resize(0);
    }

    void doMassExtinction(){
        for(uint i = 0; i < species.length(); ++i){

            species[i].population /= 2;
        }
    }

    //! Adds a new AI species
    private void createSpecies(){
        auto newSpecies = Species(world);
        species.insertLast(newSpecies);
    }

    private int timeSinceLastCycle = 0;
    private array<Species@> species;
    private CellStageWorld@ world;
}

//! \param updateSpecies will be modified to match the organelles of the microbe
void updateSpeciesFromMicrobe(CellStageWorld@ world, ObjectID microbeEntity,
    SpeciesComponent@ updateSpecies
) {
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);

    // this.name = microbeComponent.speciesName
    updateSpecies.colour = membraneComponent.getColour();

    updateSpecies.organelles.resize(0);

    // Create species' organelle data
    for(uint i = 0; i < microbeComponent.organelles.length(); ++i){

        updateSpecies.organelles.insertLast(PlacedOrganelle(microbeComponent.organelles[i]));
    }
    
    // This microbes compound amounts will be the new population average.
    updateSpecies.avgCompoundAmounts = {};
    
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){
        
        auto amount = MicrobeOperations::getCompoundAmount(world, microbeEntity, compoundId);
        updateSpecies.avgCompoundAmounts[formatUInt(compoundId)] = InitialCompound(amount);
    }
    // TODO: make this update the ProcessorComponent based on microbe thresholds
}

// TODO: make sure this is called from somewhere
void initProcessorComponent(CellStageWorld@ world, ObjectID entity,
    SpeciesComponent@ speciesComponent
) {
    assert(world.GetComponent_SpeciesComponent(entity) is speciesComponent,
        "Wrong speciesComponent passed to initProcessorComponent");

    assert(speciesComponent.organelles.length() > 0, "initProcessorComponent given a "
        "species that has no organelles");
    
    auto pc = world.GetComponent_ProcessorComponent(entity);

    if(pc is null){

        @pc = world.Create_ProcessorComponent(entity);
    }
    
    dictionary capacities = {};
    for(uint i = 0; i < speciesComponent.organelles.length(); ++i){
        auto@ processes = cast<PlacedOrganelle>(speciesComponent.organelles[i]).
            organelle.processes;
        
        for(uint a = 0; a < processes.length(); ++a){

            auto@ process = processes[a];
            auto name = process.process.internalName;
            
            if(!capacities.exists(name)){
                capacities[name] = 0;
            }

            auto dictValue = capacities[name];
            dictValue = float(dictValue) + process.capacity;
        }
    }
    
    uint64 processCount = SimulationParameters::bioProcessRegistry().getSize();
    for(uint bioProcessID = 0; bioProcessID < processCount; ++bioProcessID){
        auto name = SimulationParameters::bioProcessRegistry().getInternalName(bioProcessID);
        if(capacities.exists(name)){
            pc.setCapacity(bioProcessID, float(capacities[name]));
        }
    }
}


namespace Species{

// Given a newly-created microbe, this sets the organelles and all other
// species-specific microbe data like agent codes, for example.
//! Brief applies template to a microbe entity making sure it has all
//! the correct organelle components
void applyTemplate(CellStageWorld@ world, ObjectID microbe, SpeciesComponent@ species){

    // Fail if the species is not set up
    assert(species.organelles.length() > 0, "Error can't apply uninitialized species " +
        "template: " + species.name);
    
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbe));

    // TODO: Make this also set the microbe's ProcessorComponent
    microbeComponent.speciesName = species.name;
    MicrobeOperations::setMembraneColour(world, microbe, species.colour);

    restoreOrganelleLayout(world, microbe, microbeComponent, species);
    
    // TODO: should the compound amounts be reset before this?
    auto ids = species.avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); ++i){
        CompoundId compoundId = parseUInt(ids[i]);
        InitialCompound amount = InitialCompound(species.avgCompoundAmounts[ids[i]]);
        
        if(amount.amount != 0){
            MicrobeOperations::storeCompound(world, microbe, compoundId, amount.amount, false);
        }
    }
}

void restoreOrganelleLayout(CellStageWorld@ world, ObjectID microbeEntity,
    MicrobeComponent@ microbeComponent, SpeciesComponent@ species
) {
    // delete the the previous organelles.
    while(microbeComponent.organelles.length() > 0){

        auto organelle = microbeComponent.organelles[microbeComponent.organelles.length() - 1];
        auto q = organelle.q;
        auto r = organelle.r;
        // TODO: this could be done more efficiently
        MicrobeOperations::removeOrganelle(world, microbeEntity, {q, r});
    }

    // give it organelles
    for(uint i = 0; i < species.organelles.length(); ++i){
        
        PlacedOrganelle@ organelle = PlacedOrganelle(
            cast<PlacedOrganelle>(species.organelles[i]));
        
        MicrobeOperations::addOrganelle(world, microbeEntity, organelle);
    }
}

//! Creates a species from the initial template. This doesn't register with SpeciesSystem
//! because this is (currently) only used for the player's species which isn't managed by it
ObjectID createSpecies(CellStageWorld@ world, const string &in name,
    MicrobeTemplate@ fromTemplate
) {
    array<PlacedOrganelle@> convertedOrganelles;
    for(uint i = 0; i < fromTemplate.organelles.length(); ++i){

        OrganelleTemplatePlaced@ organelle = fromTemplate.organelles[i];

        convertedOrganelles.insertLast(PlacedOrganelle(
                getOrganelleDefinition(organelle.type), organelle.q, organelle.r,
                organelle.rotation));
    }
    
    return createSpecies(world, name, convertedOrganelles, fromTemplate.colour,
        fromTemplate.compounds);
}

//! Creates an entity that has all the species stuff on it
//! AI controlled ones need to be in addition in SpeciesSystem
ObjectID createSpecies(CellStageWorld@ world, const string &in name,
    array<PlacedOrganelle@> organelles, Float4 colour, const dictionary &in compounds
) {
    ObjectID speciesEntity = world.CreateEntity();
        
    SpeciesComponent@ speciesComponent = world.Create_SpeciesComponent(speciesEntity,
        name);
        
    @speciesComponent.avgCompoundAmounts = dictionary();

    @speciesComponent.organelles = array<SpeciesStoredOrganelleType@>();
    for(uint i = 0; i < organelles.length(); ++i){

        // This conversion does a little bit of extra calculations (that are in the
        // end not used)
        speciesComponent.organelles.insertLast(PlacedOrganelle(organelles[i]));
    }

    // Verify it //
    for(uint i = 0; i < speciesComponent.organelles.length(); ++i){

        PlacedOrganelle@ organelle = cast<PlacedOrganelle>(speciesComponent.organelles[i]);
        
        if(organelle is null){

            assert(false, "createSpecies: species.organelles has invalid object at index: " +
                i);
        }
    }

    ProcessorComponent@ processorComponent = world.Create_ProcessorComponent(
        speciesEntity);
    
    speciesComponent.colour = colour;

    // iterates over all compounds, and sets amounts and priorities
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint i = 0; i < compoundCount; ++i){

        auto compound = SimulationParameters::compoundRegistry().getTypeData(i);

        if(!compounds.exists(compound.internalName))
            continue;

        InitialCompound compoundAmount;
        if(!compounds.get(compound.internalName, compoundAmount)){

            assert(false, "createSpecies: invalid data in compounds, with key: " +
                compound.internalName);
            continue;
        }
        
        speciesComponent.avgCompoundAmounts[formatUInt(compound.id)] = compoundAmount;
    }

    dictionary capacities;
    for(uint i = 0; i < organelles.length(); ++i){

        const Organelle@ organelleDefinition = organelles[i].organelle;
        if(organelleDefinition is null){

            LOG_ERROR("Organelle table has a null organelle in it, position: " + i +
                "', that was added to a species entity");
            continue;
        }
        
        for(uint processNumber = 0;
            processNumber < organelleDefinition.processes.length(); ++processNumber)
        {
            // This name needs to match the one in bioProcessRegistry
            TweakedProcess@ process = organelleDefinition.processes[processNumber];
                
            if(!capacities.exists(process.process.internalName)){
                capacities[process.process.internalName] = 0;
            }

            // Here the second capacities[process.name] was initially capacities[process]
            // but the processes are just strings inside the Organelle class
            capacities[process.process.internalName] = float(capacities[
                    process.process.internalName]) +
                process.capacity;
        }
    }

    uint64 processCount = SimulationParameters::bioProcessRegistry().getSize();
    for(uint bioProcessId = 0; bioProcessId < processCount; ++bioProcessId){
            
        auto processName = SimulationParameters::bioProcessRegistry().getInternalName(
            bioProcessId);
            
        if(capacities.exists(processName)){

            float capacity;
            if(!capacities.get(processName, capacity)){
                LOG_ERROR("capacities has invalid value");
                continue;
            }
            
            processorComponent.setCapacity(bioProcessId, capacity);
            // This may be commented out for the reason that the default should be retained
            // } else {
            // processorComponent.setCapacity(bioProcessId, 0)
        }
    }

    return speciesEntity;    
}


//! Mutates a species' dna code randomly
string mutate(const string &in stringCode){
    //moving the stringCode to a table to facilitate changes
    string chromosomes = stringCode.substr(2);

    //try to insert a letter at the end of the table.
    if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        chromosomes += getRandomLetter();
    }

    //modifies the rest of the table.
    for(uint i = 0; i < stringCode.length(); ++i){
        uint index = stringCode.length() - i;

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
            chromosomes.erase(index, 1);
        }
        
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            chromosomes.insert(index, getRandomLetter());
        }
    }

    //transforming the table back into a string
    // TODO: remove Hardcoded microbe genes
    auto newString = "NY" + chromosomes;
    return newString;
}


//! Calls resetAutoEvo on world's SpeciesSystem
void resetAutoEvo(CellStageWorld@ world){

    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).resetAutoEvo();
}

}

