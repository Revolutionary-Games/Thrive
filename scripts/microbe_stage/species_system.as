#include "organelle_table.as"
#include "microbe_operations.as"
#include "procedural_microbes.as"


// TODO: Move the following to C++ (preferably to Leviathan itself)

float clamp(float value, float lowerBound, float upperBound) {
    if (value > upperBound)
        return upperBound;

    if (value < lowerBound)
        return lowerBound;

    return value;
}

string randomChoice(const array<string>& source) {
    return source[GetEngine().GetRandom().GetNumber(0,
                    source.length() - 1)];
}

// End of the TODO.

float randomColourChannel()
{
    return GetEngine().GetRandom().GetNumber(MIN_COLOR, MAX_COLOR);
}

float randomMutationColourChannel()
{
    return GetEngine().GetRandom().GetNumber(MIN_COLOR_MUTATION, MAX_COLOR_MUTATION);
}

float randomOpacity()
{
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY, MAX_OPACITY);
}

float randomOpacityChitin()
{
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY_CHITIN, MAX_OPACITY_CHITIN);
}

float randomOpacityBacteria()
{
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY, MAX_OPACITY+1);
}

float randomMutationOpacity()
{
    return GetEngine().GetRandom().GetNumber(MIN_OPACITY_MUTATION, MAX_OPACITY_MUTATION);
}

