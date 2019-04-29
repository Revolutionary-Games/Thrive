#include "configs.as"
#include "nucleus_organelle.as"
#include "hex.as"
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
    // TODO: set position stuff here
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
    const array<OrganelleTemplatePlaced@> &in organelleList
) {
    // This is super hacky :/
    // this is now slightly less hacky
    auto organelleHexes = getOrganelleDefinition(organelleName).getRotatedHexes(rotation);

    for(uint i = 0; i < organelleList.length(); ++i){

        auto otherOrganelle = organelleList[i];

        auto otherOrganelleHexes = getOrganelleDefinition(otherOrganelle.type).getRotatedHexes(
            otherOrganelle.rotation);

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
    const array<OrganelleTemplatePlaced@> &in organelleList
) {
    int q = 0;
    int r = 0;

    // Checks whether the center is free.
    for(int j = 0; j <= 5; ++j){
        int rotation = 360 * j / 6;
        if(isValidPlacement(organelleName, q, r, rotation, organelleList)){
            return OrganelleTemplatePlaced(organelleName, q, r, rotation);
        }
    }

    // Moving the center one hex to the bottom.
    // This way organelles are "encouraged" to be on the bottom, rather than on the top,
    // which in turn means the flagellum are more likely to be on the back side of the cell.
    auto initialOffset = Int2(HEX_NEIGHBOUR_OFFSET[formatInt(int(HEX_SIDE::TOP))]);
    q = q + initialOffset.X;
    r = r + initialOffset.Y;

    // Spiral search for space for the organelle
    int radius = 1;

    while(true){
        //Moves into the ring of radius "radius" and center the old organelle
        Int2 radiusOffset = Int2(HEX_NEIGHBOUR_OFFSET[
                formatInt(int(HEX_SIDE::BOTTOM_LEFT))]);
        q = q + radiusOffset.X;
        r = r + radiusOffset.Y;

        //Iterates in the ring
        for(int side = 1; side <= 6; ++side){
            Int2 offset = Int2(HEX_NEIGHBOUR_OFFSET[formatInt(side)]);
            //Moves "radius" times into each direction
            for(int i = 1; i <= radius; ++i){
                q = q + offset.X;
                r = r + offset.Y;

                //Checks every possible rotation value.
                for(int j = 0; j <= 5; ++j){

                    int rotation = (360 * j / 6);

                    if(isValidPlacement(organelleName, q, r, rotation, organelleList)){
                        return OrganelleTemplatePlaced(organelleName, q, r, rotation);
                    }
                }
            }
        }

        ++radius;
    }
    return null;
}

// This function takes in a positioning block from the string code and a name
// and returns an organelle with the correct position info
OrganelleTemplatePlaced@ getStringCodePosition(const string &in organelleName, string code){
    LOG_INFO(code);

    array<string>@ chromArray = code.split(",");
    //TODO:Need to add some proper error handling
    int q = 0;
    int r = 0;
    int rotation = 0;

    q=parseInt(chromArray[1]);

    LOG_INFO(""+q);
    r=parseInt(chromArray[2]);

    LOG_INFO(""+r);

    rotation=parseInt(chromArray[3]);

    LOG_INFO(""+rotation);

    return OrganelleTemplatePlaced(organelleName, q, r, rotation);
}

// Creates a list of organelles from the stringCode.
array<PlacedOrganelle@>@ positionOrganelles(const string &in stringCode){
    // TODO: remove once this works
    //LOG_INFO("DEBUG: positionOrganelles stringCode: " + stringCode);

    array<PlacedOrganelle@>@ result = array<PlacedOrganelle@>();
    array<OrganelleTemplatePlaced@> organelleList;
    array<string>@ chromArray = stringCode.split("|");
    for(uint i = 0; i < chromArray.length(); ++i){
            OrganelleTemplatePlaced@ pos;
            string geneCode = chromArray[i];

            if (geneCode.length() > 0){
                const auto letter = CharacterToString(geneCode[0]);
                LOG_WRITE(formatUInt(i) + ": " + letter);
                string name = string(organelleLetters[letter]);
                @pos = getStringCodePosition(name,geneCode);

                if(pos.type == ""){
                    assert(false, "positionOrganelles: organelleLetters didn't have the "
                    "current letter: " + letter);
                }

                organelleList.insertLast(pos);
                result.insertLast(PlacedOrganelle(getOrganelleDefinition(pos.type), pos.q, pos.r,
                    pos.rotation));
            }
    }

    return result;
}

//! Mutates a species' dna code randomly


string mutateMicrobe(const string &in stringCode, bool isBacteria)
{
    // Moving the stringCode to a table to facilitate changes
    // chrosomsomes will be the string we return at the end
    string chromosomes = stringCode;

    array<string>@ chromArray = chromosomes.split("|");

    LOG_INFO(chromArray[0]);

    // Try to insert a letter at the end of the table.
    /*if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
        chromosomes += getRandomLetter(isBacteria);
    }*/

    //TODO: Make game not delete numbers somehow

    // Modifies the rest of the table.
    /*for(uint i = 0; i < stringCode.length(); i++){
        // Index we are adding or erasing chromosomes at
        uint index = stringCode.length() - i -1;
        // Removing last organelle would be silly
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_DELETION_RATE){
            if (index != stringCode.length()-1 && chromosomes.substr(index,1) != "N")
            {
                LOG_INFO("chromosomes:"+chromosomes);
                LOG_INFO("deleteing");
               // Position to erase from
               int endPos = (posInfoEnd-posInfoStart)+1;

               // Delete organelle and its position
                chromosomes.erase(index, endPos);

            }
        }
        // We can insert new organelles at the end of the list
        if(GetEngine().GetRandom().GetNumber(0.f, 1.f) < MUTATION_CREATION_RATE){
            // There is an error here when we try to insert at the end
            // of the list so use insertlast instead in that case
            if (index != stringCode.length()-1)
            {
                chromosomes.insert(index, getRandomLetter(isBacteria));
            }
            else{
                chromosomes+=getRandomLetter(isBacteria);
            }
        }

    }*/

    return chromosomes;
}
