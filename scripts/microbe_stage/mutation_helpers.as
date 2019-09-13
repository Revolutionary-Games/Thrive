// Helpers for colours and other stuff for mutations
namespace MutationHelpers{

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

string mutateWord(const string &in name) {
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


}
