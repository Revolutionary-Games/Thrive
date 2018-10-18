#include "organelle_table.as"
#include "microbe_operations.as"
#include "procedural_microbes.as"

float randomColourChannel(){
    return GetEngine().GetRandom().GetNumber(MIN_COLOR, MAX_COLOR);
}

float randomMutationColourChannel(){
    return GetEngine().GetRandom().GetNumber(MIN_COLOR_MUTATION, MAX_COLOR_MUTATION);
}

float randomOpacity(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY, MAX_OPACITY);
}

float randomOpacityChitin(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY_CHITIN, MAX_OPACITY_CHITIN);
}

float randomOpacityBacteria(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY, MAX_OPACITY+1);
}

float randomMutationOpacity(){
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY_MUTATION, MAX_OPACITY_MUTATION);
}

Float4 randomColour(float opaqueness = randomOpacity()){
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

Float4 randomProkayroteColour(float opaqueness = randomOpacityBacteria()){
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

string generateNameSection(){
    auto prefixCofixList = SimulationParameters::speciesNameController().getPrefixCofix();
    auto prefix_v = SimulationParameters::speciesNameController().getVowelPrefixes();
    auto prefix_c = SimulationParameters::speciesNameController().getConsonantPrefixes();
    auto cofix_v = SimulationParameters::speciesNameController().getVowelCofixes();
    auto cofix_c = SimulationParameters::speciesNameController().getConsonantCofixes();
    auto suffix = SimulationParameters::speciesNameController().getSuffixes();
    string newName = "";

     if (GetEngine().GetRandom().GetNumber(0,100) >= 10){
        if (GetEngine().GetRandom().GetNumber(0,100) >= 45){
            if (GetEngine().GetRandom().GetNumber(0,100) >= 40){
                string ourPrefix = prefix_v[GetEngine().GetRandom().GetNumber(0,prefix_v.length()-1)];
                string ourCofix = cofix_v[GetEngine().GetRandom().GetNumber(0,cofix_v.length()-1)];
                string ourSuffix = suffix[GetEngine().GetRandom().GetNumber(0,suffix.length()-1)];
                LOG_INFO(ourPrefix+ourCofix+ourSuffix);
                newName = ourPrefix+ourCofix+ourSuffix;
            }
            else{
                string ourPrefix = prefix_v[GetEngine().GetRandom().GetNumber(0,prefix_v.length()-1)];
                string ourCofix = cofix_c[GetEngine().GetRandom().GetNumber(0,cofix_c.length()-1)];
                string ourSuffix = suffix[GetEngine().GetRandom().GetNumber(0,suffix.length()-1)];
                LOG_INFO(ourPrefix+ourCofix+ourSuffix);
                newName = ourPrefix+ourCofix+ourSuffix;
            }
                }
        else{
            string ourPrefix = prefix_c[GetEngine().GetRandom().GetNumber(0,prefix_c.length()-1)];
            string ourCofix = cofix_v[GetEngine().GetRandom().GetNumber(0,cofix_v.length()-1)];
            string ourSuffix = suffix[GetEngine().GetRandom().GetNumber(0,suffix.length()-1)];
            LOG_INFO(ourPrefix+ourCofix+ourSuffix);
            newName = ourPrefix+ourCofix+ourSuffix;
        }
     }
     else{
          string ourPrefixCofix = prefixCofixList[GetEngine().GetRandom().GetNumber(0,prefixCofixList.length()-1)];
          string ourSuffix = suffix[GetEngine().GetRandom().GetNumber(0,suffix.length()-1)];
          LOG_INFO(ourPrefixCofix+ourSuffix);
          newName=ourPrefixCofix+ourSuffix;
        }
    // TODO: DO more stuff here to improve names
    // (remove double letters when the prefix ends with and the cofix starts with the same letter
    // Remove weird things that come up like "rc" (Implemented through vowels and consonants)
    return newName;
}

const dictionary DEFAULT_INITIAL_COMPOUNDS =
    {
        {"atp", InitialCompound(30,300)},
        {"glucose", InitialCompound(30,300)},
        {"ammonia", InitialCompound(30,100)}
    };

string randomSpeciesName(){
    return "Species_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
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
            genus = generateNameSection();
            epithet = generateNameSection();
            // Variables used in AI to determine general behavior
            this.aggression = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_AGRESSION);
            this.fear = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_FEAR);
            this.activity = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_ACTIVITY);
            this.focus = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_FOCUS);
            LOG_INFO("Aggression is:"+aggression);
            LOG_INFO("Fear is:"+fear);
            LOG_INFO("Lethargicness is:"+activity);
            LOG_INFO("Focus is:"+focus);
            auto stringSize=GetEngine().GetRandom().GetNumber(MIN_INITIAL_LENGTH,
                MAX_INITIAL_LENGTH);
            if (GetEngine().GetRandom().GetNumber(0,100) <= 10){
                // Generate an extremely large cell, players never really had enough challenge
                LOG_INFO("Generating EPIC cell");
                stringSize = GetEngine().GetRandom().GetNumber(MIN_INITIAL_EPIC_LENGTH,
                    MAX_INITIAL_EPIC_LENGTH);
            }

            const auto cytoplasmGene = getOrganelleDefinition("cytoplasm").gene;

            // it should always have a nucleus and a cytoplasm.
            stringCode = getOrganelleDefinition("nucleus").gene +
                cytoplasmGene;


            for(int i = 0; i < stringSize; i++){
                this.stringCode += getRandomLetter(false);
            }

            // And then random cytoplasm padding
            const auto cytoplasmPadding = GetEngine().GetRandom().GetNumber(0, 20);
            if ( GetEngine().GetRandom().GetNumber(0, 100) <= 25)
            {
                for(int i = 0; i < cytoplasmPadding; i++){
                    this.stringCode.insert(GetEngine().GetRandom().GetNumber(2,stringCode.length()),cytoplasmGene);
                }
            }
            this.colour = getRightColourForSpecies();

            if (GetEngine().GetRandom().GetNumber(0,100) < 50)
            {
                this.speciesMembraneType = MEMBRANE_TYPE::MEMBRANE;
            }
            else {
                this.speciesMembraneType = MEMBRANE_TYPE::DOUBLEMEMBRANE;
                this.colour.W = randomOpacityChitin();;
            }
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
        this.isBacteria=parent.isBacteria;
        if (!isBacteria)
        {
            name = randomSpeciesName();
            epithet = generateNameSection();
            genus = parent.genus;

        // Variables used in AI to determine general behavior mutate these
        this.aggression = parent.aggression+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.fear = parent.fear+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.activity = parent.activity+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.focus = parent.focus+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        // Make sure not over or under our scales
        cleanPersonality();
        // Subtly mutate color
        if (GetEngine().GetRandom().GetNumber(0,5)==0)
            {
            this.colour = Float4(parent.colour.X+randomMutationColourChannel(),parent.colour.Y+randomMutationColourChannel(),parent.colour.Z+randomMutationColourChannel(), parent.colour.W+randomMutationOpacity());
            }
         LOG_INFO("Aggression is:"+aggression);
         LOG_INFO("Fear is:"+fear);
         LOG_INFO("Lethargicness is:"+activity);
         LOG_INFO("Focus is:"+focus);
            // Chance of new color needs to be low
            if (GetEngine().GetRandom().GetNumber(0,100)==1)
            {
                LOG_INFO("New Genus");
                // We can do more fun stuff here later
                genus = generateNameSection();
                // New genuses get to double their color change
            this.colour = Float4(parent.colour.X+randomMutationColourChannel(),parent.colour.Y+randomMutationColourChannel(),parent.colour.Z+randomMutationColourChannel(), parent.colour.W+randomMutationOpacity());
            }

            this.population = int(floor(parent.population / 2.f));
            parent.population = int(ceil(parent.population / 2.f));
            this.stringCode = Species::mutate(parent.stringCode);

            this.speciesMembraneType = parent.speciesMembraneType;

            commonConstructor(world);


            this.setupSpawn(world);
        }
        else
        {
            mutateBacteria(parent,world);
        }
    }

    private void cleanPersonality(){
    // Is there a better way of doing this while keeping it clean?
    // Aggression
    if (this.aggression > MAX_SPECIES_AGRESSION)
        {
        this.aggression=MAX_SPECIES_AGRESSION;
        }
    if (this.aggression < 0.0f)
        {
        this.aggression=0;
        }
    // Fear
    if (this.fear > MAX_SPECIES_FEAR)
        {
        this.fear=MAX_SPECIES_FEAR;
        }
    if (this.fear < 0.0f)
        {
        this.fear=0;
        }
    // Activity
    if (this.activity > MAX_SPECIES_ACTIVITY)
        {
        this.activity=MAX_SPECIES_ACTIVITY;
        }
    if (this.activity < 0.0f)
        {
        this.activity=0;
        }
    // Focus
    if (this.focus > MAX_SPECIES_FOCUS)
        {
        this.focus=MAX_SPECIES_FOCUS;
        }
    if (this.focus < 0.0f)
        {
        this.focus=0;
        }
    }

    private void commonConstructor(CellStageWorld@ world){

        @forWorld = world;

        auto organelles = positionOrganelles(stringCode);

        templateEntity = Species::createSpecies(forWorld, this.name, organelles, this.colour,
            this.isBacteria, this.speciesMembraneType,
            DEFAULT_INITIAL_COMPOUNDS, this.aggression, this.fear, this.activity, this.focus);
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
            for(int i = 0; i < GetEngine().GetRandom().GetNumber(1,5); i++){
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

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(1,7); i++){
                // Dont spawn them on top of each other because it
                // Causes them to bounce around and lag
                MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,"",true);
                curSpawn = curSpawn + Float3(lineX+GetEngine().GetRandom().GetNumber(-2,2),
                    0,linez+GetEngine().GetRandom().GetNumber(-2,2));
            }
        }
        else{
            // Network
            // Allows for "jungles of cyanobacteria"
            // Network is extremely rare
            float x = curSpawn.X;
            float z = curSpawn.Z;
            // To prevent bacteria being spawned on top of each other
            bool horizontal = false;
            bool vertical = false;

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(3,10); i++)
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
                        curSpawn.Z += GetEngine().GetRandom().GetNumber(-2,2);
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
                        curSpawn.X += GetEngine().GetRandom().GetNumber(-2,2);
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
                        curSpawn.Z -= GetEngine().GetRandom().GetNumber(-2,2);
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
                        curSpawn.X -= GetEngine().GetRandom().GetNumber(-2,2);
                        MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,
                            "", true);
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
                        MicrobeOperations::spawnBacteria(world, pos+curSpawn, this.name,true,
                            "", true);
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
            factory, 1.0f/(STARTING_SPAWN_DENSITY-(this.population*5)), //spawnDensity should depend on population
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
            factory, 1.0f/(STARTING_SPAWN_DENSITY-(this.population*5)), //spawnDensity should depend on population
            MICROBE_SPAWN_RADIUS);
    }

    void generateBacteria(CellStageWorld@ world){
        // Chance they spawn with flagella
        int bacterialFlagellumChance = 10;

        name = randomBacteriaName();
        genus = generateNameSection();
        epithet = generateNameSection();

        // Variables used in AI to determine general behavior
        this.aggression = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_AGRESSION);
        this.fear = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_FEAR);
        this.activity = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_ACTIVITY);
        this.focus = GetEngine().GetRandom().GetFloat(0.0f,
                MAX_SPECIES_FOCUS);
         LOG_INFO("Aggression is:"+aggression);
         LOG_INFO("Fear is:"+fear);
         LOG_INFO("Lethargicness is:"+activity);
         LOG_INFO("Focus is:"+focus);
        // Bacteria are tiny, start off with a max of 3 hexes (maybe
        // we should start them all off with just one? )
        auto stringSize = GetEngine().GetRandom().GetNumber(0,2);
        if (GetEngine().GetRandom().GetNumber(0,100) <= 10){
            // Generate an extremely large cell, players never really had enough challenge
            LOG_INFO("Generating EPIC bacterium");
            stringSize = GetEngine().GetRandom().GetNumber(MIN_INITIAL_EPIC_BACTERIA_LENGTH,
                MAX_INITIAL_EPIC_BACTERIA_LENGTH);
         }
        // Bacteria
        // will randomly have 1 of 3 organelles right now, photosynthesizing protiens,
        // respiratory Protiens, or Oxy Toxy Producing Protiens, also pure cytoplasm
        // aswell for variety.
        //TODO when chemosynthesis is added add a protien for that aswell
        switch(GetEngine().GetRandom().GetNumber(1,7))
        {
        case 1:
            stringCode = getOrganelleDefinition("protoplasm").gene;
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
        case 5:
            stringCode = getOrganelleDefinition("chemoSynthisizingProtiens").gene;
            break;
        case 6:
            stringCode = getOrganelleDefinition("nitrogenFixationProtiens").gene;
            break;
        default:
            stringCode = getOrganelleDefinition("protoplasm").gene;
            break;
        }

        string chosenType= stringCode;
        for(int i = 0; i < stringSize; i++){
            this.stringCode += chosenType;
        }
        // Allow bacteria to sometimes start with a flagella instead of having to evolve it
        this.colour = getRightColourForSpecies();
        if (GetEngine().GetRandom().GetNumber(1,100) <= bacterialFlagellumChance)
        {
            this.stringCode+=getOrganelleDefinition("flagellum").gene;;
        }
        if (GetEngine().GetRandom().GetNumber(0,100) < 50)
            {
            this.speciesMembraneType = MEMBRANE_TYPE::WALL;
            }
         else {
              this.speciesMembraneType = MEMBRANE_TYPE::CHITIN;
              this.colour.W = randomOpacityChitin();
               }
        commonConstructor(world);
        this.setupBacteriaSpawn(world);
    }

    void mutateBacteria(Species@ parent, CellStageWorld@ world){
        name = randomBacteriaName();
        genus = parent.genus;
        epithet = generateNameSection();

        // Variables used in AI to determine general behavior mutate these
        this.aggression = parent.aggression+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.fear = parent.fear+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.activity = parent.activity+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);
        this.focus = parent.focus+GetEngine().GetRandom().GetFloat(MIN_SPECIES_PERSONALITY_MUTATION,
                MAX_SPECIES_PERSONALITY_MUTATION);

        // Make sure not over or under our scales
        cleanPersonality();

        // Subtly mutate color
        if (GetEngine().GetRandom().GetNumber(0,5)==0)
            {
            this.colour = Float4(parent.colour.X+randomMutationColourChannel(),parent.colour.Y+randomMutationColourChannel(),parent.colour.Z+randomMutationColourChannel(), parent.colour.W+randomMutationOpacity());
            }
         LOG_INFO("Aggression is:"+aggression);
         LOG_INFO("Fear is:"+fear);
         LOG_INFO("Lethargicness is:"+activity);
         LOG_INFO("Focus is:"+focus);

        if (GetEngine().GetRandom().GetNumber(0,100)==1)
        {
            LOG_INFO("New Genus of bacteria");
            // We can do more fun stuff here later
            genus = generateNameSection();
            // New genuses get to double color change
            this.colour = Float4(parent.colour.X+randomMutationColourChannel(),parent.colour.Y+randomMutationColourChannel(),parent.colour.Z+randomMutationColourChannel(), parent.colour.W+randomMutationOpacity());
        }
        this.population = int(floor(parent.population / 2.f));
        parent.population = int(ceil(parent.population / 2.f));

        this.stringCode = Species::mutateProkaryote(parent.stringCode);
        this.speciesMembraneType = parent.speciesMembraneType;
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

    int getPopulationFromAutoEvo(){
        return this.population;
    }

    void modifyPopulationFromAUtoEvo(int population){
        this.population+=population;
    }
    Float4 getRightColourForSpecies(){
        if (isBacteria){
            return randomProkayroteColour();
        } else {
            return randomColour();
        }
    }

    string name;
    string genus;
    string epithet;
    bool isBacteria;
    double aggression = 100.0f;
    double fear = 100.0f;
    double activity = 0.0f;
    double focus = 0.0f;
    MEMBRANE_TYPE speciesMembraneType;
    string stringCode;
    int population = GetEngine().GetRandom().GetNumber(600,INITIAL_POPULATION);
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