Float4 randomColour(float opaqueness = randomOpacity())
{
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

Float4 randomProkayroteColour(float opaqueness = randomOpacityBacteria())
{
    return Float4(randomColourChannel(), randomColourChannel(), randomColourChannel(),
        opaqueness);
}

string mutateWord(string name) {
    //
    const array<string> vowels = {"a", "e", "i", "o", "u"};
    const array<string> pronoucablePermutation = {"th", "sh", "ch", "wh", "Th", "Sh", "Ch", "Wh"};
    const array<string> consonants = {"b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
                                        "n", "p", "q", "s", "t", "v", "w", "x", "y", "z"};

    string newName = name;
    int changeLimit = 1;
    int letterChangeLimit = 2;
    int letterChanges=0;
    int changes=0;

    //  th, sh, ch, wh
    for(uint i = 1; i < newName.length(); i++) {
        if(changes <= changeLimit && i > 1){
            // Index we are adding or erasing chromosomes at
            uint index = newName.length() - i - 1;
            // Are we a vowel or are we a consonant?
            bool isPermute = pronoucablePermutation.find(newName.substr(index,2)) > 0;
            string original = newName.substr(index, 2);
            if (GetEngine().GetRandom().GetNumber(0,20) <= 10 && isPermute){
                newName.erase(index, 2);
                changes++;
                newName.insert(index,randomChoice(pronoucablePermutation));
            }
        }
    }

    // 2% chance each letter
    for(uint i = 1; i < newName.length(); i++) {
        if(GetEngine().GetRandom().GetNumber(0,120) <= 1  && changes <= changeLimit){
            // Index we are adding or erasing chromosomes at
            uint index = newName.length() - i - 1;

            // Are we a vowel or are we a consonant?
            bool isVowel = vowels.find(newName.substr(index,1)) >= 0;

            bool isPermute=false;
            if (i > 1){
                if (pronoucablePermutation.find(newName.substr(index-1,2)) > 0  ||
                pronoucablePermutation.find(newName.substr(index-2,2)) > 0 ||
                pronoucablePermutation.find(newName.substr(index,2)) > 0){
                    isPermute=true;
                    //LOG_INFO(i + ":"+newName.substr(index-1,2));
                    //LOG_INFO(i + ":"+newName.substr(index-2,2));
                    //LOG_INFO(i + ":"+newName.substr(index,2));
                }
            }

            string original = newName.substr(index, 1);

            if (!isVowel && newName.substr(index,1)!="r" && !isPermute){
                newName.erase(index, 1);
                changes++;
                switch (GetEngine().GetRandom().GetNumber(0,5)) {
                    case 0:
                        newName.insert(index, randomChoice(vowels) + randomChoice(consonants));
                        break;
                    case 1:
                        newName.insert(index, randomChoice(consonants) + randomChoice(vowels));
                        break;
                    case 2:
                        newName.insert(index, original + randomChoice(consonants));
                        break;
                    case 3:
                        newName.insert(index, randomChoice(consonants) + original);
                        break;
                    case 4:
                        newName.insert(index, original + randomChoice(consonants) + randomChoice(vowels));
                        break;
                    case 5:
                        newName.insert(index, randomChoice(vowels) + randomChoice(consonants) + original);
                        break;
                }
            }
            // If is vowel
            else if (newName.substr(index,1)!="r" && !isPermute){
                newName.erase(index, 1);
                changes++;
                if(GetEngine().GetRandom().GetNumber(0,20) <= 10)
                    newName.insert(index, randomChoice(consonants) + randomChoice(vowels) + original);
                else
                    newName.insert(index, original + randomChoice(vowels) + randomChoice(consonants));
            }
        }
    }

    //Ignore the first letter and last letter
    for(uint i = 1; i < newName.length(); i++) {
        // Index we are adding or erasing chromosomes at
        uint index = newName.length() - i - 1;

        bool isPermute=false;
        if (i > 1){
            if (pronoucablePermutation.find(newName.substr(index-1,2)) > 0  ||
            pronoucablePermutation.find(newName.substr(index-2,2)) > 0 ||
            pronoucablePermutation.find(newName.substr(index,2)) > 0){
                isPermute=true;
                //LOG_INFO(i + ":"+newName.substr(index-1,2));
                //LOG_INFO(i + ":"+newName.substr(index-2,2));
                //LOG_INFO(i + ":"+newName.substr(index,2));
            }
        }

        // Are we a vowel or are we a consonant?
        bool isVowel = vowels.find(newName.substr(index,1)) >= 0;

        //50 percent chance replace
        if(GetEngine().GetRandom().GetNumber(0,20) <= 10 && changes <= changeLimit) {
            if (!isVowel && newName.substr(index,1)!="r" && !isPermute){
                newName.erase(index, 1);
                letterChanges++;
                newName.insert(index, randomChoice(consonants));
            }
            else if (!isPermute){
                newName.erase(index, 1);
                letterChanges++;
                newName.insert(index, randomChoice(vowels));
            }
        }

    }

    // Our base case
    if(letterChanges < letterChangeLimit && changes==0 ) {
        //We didnt change our word at all, try again recursviely until we do
        return mutateWord(name);
    }

    // Convert to lower case
    for(uint i = 1; i < newName.length()-1; i++) {
        if(newName[i]>=65 && newName[i]<=92){
            newName[i]=newName[i]+32;
        }
    }

    // Convert first letter to upper case
    if(newName[0]>=97 && newName[0]<=122){
        newName[0]=newName[0]-32;
    }


    LOG_INFO("Mutating Name:"+name +" to new name:"+newName);
    return newName;;
}

string generateNameSection()
{
    // TODO: this should be checked very carefully to make sure that
    // this isn't copying all these lists as that would be quite
    // inefficient
    auto prefixCofixList = SimulationParameters::speciesNameController().getPrefixCofix();
    auto prefix_v = SimulationParameters::speciesNameController().getVowelPrefixes();
    auto prefix_c = SimulationParameters::speciesNameController().getConsonantPrefixes();
    auto cofix_v = SimulationParameters::speciesNameController().getVowelCofixes();
    auto cofix_c = SimulationParameters::speciesNameController().getConsonantCofixes();
    auto suffix = SimulationParameters::speciesNameController().getSuffixes();
    auto suffix_c = SimulationParameters::speciesNameController().getConsonantSuffixes();
    auto suffix_v = SimulationParameters::speciesNameController().getVowelSuffixes();

    string newName = "";

    if (GetEngine().GetRandom().GetNumber(0,100) >= 10) {
        switch (GetEngine().GetRandom().GetNumber(0,3)) {
        case 0:
            newName = randomChoice(prefix_c) + randomChoice(suffix_v);
            break;
        case 1:
            newName = randomChoice(prefix_v) + randomChoice(suffix_c);
            break;
        case 2:
            newName = randomChoice(prefix_v) + randomChoice(cofix_c) + randomChoice(suffix_v);
            break;
        case 3:
            newName = randomChoice(prefix_c) + randomChoice(cofix_v) + randomChoice(suffix_c);
            break;
        }
    } else {
        //Developer Easter Eggs and really silly long names here
        //Our own version of wigglesoworthia for example
        switch (GetEngine().GetRandom().GetNumber(0,3))
        {
        case 0:
        case 1:
            newName = randomChoice(prefixCofixList) + randomChoice(suffix);
            break;
        case 2:
            newName = randomChoice(prefix_v) + randomChoice(cofix_c) + randomChoice(suffix);
            break;
        case 3:
            newName = randomChoice(prefix_c) + randomChoice(cofix_v) + randomChoice(suffix);
            break;
        }
    }

    // TODO: DO more stuff here to improve names
    // (remove double letters when the prefix ends with and the cofix starts with the same letter
    // Remove weird things that come up like "rc" (Implemented through vowels and consonants)
    return newName;
}

// For normal microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS =
    {
        {"atp", InitialCompound(30,300)},
        {"glucose", InitialCompound(30,300)},
        {"ammonia", InitialCompound(30,100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(0)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(0)}
    };

// For ferrophillic microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS_IRON =
    {
        {"atp", InitialCompound(30,300)},
        {"glucose", InitialCompound(10,30)},
        {"ammonia", InitialCompound(30,100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(0)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(30,300)}
    };

// For chemophillic microbes
const dictionary DEFAULT_INITIAL_COMPOUNDS_CHEMO =
    {
        {"atp", InitialCompound(30,300)},
        {"glucose", InitialCompound(10,30)},
        {"ammonia", InitialCompound(30,100)},
        {"phosphates", InitialCompound(0)},
        {"hydrogensulfide", InitialCompound(30,300)},
        {"oxytoxy", InitialCompound(0)},
        {"iron", InitialCompound(0)}
    };

string randomSpeciesName()
{
    return "Species_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
}

// Bacteria also need names
string randomBacteriaName()
{
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

    ~Species()
    {
        if(templateEntity != NULL_OBJECT){

            LOG_ERROR("Species object not extinguish()ed before destructor, doing that now");
            extinguish();
        }
    }

    // Creates a mutated version of the species
    Species(SpeciesComponent@ parent, CellStageWorld@ world, bool isBacteria)
    {
        this.isBacteria = parent.isBacteria;
        LOG_INFO("Gene Code Is:"+parent.stringCode);

        if (!isBacteria)
        {
            name = randomSpeciesName();
        }
        else {
            name = randomBacteriaName();
        }

        //Mutate the epithet
        if (GetEngine().GetRandom().GetNumber(0, 100) < MUTATION_WORD_EDIT){
            epithet = mutateWord(parent.epithet);
        }
        else {
            epithet = generateNameSection();
        }
        genus = parent.genus;
        colour=parent.colour;
        mutateBehavior(parent);

        // Make sure not over or under our scales
        cleanPersonality();


        // Subtly mutate color
        this.colour = Float4(parent.colour.X + randomMutationColourChannel(),
            parent.colour.Y + randomMutationColourChannel(),
            parent.colour.Z + randomMutationColourChannel(),
            parent.colour.W + randomMutationColourChannel());

        LOG_INFO("X:"+parent.colour.X+" Y:"+parent.colour.Y+" Z:"+parent.colour.Z+" W:"+parent.colour.W);
        LOG_INFO("X:"+colour.X+" Y:"+colour.Y+" Z:"+colour.Z+" W:"+colour.W);
        // Chance of new color needs to be low
        if (GetEngine().GetRandom().GetNumber(0,100) <= MUTATION_CHANGE_GENUS)
        {
            if (!isBacteria)
            {
                LOG_INFO("New Genus");
            }else {
                LOG_INFO("New Genus of Bacteria");
            }

            // We can do more fun stuff here later
            if (GetEngine().GetRandom().GetNumber(0, 100) < MUTATION_WORD_EDIT){
                genus = mutateWord(parent.genus);
            }
            else {
                genus = generateNameSection();
            }
            // New genuses get to double their color change
            this.colour = Float4(parent.colour.X + randomMutationColourChannel(),
                parent.colour.Y + randomMutationColourChannel(),
                parent.colour.Z + randomMutationColourChannel(),
                parent.colour.W + randomMutationColourChannel());
        }

        this.stringCode = mutateMicrobe(parent.stringCode,isBacteria);


        generateMembranes(parent);


        commonConstructor(world);


        this.setupSpawn(world);
    }

    private void generateMembranes(SpeciesComponent@ parent){
        if (GetEngine().GetRandom().GetNumber(0,100)<=20){
            if (GetEngine().GetRandom().GetNumber(0,100) < 50){
                this.speciesMembraneType = MEMBRANE_TYPE::MEMBRANE;
            }
            else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
                this.speciesMembraneType = MEMBRANE_TYPE::DOUBLEMEMBRANE;
                this.colour.W = randomOpacityChitin();
            }
            else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
                this.speciesMembraneType = MEMBRANE_TYPE::WALL;
            }
            else {
                this.speciesMembraneType = MEMBRANE_TYPE::CHITIN;
                this.colour.W = randomOpacityChitin();
            }
        }
        else{
            this.speciesMembraneType = parent.speciesMembraneType;
            }
    }

    private void cleanPersonality() {
        this.aggression = clamp(this.aggression, 0.0f, MAX_SPECIES_AGRESSION);
        this.fear = clamp(this.fear, 0.0f, MAX_SPECIES_FEAR);
        this.activity = clamp(this.activity, 0.0f, MAX_SPECIES_ACTIVITY);
        this.focus = clamp(this.focus, 0.0f, MAX_SPECIES_FOCUS);
        this.opportunism = clamp(this.opportunism, 0.0f, MAX_SPECIES_OPPORTUNISM);
    }

    private void mutateBehavior(SpeciesComponent@ parent){
        // Variables used in AI to determine general behavior mutate these
        this.aggression = parent.aggression+GetEngine().GetRandom().GetFloat(
            MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
        this.fear = parent.fear+GetEngine().GetRandom().GetFloat(
            MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
        this.activity = parent.activity+GetEngine().GetRandom().GetFloat(
            MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
        this.focus = parent.focus+GetEngine().GetRandom().GetFloat(
            MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
        this.opportunism = parent.opportunism+GetEngine().GetRandom().GetFloat(
            MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
    }

    private void commonConstructor(CellStageWorld@ world)
    {
        @forWorld = world;

        // This translates the genetic code into positions
        auto organelles = positionOrganelles(stringCode);

        // If you have iron (f is the symbol for rusticyanin)
        if (stringCode.findFirst('f') >= 0)
        {
        templateEntity = Species::createSpecies(forWorld, this.name, this.genus, this.epithet,
            organelles, this.colour, this.isBacteria, this.speciesMembraneType,
            DEFAULT_INITIAL_COMPOUNDS_IRON, this.aggression, this.fear, this.activity, this.focus, this.opportunism);
        }
        else if (stringCode.findFirst('C') >= 0 || stringCode.findFirst('c') >= 0)
        {
        templateEntity = Species::createSpecies(forWorld, this.name, this.genus, this.epithet,
            organelles, this.colour, this.isBacteria, this.speciesMembraneType,
            DEFAULT_INITIAL_COMPOUNDS_CHEMO, this.aggression, this.fear, this.activity, this.focus, this.opportunism);
        }
        else {
        templateEntity = Species::createSpecies(forWorld, this.name, this.genus, this.epithet,
            organelles, this.colour, this.isBacteria, this.speciesMembraneType,
            DEFAULT_INITIAL_COMPOUNDS, this.aggression, this.fear, this.activity, this.focus, this.opportunism);
        }

    }

    // Delete a species
    void extinguish()
    {
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

    ObjectID factorySpawn(CellStageWorld@ world, Float3 pos)
    {
        return MicrobeOperations::spawnMicrobe(world, pos, this.name,
            // Ai controlled
            true);
    }

    ObjectID bacteriaColonySpawn(CellStageWorld@ world, Float3 pos)
    {
        Float3 curSpawn = Float3(GetEngine().GetRandom().GetNumber(1, 7), 0,
            GetEngine().GetRandom().GetNumber(1, 7));

        // Three kinds of colonies are supported, line colonies and clump coloniesand Networks
        if (GetEngine().GetRandom().GetNumber(0, 4) < 2)
        {
            // Clump
            for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_COLONY_SIZE,
                    MAX_BACTERIAL_COLONY_SIZE); i++){

                //dont spawn them on top of each other because it
                //causes them to bounce around and lag
                MicrobeOperations::spawnMicrobe(world, pos + curSpawn, this.name, true, true);
                curSpawn = curSpawn + Float3(GetEngine().GetRandom().GetNumber(-7, 7), 0,
                    GetEngine().GetRandom().GetNumber(-7, 7));
            }
        }
        else if (GetEngine().GetRandom().GetNumber(0,30) > 2)
        {
            // Line
            // Allow for many types of line
            float lineX = GetEngine().GetRandom().GetNumber(-5, 5) + GetEngine().GetRandom().
                GetNumber(-5, 5);
            float linez = GetEngine().GetRandom().GetNumber(-5, 5) + GetEngine().GetRandom().
                GetNumber(-5, 5);

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_LINE_SIZE,
                    MAX_BACTERIAL_LINE_SIZE); i++){

                // Dont spawn them on top of each other because it
                // Causes them to bounce around and lag
                MicrobeOperations::spawnMicrobe(world, pos+curSpawn, this.name, true, true);
                curSpawn = curSpawn + Float3(lineX + GetEngine().GetRandom().GetNumber(-2, 2),
                    0, linez + GetEngine().GetRandom().GetNumber(-2, 2));
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

            for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_COLONY_SIZE,
                    MAX_BACTERIAL_COLONY_SIZE); i++)
            {
                if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !horizontal)
                {
                    horizontal = true;
                    vertical = false;

                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                            MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                        // Dont spawn them on top of each other because
                        // It causes them to bounce around and lag
                        curSpawn.X += GetEngine().GetRandom().GetNumber(5, 7);

                        // Add a litlle organicness to the look
                        curSpawn.Z += GetEngine().GetRandom().GetNumber(-2, 2);
                        MicrobeOperations::spawnMicrobe(world, pos + curSpawn, this.name,
                            true, true);
                    }
                }
                else if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !vertical) {
                    horizontal=false;
                    vertical=true;
                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_LINE_SIZE,MAX_BACTERIAL_LINE_SIZE); ++c){
                        // Dont spawn them on top of each other because it
                        // Causes them to bounce around and lag
                        curSpawn.Z += GetEngine().GetRandom().GetNumber(5,7);
                        // Add a litlle organicness to the look
                        curSpawn.X += GetEngine().GetRandom().GetNumber(-2,2);
                        MicrobeOperations::spawnMicrobe(world, pos+curSpawn, this.name, true,
                            true);
                    }
                }
                else if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !horizontal)
                {
                    horizontal = true;
                    vertical = false;

                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                            MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                        // Dont spawn them on top of each other because
                        // It causes them to bounce around and lag
                        curSpawn.X -= GetEngine().GetRandom().GetNumber(5, 7);
                        // Add a litlle organicness to the look
                        curSpawn.Z -= GetEngine().GetRandom().GetNumber(-2, 2);
                        MicrobeOperations::spawnMicrobe(world, pos + curSpawn, this.name,
                            true, true);
                    }
                }
                else if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !vertical) {
                    horizontal = false;
                    vertical = true;

                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                            MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                        // Dont spawn them on top of each other because it
                        //causes them to bounce around and lag
                        curSpawn.Z -= GetEngine().GetRandom().GetNumber(5, 7);
                        //add a litlle organicness to the look
                        curSpawn.X -= GetEngine().GetRandom().GetNumber(-2, 2);
                        MicrobeOperations::spawnMicrobe(world, pos+curSpawn, this.name, true,
                            true);
                    }
                }
                else {
                    // Diagonal
                    horizontal = false;
                    vertical = false;

                    for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                            MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                        // Dont spawn them on top of each other because it
                        // Causes them to bounce around and lag
                        curSpawn.Z += GetEngine().GetRandom().GetNumber(5, 7);
                        curSpawn.X += GetEngine().GetRandom().GetNumber(5, 7);
                        MicrobeOperations::spawnMicrobe(world, pos + curSpawn, this.name,
                            true, true);
                    }
                }
            }
        }

        return MicrobeOperations::spawnMicrobe(world, pos, this.name, true);
    }

    // Sets up the spawn of the species
    // This may only be called once. Otherwise old spawn types are left active
    void setupSpawn(CellStageWorld@ world)
    {
        assert(world is forWorld, "Wrong world passed to setupSpawn");

        spawningEnabled = true;

        SpawnFactoryFunc@ factory;

        if(this.isBacteria){
            @factory = SpawnFactoryFunc(this.bacteriaColonySpawn);
        } else {
            @factory = SpawnFactoryFunc(this.factorySpawn);
        }

        // And register new
        this.id = forWorld.GetSpawnSystem().addSpawnType(
            factory,
            // spawnDensity should depend on population
            // This does now but it also needs to be modified later to match changes
            1.0f / (STARTING_SPAWN_DENSITY - (min(MAX_SPAWN_DENSITY, this.population * 5))),
            BACTERIA_SPAWN_RADIUS);
    }

    //! updates the population count of the species
    void updatePopulation()
    {
        // Numbers incresed so things happen more often
        this.population += GetEngine().GetRandom().GetNumber(-700, 700);
    }

    void devestate()
    {
        // Occassionally you just need to take a deadly virus and use
        // it to make things interesting
        this.population += GetEngine().GetRandom().GetNumber(-1500, -700);
    }

    void boom()
    {
        // Occassionally you just need to give a species a nice pat on
        // the back
        this.population += GetEngine().GetRandom().GetNumber(700, 1500);
    }

    int getPopulationFromAutoEvo()
    {
        return this.population;
    }

    void modifyPopulationFromAUtoEvo(int population)
    {
        this.population+=population;
    }

    Float4 getRightColourForSpecies()
    {
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
    double opportunism = 100.0f;
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
const auto SPECIES_SIM_INTERVAL = 2500;

// If a specie's population goes below this it goes extinct.
const auto MIN_POP_SIZE = 500;

// If a specie's population goes above this it gets split in half and a
// new mutated species apears. this should be randomized
const auto MAX_POP_SIZE = 3000;

// The amount of species at the start of the microbe stage (not counting Default/Player)
const auto INITIAL_SPECIES = 7;

// The amount of bacterial species to start with
const auto INITIAL_BACTERIA = 4;

// If there are more species than this then all species get their population reduced by half
const auto MAX_SPECIES = 15;

// If there are less species than this creates new ones.
const auto MIN_SPECIES = 3;

// If there are less species than this creates new ones.
const auto MIN_BACTERIA = 2;

//! Updates the species's population and creates new ones. And keeps track of Species objects
class SpeciesSystem : ScriptSystem{

    void Init(GameWorld@ w)
    {
        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "SpeciesSystem expected CellStageWorld");
    }

    void Release()
    {
        // Destroy all species to stop complaints that they aren't extinguished
        resetAutoEvo();
    }


    void Run()
    {
        //LOG_INFO("autoevo running");
        // Update population numbers and split/extinct species as needed

        timeSinceLastCycle++;
        while(this.timeSinceLastCycle > SPECIES_SIM_INTERVAL){
           doAutoEvoStep();
        }
    }

    void doAutoEvoStep(){
        LOG_INFO("Processing Auto-evo Step");
        this.timeSinceLastCycle -= SPECIES_SIM_INTERVAL;
        bool ranEventThisStep=false;
        countSpecies();
        // Every 8 steps or so do a cambrian explosion style
        // Event, this should increase variablility significantly
        if(GetEngine().GetRandom().GetNumber(0, 200) <= 25){
            LOG_INFO("Cambrian Explosion");
            ranEventThisStep = true;
            // TODO: add a notification for when this happens
            doCambrianExplosion();
        }

        // Various mass extinction events
        // Only run one "big event" per turn
        if(species.length() > MAX_SPECIES && !ranEventThisStep){
            LOG_INFO("Mass extinction time");
            // F to pay respects: TODO: add a notification for when this happens
            ranEventThisStep = true;
            doMassExtinction();
        }

        // Add some variability, this is a less deterministic mass
        // Extinction eg, a meteor, etc.
        if(GetEngine().GetRandom().GetNumber(0, 1000) == 1 && !ranEventThisStep){
            LOG_INFO("Black swan event");
            ranEventThisStep = true;
            // F to pay respects: TODO: add a notification for when this happens
            doMassExtinction();
        }

        // Super extinction event
        if(GetEngine().GetRandom().GetNumber(0, 1000) == 1 && !ranEventThisStep){
            LOG_INFO("Great Dying");
            ranEventThisStep = true;
            // Do mass extinction code then devastate all species,
            //this should extinct quite a few of the ones that
            //arent doing well.  *Shudders*
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
            LOG_INFO(currentSpecies.name + " " + currentSpecies.population);
            bool ranSpeciesEvent = false;
            // This is also just to shake things up occassionally
            // Cambrian Explosion
            if ( currentSpecies.population > 0 &&
                GetEngine().GetRandom().GetNumber(0, 10) <= 2)
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
                    GetEngine().GetRandom().GetNumber(0, 10) <= 2 && !ranSpeciesEvent)
            {
                    // F to pay respects: TODO: add a notification for when this happens
                    LOG_INFO(currentSpecies.name + " has been devestated by disease.");
                    currentSpecies.devestate();
                    LOG_INFO(currentSpecies.name+" population is now "+
                        currentSpecies.population);
                    ranSpeciesEvent=true;
            }

                // 50% chance of splitting off two species instead of one
           if(GetEngine().GetRandom().GetNumber(0, 10) <= 5 && ranSpeciesEvent == false &&
                currentSpecies.population >= MAX_POP_SIZE){

                // To prevent ridiculous population numbers
                currentSpecies.population=MAX_POP_SIZE;
                auto oldPop = currentSpecies.population;
                auto speciesComp = world.GetComponent_SpeciesComponent(currentSpecies.templateEntity);
                auto newSpecies = Species(speciesComp, world,
                    currentSpecies.isBacteria);

                ranSpeciesEvent=true;
                species.insertLast(newSpecies);
                LOG_INFO("Species " + currentSpecies.name +
                    " split off several species, the first is:" +
                    newSpecies.name);
                // Reset pop so we can split a second time
                currentSpecies.population = oldPop;
            }

            // Reproduction and mutation
            if(currentSpecies.population >= MAX_POP_SIZE){
                // To prevent ridiculous population numbers
                currentSpecies.population=MAX_POP_SIZE;

                currentSpecies.population = int(floor(currentSpecies.population / 2.f));

                auto speciesComp = world.GetComponent_SpeciesComponent(currentSpecies.templateEntity);
                auto newSpecies = Species(speciesComp, world,
                    currentSpecies.isBacteria);

                newSpecies.population = int(ceil(currentSpecies.population));

                species.insertLast(newSpecies);
                LOG_INFO("Species " + currentSpecies.name + " split off a child species:" +
                    newSpecies.name);
            }


             // Extinction, this is not an event since things with
             // low population need to be killed off.
            if(currentSpecies.population <= MIN_POP_SIZE){
                LOG_INFO("Species " + currentSpecies.name + " went extinct");
                currentSpecies.extinguish();
                species.removeAt(index);
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

    void splitSpecies(SpeciesComponent@ currentSpecies){
        auto newSpecies = Species(currentSpecies, world,
                currentSpecies.isBacteria);

        int population = GetEngine().GetRandom().GetNumber(600,INITIAL_POPULATION);
        newSpecies.population=GetEngine().GetRandom().GetNumber(600,INITIAL_POPULATION);
        species.insertLast(newSpecies);
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

    void resetAutoEvo()
    {
        for(uint i = 0; i < species.length(); i++){

            species[i].extinguish();
        }

        species.resize(0);
        currentBacteriaAmount = 0;
        currentEukaryoteAmount = 0;
    }

    void doMassExtinction()
    {
        // This doesnt seem like a powerful event
        for(uint i = 0; i < species.length(); i++){
            species[i].population /= 2;
        }
    }

    void doDevestation()
    {
        // Devastate all species.
        for(uint i = 0; i < species.length(); i++){
            species[i].devestate();
        }
    }

    void doCambrianExplosion()
    {
        for(uint i = 0; i < species.length(); i++){
            species[i].population *= 2;
        }
    }

    void countSpecies()
    {
         currentBacteriaAmount=0;
         currentEukaryoteAmount=0;
        for(uint i = 0; i < species.length(); i++){
            if (species[i].isBacteria){
                currentBacteriaAmount++;
           }
            else {
                currentEukaryoteAmount++;
            }
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
    SpeciesComponent@ updateSpecies)
{
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



namespace Species{

// Given a newly-created microbe, this sets the organelles and all other
// species-specific microbe data like agent codes, for example.
//! Brief applies template to a microbe entity making sure it has all
//! the correct organelle components
//! \param editShape if the physics body is not created yet this function can directly
//! edit the shape without trying to alter the body
void applyTemplate(CellStageWorld@ world, ObjectID microbe, SpeciesComponent@ species,
    PhysicsShape@ editShape = null)
{
    // Fail if the species is not set up
    assert(species.organelles.length() > 0, "Error can't apply uninitialized species " +
        "template: " + species.name);

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbe));

    microbeComponent.speciesName = species.name;
    MicrobeOperations::setMembraneType(world, microbe, species.speciesMembraneType);
    MicrobeOperations::setMembraneColour(world, microbe, species.colour);

    restoreOrganelleLayout(world, microbe, microbeComponent, species, editShape);

    // Another place where compound amounts are something we need to worry about
    auto ids = species.avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); i++){
        CompoundId compoundId = parseUInt(ids[i]);
        InitialCompound amount = InitialCompound(species.avgCompoundAmounts[ids[i]]);
        MicrobeOperations::setCompound(world, microbe, compoundId, amount.amount);
    }

}

void restoreOrganelleLayout(CellStageWorld@ world, ObjectID microbeEntity,
    MicrobeComponent@ microbeComponent, SpeciesComponent@ species,
    PhysicsShape@ editShape = null)
{
    // delete the the previous organelles.
    while(microbeComponent.organelles.length() > 0){

        assert(editShape is null,
            "can't directly edit a shape on a cell with existing organelles");

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

        MicrobeOperations::addOrganelle(world, microbeEntity, organelle, editShape);
    }

    // Cache isBacteria from species. This can be changed depending on
    // the added organelles in the editor
    microbeComponent.isBacteria = species.isBacteria;

    // Call this  to reset processor component
    MicrobeOperations::rebuildProcessList(world,microbeEntity);
}



//! Creates a species from the initial template. This doesn't register with SpeciesSystem
//! because this is (currently) only used for the player's species which isn't managed by it
ObjectID createSpecies(CellStageWorld@ world, const string &in name,
    MicrobeTemplate@ fromTemplate)
{
    array<PlacedOrganelle@> convertedOrganelles;
    for(uint i = 0; i < fromTemplate.organelles.length(); i++){

        OrganelleTemplatePlaced@ organelle = fromTemplate.organelles[i];

        convertedOrganelles.insertLast(PlacedOrganelle(
                getOrganelleDefinition(organelle.type), organelle.q, organelle.r,
                organelle.rotation));
    }

    return createSpecies(world, name, fromTemplate.genus, fromTemplate.epithet, convertedOrganelles,
        fromTemplate.colour, fromTemplate.isBacteria, fromTemplate.speciesMembraneType,
        fromTemplate.compounds, 100.0f, 100.0f, 100.0f, 200.0f, 100.0f);
}
//! Creates an entity that has all the species stuff on it
//! AI controlled ones need to be in addition in SpeciesSystem
ObjectID createSpecies(CellStageWorld@ world, const string &in name, const string &in genus,
    const string &in epithet, array<PlacedOrganelle@> organelles, Float4 colour,
    bool isBacteria, MEMBRANE_TYPE speciesMembraneType,  const dictionary &in compounds,
    double aggression, double fear, double activity, double focus, double opportunism)
{
    ObjectID speciesEntity = world.CreateEntity();

    SpeciesComponent@ speciesComponent = world.Create_SpeciesComponent(speciesEntity,
        name);

    speciesComponent.genus = genus;
    speciesComponent.epithet = epithet;


    @speciesComponent.avgCompoundAmounts = dictionary();

    @speciesComponent.organelles = array<SpeciesStoredOrganelleType@>();
    speciesComponent.stringCode="";

    // Translate positions over
    for(uint i = 0; i < organelles.length(); ++i){
        auto organelle = cast<PlacedOrganelle>(organelles[i]);
        speciesComponent.organelles.insertLast(organelle);
        speciesComponent.stringCode += organelle.organelle.gene;
        // This will always be added after each organelle so its safe to assume its there
        speciesComponent.stringCode+=","+organelle.q+","+
            organelle.r+","+
            organelle.rotation;
        if (i != organelles.length()-1){
            speciesComponent.stringCode+="|";
        }
    }

    // Verify it //
    for(uint i = 0; i < speciesComponent.organelles.length(); i++){

        PlacedOrganelle@ organelle = cast<PlacedOrganelle>(speciesComponent.organelles[i]);

        if(organelle is null){

            assert(false, "createSpecies: species.organelles has invalid object at index: " +
                i);
        }
    }

    speciesComponent.colour = colour;

    speciesComponent.speciesMembraneType = speciesMembraneType;

    //We need to know this is baceria
    speciesComponent.isBacteria = isBacteria;
    // We need to know our aggression and fear variables
    speciesComponent.aggression = aggression;
    speciesComponent.fear = fear;
    speciesComponent.activity = activity;
    speciesComponent.focus = focus;
    speciesComponent.opportunism = opportunism;

    // Iterates over all compounds, and sets amounts and priorities
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


    return speciesEntity;
}

//! Calls resetAutoEvo on world's SpeciesSystem
void resetAutoEvo(CellStageWorld@ world){
    cast<SpeciesSystem>(world.GetScriptSystem("SpeciesSystem")).resetAutoEvo();
}
}
