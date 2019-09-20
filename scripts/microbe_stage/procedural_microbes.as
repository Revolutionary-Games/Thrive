#include "configs.as"
#include "nucleus_organelle.as"
#include "hex.as"
#include "mutation_helpers.as"
// Lists of valid organelles to choose from for mutation
dictionary organelleLetters = {};
array<string> VALID_ORGANELLES = {};
array<string> VALID_ORGANELLE_LETTERS = {};
array<float> VALID_ORGANELLE_CHANCES = {};
array<float> VALID_PROKARYOTE_ORGANELLE_CHANCES = {};

// These have to be global to work or we need a place to put them that
// isnt global note the eukaryote one is old, so this is how this was
// already programmed
float maxEukaryoteScore = 0;
float maxProkaryoteScore = 0;

//! Called from setupOrganelles
void setupOrganelleLetters(){

    auto keys = _mainOrganelleTable.getKeys();

    for(uint i = 0; i < keys.length(); ++i){

        auto organelleName = keys[i];
        auto organelleInfo = getOrganelleDefinition(organelleName);

        // Getting the organelle letters from the organelle table.
        organelleLetters[organelleInfo.gene] = organelleName;

        if(!organelleInfo.hasComponent(nucleusComponentFactory.name)){

            VALID_ORGANELLES.insertLast(organelleName);
            VALID_ORGANELLE_CHANCES.insertLast(organelleInfo.chanceToCreate);
            VALID_PROKARYOTE_ORGANELLE_CHANCES.insertLast(organelleInfo.prokaryoteChance);
            VALID_ORGANELLE_LETTERS.insertLast(organelleInfo.gene);

        // Getting the max chance score for the roulette selection.
        maxEukaryoteScore += organelleInfo.chanceToCreate;
        maxProkaryoteScore += organelleInfo.prokaryoteChance;
        }
    }
}

// Returns a random organelle letter
// TODO: verify that this has a good chance of returning also the last organelle
// TODO: is there a way to make this run faster?
string getRandomLetter(bool isBacteria){

    // This is actually essentially the entire mutation system here
    if (!isBacteria)
    {
        float i = GetEngine().GetRandom().GetNumber(0.f, maxEukaryoteScore);
        for(uint index = 0; index < VALID_ORGANELLES.length(); ++index){

            i -= VALID_ORGANELLE_CHANCES[index];

            if(i <= 0){
                return VALID_ORGANELLE_LETTERS[index];
            }
        }
    }
    else
    {
        float i = GetEngine().GetRandom().GetNumber(0.f, maxProkaryoteScore);
        for(uint index = 0; index < VALID_ORGANELLES.length(); ++index){
            i -= VALID_PROKARYOTE_ORGANELLE_CHANCES[index];

            if(i <= 0){
                return VALID_ORGANELLE_LETTERS[index];
            }
        }
    }

    // Just in case
    //LOG_WARNING("getRandomLetter: just in case case hit");
    return getOrganelleDefinition("cytoplasm").gene;
}

// Checks whether an organelle in a certain position would fit within a list of other organelles.
bool isValidPlacement(const string &in organelleName, int q, int r, int rotation,
    const  array<PlacedOrganelle@>@ &in organelleList
) {

    // this is now slightly less hacky but it could be btter
    const auto organelleHexes = getOrganelleDefinition(organelleName).getRotatedHexes(rotation);

    for(uint i = 0; i < organelleList.length(); ++i){

        auto otherOrganelle = organelleList[i];
        auto organelleDef = getOrganelleDefinition(otherOrganelle.organelle.name);

        // The organelles hexes
        auto otherOrganelleHexes = organelleDef.getRotatedHexes(organelleList[i].rotation);

        for(uint thisHexIndex = 0; thisHexIndex < organelleHexes.length(); ++thisHexIndex){

            for(uint otherHexIndex = 0; otherHexIndex < otherOrganelleHexes.length();
                ++otherHexIndex)
            {
                const auto hex = organelleHexes[thisHexIndex];
                const auto otherHex = otherOrganelleHexes[otherHexIndex];
                if(hex.q + q == otherHex.q + otherOrganelle.q &&
                    hex.r + r == otherHex.r + otherOrganelle.r)
                {
                    return false;
                }
            }
        }
    }

    return true;
}

