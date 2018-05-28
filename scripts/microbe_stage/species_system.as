#include "organelle_table.as"
#include "microbe_operations.as"
#include "procedural_microbes.as"

float randomColourChannel(){
    return GetEngine().GetRandom().GetNumber(MIN_COLOR, MAX_COLOR);
}

float randomOpacity(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY, MAX_OPACITY);
}

float randomOpacityBacteria(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY+1, MAX_OPACITY+1);
}


Float4 randomColour(float opaqueness = randomOpacity()){
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

Float4 randomProkayroteColour(float opaqueness = randomOpacityBacteria()){
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

const dictionary DEFAULT_INITIAL_COMPOUNDS =
    {
        {"atp", InitialCompound(60, 10)},
        {"glucose", InitialCompound(10)},
        {"reproductase", InitialCompound(0, 8)},
        {"oxytoxy", InitialCompound(1)}
    };

string randomSpeciesName(){
    return "Species_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
    // Gotta use the latin names (But they aren't used?)
}

// Bacteria also need names
string randomBacteriaName(){
    return "Bacteria_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
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
    Species(CellStageWorld@ world, bool isBacteria){
        this.isBacteria=isBacteria;
        if (!isBacteria)
        {
            name = randomSpeciesName();
            auto stringSize = GetEngine().GetRandom().GetNumber(MIN_INITIAL_LENGTH,
                MAX_INITIAL_LENGTH);

            //it should always have a nucleus and a cytoplasm.
            stringCode = getOrganelleDefinition("nucleus").gene +
                getOrganelleDefinition("cytoplasm").gene;

            for(int i = 0; i < stringSize; ++i){
                this.stringCode += getRandomLetter(false);
            }
            this.speciesMembraneType = MEMBRANE_TYPE::MEMBRANE;
            this.colour = getRightColourForSpecies();
            commonConstructor(world);
            this.setupSpawn(world);

        }
        else{
            // We are creating a bacteria right now
            generateBacteria(world);
        }
    }

    ~Species(){

        if(templateEntity != NULL_OBJECT){

            LOG_ERROR("Species object not extinguish()ed before destructor, doing that now");
            extinguish();
        }
    }

    // Creates a mutated version of the species and reduces the species population by half
    Species(Species@ parent, CellStageWorld@ world, bool isBacteria){
        this.isBacteria=isBacteria;
        if (!isBacteria)
        {
            name = randomSpeciesName();
            //chance of new color needs to be low
            if (GetEngine().GetRandom().GetNumber(0,100)==1)
            {
                LOG_INFO("New Clade");
                //we can do more fun stuff here later
                this.colour = randomColour();
            }
            else
            {
                this.colour = parent.colour;
            }
            this.population = int(floor(parent.population / 2.f));
            parent.population = int(ceil(parent.population / 2.f));
            this.stringCode = Species::mutate(parent.stringCode);

            this.speciesMembraneType = MEMBRANE_TYPE::MEMBRANE;
            this.colour = getRightColourForSpecies();

            commonConstructor(world);


            this.setupSpawn(world);
        }
        else
        {
            mutateBacteria(parent,world);
        }
    }

    private void commonConstructor(CellStageWorld@ world){

        @forWorld = world;

        auto organelles = positionOrganelles(stringCode);

        templateEntity = Species::createSpecies(forWorld, this.name, organelles, this.colour,
            this.isBacteria, this.speciesMembraneType,
            DEFAULT_INITIAL_COMPOUNDS);
    }

    // Delete a species
    void extinguish(){
        if(forWorld !is null){
            LOG_INFO("Species " + name + " has been extinguished");
            forWorld.GetSpawnSystem().removeSpawnType(this.id);
            //this.template.destroy() //game crashes if i do that.
            // Let's hope this doesn't crash then
            if(templateEntity != NULL_OBJECT){
                forWorld.QueueDestroyEntity(templateEntity);
                templateEntity = NULL_OBJECT;
            }

            @forWorld = null;
        }
    }

    ObjectID factorySpawn(CellStageWorld@ world, Float3 pos){

        LOG_INFO("New member of species spawned: " + this.name);
        return MicrobeOperations::spawnMicrobe(world, pos, this.name,
            // Ai controlled
            true,
            // No individual name (could be good for debugging)
            "");
    }

    ObjectID bacteriaColonySpawn(CellStageWorld@ world, Float3 pos){
        LOG_INFO("New colony of species spawned: " + this.name);
        Float3 curSpawn = Float3(GetEngine().GetRandom().GetNumber(1,7),0,GetEngine().
            GetRandom().GetNumber(1,7));
        // Three kinds of colonies are supported, line colonies and clump coloniesand Networks

        if (GetEngine().GetRandom().GetNumber(0,4) < 2)
        {
            // Clump
            for(int i = 0; i < GetEngine().GetRandom().GetNumber(1,5); ++i){
                //dont spawn them on top of each other because it
                //causes them to bounce around and lag
                MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",true);
                curSpawn = curSpawn + Float3(GetEngine().GetRandom().GetNumber(-7,7),0,
                    GetEngine().GetRandom().GetNumber(-7,7));
            }
        }
        else if (GetEngine().GetRandom().GetNumber(0,30) > 2)
        {
            // Line
            // Allow for many types of line
            float lineX = GetEngine().GetRandom().GetNumber(-5,5)+GetEngine().GetRandom().
                GetNumber(-5,5);
            float linez = GetEngine().GetRandom().GetNumber(-5,5)+GetEngine().GetRandom().
                GetNumber(-5,5);

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(1,7); ++i){
                // Dont spawn them on top of each other because it
                // Causes them to bounce around and lag
                MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",true);
                curSpawn = curSpawn + Float3(lineX+GetEngine().GetRandom().GetNumber(-1,1),
                    0,linez+GetEngine().GetRandom().GetNumber(-1,1));
            }
        }
        else{
        // Network
    // Allows for "jungles of cyanobacteria"
            // Ntwork is extremely rare
            float x = curSpawn.X;
            float z = curSpawn.Z;
            // To prevent bacteria being spawned on top of each other
            bool horizontal = false;
            bool vertical = false;

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(3,10); ++i)
            {
                if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !horizontal)
                    {
                    horizontal=true;
                    vertical=false;
                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(3,5); ++c){
                        // Dont spawn them on top of each other because
                        // It causes them to bounce around and lag
                        curSpawn.X += GetEngine().GetRandom().GetNumber(5,7);
                        // Add a litlle organicness to the look
                        curSpawn.Z += GetEngine().GetRandom().GetNumber(-1,1);
                        MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name, true,
                            "",true);
                    }
                }
            else if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !vertical) {
                horizontal=false;
                vertical=true;
                for(int c = 0; c < GetEngine().GetRandom().GetNumber(3,5); ++c){
                    // Dont spawn them on top of each other because it
                    // Causes them to bounce around and lag
                    curSpawn.Z += GetEngine().GetRandom().GetNumber(5,7);
                    // Add a litlle organicness to the look
                    curSpawn.X += GetEngine().GetRandom().GetNumber(-1,1);
                    MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",
                        true);
                }
            }
            else if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !horizontal)
                    {
                    horizontal=true;
                    vertical=false;
                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(3,5); ++c){
                        // Dont spawn them on top of each other because
                        // It causes them to bounce around and lag
                        curSpawn.X -= GetEngine().GetRandom().GetNumber(5,7);
                        // Add a litlle organicness to the look
                        curSpawn.Z -= GetEngine().GetRandom().GetNumber(-1,1);
                        MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name, true,
                            "",true);
                    }
                }
            else if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !vertical) {
                horizontal=false;
                vertical=true;
                for(int c = 0; c < GetEngine().GetRandom().GetNumber(3,5); ++c){
                    // Dont spawn them on top of each other because it
                    //causes them to bounce around and lag
                    curSpawn.Z -= GetEngine().GetRandom().GetNumber(5,7);
                    //add a litlle organicness to the look
                    curSpawn.X -= GetEngine().GetRandom().GetNumber(-1,1);
                    MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",
                        true);
                }
            }
            else {
                // Diaganol
                horizontal=false;
                vertical=false;
                for(int c = 0; c < GetEngine().GetRandom().GetNumber(3,5); ++c){
                    // Dont spawn them on top of each other because it
                    // Causes them to bounce around and lag
                    curSpawn.Z += GetEngine().GetRandom().GetNumber(5,7);
                    curSpawn.X += GetEngine().GetRandom().GetNumber(5,7);
                    MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",
                        true);
                }
            }
            }
        }
        return MicrobeOperations::spawnBacteria(world, pos, this.name,true,"",false);

    }


    void setupBacteriaSpawn(CellStageWorld@ world){

        assert(world is forWorld, "Wrong world passed to setupSpawn");

        spawningEnabled = true;

        SpawnFactoryFunc@ factory = SpawnFactoryFunc(this.bacteriaColonySpawn);

        // And register new
        LOG_INFO("Registering bacteria to spawn: " + name);
        this.id = forWorld.GetSpawnSystem().addSpawnType(
            factory, DEFAULT_SPAWN_DENSITY, //spawnDensity should depend on population
            BACTERIA_SPAWN_RADIUS);
    }

    // Sets up the spawn of the species
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

    void generateBacteria(CellStageWorld@ world){
        name = randomBacteriaName();
        // Bacteria are tiny, start off with a max of 3 hexes (maybe we should start them all off with just one? )
        auto stringSize = GetEngine().GetRandom().GetNumber(0,3);
        // Bacteria
        // will randomly have 1 of 3 organelles right now, photosynthesizing protiens,
        // respiratory Protiens, or Oxy Toxy Producing Protiens, also pure cytoplasm
        // aswell for variety.
    //TODO when chemosynthesis is added add a protien for that aswell
        switch( GetEngine().GetRandom().GetNumber(1,5))
        {
        case 1:
            stringCode = getOrganelleDefinition("cytoplasm").gene;
            break;
        case 2:
            stringCode = getOrganelleDefinition("respiartoryProteins").gene;
            break;
        case 3:
            stringCode = getOrganelleDefinition("photosyntheticProteins").gene;
            break;
        case 4:
            stringCode = getOrganelleDefinition("oxytoxyProteins").gene;
            break;
        default:
            stringCode = getOrganelleDefinition("cytoplasm").gene;
            break;
        }

        string chosenType= stringCode;
        for(int i = 0; i < stringSize; ++i){
            this.stringCode += chosenType;
        }
        this.speciesMembraneType = MEMBRANE_TYPE::WALL;
        this.colour = getRightColourForSpecies();
        commonConstructor(world);
        this.setupBacteriaSpawn(world);
    }

    void mutateBacteria(Species@ parent, CellStageWorld@ world){
        name = randomBacteriaName();
        if (GetEngine().GetRandom().GetNumber(0,100)==1)
        {
            LOG_INFO("New Clade of bacteria");
            // We can do more fun stuff here later, such as genus names
            this.colour = randomProkayroteColour();
        }
        else
        {
            this.colour = parent.colour;
        }
        this.population = int(floor(parent.population / 2.f));
        parent.population = int(ceil(parent.population / 2.f));

        this.stringCode = Species::mutateProkaryote(parent.stringCode);
        this.speciesMembraneType = MEMBRANE_TYPE::WALL;
        this.colour = getRightColourForSpecies();
        commonConstructor(world);
        this.setupBacteriaSpawn(world);
    }

    //! updates the population count of the species
    void updatePopulation(){
        // Numbers incresed so things happen more often
        this.population += GetEngine().GetRandom().GetNumber(-700, 700);
    }

    void devestate(){
        // Occassionally you just need to take a deadly virus and use
        // it to make things interesting
        this.population += GetEngine().GetRandom().GetNumber(-1500, -700);
    }

    void boom(){
        // Occassionally you just need to give a species a nice pat on
        // the back
        this.population += GetEngine().GetRandom().GetNumber(700, 1500);
    }

    Float4 getRightColourForSpecies(){
        if (isBacteria){
            return randomProkayroteColour();
        } else {
            return randomColour();
        }
    }

    string name;
    bool isBacteria;
    MEMBRANE_TYPE speciesMembraneType;
    string stringCode;
    int population = INITIAL_POPULATION;
    Float4 colour = getRightColourForSpecies();

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
const auto SPECIES_SIM_INTERVAL = 10000;