// How big is a newly created species's population.
const auto INITIAL_POPULATION = 3000;

// How much time does it take for the simulation to update.
const auto SPECIES_SIM_INTERVAL = 5000;

// If a specie's population goes below this it goes extinct.
const auto MIN_POP_SIZE = 2;

// If a specie's population goes above this it gets split in half and a
// new mutated species apears. this should be randomized
const auto MAX_POP_SIZE = 6000;

// The amount of species at the start of the microbe stage (not counting Default/Player)
const auto INITIAL_SPECIES = 7;

// The amount of bacterial species to start with
const auto INITIAL_BACTERIA = 4;

// If there are more species than this then all species get their population reduced by half
const auto MAX_SPECIES = 15;

// If there are more bacteria than this then all species get their population reduced by half
const auto MAX_BACTERIA = 6;

// If there are less species than this creates new ones.
const auto MIN_SPECIES = 3;

// If there are less species than this creates new ones.
const auto MIN_BACTERIA = 2;

//! Updates the species's population and creates new ones. And keeps track of Species objects
class SpeciesSystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "SpeciesSystem expected CellStageWorld");

        // This is needed to actually have AI species in the world
        createNewEcoSystem();
    }

    void Release(){
        // Destroy all species to stop complaints that they aren't extinguished
        resetAutoEvo();
    }

    void Run(){
        //LOG_INFO("autoevo running");
        // Update population numbers and split/extinct species as needed

        timeSinceLastCycle++;
        while(this.timeSinceLastCycle > SPECIES_SIM_INTERVAL){
            LOG_INFO("Processing Auto-evo Step");
            this.timeSinceLastCycle -= SPECIES_SIM_INTERVAL;
            bool ranEventThisStep=false;

            // Every 8 steps or so do a cambrian explosion style
            // Event, this should increase variablility significantly
            if(GetEngine().GetRandom().GetNumber(0,200) <= 25){
                LOG_INFO("Cambrian Explosion");
                ranEventThisStep=true;
                // TODO: add a notification for when this happens
                doCambrianExplosion();
            }
            // Various mass extinction events
            // Only run one "big event" per turn
            if(species.length() > MAX_SPECIES+MAX_BACTERIA && !ranEventThisStep){
                LOG_INFO("Mass extinction time");
                // F to pay respects: TODO: add a notification for when this happens
                ranEventThisStep=true;
                doMassExtinction();
            }
            // Add some variability, this is a less deterministic mass
            // Extinction eg, a meteor, etc.
            if(GetEngine().GetRandom().GetNumber(0,1000) == 1 && !ranEventThisStep){
                LOG_INFO("Black swan event");
                ranEventThisStep=true;
                // F to pay respects: TODO: add a notification for when this happens
                doMassExtinction();
            }

            // Super extinction event
            if(GetEngine().GetRandom().GetNumber(0,1000) == 1 && !ranEventThisStep){
                LOG_INFO("Great Dying");
                ranEventThisStep=true;
                // Do mass extinction code then devastate all species, this should extinct quite a few of the ones that arent doing well.
                //*Shudders*
                doMassExtinction();
                doDevestation();
            }

            auto numberOfSpecies = species.length();
            for(uint i = 0; i < numberOfSpecies; i++){
                // Traversing the population backwards to avoid
                // "chopping down the branch i'm sitting in"
                auto index = numberOfSpecies - 1 - i;
                auto currentSpecies = species[index];
                currentSpecies.updatePopulation();
                auto population = currentSpecies.population;
                LOG_INFO(currentSpecies.name+" "+currentSpecies.population);
                bool ranSpeciesEvent=false;


                // This is also just to shake things up occassionally
                // Cambrian Explosion
                if ( currentSpecies.population > 0 &&
                    GetEngine().GetRandom().GetNumber(0,10) <= 2)
                {
                    // P to pat back: TODO: add a notification for when this happens
                    LOG_INFO(currentSpecies.name + " is diversifying!");
                    currentSpecies.boom();
                    LOG_INFO(currentSpecies.name+" population is now "+
                        currentSpecies.population);
                    ranSpeciesEvent=true;
                }

                // This is just to shake things up occassionally
                if ( currentSpecies.population > 0 &&
                    GetEngine().GetRandom().GetNumber(0,10) <= 2 && !ranSpeciesEvent)
                {
                    // F to pay respects: TODO: add a notification for when this happens
                    LOG_INFO(currentSpecies.name + " has been devestated by disease.");
                    currentSpecies.devestate();
                    LOG_INFO(currentSpecies.name+" population is now "+
                        currentSpecies.population);
                    ranSpeciesEvent=true;
                }

                // 50% chance of splitting off two species instead of one
                if(GetEngine().GetRandom().GetNumber(0,10) <= 5 && ranSpeciesEvent==false &&
                currentSpecies.population >= MAX_POP_SIZE){
                    auto oldPop = currentSpecies.population;
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
                    ranSpeciesEvent=true;
                    species.insertLast(newSpecies);
                    LOG_INFO("Species " + currentSpecies.name + " split off several species, the first is:" +
                        newSpecies.name);
                    // Reset pop so we can split a second time
                    currentSpecies.population = oldPop;
                }

                // Reproduction and mutation
                // TODO:Bacteria should mutate more often then eukaryotes but this is fine for now
                if(currentSpecies.population >= MAX_POP_SIZE){
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


                // Extinction, thisi s not an event since things with low population need to be killed off.
                if(currentSpecies.population <= MIN_POP_SIZE){
                    LOG_INFO("Species " + currentSpecies.name + " went extinct");
                    currentSpecies.extinguish();
                    species.removeAt(index);
                    // Tweak numbers here
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

            // These are kind of arbitray, we should pronbabbly make it less arbitrary
            // New species
            while(currentEukaryoteAmount < MIN_SPECIES){
                LOG_INFO("Creating new species as there's too few");
                createSpecies();
                currentEukaryoteAmount++;
            }

            // New bacteria
            while(currentBacteriaAmount < MIN_BACTERIA){
                LOG_INFO("Creating new prokaryote as there's too few");
                createBacterium();
                currentBacteriaAmount++;
            }



        }
    }

    void Clear(){}

    void CreateAndDestroyNodes(){}

    void updatePopulationForSpecies(string speciesName, int num)
        {
             auto numberOfSpecies = species.length();
            for(uint i = 0; i < numberOfSpecies; i++){
            if (species[i].name == speciesName)
                {
                species[i].population+=num;
                }
            }
        }

    int getSpeciesPopulation(string speciesName)
        {
            auto numberOfSpecies = species.length();
            for(uint i = 0; i < numberOfSpecies; i++){
            if (species[i].name == speciesName)
                {
                return species[i].population;
                }
            }
            return -1;
        }

    void resetAutoEvo(){

        for(uint i = 0; i < species.length(); i++){

            species[i].extinguish();
        }

        species.resize(0);
    currentBacteriaAmount = 0;
    currentEukaryoteAmount = 0;
    }

    void doMassExtinction(){
        // This doesnt seem like a powerful event
        for(uint i = 0; i < species.length(); i++){
            species[i].population /= 2;
        }
    }

    void doDevestation(){
        // Devastate all species.
        for(uint i = 0; i < species.length(); i++){
            species[i].devestate();
        }
    }

    void doCambrianExplosion(){
        for(uint i = 0; i < species.length(); i++){
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

    void createNewEcoSystem()
        {
        for(int i = 0; i < INITIAL_SPECIES; i++){
            createSpecies();
            currentEukaryoteAmount++;
        }

        //generate bacteria aswell
        for(int i = 0; i < INITIAL_BACTERIA; i++){
            createBacterium();
            currentBacteriaAmount++;
        }
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
    for(uint i = 0; i < microbeComponent.organelles.length(); i++){

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
    for(uint i = 0; i < speciesComponent.organelles.length(); i++){
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
    // Another place where compound amounts are something we need to worry about
    auto ids = species.avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); i++){
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

        // TODO: only ones that have been removed should be deleted

        auto organelle = microbeComponent.organelles[microbeComponent.organelles.length() - 1];
        auto q = organelle.q;
        auto r = organelle.r;
        // TODO: this could be done more efficiently
        MicrobeOperations::removeOrganelle(world, microbeEntity, {q, r});
    }

    // give it organelles
    for(uint i = 0; i < species.organelles.length(); i++){

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
    for(uint i = 0; i < fromTemplate.organelles.length(); i++){

        OrganelleTemplatePlaced@ organelle = fromTemplate.organelles[i];

        convertedOrganelles.insertLast(PlacedOrganelle(
                getOrganelleDefinition(organelle.type), organelle.q, organelle.r,
                organelle.rotation));
    }

    return createSpecies(world, name, convertedOrganelles, fromTemplate.colour, fromTemplate.isBacteria, fromTemplate.speciesMembraneType,
        fromTemplate.compounds, 100.0f, 100.0f, 100.0f, 200.0f);
}

//! Creates an entity that has all the species stuff on it
//! AI controlled ones need to be in addition in SpeciesSystem
ObjectID createSpecies(CellStageWorld@ world, const string &in name,
    array<PlacedOrganelle@> organelles, Float4 colour, bool isBacteria, MEMBRANE_TYPE speciesMembraneType,  const dictionary &in compounds, double aggression, double fear, double activity, double focus
) {
    ObjectID speciesEntity = world.CreateEntity();

    SpeciesComponent@ speciesComponent = world.Create_SpeciesComponent(speciesEntity,
        name);

    @speciesComponent.avgCompoundAmounts = dictionary();

    @speciesComponent.organelles = array<SpeciesStoredOrganelleType@>();
    for(uint i = 0; i < organelles.length(); i++){

        // This conversion does a little bit of extra calculations (that are in the
        // end not used)
        speciesComponent.organelles.insertLast(PlacedOrganelle(organelles[i]));
    }

    // Verify it //
    for(uint i = 0; i < speciesComponent.organelles.length(); i++){

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
    // we need to know our aggression and fear variables
    speciesComponent.aggression = aggression;
    speciesComponent.fear = fear;
    speciesComponent.activity = activity;
    speciesComponent.focus = focus;
    // iterates over all compounds, and sets amounts and priorities
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint i = 0; i < compoundCount; i++){

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
    for(uint i = 0; i < organelles.length(); i++){

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
    LOG_INFO(processCount+" is the amount of processes");
    for(uint bioProcessId = 0; bioProcessId < processCount; ++bioProcessId){
        auto processName = SimulationParameters::bioProcessRegistry().getInternalName(
            bioProcessId);

        if(capacities.exists(processName)){
            float capacity;
            if(!capacities.get(processName, capacity)){
                LOG_ERROR("capacities has invalid value");
                continue;
            }
    LOG_INFO(bioProcessId+" has been set");
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
    // Moving the stringCode to a table to facilitate changes
    string chromosomes = stringCode.substr(2);
    // Try to insert a letter at the end of the table.
    if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        chromosomes += getRandomLetter(false);
    }

    // Modifies the rest of the table.
    for(uint i = 0; i < stringCode.length(); i++){
        // Index we are adding or erasing chromosomes at
        uint index = stringCode.length() - i - 1;

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
        // Removing the last organelle is pointless, that would kill the creature (also caused errors).
            if (index != stringCode.length()-1)
            {
            chromosomes.erase(index, 1);
            }
        }

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            // There is an error here when we try to insert at the end of the list so use insertlast instead in that case
            if (index != stringCode.length()-1)
            {
            chromosomes.insert(index, getRandomLetter(false));
            }
            else{
            chromosomes+=getRandomLetter(false);
            }
        }
    }

    // Transforming the table back into a string
    // TODO: remove Hardcoded microbe genes
    auto newString = "NY" + chromosomes;
    return newString;
}

// Mutate a Bacterium
string mutateProkaryote(const string &in stringCode ){
    // Moving the stringCode to a table to facilitate changes
    string chromosomes = stringCode;
    // Try to insert a letter at the end of the table.
    if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        chromosomes += getRandomLetter(true);
    }
    // Modifies the rest of the table.
    for(uint i = 0; i < stringCode.length(); i++){
        // Index we are adding or erasing chromosomes at
        uint index = stringCode.length() - i -1;
        // Bacteria can be size 1 so removing their only organelle is a bad idea
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
            if (index != stringCode.length()-1)
            {
            chromosomes.erase(index, 1);
            }
        }

        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            // There is an error here when we try to insert at the end of the list so use insertlast instead in that case
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