// Finds a valid position to place the organelle and returns it
// We should be able to get far more creative with our cells now
OrganelleTemplatePlaced@ getRealisticPosition(const string &in organelleName,
    array<PlacedOrganelle@>@ organelleList
) {
    int q = 0;
    int r = 0;

    array<PlacedOrganelle@>@ organelleShuffledArray = organelleList;

    // Shuffle the Array to make sure its not always placing at the same part of the cell
    organelleShuffledArray.sort( function(a,b) {
        return GetEngine().GetRandom().GetNumber(0,100) <= 50;
    }
    );

    // Loop through all the organelles and find an open spot to place our new organelle attached to existing organelles
    // This almost always is over at the first iteration, so its not a huge performance hog
    for(uint i = 0; i < organelleShuffledArray.length(); ++i){
        // The organelle we wish to be next to
        auto otherOrganelle = organelleShuffledArray[i].organelle;
        auto organelleDef = getOrganelleDefinition(otherOrganelle.name);

        // The organelles hexes
        auto hexes = organelleDef.getRotatedHexes(organelleShuffledArray[i].rotation);

        // Middle of our organelle
        q = organelleShuffledArray[i].q;
        r = organelleShuffledArray[i].r;

        for(uint z = 0; z < hexes.length(); ++z){
            // Off set by hexes in organelle we are looking at
            q+=hexes[z].q;
            r+=hexes[z].r;

            for(int side = 1; side <= 6; ++side){
                Int2 offset = Int2(HEX_NEIGHBOUR_OFFSET[formatInt(side)]);
                // Offset by hex offset
                q = q + offset.X;
                r = r + offset.Y;

                //Check every possible rotation value.
                for(int j = 0; j <= 5; ++j){
                    int rotation = (360 * j / 6);
                    if(isValidPlacement(organelleName, q, r, rotation, organelleShuffledArray)){
                        return OrganelleTemplatePlaced(organelleName, q, r, rotation);
                    }
                }
            }

        //Gotta reset each time
        q = organelleShuffledArray[i].q;
        r = organelleShuffledArray[i].r;

        }
    }
    // We didnt find an open spot, that doesnt mak emuch sense
    return null;
}

// This function takes in a positioning block from the string code and a name
// and returns an organelle with the correct position info
OrganelleTemplatePlaced@ getStringCodePosition(const string &in organelleName, const string &in code){
    //LOG_INFO(code);

    array<string>@ chromArray = code.split(",");
    //TODO:Need to add some proper error handling
    int q = 0;
    int r = 0;
    int rotation = 0;

    q=parseInt(chromArray[1]);

    //LOG_INFO(""+q);
    r=parseInt(chromArray[2]);

    //LOG_INFO(""+r);

    rotation=parseInt(chromArray[3]);

    //LOG_INFO(""+rotation);

    return OrganelleTemplatePlaced(organelleName, q, r, rotation);
}

// Creates a list of organelles from the stringCode.
array<PlacedOrganelle@>@ positionOrganelles(const string &in stringCode){
    // TODO: remove once this works
    //LOG_INFO("DEBUG: positionOrganelles stringCode: " + stringCode);

    array<PlacedOrganelle@>@ result = array<PlacedOrganelle@>();
    const array<string>@ chromArray = stringCode.split("|");
    for(uint i = 0; i < chromArray.length(); ++i){
            OrganelleTemplatePlaced@ pos;
            string geneCode = chromArray[i];

            if (geneCode.length() > 0){
                const auto letter = CharacterToString(geneCode[0]);
                //LOG_WRITE(formatUInt(i) + ": " + letter);
                string name = string(organelleLetters[letter]);
                @pos = getStringCodePosition(name,geneCode);

                if(pos.type == ""){
                    assert(false, "positionOrganelles: organelleLetters didn't have the "
                    "current letter: " + letter);
                }

                result.insertLast(PlacedOrganelle(getOrganelleDefinition(pos.type), pos.q, pos.r,
                    pos.rotation));
            }
    }

    return result;
}

//! Mutates a species' dna code randomly