//if a specie's population goes below this it goes extinct.
const auto MIN_POP_SIZE = 500;

//if a specie's population goes above this it gets split in half and a
//new mutated species apears. this should be randomized
const auto MAX_POP_SIZE = 5000;

//the amount of species at the start of the microbe stage (not counting Default/Player)
const auto INITIAL_SPECIES = 7;

//the amount of bacteria
const auto INITIAL_BACTERIA = 4;

//if there are more species than this then all species get their population reduced by half
const auto MAX_SPECIES = 15;

//if there are more bacteria than this then all species get their population reduced by half
const auto MAX_BACTERIA = 6;

//if there are less species than this creates new ones.
const auto MIN_SPECIES = 3;

//if there are less species than this creates new ones.
const auto MIN_BACTERIA = 2;

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
            currentEukaryoteAmount++;
        }

        //generate bacteria aswell
        for(int i = 0; i < INITIAL_BACTERIA; ++i){
            createBacterium();
            currentBacteriaAmount++;
        }
    }

    void Release(){
        // Destroy all species to stop complaints that they aren't extinguished
        resetAutoEvo();
    }

    void Run(){
        //LOG_INFO("autoevo running");
        timeSinceLastCycle++;
        while(this.timeSinceLastCycle > SPECIES_SIM_INTERVAL){
            LOG_INFO("Processing Auto-evo Step");
            this.timeSinceLastCycle -= SPECIES_SIM_INTERVAL;

            //update population numbers and split/extinct species as needed
            auto numberOfSpecies = species.length();
            for(uint i = 0; i < numberOfSpecies; ++i){
                //traversing the population backwards to avoid
                //"chopping down the branch i'm sitting in"
                auto index = numberOfSpecies - 1 - i;
                auto currentSpecies = species[index];
                currentSpecies.updatePopulation();
                auto population = currentSpecies.population;
                LOG_INFO(currentSpecies.name+" "+currentSpecies.population);

                //this is just to shake things up occassionally
                if ( currentSpecies.population > 0 &&
                    GetEngine().GetRandom().GetNumber(0,10) <= 2)
                {
                    //F to pay respects: TODO: add a notification for when this happens
                    LOG_INFO(currentSpecies.name + " has been devestated by disease.");
                    currentSpecies.devestate();
                    LOG_INFO(currentSpecies.name+" population is now "+
                        currentSpecies.population);
                }

                //this is also just to shake things up occassionally
                //cambrian explosion
                if ( currentSpecies.population > 0 &&
                    GetEngine().GetRandom().GetNumber(0,10) <= 2)
                {
                    //P to pat back: TODO: add a notification for when this happens
                    LOG_INFO(currentSpecies.name + " is diversifying!");
                    currentSpecies.boom();
                    LOG_INFO(currentSpecies.name+" population is now "+
                        currentSpecies.population);
                }

                //reproduction/mutation
                //bacteria should mutate more often then eukaryote sbut this is fine for now
                if(population > MAX_POP_SIZE){
                    auto newSpecies = Species(currentSpecies, world,
                        currentSpecies.isBacteria);

                    if (newSpecies.isBacteria)
                    {
                        currentBacteriaAmount+=1;
                    }
                    else
                    {
                        currentEukaryoteAmount+=1;
                    }
                    species.insertLast(newSpecies);
                    LOG_INFO("Species " + currentSpecies.name + " split off a child species:" +
                        newSpecies.name);
                }

                //extinction
                if(population < MIN_POP_SIZE){
                    LOG_INFO("Species " + currentSpecies.name + " went extinct");
                    currentSpecies.extinguish();
                    species.removeAt(index);
                    //tweak numbers here
                    if (currentSpecies.isBacteria)
                    {
                        currentBacteriaAmount-=1;
                    }
                    else
                    {
                        currentEukaryoteAmount-=1;
                    }
                }
            }

            //These are kind of arbitray, we should pronbabbly make it less arbitrary
            //new species
            while(currentEukaryoteAmount < MIN_SPECIES){
                LOG_INFO("Creating new species as there's too few");
                createSpecies();
                currentEukaryoteAmount++;
            }

            //new bacteria
            while(currentBacteriaAmount < MIN_BACTERIA){
                LOG_INFO("Creating new prokaryote as there's too few");
                createBacterium();
                currentBacteriaAmount++;
            }

            //mass extinction events

            if(species.length() > MAX_SPECIES+MAX_BACTERIA){
                LOG_INFO("Mass extinction time");
                //F to pay respects: TODO: add a notification for when this happens
                doMassExtinction();
            }
            //add soem variability, this is a less deterministic mass
            //extinction eg, a meteor, etc.
            if(GetEngine().GetRandom().GetNumber(0,1000) == 1){
                LOG_INFO("Black swan event");
                //F to pay respects: TODO: add a notification for when this happens
                doMassExtinction();
            }

            //exvery 8 steps or so do a cambrian explosion style
            //event, this should increase variablility significantly
            if(GetEngine().GetRandom().GetNumber(0,200) <= 25){
                LOG_INFO("Cambrian Explosion");
                //F to pay respects: TODO: add a notification for when this happens
                doCambrianExplosion();
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
        //this doesnt seem like a powerful event
        for(uint i = 0; i < species.length(); ++i){
            species[i].population /= 2;
        }
    }

    void doCambrianExplosion(){
        for(uint i = 0; i < species.length(); ++i){
            species[i].population *= 2;
        }
    }

    //! Adds a new AI species
    private void createSpecies(){
        auto newSpecies = Species(world, false);
        species.insertLast(newSpecies);
    }

    //! Adds a new AI bacterium
    private void createBacterium(){
        auto newSpecies = Species(world, true);
        species.insertLast(newSpecies);
    }

    private int timeSinceLastCycle = 0;
    private array<Species@> species;
    private CellStageWorld@ world;
    //used for keeping track of amount of eukaryotes and prokaryotes
    private int currentBacteriaAmount = 0;
    private int currentEukaryoteAmount = 0;
}

//! \param updateSpecies will be modified to match the organelles of the microbe
// This isnt used anywhere by the way
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
    MicrobeOperations::setMembraneType(world, microbe, species.speciesMembraneType);
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

    return createSpecies(world, name, convertedOrganelles, fromTemplate.colour, fromTemplate.isBacteria, fromTemplate.speciesMembraneType,
        fromTemplate.compounds);
}

