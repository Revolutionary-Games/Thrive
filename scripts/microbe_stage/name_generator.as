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

string randomSpeciesName()
{
    return "Species_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
}

// Bacteria also need names
string randomBacteriaName()
{
    return "Bacteria_" + formatInt(GetEngine().GetRandom().GetNumber(0, 10000));
}