string translateOrganelleToGene(OrganelleTemplatePlaced@ ourOrganelle){
    string completeString = "";
    auto organelle = getOrganelleDefinition(ourOrganelle.type);
    completeString=organelle.gene+","+
        ourOrganelle.q+","+
        ourOrganelle.r+","+
        ourOrganelle.rotation;
    return completeString;

}

// Pass in the string code, isbacteria
string mutateMicrobe(const string &in stringCode, bool isBacteria)
{
    array<string>@ chromArray = stringCode.split("|");
    auto modifiedArray = chromArray;
   //LOG_INFO(chromArray[0]);
    string completeString = "";

    // Delete or replace an organelle randomly
    for(uint i = 0; i < chromArray.length(); i++){
        string chromosomes = chromArray[i];
        // Removing last organelle would be silly
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE && chromosomes.length() > 0){
            if (i != chromArray.length()-1 && CharacterToString(chromosomes[0]) != "N"){
                //LOG_INFO("deleteing");
                //LOG_INFO("chromosomes:"+chromArray[i]);
               // Delete organelle and its position
                modifiedArray.removeAt(i);

            }
        }else if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_REPLACEMENT_RATE && chromosomes.length() > 0){
            if (CharacterToString(chromosomes[0]) != "N"){
                //LOG_INFO("Replacing");
                //LOG_INFO("chromosomes:"+chromArray[i]);
                chromosomes[0]=getRandomLetter(isBacteria)[0];
                if (i != chromArray.length()-1){
                    modifiedArray.removeAt(i);
                    modifiedArray.insertAt(i,chromosomes);
                }
                else{
                    modifiedArray.removeAt(i);
                    modifiedArray.insertLast(chromosomes);
                }
            }
        }

    }


    completeString = join(modifiedArray,"|");

    // Can add up to 6 new organelles (Which should allow AI to catch up to player more
    // We can insert new organelles at the end of the list
    if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        const auto organelleList = positionOrganelles(completeString);
        const auto letter = getRandomLetter(isBacteria);
        string name = string(organelleLetters[letter]);
        const string returnedGenome = translateOrganelleToGene(getRealisticPosition(name,organelleList));
        //LOG_INFO("Adding");
        //LOG_INFO("chromosomes:"+returnedGenome);
        completeString+="|"+returnedGenome;
    }

    /*
    Probability of mutation occuring 5 time(s) = 0.15 = 1.0E-5
    Probability of mutation NOT occuring = (1 - 0.1)5 = 0.59049
    Probability of mutation occuring = 1 - (1 - 0.1)5 = 0.40951
    */
    // We can insert new organelles at the end of the list
    for(int n = 0; n < 5; n++ ){
    // We can insert new organelles at the end of the list
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_EXTRA_CREATION_RATE){
            const auto organelleList = positionOrganelles(completeString);
            const auto letter = getRandomLetter(isBacteria);
            string name = string(organelleLetters[letter]);
            const string returnedGenome = translateOrganelleToGene(getRealisticPosition(name,organelleList));
            //LOG_INFO("Adding");
            //LOG_INFO("chromosomes:"+returnedGenome);
            completeString+="|"+returnedGenome;
        }
    }

    if (isBacteria){
        if(GetEngine().GetRandom().GetNumber(0.f, 100.f) <= MUTATION_BACTERIA_TO_EUKARYOTE){
            const auto organelleList = positionOrganelles(completeString);
            const auto letter = "N";
            string name = string(organelleLetters[letter]);
            const string returnedGenome = translateOrganelleToGene(getRealisticPosition(name,organelleList));
            //LOG_INFO("Adding");
            //LOG_INFO("chromosomes:"+returnedGenome);
            completeString+="|"+returnedGenome;
        }
    }

    // LOG_INFO("Mutated: "+completeString);
    return completeString;
}

//! Generates a few random species in all patches
void generateRandomSpeciesForFreeBuild(PatchMap@ map)
{
    auto@ patches = map.getPatches();

    for(uint i = 0; i < patches.length(); ++i){

        const int species = GetEngine().GetRandom().GetNumber(1, 4);
        for(int count = 0; count < species; ++count){

            patches[i].addSpecies(createRandomSpecies(),
                GetEngine().GetRandom().GetNumber(INITIAL_SPLIT_POPULATION_MIN,
                    INITIAL_SPLIT_POPULATION_MAX));
        }
    }
}