//! Creates an entity that has all the species stuff on it
//! AI controlled ones need to be in addition in SpeciesSystem
ObjectID createSpecies(CellStageWorld@ world, const string &in name,
    array<PlacedOrganelle@> organelles, Float4 colour, bool isBacteria, MEMBRANE_TYPE speciesMembraneType,  const dictionary &in compounds
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

    speciesComponent.speciesMembraneType = speciesMembraneType;

    //we need to know this is baceria
    speciesComponent.isBacteria = isBacteria;
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
        chromosomes += getRandomLetter(false);
    }

    //modifies the rest of the table.
    for(uint i = 0; i < stringCode.length(); ++i){
        //index we are adding or erasing chromosomes at
        uint index = stringCode.length() - i - 1;

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
        //removing the last organelle is pointless, that would kill the creature (also caused errors).
            if (index != stringCode.length()-1)
            {
            chromosomes.erase(index, 1);
            }
        }

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            //there is an error here when we try to insert at the end of the list so use insertlast instead in that case
            if (index != stringCode.length()-1)
            {
            chromosomes.insert(index, getRandomLetter(false));
            }
            else{
            chromosomes+=getRandomLetter(false);
            }
        }
    }

    //transforming the table back into a string
    // TODO: remove Hardcoded microbe genes
    auto newString = "NY" + chromosomes;
    return newString;
}

//mutate bacteria
string mutateProkaryote(const string &in stringCode ){
    //moving the stringCode to a table to facilitate changes
    string chromosomes = stringCode;
    //try to insert a letter at the end of the table.
    if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        chromosomes += getRandomLetter(true);
    }
    //modifies the rest of the table.
    for(uint i = 0; i < stringCode.length(); ++i){
        //index we are adding or erasing chromosomes at
        uint index = stringCode.length() - i -1;
        //bacteria can be size 1 so removing their only organelle is a bad idea
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
            if (index != stringCode.length()-1)
            {
            chromosomes.erase(index, 1);
            }
        }

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            //there is an error here when we try to insert at the end of the list so use insertlast instead in that case
            if (index != stringCode.length()-1)
            {
            chromosomes.insert(index, getRandomLetter(true));
            }
            else{
            chromosomes+=getRandomLetter(true);
            }
        }
    }

    auto newString = "" + chromosomes;
    return newString;
}

//! Calls resetAutoEvo on world's SpeciesSystem
void resetAutoEvo(CellStageWorld@ world){
    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).resetAutoEvo();
}

}

